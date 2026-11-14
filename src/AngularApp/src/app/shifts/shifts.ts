import { Component, computed, effect, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CollaboratorApi } from '../api/collaborator-api';
import { Collaborator, Shift } from '../api/models';
import { ShiftApi } from '../api/shift-api';
import { toDateString, toTimeString } from '../shared/dates';
import { exportShiftsToExcel } from '../shared/shifts-excel';
import { parseShiftMessage, problemMessages, ShiftMessage } from '../shared/server-errors';
import { toNotBeforeFrom } from '../shared/validators';
import { ShiftDialog, ShiftDialogData } from './shift-dialog';

// A shift plus its collaborator, so the grid can show, sort and export it.
type ShiftRow = Shift & { collaborator: string; email: string };

type ShiftFilters = Shifts['filters']['value'];

@Component({
  selector: 'app-shifts',
  imports: [
    ReactiveFormsModule, MatTableModule, MatSortModule, MatButtonModule, MatIconModule, MatTooltipModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatDatepickerModule, MatTimepickerModule,
  ],
  templateUrl: './shifts.html',
})
export class Shifts {
  private readonly shiftApi = inject(ShiftApi);
  private readonly collaboratorApi = inject(CollaboratorApi);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly sort = viewChild.required(MatSort);

  protected readonly columns = ['collaborator', 'date', 'startTime', 'endTime', 'actions'];
  protected readonly importColumns = ['who', 'date', 'start', 'end', 'message'];
  protected readonly dataSource = new MatTableDataSource<ShiftRow>();

  protected readonly collaborators = signal<Collaborator[]>([]);
  private readonly shifts = signal<Shift[]>([]);
  protected readonly importErrors = signal<ShiftMessage[]>([]);
  protected readonly error = signal('');

  private readonly rows = computed<ShiftRow[]>(() => {
    const byId = new Map(this.collaborators().map(c => [c.id, c]));
    return this.shifts().map(s => {
      const c = byId.get(s.collaboratorId);
      return { ...s, collaborator: c ? `${c.name} (${c.email})` : '', email: c?.email ?? '' };
    });
  });

  // Filtering happens here in the browser, on the shifts already loaded: no request to the backend.
  protected readonly filters = new FormGroup({
    collaboratorId: new FormControl<string | null>(null),
    from: new FormControl<Date | null>(null),
    to: new FormControl<Date | null>(null, toNotBeforeFrom),
    startFrom: new FormControl<Date | null>(null),
    endUntil: new FormControl<Date | null>(null),
  });

  constructor() {
    effect(() => (this.dataSource.sort = this.sort()));
    effect(() => (this.dataSource.data = this.rows()));

    this.dataSource.filterPredicate = row => this.filters.valid ? matches(row, this.filters.getRawValue()) : true;
    this.filters.controls.from.valueChanges.subscribe(() => this.filters.controls.to.updateValueAndValidity({ emitEvent: false }));
    // Any non-empty string makes MatTableDataSource run filterPredicate again.
    this.filters.valueChanges.pipe(takeUntilDestroyed()).subscribe(value => (this.dataSource.filter = JSON.stringify(value)));

    this.collaboratorApi.getAll().subscribe(collaborators => this.collaborators.set(collaborators));
    this.load();
  }

  protected load() {
    this.shiftApi.getAll().subscribe({
      next: shifts => {
        this.error.set('');
        this.shifts.set(shifts);
      },
      error: error => this.error.set(problemMessages(error).join(' ')),
    });
  }

  protected open(shift?: Shift) {
    const data: ShiftDialogData = { shift, collaborators: this.collaborators() };
    this.dialog.open(ShiftDialog, { data })
      .afterClosed()
      .subscribe(saved => saved && this.load());
  }

  protected remove(shift: ShiftRow) {
    if (!confirm(`Delete the shift of ${shift.collaborator} on ${shift.date}?`))
      return;
    this.shiftApi.remove(shift.id).subscribe({
      next: () => this.load(),
      error: error => this.error.set(problemMessages(error).join(' ')),
    });
  }

  protected import(input: HTMLInputElement) {
    const file = input.files?.[0];
    input.value = ''; // so choosing the same file again (after fixing it) triggers another upload
    if (!file)
      return;

    this.shiftApi.import(file).subscribe({
      next: result => {
        this.importErrors.set([]);
        this.snackBar.open(`Imported ${result.imported} shifts`, 'OK', { duration: 4000 });
        this.load();
      },
      // Nothing was saved (the import is all or nothing), so the grid stays as it is.
      error: error => this.importErrors.set(problemMessages(error).map(parseShiftMessage)),
    });
  }

  /** Exactly what the grid shows: the filtered rows, in the grid's sort order. No request to the backend. */
  protected async export() {
    const rows = this.dataSource.sortData(this.dataSource.filteredData.slice(), this.sort());
    try {
      await exportShiftsToExcel(rows);
    } catch {
      this.error.set('The export failed, please try again.');
    }
  }
}

// Dates and times are compared as text: "2026-11-13" and "09:00:00" sort the same way as their values.
function matches(row: ShiftRow, f: ShiftFilters): boolean {
  return (!f.collaboratorId || row.collaboratorId === f.collaboratorId)
    && (!f.from || row.date >= toDateString(f.from))
    && (!f.to || row.date <= toDateString(f.to))
    && (!f.startFrom || row.startTime >= toTimeString(f.startFrom))
    && (!f.endUntil || row.endTime <= toTimeString(f.endUntil));
}
