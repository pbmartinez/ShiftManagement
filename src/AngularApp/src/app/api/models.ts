// Same shape as the backend DTOs (Domain/Dtos), as System.Text.Json sends them (camelCase).
export interface Collaborator {
  id: string;
  name: string;
  email: string;
  country: string;
}

export interface Shift {
  id: string;
  collaboratorId: string;
  date: string;      // "2026-11-13"  (DateOnly)
  startTime: string; // "09:00:00"    (TimeOnly)
  endTime: string;
  holiday?: string | null; // only in GET api/shift: the holiday's name in the collaborator's country
}

export interface Holiday {
  date: string; // "2026-01-01"
  name: string;
}

export interface Country {
  countryCode: string; // "UY"
  name: string;
}

// Entity.Id is required on the backend; an empty Guid tells it to generate a new one.
export const NEW_ID = '00000000-0000-0000-0000-000000000000';
