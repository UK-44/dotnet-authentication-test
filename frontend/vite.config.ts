import vue from '@vitejs/plugin-vue'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue()],
  server: {
    // /api をバックエンドに転送（docker では API_URL=http://backend:8080、ローカルでは dotnet run のポート）
    proxy: {
      '/api': process.env.API_URL ?? 'http://localhost:5087',
    },
  },
})
