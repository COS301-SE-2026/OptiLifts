import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { BrowserRouter } from 'react-router-dom'
import { Toaster } from './components/ui/alert'
import "@fontsource/bebas-neue/400.css";
import "@fontsource/barlow/300.css";
import "@fontsource/barlow/400.css";
import "@fontsource/barlow/500.css";
import "@fontsource/barlow/600.css";
import './index.css'
import App from './App.tsx'
import { AuthProvider } from './context/auth-context'
import * as Sentry from "@sentry/react";

Sentry.init({
  dsn: import.meta.env.VITE_SENTRY_DSN,
  integrations: [
    Sentry.browserTracingIntegration(),
  ],

  environment: import.meta.env.MODE,
  enabled: import.meta.env.PROD && window.location.hostname !== 'localhost',

  tracesSampleRate: 1.0,
  replaysSessionSampleRate: 0.1,
  replaysOnErrorSampleRate: 1.0,
});

Sentry.lazyLoadIntegration("replayIntegration").then((replayIntegration) => { //NOSONAR, using promise chain to prevent it from blocking render of frontend
  Sentry.addIntegration(replayIntegration({ 
    maskAllText: false,
    blockAllMedia: true,
  }));
});


createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <AuthProvider>
        <App />
        <Toaster />
      </AuthProvider>
    </BrowserRouter>
  </StrictMode>,
)
