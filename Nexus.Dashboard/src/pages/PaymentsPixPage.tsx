import { useState } from 'react';
import { Link } from 'react-router-dom';
import { searchAccountsPicker } from '../api/accountPickerSources';
import { generatePix } from '../api/payments';
import type { GatewayPixResult } from '../api/types';
import { AccountPickerModal } from '../components/AccountPickerModal';
import { OperationPickerModal } from '../components/OperationPickerModal';

export function PaymentsPixPage() {
  const [operationId, setOperationId] = useState('');
  const [operationLabel, setOperationLabel] = useState<string | null>(null);
  const [amount, setAmount] = useState(0);
  const [operatorAccountId, setOperatorAccountId] = useState<string | null>(null);
  const [operatorLabel, setOperatorLabel] = useState<string | null>(null);
  const [strawManAccountId, setStrawManAccountId] = useState<string | null>(null);
  const [strawLabel, setStrawLabel] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [lastResult, setLastResult] = useState<GatewayPixResult | null>(null);
  const [pixCopied, setPixCopied] = useState(false);
  const [generateBusy, setGenerateBusy] = useState(false);
  const [operationPickerOpen, setOperationPickerOpen] = useState(false);
  const [operatorPickerOpen, setOperatorPickerOpen] = useState(false);
  const [strawPickerOpen, setStrawPickerOpen] = useState(false);

  async function handleGenerate() {
    setError('');
    setLastResult(null);
    setPixCopied(false);
    if (!operationId.trim()) {
      setError('Selecione uma operação.');
      return;
    }
    setGenerateBusy(true);
    try {
      const result = await generatePix({
        operationId: operationId.trim(),
        amount,
        operatorAccountId,
        strawManAccountId,
      });
      if (!result.ok) {
        setError(result.error);
        return;
      }
      setLastResult(result.data);
    } catch (ex) {
      setError(ex instanceof Error ? ex.message : 'Ocorreu um erro inesperado. Tente novamente.');
    } finally {
      setGenerateBusy(false);
    }
  }

  async function copyPixCode() {
    if (!lastResult?.code) return;
    await navigator.clipboard.writeText(lastResult.code);
    setPixCopied(true);
  }

  return (
    <>
      <section className="page-header ops-page-header">
        <div>
          <h1>Pagamentos PIX</h1>
          <p className="muted page-lead">Gere cobrança PIX vinculada a uma operação; operador e laranja são opcionais (contas).</p>
          <p className="muted small page-nav-back"><Link to="/dashboard/payments">← Registros de pagamentos</Link></p>
        </div>
      </section>

      <section className="card ops-card">
        <div className="card-title-row">
          <h2>Gerar PIX</h2>
          <span className="post-badge">POST /api/gateways/pix</span>
        </div>
        <div className="form-grid form-grid-wide">
          <div className="field">
            <label>Operação</label>
            <div className="account-select-row">
              <button type="button" className="account-select-trigger" onClick={() => setOperationPickerOpen(true)}>
                {operationLabel ?? 'Selecionar operação'}
              </button>
              <button type="button" className="btn-icon btn-icon-green" onClick={() => setOperationPickerOpen(true)} title="Selecionar operação">＋</button>
              {operationId ? (
                <button type="button" className="btn btn-ghost btn-small" onClick={() => { setOperationId(''); setOperationLabel(null); }}>Limpar</button>
              ) : null}
            </div>
          </div>
          <div className="field">
            <label htmlFor="pixAmount">Valor</label>
            <input id="pixAmount" type="number" className="nexus-input" value={amount} onChange={(e) => setAmount(Number(e.target.value))} />
          </div>
          <div className="field span-2">
            <label>Conta do operador <span className="muted small">opcional</span></label>
            <div className="account-select-row">
              <button type="button" className="account-select-trigger" onClick={() => setOperatorPickerOpen(true)}>
                {operatorLabel ?? 'Nenhum'}
              </button>
              <button type="button" className="btn-icon btn-icon-green" onClick={() => setOperatorPickerOpen(true)}>＋</button>
              {operatorAccountId ? (
                <button type="button" className="btn btn-ghost btn-small" onClick={() => { setOperatorAccountId(null); setOperatorLabel(null); }}>Limpar</button>
              ) : null}
            </div>
          </div>
          <div className="field span-2">
            <label>Conta laranja <span className="muted small">opcional</span></label>
            <div className="account-select-row">
              <button type="button" className="account-select-trigger" onClick={() => setStrawPickerOpen(true)}>
                {strawLabel ?? 'Nenhum'}
              </button>
              <button type="button" className="btn-icon btn-icon-warm" onClick={() => setStrawPickerOpen(true)}>＋</button>
              {strawManAccountId ? (
                <button type="button" className="btn btn-ghost btn-small" onClick={() => { setStrawManAccountId(null); setStrawLabel(null); }}>Limpar</button>
              ) : null}
            </div>
          </div>
        </div>
        <div className="card-actions">
          <button type="button" className="btn btn-primary" onClick={() => void handleGenerate()} disabled={generateBusy}>
            {generateBusy ? 'Gerando…' : 'Gerar PIX'}
          </button>
        </div>
      </section>

      <OperationPickerModal
        open={operationPickerOpen}
        onClose={() => setOperationPickerOpen(false)}
        title="Selecionar operação"
        subtitle="A cobrança PIX será criada no contexto desta operação."
        onSelected={(row) => {
          setOperationId(row.id);
          setOperationLabel(`${row.name} (${row.id})`);
        }}
      />

      <AccountPickerModal
        open={operatorPickerOpen}
        onClose={() => setOperatorPickerOpen(false)}
        searchAccounts={searchAccountsPicker}
        title="Conta do operador"
        subtitle="Opcional — filtra a cobrança ao operador."
        onSelected={(row) => {
          setOperatorAccountId(row.id);
          setOperatorLabel(`${row.username} (${row.id})`);
        }}
      />

      <AccountPickerModal
        open={strawPickerOpen}
        onClose={() => setStrawPickerOpen(false)}
        searchAccounts={searchAccountsPicker}
        title="Conta laranja"
        subtitle="Opcional — alinha credenciais de gateway com laranja."
        onSelected={(row) => {
          setStrawManAccountId(row.id);
          setStrawLabel(`${row.username} (${row.id})`);
        }}
      />

      {error ? (
        <section className="feedback-block error">
          <h3>Falha ao gerar cobrança</h3>
          <p>{error}</p>
        </section>
      ) : null}

      {lastResult ? (
        <section className="card success-panel">
          <h3>Cobrança gerada</h3>
          <p><strong>ID do pagamento:</strong> <span className="mono">{lastResult.id}</span></p>
          <p><strong>Código PIX:</strong></p>
          <div className="copy-wrap">
            <textarea readOnly rows={6} className="nexus-input" value={lastResult.code} />
            <button type="button" className="btn btn-ghost" onClick={() => void copyPixCode()}>Copiar</button>
          </div>
          {pixCopied ? <p className="feedback success">Código PIX copiado.</p> : null}
        </section>
      ) : null}
    </>
  );
}
