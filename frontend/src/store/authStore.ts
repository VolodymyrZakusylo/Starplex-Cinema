import { create } from 'zustand';

interface UserState {
  token: string | null;
  userId: string | null;
  email: string | null;
  firstName: string | null;
  lastName: string | null;
  roles: string[];
  cinemaId: string | null;
}

interface AuthInterface {
  user: UserState | null;
  isAuthenticated: boolean;
  isInitialized: boolean;
  login: (authData: {
    token: string;
    refreshToken: string;
    userId: string;
    email: string;
    firstName: string;
    lastName: string;
    roles: string[];
    cinemaId?: string | null;
  }) => void;
  logout: () => void;
  checkAuth: () => void;
}

export const useAuthStore = create<AuthInterface>((set) => ({
  user: null,
  isAuthenticated: false,
  isInitialized: false,

  login: (authData) => {
    localStorage.setItem('token', authData.token);
    localStorage.setItem('refreshToken', authData.refreshToken);
    localStorage.setItem('userId', authData.userId);
    localStorage.setItem('email', authData.email);
    localStorage.setItem('firstName', authData.firstName);
    localStorage.setItem('lastName', authData.lastName);
    localStorage.setItem('roles', JSON.stringify(authData.roles));
    
    if (authData.cinemaId) {
      localStorage.setItem('cinemaId', authData.cinemaId);
    } else {
      localStorage.removeItem('cinemaId');
    }

    set({
      user: {
        token: authData.token,
        userId: authData.userId,
        email: authData.email,
        firstName: authData.firstName,
        lastName: authData.lastName,
        roles: authData.roles,
        cinemaId: authData.cinemaId || null,
      },
      isAuthenticated: true,
      isInitialized: true,
    });
  },

  logout: () => {
    localStorage.clear();
    set({ user: null, isAuthenticated: false, isInitialized: true });
  },

  checkAuth: () => {
    const token = localStorage.getItem('token');
    const userId = localStorage.getItem('userId');
    const email = localStorage.getItem('email');
    const firstName = localStorage.getItem('firstName');
    const lastName = localStorage.getItem('lastName');
    const storedRoles = localStorage.getItem('roles');
    const cinemaId = localStorage.getItem('cinemaId');

    if (token && userId) {
      const roles = storedRoles ? JSON.parse(storedRoles) : [];

      set({
        user: {
          token,
          userId,
          email,
          firstName,
          lastName,
          roles: roles,
          cinemaId: cinemaId,
        },
        isAuthenticated: true,
        isInitialized: true,
      });
    } else {
      set({ 
        user: null, 
        isAuthenticated: false, 
        isInitialized: true 
      });
    }
  }
}));