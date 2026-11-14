import { HttpClient } from '@angular/common/http';
import { inject } from '@angular/core';

/** The endpoints every ApiBaseController<TDto> on the backend has. */
export abstract class CrudApi<T extends { id: string }> {
  protected readonly http = inject(HttpClient);

  protected constructor(protected readonly url: string) {}

  getAll() {
    return this.http.get<T[]>(this.url);
  }

  create(item: T) {
    return this.http.post<T>(this.url, item);
  }

  update(item: T) {
    return this.http.put<void>(`${this.url}/${item.id}`, item);
  }

  remove(id: string) {
    return this.http.delete<void>(`${this.url}/${id}`);
  }
}
