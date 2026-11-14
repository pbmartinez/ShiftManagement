import { catchError, map, merge, Observable, of, startWith, switchMap } from 'rxjs';
import { Component, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatTimepickerModule } from '@angular/material/timepicker';
import { HolidayApi } from '../api/holiday-api';
import { Collaborator, NEW_ID, Shift } from '../api/models';
import { ShiftApi } from '../api/shift-api';
import { fromDateString, fromTimeString, toDateString, toTimeString } from '../shared/dates';
import { applyServerErrors, clearServerErrors } from '../shared/server-errors';
import { endAfterStart } from '../shared/validators';

export interface ShiftDialogData {
  shift?: Shift;
  collaborators: Collaborator[];
}

@Component({
  selector: 'app-shift-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatDatepickerModule, MatTimepickerModule, MatButtonModule],
  templateUrl: './shift-dialog.html',
})
export class ShiftDialog {
  private readonly api = inject(ShiftApi);
  private readonly holidayApi = inject(HolidayApi);
  private readonly dialogRef = inject(MatDialogRef<ShiftDialog>);
  protected readonly data = inject<ShiftDialogData>(MAT_DIALOG_DATA);

  protected readonly saving = signal(false);
  protected readonly formErrors = signal<string[]>([]);

  // Same rules as ShiftDto: a collaborator, Date, StartTime and EndTime required, and EndTime after StartTime.
  protected readonly form = new FormGroup({
    collaboratorId: new FormControl(this.data.shift?.collaboratorId ?? null, Validators.required),
    date: new FormControl(this.data.shift ? fromDateString(this.data.shift.date) : null, Validators.required),
    startTime: new FormControl(this.data.shift ? fromTimeString(this.data.shift.startTime) : null, Validators.required),
    endTime: new FormControl(this.data.shift ? fromTimeString(this.data.shift.endTime) : null, [Validators.required, endAfterStart]),
  });

  // Live check, before saving: is the chosen date a holiday in the collaborator's country? Only a warning.
  protected readonly holiday = toSignal(
    merge(this.form.controls.collaboratorId.valueChanges, this.form.controls.date.valueChanges).pipe(
      startWith(null),
      switchMap(() => {
        const { collaboratorId, date } = this.form.getRawValue();
        const country = this.data.collaborators.find(c => c.id === collaboratorId)?.country;
        if (!country || !date)
          return of(null);
        return this.holidayApi.get(country, date.getFullYear()).pipe(
          map(holidays => holidays.find(h => h.date === toDateString(date))),
          map(h => (h ? `Holiday in ${country}: ${h.name}` : null)),
          catchError(() => of(null)));
      })),
    { initialValue: null });

  constructor() {
    this.form.controls.startTime.valueChanges.subscribe(() => this.form.controls.endTime.updateValueAndValidity());
    this.form.valueChanges.subscribe(() => clearServerErrors(this.form));
  }

  protected save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const shift: Shift = {
      id: this.data.shift?.id ?? NEW_ID,
      collaboratorId: value.collaboratorId!,
      date: toDateString(value.date!),
      startTime: toTimeString(value.startTime!),
      endTime: toTimeString(value.endTime!),
    };
    const request: Observable<unknown> = this.data.shift ? this.api.update(shift) : this.api.create(shift);

    this.saving.set(true);
    this.formErrors.set([]);
    request.subscribe({
      next: () => this.dialogRef.close(true),
      error: error => {
        this.saving.set(false);
        this.formErrors.set(applyServerErrors(this.form, error));
      },
    });
  }
}
