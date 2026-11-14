import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';

/** What ASP.NET Core returns for a 400: { errors: { "StartTime": ["..."], "": ["..."] } }. */
interface ValidationProblem {
  errors?: Record<string, string[]>;
}

export interface ShiftMessage {
  who?: string; // email, collaborator id, or empty when the file had no email
  date?: string;
  start?: string;
  end?: string;
  message: string;
}

// Backend messages about a shift start with ShiftDto.ToString(): "ana@x.com 2026-11-13 09:30-10:30: overlaps ..."
const SHIFT_MESSAGE = /^(\S*) (\d{4}-\d{2}-\d{2}) (\d{2}:\d{2})-(\d{2}:\d{2}): (.*)$/;

export function parseShiftMessage(text: string): ShiftMessage {
  const match = SHIFT_MESSAGE.exec(text);
  return match ? { who: match[1], date: match[2], start: match[3], end: match[4], message: match[5] } : { message: text };
}

/** Every message in the response, once (an overlap comes under both StartTime and EndTime). */
export function problemMessages(error: unknown): string[] {
  if (!(error instanceof HttpErrorResponse) || error.status !== 400)
    return ['Something went wrong, please try again.'];
  if (typeof error.error === 'string')
    return [error.error];
  const problem = error.error as ValidationProblem;
  return [...new Set(Object.values(problem.errors ?? {}).flat())];
}

/**
 * Puts each server error under the input it belongs to ("StartTime" -> startTime control) as { server: message }.
 * Returns the messages that belong to no input, to show at the top of the form.
 */
export function applyServerErrors(form: FormGroup, error: unknown): string[] {
  if (!(error instanceof HttpErrorResponse) || error.status !== 400 || typeof error.error === 'string')
    return problemMessages(error);

  const unattached: string[] = [];
  for (const [key, messages] of Object.entries((error.error as ValidationProblem).errors ?? {})) {
    const name = Object.keys(form.controls).find(c => c.toLowerCase() === key.replace(/^\$\./, '').toLowerCase());
    const texts = messages.map(m => parseShiftMessage(m).message);
    if (name) {
      form.controls[name].setErrors({ ...form.controls[name].errors, server: texts.join(' ') });
      form.controls[name].markAsTouched();
    } else {
      unattached.push(...texts);
    }
  }
  return unattached;
}

/** Server errors go away as soon as the user changes anything: the next save asks the server again. */
export function clearServerErrors(form: FormGroup) {
  for (const control of Object.values(form.controls))
    if (control.hasError('server'))
      control.updateValueAndValidity({ emitEvent: false });
}
