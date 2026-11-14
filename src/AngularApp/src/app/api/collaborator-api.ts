import { Injectable } from '@angular/core';
import { CrudApi } from './crud-api';
import { Collaborator } from './models';

@Injectable({ providedIn: 'root' })
export class CollaboratorApi extends CrudApi<Collaborator> {
  constructor() {
    super('/api/collaborator');
  }
}
