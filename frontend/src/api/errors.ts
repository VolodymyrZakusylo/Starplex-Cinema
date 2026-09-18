import { isAxiosError } from 'axios';

export function getApiErrorMessage(error: unknown, fallback: string): string {
  if (!isAxiosError(error)) return fallback;
  const data = error.response?.data;
  if (!data || typeof data !== 'object') return fallback;
  if (data.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors).flat().filter((value): value is string => typeof value === 'string');
    if (messages.length) return messages.join(' ');
  }
  if (typeof data.detail === 'string' && data.detail) return data.detail;
  if (typeof data.message === 'string' && data.message) return data.message;
  if (typeof data.Message === 'string' && data.Message) return data.Message;
  return fallback;
}
