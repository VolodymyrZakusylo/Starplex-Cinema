import api from './axios';
import type { MovieDto, MovieShortDto, TmdbSearchValue } from '@/types/movies';

export const moviesApi = {
  getAll: async (status?: 'NowShowing' | 'ComingSoon', cinemaId?: string): Promise<MovieDto[]> => {
    const params: Record<string, string> = {};
    if (status) params.status = status;
    if (cinemaId) params.cinemaId = cinemaId;

    const response = await api.get<MovieDto[]>('/Movies', { params });
    return response.data;
  },

  getShortList: async (): Promise<MovieShortDto[]> => {
    const response = await api.get<MovieShortDto[]>('/Movies');
    return response.data;
  },

  getById: async (id: string): Promise<MovieDto> => {
    const response = await api.get<MovieDto>(`/Movies/${id}`);
    return response.data;
  },

  searchTmdb: async (query: string): Promise<TmdbSearchValue[]> => {
    const response = await api.get<TmdbSearchValue[]>(`/Movies/search-tmdb?query=${encodeURIComponent(query)}`);
    return response.data;
  },

  importTmdb: async (tmdbId: number): Promise<void> => {
    await api.post(`/Movies/import-tmdb/${tmdbId}`, {});
  },

  create: async (formData: FormData): Promise<void> => {
    await api.post('/Movies', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  update: async (id: string, formData: FormData): Promise<void> => {
    await api.put(`/Movies/${id}`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/Movies/${id}`);
  }
};