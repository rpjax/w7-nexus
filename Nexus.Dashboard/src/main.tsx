import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './auth/AuthContext';
import { OperationCapabilitiesProvider } from './auth/OperationCapabilitiesContext';
import { NotificationProvider } from './notifications/NotificationContext';
import App from './App';
import './styles/dashboard.css';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <BrowserRouter>
      <NotificationProvider>
        <AuthProvider>
          <OperationCapabilitiesProvider>
            <App />
          </OperationCapabilitiesProvider>
        </AuthProvider>
      </NotificationProvider>
    </BrowserRouter>
  </StrictMode>,
);
