import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

const devProxyUserId = process.env.DEV_PROXY_USER_ID || 'local-dev-user';
const devClientPrincipal = Buffer.from(JSON.stringify({ userId: devProxyUserId })).toString('base64');

export default defineConfig({
  plugins: [react()],
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
