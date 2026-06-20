import { useEffect, useId, useMemo, useState } from 'react';
import { BRAZILIAN_BANKS, type BrazilianBankOption } from '../../data/brazilianBanks';

type BrazilianBankSelectProps = {
  value: number | null;
  onChange: (value: number | null) => void;
  disabled?: boolean;
};

const PAGE_SIZE = 12;

export function BrazilianBankSelect({ value, onChange, disabled = false }: BrazilianBankSelectProps) {
  const listId = useId();
  const [keyword, setKeyword] = useState('');
  const [open, setOpen] = useState(false);

  const selected = useMemo(
    () => BRAZILIAN_BANKS.find((b: BrazilianBankOption) => b.value === value) ?? null,
    [value],
  );

  const filtered = useMemo(() => {
    const term = keyword.trim().toLowerCase();
    if (!term) return BRAZILIAN_BANKS.slice(0, PAGE_SIZE);
    return BRAZILIAN_BANKS.filter(
      (b: BrazilianBankOption) =>
        b.name.toLowerCase().includes(term)
        || b.code.includes(term)
        || b.key.toLowerCase().includes(term),
    ).slice(0, PAGE_SIZE);
  }, [keyword]);

  useEffect(() => {
    if (selected && !keyword) {
      setKeyword(`${selected.code} — ${selected.name}`);
    }
  }, [selected, keyword]);

  return (
    <div className="bank-select">
      <input
        id={listId}
        className="nexus-input"
        value={keyword}
        disabled={disabled}
        placeholder="Buscar banco por nome ou código…"
        onFocus={() => setOpen(true)}
        onChange={(e) => {
          setKeyword(e.target.value);
          setOpen(true);
          if (!e.target.value.trim()) onChange(null);
        }}
        onBlur={() => window.setTimeout(() => setOpen(false), 150)}
        autoComplete="off"
      />
      {open && filtered.length > 0 ? (
        <ul className="bank-select__list" role="listbox" aria-labelledby={listId}>
          {filtered.map((bank: BrazilianBankOption) => (
            <li key={bank.key}>
              <button
                type="button"
                className="bank-select__option"
                onMouseDown={(e) => e.preventDefault()}
                onClick={() => {
                  onChange(bank.value);
                  setKeyword(`${bank.code} — ${bank.name}`);
                  setOpen(false);
                }}
              >
                <span className="bank-select__code">{bank.code}</span>
                <span>{bank.name}</span>
              </button>
            </li>
          ))}
        </ul>
      ) : null}
    </div>
  );
}
