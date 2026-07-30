import api from './axios';
import type { Movie } from '@/types';

export const moviesApi = {
  getAll: async (status?: 'NowShowing' | 'ComingSoon', cinemaId?: string): Promise<Movie[]> => {
    const params: Record<string, string> = {};
    if (status) params.status = status;
    if (cinemaId) params.cinemaId = cinemaId;

    const response = await api.get<Movie[]>('/movies', { params });
    return response.data;
  },

  getById: async (id: string): Promise<Movie> => {
    const response = await api.get<Movie>(`/movies/${id}`);
    return response.data;
  }
};