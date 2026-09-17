import api from './axios';
import type { PagedUserStaffResponse } from '@/types/admin';

export interface GetStaffParams {
  searchTerm?: string;
  roleFilter?: string;
  cinemaIdFilter?: string;
  page?: number;
  pageSize?: number;
}

export const usersApi = {
  getStaff: async (params?: GetStaffParams): Promise<PagedUserStaffResponse> => {
    const response = await api.get<PagedUserStaffResponse>('/Users/staff', { params });
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
