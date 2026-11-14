import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Country, Holiday } from './models';

/** Holidays and countries from Nager.Date, through the backend (which caches them). */
@Injectable({ providedIn: 'root' })
export class HolidayApi {
  private readonly http = inject(HttpClient);

  countries() {
    return this.http.get<Country[]>('/api/holiday/countries');
  }

  get(country: string, year: number) {
    return this.http.get<Holiday[]>(`/api/holiday/${country}/${year}`);
  }
}
