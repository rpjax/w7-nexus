import { useMemo, useState } from 'react';
import { searchAdministratorAccountsPicker, searchOpAdminStrawMenPicker } from '../api/accountPickerSources';
import { searchAdministratorOperationsPicker } from '../api/operationPickerSources';
import { generatePix } from '../api/payments';
import type { GatewayPixResult } from '../api/types';
import { AccountPickerModal } from '../components/AccountPickerModal';
import { PixEntityField } from '../components/finance/PixEntityField';
import { Icon } from '../components/IconButton';
import { OperationPickerModal } from '../components/OperationPickerModal';
import { PageHeading } from '../layouts/PageHeading';
import { formatMoney } from '../utils/financeLabels';
import { shortId } from '../utils/format';

function parseAmountInput(raw: string): number {
  const normalized = raw.replace(',', '.').trim();
  if (!normalized) return 0;
  const value = Number(normalized);
  return Number.isFinite(value) ? value : 0;
}

export function PaymentsPixPage() {
  const [operationId, setOperationId] = useState('');
  const [operationName, setOperationName] = useState<string | null>(null);
  const [amountInput, setAmountInput] = useState('');
  const [operatorAccountId, setOperatorAccountId] = useState<string | null>(null);
  const [operatorName, setOperatorName] = useState<string | null>(null);
  const [strawManAccountId, setStrawManAccountId] = useState<string | null>(null);
  const [strawName, setStrawName] = useState<string | null>(null);
  const [error, setError] = useState('');
  const [lastResult, setLastResult] = useState<GatewayPixResult | null>(null);
  const [pixCopied, setPixCopied] = useState(false);
  const [generateBusy, setGenerateBusy] = useState(false);
  const [operationPickerOpen, setOperationPickerOpen] = useState(false);
  const [operatorPickerOpen, setOperatorPickerOpen] = useState(false);
  const [strawPickerOpen, setStrawPickerOpen] = useState(false);

  const amount = useMemo(() => parseAmountInput(amountInput), [amountInput]);
  const canGenerate = Boolean(operationId.trim()) && amount > 0 && !generateBusy;

  async function handleGenerate() {
    setError('');
    setLastResult(null);
    setPixCopied(false);
    if (!operationId.trim()) {
      setError('Selecione uma operação.');
      return;
    }
    if (amount <= 0) {
      setError('Informe um valor maior que zero.');
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

  function clearOperation() {
    setOperationId('');
    setOperationName(null);
  }

  function clearOperator() {
    setOperatorAccountId(null);
    setOperatorName(null);
  }

  function clearStraw() {
    setStrawManAccountId(null);
    setStrawName(null);
  }

  return (
    <div className="ops-page pix-page">
      <PageHeading
        kicker="Financeiro"
        title="Pagamentos PIX"
        subtitle="Gere cobrança PIX vinculada a uma operação. Operador e laranja são opcionais."
        backLink={{ to: '/dashboard/payments', label: 'Registros de pagamentos' }}
      />

      <section className="pix-workspace" aria-labelledby="pix-form-title">
        <header className="pix-workspace__hero">
          <div className="pix-workspace__hero-main">
            <span className="pix-workspace__badge">Cobrança instantânea</span>
            <p className="pix-workspace__amount" aria-live="polite">
              {amount > 0 ? formatMoney(amount) : 'R$ 0,00'}
            </p>
            <p className="pix-workspace__hero-hint muted small">
              {operationName
                ? `Operação ${operationName}`
                : 'Selecione a operação e o valor para gerar o PIX.'}
            </p>
          </div>
          <div className="pix-workspace__hero-mark" aria-hidden="true">
            <span className="pix-workspace__pix-icon">PIX</span>
          </div>
        </header>

        <div className="pix-workspace__divider" aria-hidden="true" />

        {error ? (
          <div className="pix-alert pix-alert--error" role="alert">
            <p className="pix-alert__title">Falha ao gerar cobrança</p>
            <p className="pix-alert__text">{error}</p>
          </div>
        ) : null}

        <div className="pix-workspace__body">
          <section className="admin-op-section pix-section">
            <div className="admin-op-section__head">
              <div className="admin-op-section__head-text">
                <span className="admin-op-section__kicker">Passo 1</span>
                <h2 id="pix-form-title" className="admin-op-section-title">Contexto</h2>
                <p className="admin-op-section-desc muted small">
                  A cobrança será registrada no contexto da operação escolhida.
                </p>
              </div>
            </div>
            <div className="admin-op-section__body">
              <PixEntityField
                label="Operação"
                emptyLabel="Selecionar operação"
                name={operationName}
                id={operationId || null}
                onPick={() => setOperationPickerOpen(true)}
                onClear={clearOperation}
                accent="blue"
              />
            </div>
          </section>

          <section className="admin-op-section pix-section">
            <div className="admin-op-section__head">
              <div className="admin-op-section__head-text">
                <span className="admin-op-section__kicker">Passo 2</span>
                <h2 className="admin-op-section-title">Valor</h2>
                <p className="admin-op-section-desc muted small">
                  Informe o montante da cobrança em reais.
                </p>
              </div>
            </div>
            <div className="admin-op-section__body">
              <label className="pix-amount-field" htmlFor="pixAmount">
                <span className="pix-amount-field__prefix">R$</span>
                <input
                  id="pixAmount"
                  type="text"
                  inputMode="decimal"
                  className="nexus-input pix-amount-field__input"
                  value={amountInput}
                  onChange={(e) => setAmountInput(e.target.value)}
                  placeholder="0,00"
                  autoComplete="off"
                />
              </label>
            </div>
          </section>

          <section className="admin-op-section pix-section">
            <div className="admin-op-section__head">
              <div className="admin-op-section__head-text">
                <span className="admin-op-section__kicker">Passo 3</span>
                <h2 className="admin-op-section-title">Vinculação</h2>
                <p className="admin-op-section-desc muted small">
                  Opcional — refine repasse e credenciais de gateway.
                </p>
              </div>
            </div>
            <div className="admin-op-section__body pix-link-grid">
              <PixEntityField
                label="Operador"
                hint="Filtra a cobrança ao operador selecionado."
                optional
                emptyLabel="Nenhum operador"
                name={operatorName}
                id={operatorAccountId}
                onPick={() => setOperatorPickerOpen(true)}
                onClear={clearOperator}
                accent="green"
              />
              <PixEntityField
                label="Laranja"
                hint="Alinha credenciais de gateway com a conta laranja."
                optional
                emptyLabel="Nenhum laranja"
                name={strawName}
                id={strawManAccountId}
                onPick={() => setStrawPickerOpen(true)}
                onClear={clearStraw}
                accent="warm"
              />
            </div>
          </section>
        </div>

        <footer className="pix-workspace__footer">
          <span className="pix-workspace__endpoint muted small">POST /api/gateways/pix</span>
          <button
            type="button"
            className="btn btn-primary btn-with-icon pix-workspace__submit"
            onClick={() => void handleGenerate()}
            disabled={!canGenerate}
          >
            <Icon name="link" />
            {generateBusy ? 'Gerando…' : 'Gerar PIX'}
          </button>
        </footer>
      </section>

      {lastResult ? (
        <section className="pix-result" aria-live="polite">
          <header className="pix-result__head">
            <div>
              <span className="pix-result__kicker">Cobrança pronta</span>
              <h2 className="pix-result__title">Código PIX gerado</h2>
              <p className="pix-result__meta muted small">
                ID do pagamento: <span className="mono" title={lastResult.id}>{shortId(lastResult.id, 28)}</span>
              </p>
            </div>
            <span className="pix-result__status">Pendente</span>
          </header>
          <div className="pix-result__code-wrap">
            <textarea
              readOnly
              rows={5}
              className="nexus-input pix-result__code"
              value={lastResult.code}
              aria-label="Código PIX copia e cola"
            />
            <button
              type="button"
              className="btn btn-primary btn-with-icon pix-result__copy"
              onClick={() => void copyPixCode()}
            >
              <Icon name="copy" />
              {pixCopied ? 'Copiado' : 'Copiar código'}
            </button>
          </div>
          {pixCopied ? (
            <p className="pix-result__copied feedback success">Código PIX copiado para a área de transferência.</p>
          ) : null}
        </section>
      ) : null}

      <OperationPickerModal
        open={operationPickerOpen}
        onClose={() => setOperationPickerOpen(false)}
        searchOperations={searchAdministratorOperationsPicker}
        title="Selecionar operação"
        subtitle="Todas as operações do sistema — a cobrança PIX será criada no contexto da operação escolhida."
        onSelected={(row) => {
          setOperationId(row.id);
          setOperationName(row.name);
        }}
      />

      <AccountPickerModal
        open={operatorPickerOpen}
        onClose={() => setOperatorPickerOpen(false)}
        searchAccounts={searchAdministratorAccountsPicker}
        title="Conta do operador"
        subtitle="Opcional — filtra a cobrança ao operador."
        onSelected={(row) => {
          setOperatorAccountId(row.id);
          setOperatorName(row.username);
        }}
      />

      <AccountPickerModal
        open={strawPickerOpen}
        onClose={() => setStrawPickerOpen(false)}
        searchAccounts={searchOpAdminStrawMenPicker}
        title="Conta laranja"
        subtitle="Opcional — alinha credenciais de gateway com laranja."
        onSelected={(row) => {
          setStrawManAccountId(row.id);
          setStrawName(row.username);
        }}
      />
    </div>
  );
}
