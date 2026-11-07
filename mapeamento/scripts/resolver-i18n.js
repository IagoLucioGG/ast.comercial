// Resolve as chaves de traducao nos templates AngularJS usando o dicionario pt-BR.
// Uso: node resolver-i18n.js <pasta-templates> <pt-BR.json> <pasta-saida>
// Troca {{ 'A.B' | translate }}, translate="A.B" e [translate]="..." pelo texto; relata chaves nao resolvidas.
const fs = require('fs');
const path = require('path');

const [, , pastaIn, dictFile, pastaOut] = process.argv;
if (!pastaIn || !dictFile || !pastaOut) { console.error('uso: node resolver-i18n.js <templates> <pt-BR.json> <saida>'); process.exit(1); }
const dict = JSON.parse(fs.readFileSync(dictFile, 'utf8'));

function busca(chave) {
  let o = dict;
  for (const p of chave.split('.')) {
    if (o && typeof o === 'object' && p in o) o = o[p]; else return undefined;
  }
  return typeof o === 'string' ? o : undefined;
}

const naoResolvidas = new Map();
let totalTrocas = 0;
let arquivos = 0;

function anda(dir, rel = '') {
  for (const e of fs.readdirSync(dir, { withFileTypes: true })) {
    const abs = path.join(dir, e.name);
    const r = path.join(rel, e.name);
    if (e.isDirectory()) { anda(abs, r); continue; }
    if (!e.name.endsWith('.html')) continue;
    let t = fs.readFileSync(abs, 'utf8');
    const troca = (chave, original) => {
      const v = busca(chave);
      if (v === undefined) {
        naoResolvidas.set(chave, (naoResolvidas.get(chave) || 0) + 1);
        return original;
      }
      totalTrocas++;
      return v;
    };
    // {{ 'CHAVE' | translate }}  (com ou sem parametros depois de translate)
    t = t.replace(/\{\{\s*'([A-Z0-9_.]+)'\s*\|\s*translate[^}]*\}\}/g, (m, k) => troca(k, m));
    t = t.replace(/\{\{\s*"([A-Z0-9_.]+)"\s*\|\s*translate[^}]*\}\}/g, (m, k) => troca(k, m));
    // <x translate="CHAVE">conteudo</x>: substitui o conteudo quando vazio
    t = t.replace(/(<[a-zA-Z][^>]*\stranslate="([A-Z0-9_.]+)"[^>]*>)(\s*)(<\/)/g, (m, abre, k, esp, fecha) => {
      const v = busca(k);
      if (v === undefined) { naoResolvidas.set(k, (naoResolvidas.get(k) || 0) + 1); return m; }
      totalTrocas++;
      return abre + v + fecha;
    });
    const destino = path.join(pastaOut, r);
    fs.mkdirSync(path.dirname(destino), { recursive: true });
    fs.writeFileSync(destino, t, 'utf8');
    arquivos++;
  }
}
anda(pastaIn);

const lista = [...naoResolvidas.entries()].sort((a, b) => b[1] - a[1]);
fs.writeFileSync(path.join(pastaOut, '_chaves-nao-resolvidas.txt'), lista.map(([k, n]) => `${n}\t${k}`).join('\n'), 'utf8');
console.log(`arquivos: ${arquivos} | trocas: ${totalTrocas} | chaves nao resolvidas (distintas): ${lista.length}`);
console.log(lista.slice(0, 15).map(([k, n]) => `  ${n}x ${k}`).join('\n'));
