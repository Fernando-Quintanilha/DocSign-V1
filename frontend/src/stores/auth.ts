import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  token: string | null;
  refreshToken: string | null;
  admin: AdminInfo | null;
  setAuth: (token: string, refreshToken: string, admin: AdminInfo) => void;
  setTokens: (token: string, refreshToken: string) => void;
  logout: () => void;
}

export interface AdminInfo {
  id: string;
  name: string;
  email: string;
  companyName: string;
  role: 'Admin' | 'SuperAdmin';
  planName: string;
  emailVerified: boolean;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      refreshToken: null,
      admin: null,
      setAuth: (token, refreshToken, admin) => set({ token, refreshToken, admin }),
      setTokens: (token, refreshToken) => set({ token, refreshToken }),
      logout: () => set({ token: null, refreshToken: null, admin: null }),
    }),
    {
      name: 'holeritesign-auth',
    }
  )
);
