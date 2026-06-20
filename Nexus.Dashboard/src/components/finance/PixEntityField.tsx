import { IconButton } from '../IconButton';
import { shortId } from '../../utils/format';

type PixEntityFieldProps = {
  label: string;
  hint?: string;
  optional?: boolean;
  emptyLabel: string;
  name?: string | null;
  id?: string | null;
  onPick: () => void;
  onClear?: () => void;
  accent?: 'blue' | 'green' | 'warm';
};

function personInitial(value: string): string {
  const trimmed = value.trim();
  return trimmed ? trimmed[0]!.toUpperCase() : '?';
}

export function PixEntityField({
  label,
  hint,
  optional = false,
  emptyLabel,
  name,
  id,
  onPick,
  onClear,
  accent = 'blue',
}: PixEntityFieldProps) {
  const selected = Boolean(id?.trim());
  const displayName = name?.trim() || id || emptyLabel;

  return (
    <div className={`pix-entity-field pix-entity-field--${accent}`}>
      <div className="pix-entity-field__label-row">
        <span className="pix-entity-field__label">{label}</span>
        {optional ? <span className="pix-entity-field__optional">opcional</span> : null}
      </div>
      {hint ? <p className="pix-entity-field__hint muted small">{hint}</p> : null}
      <div className="pix-entity-field__row">
        <button
          type="button"
          className={`pix-entity-field__trigger ${selected ? 'is-selected' : 'is-empty'}`}
          onClick={onPick}
        >
          {selected ? (
            <span className="pix-entity-field__avatar" aria-hidden="true">
              {personInitial(displayName)}
            </span>
          ) : null}
          <span className="pix-entity-field__body">
            <span className="pix-entity-field__name">{selected ? displayName : emptyLabel}</span>
            {selected && id ? (
              <span className="pix-entity-field__id mono" title={id}>{shortId(id, 24)}</span>
            ) : null}
          </span>
          <span className="pix-entity-field__chevron" aria-hidden="true">›</span>
        </button>
        {selected && onClear ? (
          <IconButton icon="x" label={`Limpar ${label.toLowerCase()}`} onClick={onClear} />
        ) : null}
      </div>
    </div>
  );
}
