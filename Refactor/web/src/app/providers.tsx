import type { ReactNode } from 'react';
import { AuthProvider } from '@/auth/AuthContext';
import { MandateProvider } from '@/auth/MandateContext';
import { FeedbackProvider } from '@/feedback';

export function AppProviders({ children }: { children: ReactNode }) {
  return (
    <FeedbackProvider>
      <AuthProvider>
        <MandateProvider>
          {children}
        </MandateProvider>
      </AuthProvider>
    </FeedbackProvider>
  );
}
