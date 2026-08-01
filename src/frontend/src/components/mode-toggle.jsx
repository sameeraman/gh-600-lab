import { MoonIcon, SunIcon } from 'lucide-react';

import { Button } from '@/components/ui/button';
import { useTheme } from '@/components/theme-provider';

export function ModeToggle() {
  const { isDark, setTheme } = useTheme();

  return (
    <Button
      variant="ghost"
      size="icon"
      className="relative"
      aria-label="Toggle theme"
      title={isDark ? 'Switch to light theme' : 'Switch to dark theme'}
      onClick={() => setTheme(isDark ? 'light' : 'dark')}
    >
      <SunIcon className="size-[1.15rem] scale-100 rotate-0 transition-transform duration-300 dark:scale-0 dark:-rotate-90" />
      <MoonIcon className="absolute size-[1.15rem] scale-0 rotate-90 transition-transform duration-300 dark:scale-100 dark:rotate-0" />
    </Button>
  );
}
