// The backend sends DateOnly as "2026-11-13" and TimeOnly as "09:00:00"; Material pickers work with Date.
// Built from local parts on purpose: toISOString() would convert to UTC and could move the day.
const pad = (n: number) => String(n).padStart(2, '0');

export const toDateString = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}`;

export const toTimeString = (d: Date) => `${pad(d.getHours())}:${pad(d.getMinutes())}:00`;

export function fromDateString(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, month - 1, day);
}

export function fromTimeString(value: string): Date {
  const [hours, minutes] = value.split(':').map(Number);
  return new Date(2000, 0, 1, hours, minutes);
}

export const minutesOf = (d: Date) => d.getHours() * 60 + d.getMinutes();
