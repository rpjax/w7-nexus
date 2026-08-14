import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from 'react';
import { getMyMandate, type MyMandate } from '@/api/authenticated/mandates';
import { useAuth } from '@/auth/AuthContext';
import { reportError } from '@/feedback';
import { isAdministrator } from '@/utils/accountAccess';

export type HubAccess = {
  admin: boolean;
  canGrant: boolean;
  canManageOperations: boolean;
  canManageGateways: boolean;
  canSeeFinance: boolean;
  canActAsOperator: boolean;
  canRecruit: boolean;
  presets: string[];
};

const emptyAccess = (admin: boolean): HubAccess => ({
  admin,
  canGrant: admin,
  canManageOperations: admin,
  canManageGateways: admin,
  canSeeFinance: admin,
  canActAsOperator: admin,
  canRecruit: admin,
  presets: [],
});

function fromMandate(admin: boolean, mandate: MyMandate | null): HubAccess {
  if (!mandate) return emptyAccess(admin);
  return {
    admin,
    canGrant: admin || mandate.canGrant,
    canManageOperations: admin || mandate.canManageOperations,
    canManageGateways: admin || mandate.canManageGateways,
    canSeeFinance: admin || mandate.canSeeFinance,
    canActAsOperator: admin || mandate.canActAsOperator,
    canRecruit: admin || mandate.canRecruit,
    presets: mandate.appliedPresets ?? [],
  };
}

type MandateContextValue = {
  access: HubAccess;
  loading: boolean;
  reload: () => Promise<void>;
};

const MandateContext = createContext<MandateContextValue | null>(null);

export function MandateProvider({ children }: { children: ReactNode }) {
  const { user, isAuthenticated } = useAuth();
  const admin = isAdministrator(user?.roles);
  const [mandate, setMandate] = useState<MyMandate | null>(null);
  const [loading, setLoading] = useState(false);

  const reload = useCallback(async () => {
    if (!isAuthenticated) {
      setMandate(null);
      return;
    }
    setLoading(true);
    const result = await getMyMandate();
    if (!result.ok) {
      if (result.status !== 401 && result.status !== 403 && result.status !== 404) {
        reportError(result.error);
      }
      setMandate(null);
    } else {
      setMandate(result.data);
    }
    setLoading(false);
  }, [isAuthenticated]);

  useEffect(() => {
    void reload();
  }, [reload]);

  const value = useMemo<MandateContextValue>(
    () => ({
      access: fromMandate(admin, mandate),
      loading,
      reload,
    }),
    [admin, mandate, loading, reload],
  );

  return <MandateContext.Provider value={value}>{children}</MandateContext.Provider>;
}

export function useHubAccess(): HubAccess {
  const context = useContext(MandateContext);
  if (!context) {
    throw new Error('useHubAccess must be used within MandateProvider');
  }
  return context.access;
}

export function useMandateHub(): MandateContextValue {
  const context = useContext(MandateContext);
  if (!context) {
    throw new Error('useMandateHub must be used within MandateProvider');
  }
  return context;
}
