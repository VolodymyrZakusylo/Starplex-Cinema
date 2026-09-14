import api from './axios';
import type { DiscountDto } from '@/types/discounts';

export const discountsApi = {
  getAll: async (): Promise<DiscountDto[]> => {
    const response = await api.get<DiscountDto[]>('/Discounts');
    return response.data;
  },

  create: async (payload: any): Promise<void> => {
    await api.post('/Discounts/create', payload);
  },

  delete: async (id: string): Promise<void> => {
    await api.delete(`/Discounts/${id}`);
  },

  validate: async (code: string): Promise<{ percentage: number }> => {
    const response = await api.get<{ percentage: number }>(`/Discounts/validate/${encodeURIComponent(code)}`);
    return response.data;
  }
};
