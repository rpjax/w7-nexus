// Run against the Nexus MongoDB database before deploying OwnerId rename.
// Example: mongosh "mongodb://localhost:27017/nexus" rename-strawman-id-to-owner-id.js

db.bank_accounts.updateMany({}, { $rename: { StrawManId: "OwnerId" } });
db.crypto_wallets.updateMany({}, { $rename: { StrawManId: "OwnerId" } });

for (const field of [
  "OriginBankAccount",
  "OriginCryptoWallet",
  "DestinationBankAccount",
  "DestinationCryptoWallet",
]) {
  db.transfers.updateMany(
    { [`${field}.StrawManId`]: { $exists: true } },
    [
      { $set: { [`${field}.OwnerId`]: `$${field}.StrawManId` } },
      { $unset: [`${field}.StrawManId`] },
    ],
  );
}
