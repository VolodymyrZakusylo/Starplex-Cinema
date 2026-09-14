import api from './axios';
import type { AdminStatsDto } from '@/types/admin';

export const analyticsApi = {
  getAdminStats: async (params?: { days?: number; isOnline?: string; cinemaId?: string }): Promise<AdminStatsDto> => {
    const queryParams: Record<string, any> = {};
    if (params?.days) queryParams.days = params.days;
    if (params?.isOnline) queryParams.isOnline = params.isOnline;
    if (params?.cinemaId && params.cinemaId !== 'all') queryParams.cinemaId = params.cinemaId;

    const response = await api.get<AdminStatsDto>('/Analytics/admin-stats', { params: queryParams });
    return response.data;
  }
};
