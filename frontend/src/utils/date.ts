/**
 * Returns a YYYY-MM-DD date string in Europe/Kyiv timezone,
 * offset by a given number of days from Kyiv calendar today.
 */
export const getKyivDateString = (offsetDays = 0): string => {
  const todayKyivStr = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Kyiv' }).format(new Date());
  if (offsetDays === 0) {
    return todayKyivStr;
  }
  const [year, month, day] = todayKyivStr.split('-').map(Number);
  const kyivNoon = new Date(Date.UTC(year, month - 1, day, 12, 0, 0));
  kyivNoon.setUTCDate(kyivNoon.getUTCDate() + offsetDays);
  return new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Kyiv' }).format(kyivNoon);
};

/**
 * Converts a UTC ISO string to YYYY-MM-DDTHH:mm formatted in Europe/Kyiv timezone.
 */
export const isoToKyivDateTimeLocal = (isoString: string): string => {
  if (!isoString) return '';
  const date = new Date(isoString);
  if (isNaN(date.getTime())) return '';

  const formatter = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'Europe/Kyiv',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  });

  const parts = formatter.formatToParts(date);
  const year = parts.find(p => p.type === 'year')?.value;
  const month = parts.find(p => p.type === 'month')?.value;
  const day = parts.find(p => p.type === 'day')?.value;
  const hour = parts.find(p => p.type === 'hour')?.value;
  const minute = parts.find(p => p.type === 'minute')?.value;

  if (!year || !month || !day || !hour || !minute) return '';
  return `${year}-${month}-${day}T${hour}:${minute}`;
};

/**
 * Converts a YYYY-MM-DDTHH:mm string representing Europe/Kyiv business time into a UTC ISO string.
 * Performs iterative refinement and strict round-trip verification to ensure the wall-clock time
 * exists and is valid in Europe/Kyiv (e.g. throwing an error if the time falls inside a DST gap).
 */
export const kyivDateTimeLocalToUtcIso = (dateTimeLocalStr: string): string => {
  if (!dateTimeLocalStr) {
    throw new Error('Вкажіть час початку сеансу.');
  }

  const [datePart, timePart] = dateTimeLocalStr.split('T');
  if (!datePart || !timePart) {
    throw new Error('Недійсний формат дати та часу.');
  }

  const [year, month, day] = datePart.split('-').map(Number);
  const [hour, minute] = timePart.split(':').map(Number);

  if (isNaN(year) || isNaN(month) || isNaN(day) || isNaN(hour) || isNaN(minute)) {
    throw new Error('Недійсний формат дати та часу.');
  }

  const pad = (n: number) => String(n).padStart(2, '0');
  const targetFormatted = `${year}-${pad(month)}-${pad(day)}T${pad(hour)}:${pad(minute)}`;

  let currentUtcMs = Date.UTC(year, month - 1, day, hour, minute, 0, 0);

  for (let pass = 0; pass < 3; pass++) {
    const currentIso = new Date(currentUtcMs).toISOString();
    const formattedKyiv = isoToKyivDateTimeLocal(currentIso);

    if (formattedKyiv === targetFormatted) {
      return currentIso;
    }

    const [kDate, kTime] = formattedKyiv.split('T');
    const [kYear, kMonth, kDay] = kDate.split('-').map(Number);
    const [kHour, kMinute] = kTime.split(':').map(Number);

    const targetMinutes = Date.UTC(year, month - 1, day, hour, minute) / 60000;
    const actualKyivMinutes = Date.UTC(kYear, kMonth - 1, kDay, kHour, kMinute) / 60000;
    const diffMs = (targetMinutes - actualKyivMinutes) * 60000;

    if (diffMs === 0) break;
    currentUtcMs += diffMs;
  }

  const finalIso = new Date(currentUtcMs).toISOString();
  const roundTripKyiv = isoToKyivDateTimeLocal(finalIso);

  if (roundTripKyiv !== targetFormatted) {
    throw new Error('Вибраний час не існує в часовому поясі Europe/Kyiv через перехід на літній час.');
  }

  return finalIso;
};
