import api from './axios';
import type { AuditLogDto, PagedResponse } from '@/types/admin';

export const auditApi = {
  getLogs: async (params?: Record<string, any>): Promise<PagedResponse<AuditLogDto>> => {
    const response = await api.get<PagedResponse<AuditLogDto>>('/Audit/logs', { params });
    return response.data;
  }
};
