import { createContext, useCallback, useContext, useEffect, useState } from 'react';

// jsdom has no matchMedia, so every read of the system preference goes through this guard.
function prefersDark() {
  return typeof window !== 'undefined' && typeof window.matchMedia === 'function'
    ? window.matchMedia('(prefers-color-scheme: dark)').matches
    : false;
}

const ThemeProviderContext = createContext({
  theme: 'system',
  isDark: false,
  setTheme: () => null
});

export function ThemeProvider({
  children,
  defaultTheme = 'system',
  storageKey = 'todo-ui-theme',
  ...props
}) {
  const [theme, setThemeState] = useState(() => {
    try {
      return localStorage.getItem(storageKey) || defaultTheme;
    } catch {
      return defaultTheme;
    }
  });

  const [systemIsDark, setSystemIsDark] = useState(prefersDark);

  useEffect(() => {
    if (typeof window.matchMedia !== 'function') return undefined;

    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const onChange = event => setSystemIsDark(event.matches);

    media.addEventListener('change', onChange);
    return () => media.removeEventListener('change', onChange);
  }, []);

  const isDark = theme === 'system' ? systemIsDark : theme === 'dark';

  useEffect(() => {
    const root = window.document.documentElement;
    root.classList.toggle('dark', isDark);
    root.classList.toggle('light', !isDark);
    root.style.colorScheme = isDark ? 'dark' : 'light';
  }, [isDark]);

  const setTheme = useCallback(
    nextTheme => {
      try {
        localStorage.setItem(storageKey, nextTheme);
      } catch {
        // Private browsing can reject writes; the theme still applies for this session.
      }
      setThemeState(nextTheme);
    },
    [storageKey]
  );

  return (
    <ThemeProviderContext.Provider {...props} value={{ theme, isDark, setTheme }}>
      {children}
    </ThemeProviderContext.Provider>
  );
}

export function useTheme() {
  return useContext(ThemeProviderContext);
}
