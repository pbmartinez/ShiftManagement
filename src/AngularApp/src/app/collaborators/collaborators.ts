import { Component, effect, inject, signal, viewChild } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSort, MatSortModule } from '@angular/material/sort';
import { MatTableDataSource, MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { CollaboratorApi } from '../api/collaborator-api';
import { Collaborator } from '../api/models';
import { problemMessages } from '../shared/server-errors';
import { CollaboratorDialog } from './collaborator-dialog';

@Component({
  selector: 'app-collaborators',
  imports: [ReactiveFormsModule, MatTableModule, MatSortModule, MatButtonModule, MatIconModule, MatFormFieldModule, MatInputModule, MatTooltipModule],
  templateUrl: './collaborators.html',
})
export class Collaborators {
  private readonly api = inject(CollaboratorApi);
  private readonly dialog = inject(MatDialog);
  private readonly sort = viewChild.required(MatSort);

  protected readonly columns = ['name', 'email', 'country', 'actions'];
  protected readonly dataSource = new MatTableDataSource<Collaborator>();
  protected readonly error = signal('');

  // Filtering happens here in the browser, on the collaborators already loaded.
  protected readonly filters = new FormGroup({
    name: new FormControl('', { nonNullable: true }),
    email: new FormControl('', { nonNullable: true }),
    country: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    effect(() => (this.dataSource.sort = this.sort()));
    // Every filled-in box must be contained (ignoring case) in that column.
    this.dataSource.filterPredicate = (row, _) => Object.entries(this.filters.getRawValue())
      .every(([field, text]) => row[field as keyof Collaborator].toLowerCase().includes(text.trim().toLowerCase()));
    this.filters.valueChanges.pipe(takeUntilDestroyed()).subscribe(value => (this.dataSource.filter = JSON.stringify(value)));
    this.load();
  }

  protected load() {
    this.api.getAll().subscribe({
      next: collaborators => {
        this.error.set('');
        this.dataSource.data = collaborators;
      },
      error: error => this.error.set(problemMessages(error).join(' ')),
    });
  }

  protected open(collaborator?: Collaborator) {
    this.dialog.open(CollaboratorDialog, { data: collaborator })
      .afterClosed()
      .subscribe(saved => saved && this.load());
  }

  protected remove(collaborator: Collaborator) {
    if (!confirm(`Delete ${collaborator.name}? Their shifts are deleted too.`))
      return;
    this.api.remove(collaborator.id).subscribe({
      next: () => this.load(),
      error: error => this.error.set(problemMessages(error).join(' ')),
    });
  }
}
