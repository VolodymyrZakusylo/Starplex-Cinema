import axios from 'axios';

const backendUrl = import.meta.env.VITE_BACKEND_URL
  ? import.meta.env.VITE_BACKEND_URL.replace(/\/$/, '')
  : '';

const api = axios.create({
  baseURL: backendUrl ? `${backendUrl}/api` : '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true;

      try {
        const refreshToken = localStorage.getItem('refreshToken');
        const userId = localStorage.getItem('userId');

        if (!refreshToken) {
          throw new Error('No refresh token available');
        }

        const refreshEndpoint = backendUrl ? `${backendUrl}/api/auth/refresh-token` : '/api/auth/refresh-token';
        const response = await axios.post(refreshEndpoint, {
          userId: userId,
          refreshToken: refreshToken
        });

        const { token: newCode, refreshToken: newRefresh } = response.data;

        localStorage.setItem('token', newCode);
        localStorage.setItem('refreshToken', newRefresh);

        originalRequest.headers.Authorization = `Bearer ${newCode}`;
        return api(originalRequest);
      } catch (refreshError) {
        localStorage.clear();
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default api;