import api from './axios';
import type { SessionDto } from '@/types/sessions';

export const sessionsApi = {
  getByCinema: async (cinemaId: string): Promise<SessionDto[]> => {
    const response = await api.get<SessionDto[]>(`/Sessions/cinema/${cinemaId}`);
    return response.data;
  },

  create: async (payload: any): Promise<void> => {
    await api.post('/Sessions', payload);
  },

  update: async (id: string, payload: any): Promise<void> => {
    await api.put(`/Sessions/${id}`, payload);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/Sessions/${id}`);
  },

  move: async (id: string, newStartTime: string): Promise<void> => {
    await api.put('/Sessions/move', { id, newStartTime });
  },

  generateSchedule: async (payload: any): Promise<{ success: boolean; count: number; message: string }> => {
    const response = await api.post<{ success: boolean; count: number; message: string }>('/Sessions/generate-schedule', payload);
    return response.data;
  }
};
