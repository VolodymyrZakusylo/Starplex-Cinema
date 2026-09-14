import api from './axios';
import type { HallDto } from '@/types/halls';

export const hallsApi = {
  getByCinema: async (cinemaId: string): Promise<HallDto[]> => {
    const response = await api.get<HallDto[]>(`/Halls/cinema/${cinemaId}`);
    return response.data;
  },

  getById: async (id: string): Promise<HallDto> => {
    const response = await api.get<HallDto>(`/Halls/${id}`);
    return response.data;
  },

  create: async (payload: any): Promise<void> => {
    await api.post('/Halls', payload);
  },

  update: async (id: string, payload: any): Promise<void> => {
    await api.put(`/Halls/${id}`, payload);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/Halls/${id}`);
  },

  updateSeatProperties: async (seatId: string, payload: { type: number; status: number }): Promise<void> => {
    await api.put(`/Halls/seats/${seatId}/properties`, payload);
  }
};
