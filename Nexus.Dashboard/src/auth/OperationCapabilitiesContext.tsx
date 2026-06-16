import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { searchOperationAdministratorOperations } from '../api/operationAdministrator/operations';
import { searchTeamLeaderLedTeams } from '../api/teamLeader/operations';
import { useAuth } from './AuthContext';
import { isAdministrator } from './roles';

type OperationCapabilities = {
  operationAdministrator: boolean;
  teamLeader: boolean;
  loading: boolean;
};

const defaultCapabilities: OperationCapabilities = {
  operationAdministrator: false,
  teamLeader: false,
  loading: true,
};

const OperationCapabilitiesContext = createContext<OperationCapabilities>(defaultCapabilities);

export function OperationCapabilitiesProvider({ children }: { children: ReactNode }) {
  const { user, isInitializing } = useAuth();
  const [capabilities, setCapabilities] = useState<OperationCapabilities>(defaultCapabilities);

  const refresh = useCallback(async () => {
    if (isInitializing) return;

    if (!user) {
      setCapabilities({ operationAdministrator: false, teamLeader: false, loading: false });
      return;
    }

    if (isAdministrator(user)) {
      setCapabilities({ operationAdministrator: false, teamLeader: false, loading: false });
      return;
    }

    setCapabilities((prev) => ({ ...prev, loading: true }));

    const [operationAdminResult, teamLeaderResult] = await Promise.all([
      searchOperationAdministratorOperations({ limit: 1, offset: 0 }),
      searchTeamLeaderLedTeams({ limit: 1, offset: 0 }),
    ]);

    setCapabilities({
      operationAdministrator: operationAdminResult.ok,
      teamLeader: teamLeaderResult.ok,
      loading: false,
    });
  }, [isInitializing, user]);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const value = useMemo(() => capabilities, [capabilities]);

  return (
    <OperationCapabilitiesContext.Provider value={value}>
      {children}
    </OperationCapabilitiesContext.Provider>
  );
}

export function useOperationCapabilities() {
  return useContext(OperationCapabilitiesContext);
}
