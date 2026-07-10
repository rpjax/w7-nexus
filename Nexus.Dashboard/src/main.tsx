import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { NotificationProvider } from './notifications/NotificationContext';
import { AnimatedBackground } from './components/AnimatedBackground';
import App from './App';
import './styles/animated-background.css';
import './styles/dashboard.css';
import './styles/scripts.css';
import './styles/api-docs.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <NotificationProvider>
        <AuthProvider>
          <AnimatedBackground />
          <div className="app-frame">
            <App />
          </div>
        </AuthProvider>
      </NotificationProvider>
    </BrowserRouter>
  </StrictMode>,
);
