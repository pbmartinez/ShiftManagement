import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';
import { minutesOf } from './dates';

/**
 * Put on the "end" control: its value must be after the sibling `startField`.
 * Re-run it when the start changes (start.valueChanges -> end.updateValueAndValidity()).
 */
export function after(startField: string, error: string, compare: (start: Date, end: Date) => boolean): ValidatorFn {
  return (end: AbstractControl): ValidationErrors | null => {
    const start = end.parent?.get(startField)?.value as Date | null;
    return start && end.value && !compare(start, end.value) ? { [error]: true } : null;
  };
}

// Same rule as ShiftDto.Validate: EndTime must be after StartTime (so no overnight shifts).
export const endAfterStart = after('startTime', 'endBeforeStart', (start, end) => minutesOf(end) > minutesOf(start));

// For the date range filter: To can be the same day as From, but not before it.
export const toNotBeforeFrom = after('from', 'toBeforeFrom', (from, to) => to >= from);
