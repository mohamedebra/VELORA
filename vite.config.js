import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    proxy: {
      '/api': { target: 'http://127.0.0.1:5080', changeOrigin: true },
    },
  },
  build: {
    rollupOptions: {
      input: {
        customer: path.resolve(root, 'index.html'),
        staffPortal: path.resolve(root, 'portal.html'),
      },
    },
  },
});
