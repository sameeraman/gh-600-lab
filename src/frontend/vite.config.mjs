import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

const devProxyUserId = process.env.DEV_PROXY_USER_ID || 'local-dev-user';
const devClientPrincipal = Buffer.from(JSON.stringify({ userId: devProxyUserId })).toString('base64');

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    port: 3000,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        configure: proxy => {
          proxy.on('proxyReq', proxyReq => {
            proxyReq.setHeader('x-ms-client-principal', devClientPrincipal);
          });
        }
      }
    }
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './tests/setup.js',
    exclude: ['e2e/**', 'node_modules/**']
  }
});
