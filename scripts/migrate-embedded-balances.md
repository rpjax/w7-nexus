# Migração de saldos embutidos para coleções dedicadas

Script one-off para extrair `balances[]` de `bank_accounts` / `crypto_wallets`
para `bank_balances` / `crypto_balances` após deploy da refatoração de transferências.

## Pré-requisitos

- Backup do banco antes de executar
- Aplicação em versão que **não** escreve mais em arrays embutidos
- Coleções `bank_balances` e `crypto_balances` já criadas (índices opcionais em `bankAccountId` / `cryptoWalletId`)

## MongoDB shell

```javascript
// bank_accounts → bank_balances
db.bank_accounts.find({ balances: { $exists: true, $ne: [] } }).forEach(function (account) {
  (account.balances || []).forEach(function (balance) {
    db.bank_balances.insertOne({
      _id: balance._id || balance.id,
      bankAccountId: account._id,
      amountBrl: balance.amountBrl,
      transferId: balance.transferId,
      createdAt: balance.createdAt || new Date(),
      splits: balance.splits || [],
      origin: balance.origin
    });
  });
  db.bank_accounts.updateOne(
    { _id: account._id },
    { $unset: { balances: "" } }
  );
});

// crypto_wallets → crypto_balances
db.crypto_wallets.find({ balances: { $exists: true, $ne: [] } }).forEach(function (wallet) {
  (wallet.balances || []).forEach(function (balance) {
    db.crypto_balances.insertOne({
      _id: balance._id || balance.id,
      cryptoWalletId: wallet._id,
      chain: balance.chain,
      asset: balance.asset,
      amount: balance.amount,
      transferId: balance.transferId,
      createdAt: balance.createdAt || new Date(),
      splits: balance.splits || [],
      origin: balance.origin
    });
  });
  db.crypto_wallets.updateOne(
    { _id: wallet._id },
    { $unset: { balances: "" } }
  );
});
```

## Verificação

```javascript
db.bank_accounts.countDocuments({ balances: { $exists: true } });
db.crypto_wallets.countDocuments({ balances: { $exists: true } });
// Esperado: 0 após migração

db.bank_balances.countDocuments();
db.crypto_balances.countDocuments();
```

## Rollback

Restaurar backup. Não há rollback automático in-place — os arrays embutidos são removidos após inserção nas novas coleções.
