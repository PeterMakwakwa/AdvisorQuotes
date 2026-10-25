/// <reference types="vitest/config" />
import { defineConfig, loadEnv } from 'vite';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';

// The browser calls /api on its own origin and Vite forwards it to the Quotes API, so the API needs no CORS policy.
// Point QUOTES_API_URL at a different API without changing code; `npm run dev:source` loads .env.source, which targets
// `dotnet run` on http://localhost:5039.
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '');
  const apiProxy = {
    '/api': { target: env.QUOTES_API_URL ?? 'http://localhost:8080', changeOrigin: true },
  };

  return {
    plugins: [react(), tailwindcss()],
    // Root-relative, so it resolves from wherever Vite runs (an absolute path breaks under folders such as C#).
    resolve: { alias: [{ find: /^@\//, replacement: '/src/' }] },
    build: {
      rolldownOptions: {
        output: {
          // Framework code changes far less often than app code, so it gets its own long-cached chunks.
          codeSplitting: {
            groups: [
              { name: 'react', test: /node_modules[\\/](react|react-dom|scheduler)[\\/]/ },
              { name: 'router', test: /node_modules[\\/]react-router[\\/]/ },
              { name: 'query', test: /node_modules[\\/]@tanstack[\\/]/ },
              { name: 'ui', test: /node_modules[\\/](@radix-ui|radix-ui|@floating-ui|sonner|cn|next-themes)[\\/]/ },
            ],
          },
        },
      },
    },
    server: { port: 5173, proxy: apiProxy },
    preview: { port: 4173, proxy: apiProxy },
    test: {
      environment: 'jsdom',
      globals: true,
      setupFiles: ['./src/test/setup.ts'],
      css: false,
    },
  };
});
