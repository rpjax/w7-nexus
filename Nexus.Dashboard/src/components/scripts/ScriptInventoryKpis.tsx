type ScriptInventoryKpisProps = {
  total: number;
  hostScoped: number;
  nameOnly: number;
  missingProd: number;
};

export function ScriptInventoryKpis({ total, hostScoped, nameOnly, missingProd }: ScriptInventoryKpisProps) {
  return (
    <div className="scripts-kpis" role="list" aria-label="Resumo do inventário">
      <div className="scripts-kpi" role="listitem">
        <span className="scripts-kpi__value">{total}</span>
        <span className="scripts-kpi__label">Scripts</span>
      </div>
      <div className="scripts-kpi" role="listitem">
        <span className="scripts-kpi__value">{hostScoped}</span>
        <span className="scripts-kpi__label">Por host</span>
      </div>
      <div className="scripts-kpi" role="listitem">
        <span className="scripts-kpi__value">{nameOnly}</span>
        <span className="scripts-kpi__label">Só por nome</span>
      </div>
      <div className="scripts-kpi scripts-kpi--warn" role="listitem">
        <span className="scripts-kpi__value">{missingProd}</span>
        <span className="scripts-kpi__label">Sem prod</span>
      </div>
    </div>
  );
}
