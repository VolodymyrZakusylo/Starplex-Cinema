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
