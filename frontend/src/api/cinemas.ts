import api from './axios';
import type { CinemaDto } from '@/types/cinemas';

export const cinemasApi = {
  getAll: async (): Promise<CinemaDto[]> => {
    const response = await api.get<CinemaDto[]>('/Cinemas');
    return response.data;
  },

  getById: async (id: string): Promise<CinemaDto> => {
    const response = await api.get<CinemaDto>(`/Cinemas/${id}`);
    return response.data;
  },

  create: async (data: { name: string; city: string; address: string }): Promise<void> => {
    await api.post('/Cinemas', data);
  },

  update: async (id: string, data: { id: string; name: string; city: string; address: string }): Promise<void> => {
    await api.put(`/Cinemas/${id}`, data);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/Cinemas/${id}`);
  }
};
