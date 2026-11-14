import { saveFile } from './save-file';

export interface ShiftExcelRow {
  email: string;
  date: string;      // "2026-11-13"
  startTime: string; // "09:00:00"
  endTime: string;
}

/**
 * Builds shifts.xlsx in the browser, with the same layout the backend import reads
 * (Email, Date, Start, End), so an exported file can be imported back.
 */
export async function exportShiftsToExcel(rows: ShiftExcelRow[]) {
  // Loaded only when someone exports, so it doesn't slow down the first page load.
  const { Workbook } = await import('exceljs');

  const workbook = new Workbook();
  const sheet = workbook.addWorksheet('Shifts');
  sheet.columns = [
    { header: 'Email', key: 'email', width: 30 },
    { header: 'Date', key: 'date', width: 12, style: { numFmt: 'yyyy-mm-dd' } },
    { header: 'Start', key: 'start', width: 8, style: { numFmt: 'h:mm' } },
    { header: 'End', key: 'end', width: 8, style: { numFmt: 'h:mm' } },
  ];
  sheet.getRow(1).font = { bold: true };

  for (const row of rows)
    sheet.addRow({ email: row.email, date: excelDate(row.date), start: excelTime(row.startTime), end: excelTime(row.endTime) });

  const buffer = await workbook.xlsx.writeBuffer();
  saveFile(new Blob([buffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' }), 'shifts.xlsx');
}

// A UTC date, so Excel gets exactly that day whatever the browser's time zone is.
function excelDate(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(Date.UTC(year, month - 1, day));
}

// Excel stores a time as a fraction of a day: 09:00 -> 0.375.
function excelTime(value: string): number {
  const [hours, minutes] = value.split(':').map(Number);
  return (hours * 60 + minutes) / (24 * 60);
}
