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
    envPrefix: ['VITE_', 'GOOGLE_'],
    define: {
      'import.meta.env.GOOGLE_CLIENT_ID': JSON.stringify(env.GOOGLE_CLIENT_ID ?? ''),
    },
    plugins: [
      react(),
      VitePWA({
        registerType: 'autoUpdate',
        workbox: {
          inlineWorkboxRuntime: true,
          globPatterns: ['**/*.{js,css,html,ico,png,svg,woff,woff2}'],
          skipWaiting: true,
          clientsClaim: true,
          runtimeCaching: [
            {
              // for exer images: azure blob in deployed envs, azurite (docker-compose) locally.
              // adaptImgUrl rewrites the container hostname to 127.0.0.1 before the browser fetches.
              urlPattern: ({ url }) =>
                url.hostname.endsWith('.blob.core.windows.net') ||
                (url.hostname === '127.0.0.1' && url.port === '10000'),
              handler: 'CacheFirst',
              options: {
                cacheName: 'exercise-images',
                expiration: { maxEntries: 300, maxAgeSeconds: 60 * 60 * 24 * 30 },
                cacheableResponse: { statuses: [0, 200] },
              },
            },
          ],
        },
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