import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const csPath = path.resolve(__dirname, '../../Nexus/Withdrawals/Aggregates/BrazilianBank.cs');
const src = fs.readFileSync(csPath, 'utf8');
const re = /\[BrazilianBankMetadata\("([^"]+)", "([^"]+)", "([^"]+)"\)\]\s*\n\s*(\w+)\s*=\s*(\d+)/g;
const banks = [];
let m;
while ((m = re.exec(src)) !== null) {
  banks.push({ key: m[4], value: Number(m[5]), name: m[1], code: m[2], ispb: m[3] });
}
const out = `export type BrazilianBankOption = { key: string; value: number; name: string; code: string; ispb: string; };

export const BRAZILIAN_BANKS: BrazilianBankOption[] = ${JSON.stringify(banks, null, 2)};
`;
const dest = path.resolve(__dirname, '../src/data/brazilianBanks.ts');
fs.mkdirSync(path.dirname(dest), { recursive: true });
fs.writeFileSync(dest, out);
console.log(`Generated ${banks.length} banks -> ${dest}`);
