import { fileURLToPath } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      registerType: 'autoUpdate',
      injectRegister: 'inline',
      manifest: {
        name: 'OptiLifts',
        short_name: 'OptiLifts',
        description: 'OptiLifts demo app',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        icons: [ //this is a placeholder for our PWA icon, replace with a 192x512px icon 
          {
            src: '/favicon.svg',
            sizes: 'any',
            type: 'image/svg+xml',
            purpose: 'any maskable',
          },
        ],
      },
    }),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)), 
      //can say @/components/ui/button instead of ../../../components/ui/button 
    },
  },
})