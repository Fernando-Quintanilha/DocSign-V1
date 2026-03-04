import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface ThemeState {
  dark: boolean;
  toggle: () => void;
  setDark: (v: boolean) => void;
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set) => ({
      dark: false,
      toggle: () =>
        set((s) => {
          const next = !s.dark;
          applyTheme(next);
          return { dark: next };
        }),
      setDark: (v: boolean) => {
        applyTheme(v);
        set({ dark: v });
      },
    }),
    { name: 'holeritesign-theme' }
  )
);

function applyTheme(dark: boolean) {
  if (dark) {
    document.documentElement.classList.add('dark');
  } else {
    document.documentElement.classList.remove('dark');
  }
}

// Apply on load
const stored = localStorage.getItem('holeritesign-theme');
if (stored) {
  try {
    const parsed = JSON.parse(stored);
    if (parsed?.state?.dark) {
      document.documentElement.classList.add('dark');
    }
  } catch {
    // ignore
  }
}
