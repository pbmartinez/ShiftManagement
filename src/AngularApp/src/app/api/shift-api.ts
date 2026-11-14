import { Injectable } from '@angular/core';
import { CrudApi } from './crud-api';
import { Shift } from './models';

@Injectable({ providedIn: 'root' })
export class ShiftApi extends CrudApi<Shift> {
  constructor() {
    super('/api/Shift');
  }

  import(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<{ imported: number }>(`${this.url}/import`, form);
  }
}
