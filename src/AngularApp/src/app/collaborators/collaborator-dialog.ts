import { Observable } from 'rxjs';
import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { CollaboratorApi } from '../api/collaborator-api';
import { Collaborator, NEW_ID } from '../api/models';
import { applyServerErrors, clearServerErrors } from '../shared/server-errors';

@Component({
  selector: 'app-collaborator-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './collaborator-dialog.html',
})
export class CollaboratorDialog {
  private readonly api = inject(CollaboratorApi);
  private readonly dialogRef = inject(MatDialogRef<CollaboratorDialog>);
  protected readonly collaborator = inject<Collaborator | undefined>(MAT_DIALOG_DATA);

  protected readonly saving = signal(false);
  protected readonly formErrors = signal<string[]>([]);

  // Same rules as CollaboratorDto: Name and Country required, Email required (non-nullable) and a valid address.
  protected readonly form = new FormGroup({
    name: new FormControl(this.collaborator?.name ?? '', { nonNullable: true, validators: Validators.required }),
    email: new FormControl(this.collaborator?.email ?? '', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    country: new FormControl(this.collaborator?.country ?? '', { nonNullable: true, validators: Validators.required }),
  });

  constructor() {
    this.form.valueChanges.subscribe(() => clearServerErrors(this.form));
  }

  protected save() {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const collaborator: Collaborator = { id: this.collaborator?.id ?? NEW_ID, ...this.form.getRawValue() };
    const request: Observable<unknown> = this.collaborator ? this.api.update(collaborator) : this.api.create(collaborator);

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
