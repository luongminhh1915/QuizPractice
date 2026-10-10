import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The API runs on http://localhost:5031 (launch profile "http"). In development the
// Vite server forwards /api and /uploads there, so the browser sees one origin, as
// it will in production where the API host serves this build (SDS I.1): the sign-in
// cookie works and no CORS is needed.
const api = 'http://localhost:5031'

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    // Fail instead of moving to 5174: launch.json and the README expect 5173.
    strictPort: true,
    proxy: {
      '/api': api,
      '/uploads': api,
      // Images of the development sample data (README, "Dữ liệu mẫu").
      '/seed': api,
    },
  },
})
