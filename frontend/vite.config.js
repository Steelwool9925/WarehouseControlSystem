import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Pinned so the dev server never silently shifts ports and breaks the backend's
    // CORS allow-list (see backend/WarehouseControl.Api/Program.cs).
    port: 5173,
    strictPort: true,
  },
})
