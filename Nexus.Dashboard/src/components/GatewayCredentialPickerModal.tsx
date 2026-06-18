import { useEffect, useId, useState } from 'react';
import { searchCredentialsForPicker } from '../api/gateways';
import type { GatewayCredentialPickerRow, GatewayPrefix } from '../api/types';
import { IconButton } from './IconButton';
import { PaginationBar } from './ListControls';

type GatewayKind = GatewayPrefix;

const GATEWAY_LABELS: Record<GatewayKind, string> = {
  frendz: 'Frendz',
  sigilopay: 'SigiloPay',
  wintech: 'Wintech',
};

type GatewayCredentialPickerModalProps = {
  open: boolean;
  onClose: () => void;
  title?: string;
  subtitle?: string;
  disabledCredentialIds?: Set<string>;
  disabledBadgeText?: string;
  onSelected: (row: GatewayCredentialPickerRow) => void;
};

const PAGE_SIZE = 8;

export function GatewayCredentialPickerModal({
  open,
  onClose,
  title = 'Selecionar credencial',
  subtitle,
  disabledCredentialIds,
  disabledBadgeText = 'Já na lista',
  onSelected,
}: GatewayCredentialPickerModalProps) {
  const searchInputId = useId();
  const [gateway, setGateway] = useState<GatewayKind>('frendz');
  const [keyword, setKeyword] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalItems, setTotalItems] = useState(0);
  const [items, setItems] = useState<GatewayCredentialPickerRow[]>([]);
  const [loading, setLoading] = useState(false);

  const totalPages = totalItems === 0 ? 1 : Math.ceil(totalItems / PAGE_SIZE);

  useEffect(() => {
    if (!open) return;
    setGateway('frendz');
    setCurrentPage(1);
    setKeyword('');
    void load('frendz', 1, '');
  }, [open]);

  async function load(gw: GatewayKind, page: number, term: string) {
    setLoading(true);
    try {
      const result = await searchCredentialsForPicker(gw, {
        limit: PAGE_SIZE,
        offset: (page - 1) * PAGE_SIZE,
        keyword: term.trim() || null,
      });
      if (!result.ok) {
        setItems([]);
        setTotalItems(0);
        return;
      }
      const label = GATEWAY_LABELS[gw];
      setTotalItems(result.data?.total ?? 0);
      setItems((result.data?.items ?? []).map((it) => ({
        id: it.id,
        name: it.name?.trim() ? it.name : it.id,
        gatewayLabel: label,
      })));
    } finally {
      setLoading(false);
    }
  }

  function isDisabled(id: string) {
    return disabledCredentialIds?.has(id) ?? false;
  }

  async function setGatewayAndLoad(next: GatewayKind) {
    if (gateway === next) return;
    setGateway(next);
    setCurrentPage(1);
    await load(next, 1, keyword);
  }

  async function search() {
    setCurrentPage(1);
    await load(gateway, 1, keyword);
  }

  async function prevPage() {
    if (currentPage <= 1) return;
    const next = currentPage - 1;
    setCurrentPage(next);
    await load(gateway, next, keyword);
  }

  async function nextPage() {
    if (currentPage >= totalPages) return;
    const next = currentPage + 1;
    setCurrentPage(next);
    await load(gateway, next, keyword);
  }

  function pick(row: GatewayCredentialPickerRow) {
    if (isDisabled(row.id)) return;
    onSelected(row);
    onClose();
  }

  if (!open) return null;

  return (
    <div className="dialog-backdrop dialog-backdrop--picker account-picker-backdrop" onClick={onClose}>
      <div className="dialog-card account-picker" onClick={(e) => e.stopPropagation()}>
        <div className="account-picker-header">
          <div>
            <h3 className="account-picker-title">{title}</h3>
            {subtitle ? <p className="account-picker-sub muted">{subtitle}</p> : null}
          </div>
          <IconButton icon="x" label="Fechar" onClick={onClose} />
        </div>

        <div className="credential-picker-gateway-tabs" role="tablist" aria-label="Gateway">
          {(Object.keys(GATEWAY_LABELS) as GatewayKind[]).map((gw) => (
            <button
              key={gw}
              type="button"
              role="tab"
              className={`credential-picker-tab ${gateway === gw ? 'is-active' : ''}`}
              aria-selected={gateway === gw}
              onClick={() => void setGatewayAndLoad(gw)}
            >
              {GATEWAY_LABELS[gw]}
            </button>
          ))}
        </div>

        <div className="toolbar account-picker-toolbar">
          <div className="field grow">
            <label htmlFor={searchInputId}>Pesquisar</label>
            <input
              id={searchInputId}
              className="nexus-input account-picker-search"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
              onKeyDown={(e) => { if (e.key === 'Enter') void search(); }}
              placeholder="Nome ou ID da credencial…"
            />
          </div>
          <IconButton icon="search" label="Buscar credenciais" variant="primary" onClick={() => void search()} />
        </div>

        <div className="account-picker-list" tabIndex={-1}>
          {loading ? (
            <p className="muted account-picker-hint">Carregando…</p>
          ) : items.length === 0 ? (
            <p className="muted account-picker-hint">Nenhuma credencial encontrada.</p>
          ) : (
            items.map((row) => {
              const disabled = isDisabled(row.id);
              return (
                <button
                  key={row.id}
                  type="button"
                  className={`account-picker-row ${disabled ? 'is-disabled' : ''}`}
                  disabled={disabled}
                  onClick={() => pick(row)}
                >
                  <span className="account-picker-meta">
                    <span className="account-picker-name">{row.name}</span>
                    <span className="account-picker-id">{row.id}</span>
                  </span>
                  {disabled ? <span className="account-picker-badge">{disabledBadgeText}</span> : null}
                </button>
              );
            })
          )}
        </div>

        <PaginationBar
          className="account-picker-pagination"
          currentPage={currentPage}
          totalPages={totalPages}
          disabled={loading}
          onPrev={() => void prevPage()}
          onNext={() => void nextPage()}
        />
      </div>
    </div>
  );
}
