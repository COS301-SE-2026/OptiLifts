import { fileURLToPath } from 'node:url'
import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'
import { sentryVitePlugin } from '@sentry/vite-plugin'

export default defineConfig(({ mode }) => {
  const rootDir = fileURLToPath(new URL('..', import.meta.url))
  const env = loadEnv(mode, rootDir, '')

  const apiBaseUrl = env.API_BASE_URL ?? 'http://localhost:5036'
  const frontendPort = Number(env.FRONTEND_PORT ?? '5173')

  return {
    envDir: rootDir,
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

      env.SENTRY_AUTH_TOKEN ? sentryVitePlugin({ //only runs if env variable which is only in CD
        authToken: process.env.SENTRY_AUTH_TOKEN,
        org: "hatrock-un",
        project: "hatrock-frontend",
        release: {
          name: process.env.SENTRY_RELEASE,
        }
      }) : null,

    ],
    test: {
      environment: 'jsdom',
      coverage: {
        reporter: ['text', 'json-summary'],
      },
    },
    server: {
      port: frontendPort,
      proxy: {
        '/api': {
          target: apiBaseUrl,
          changeOrigin: true,
          secure: false,
        },
      },
    },
    preview: {
      port: frontendPort,
    },
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
        //can say @/components/ui/button instead of ../../../components/ui/button 
      },
    },

    build: {
      sourcemap: true,
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (id.includes('@sentry')) {
              return 'sentry';
            }
          }
        },
      },
    },
  }
})