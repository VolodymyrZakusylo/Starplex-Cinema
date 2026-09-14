import api from './axios';
import type { UserStaffDto, PagedResponse } from '@/types/admin';

export const usersApi = {
  getStaff: async (params?: Record<string, any>): Promise<PagedResponse<UserStaffDto>> => {
    const response = await api.get<PagedResponse<UserStaffDto>>('/Users/staff', { params });
    return response.data;
  },

  updateRole: async (userId: string, newRole: string, cinemaId?: string | null): Promise<void> => {
    await api.put(`/Users/${userId}/role`, { newRole, cinemaId });
  },

  updateProfile: async (data: { firstName: string; lastName: string; email?: string }): Promise<void> => {
    await api.put('/User/update-profile', data);
  },

  changePassword: async (data: { oldPassword: string; newPassword: string }): Promise<void> => {
    await api.post('/User/change-password', data);
  },

  deleteAccount: async (): Promise<void> => {
    await api.delete('/User/delete-account');
  }
};
