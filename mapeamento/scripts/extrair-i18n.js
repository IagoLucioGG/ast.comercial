// Extrai os dicionarios de traducao embutidos no angular-app.min.js (objetos literais) para JSON.
// Uso: node extrair-i18n.js <angular-app.min.js> <pasta-saida>
// Seguranca: NADA do bundle e executado. Um parser proprio le o literal (objeto/string/array/booleano/numero).
// Template literals com interpolacao ${...} sao mantidos como texto cru.
const fs = require('fs');
const path = require('path');

const [, , arquivo, saida] = process.argv;
if (!arquivo || !saida) { console.error('uso: node extrair-i18n.js <bundle.js> <saida>'); process.exit(1); }
const src = fs.readFileSync(arquivo, 'utf8');

function pulaString(s, i) {
  const q = s[i];
  i++;
  while (i < s.length) {
    if (s[i] === '\\') { i += 2; continue; }
    if (s[i] === q) return i + 1;
    i++;
  }
  return i;
}

function pilhaAte(s, pos) {
  const pilha = [];
  for (let i = 0; i < pos; ) {
    const c = s[i];
    if (c === '"' || c === "'" || c === '`') { i = pulaString(s, i); continue; }
    if (c === '{') pilha.push(i);
    else if (c === '}') pilha.pop();
    i++;
  }
  return pilha;
}

function achaFechamento(s, ini) {
  let prof = 0;
  for (let i = ini; i < s.length; ) {
    const c = s[i];
    if (c === '"' || c === "'" || c === '`') { i = pulaString(s, i); continue; }
    if (c === '{') prof++;
    else if (c === '}') { prof--; if (prof === 0) return i; }
    i++;
  }
  return -1;
}

// ---- parser de literal JS (sem executar) ----
function parse(txt) {
  let i = 0;
  const ws = () => { while (i < txt.length && /\s/.test(txt[i])) i++; };

  function decodeEscapes(raw) {
    return raw.replace(/\\(u\{[0-9a-fA-F]+\}|u[0-9a-fA-F]{4}|x[0-9a-fA-F]{2}|[\s\S])/g, (m, g) => {
      if (g[0] === 'u' && g.length > 1) {
        const hex = g[1] === '{' ? g.slice(2, -1) : g.slice(1);
        return String.fromCodePoint(parseInt(hex, 16));
      }
      if (g[0] === 'x' && g.length === 3) return String.fromCharCode(parseInt(g.slice(1), 16));
      switch (g) {
        case 'n': return '\n'; case 'r': return '\r'; case 't': return '\t';
        case 'b': return '\b'; case 'f': return '\f'; case 'v': return '\v';
        case '0': return '\0'; case '\n': return '';
        default: return g;
      }
    });
  }

  function parseString() {
    const q = txt[i];
    const ini = i + 1;
    const fim = pulaString(txt, i) - 1;
    i = fim + 1;
    return decodeEscapes(txt.slice(ini, fim));
  }

  function parseKey() {
    ws();
    if (txt[i] === '"' || txt[i] === "'") return parseString();
    const m = /^[A-Za-z_$][\w$]*|^\d+/.exec(txt.slice(i, i + 200));
    if (!m) throw new Error(`chave invalida em ${i}: ${JSON.stringify(txt.slice(i, i + 40))}`);
    i += m[0].length;
    return m[0];
  }

  function parseValue() {
    ws();
    const c = txt[i];
    if (c === '{') {
      i++;
      const obj = {};
      ws();
      while (txt[i] !== '}') {
        const k = parseKey();
        ws();
        if (txt[i] !== ':') throw new Error(`esperava ':' em ${i}`);
        i++;
        obj[k] = parseValue();
        ws();
        if (txt[i] === ',') { i++; ws(); }
      }
      i++;
      return obj;
    }
    if (c === '[') {
      i++;
      const arr = [];
      ws();
      while (txt[i] !== ']') {
        arr.push(parseValue());
        ws();
        if (txt[i] === ',') { i++; ws(); }
      }
      i++;
      return arr;
    }
    if (c === '"' || c === "'" || c === '`') return parseString();
    const m = /^(!0|!1|true|false|null|void 0|undefined|-?\d+(?:\.\d+)?)/.exec(txt.slice(i, i + 20));
    if (m) {
      i += m[0].length;
      if (m[0] === '!0' || m[0] === 'true') return true;
      if (m[0] === '!1' || m[0] === 'false') return false;
      if (m[0] === 'null' || m[0] === 'void 0' || m[0] === 'undefined') return null;
      return Number(m[0]);
    }
    throw new Error(`valor nao suportado em ${i}: ${JSON.stringify(txt.slice(i, i + 60))}`);
  }

  const v = parseValue();
  ws();
  if (i !== txt.length) throw new Error(`sobrou conteudo em ${i}`);
  return v;
}

function contaFolhas(o) {
  if (o && typeof o === 'object') return Object.values(o).reduce((a, v) => a + contaFolhas(v), 0);
  return 1;
}

const ancora = 'TRIGGER_LABEL:';
const vistos = new Set();
const blocos = [];
let from = 0;
for (;;) {
  const k = src.indexOf(ancora, from);
  if (k < 0) break;
  from = k + 1;
  const pilha = pilhaAte(src, k);
  if (pilha.length < 2) continue;
  const ab = pilha[pilha.length - 2]; // raiz do dicionario (pai de AUTOMATIONS)
  if (vistos.has(ab)) continue;
  vistos.add(ab);
  const fe = achaFechamento(src, ab);
  if (fe < 0) continue;
  blocos.push({ ab, fe, txt: src.slice(ab, fe + 1) });
}

fs.mkdirSync(saida, { recursive: true });
let n = 0;
const resumo = [];
for (const b of blocos) {
  let obj;
  try { obj = parse(b.txt); } catch (e) { resumo.push(`erro @${b.ab}: ${e.message}`); continue; }
  const rotulo = (obj.AUTOMATIONS && obj.AUTOMATIONS.TRIGGER_LABEL) || 'desconhecido';
  const nome = `i18n_${String(rotulo).replace(/[^\w]/g, '_')}_${n}.json`;
  fs.writeFileSync(path.join(saida, nome), JSON.stringify(obj, null, 1), 'utf8');
  resumo.push(`${nome}: modulos=${Object.keys(obj).length} textos=${contaFolhas(obj)} bytes_origem=${b.txt.length}`);
  n++;
}
console.log(resumo.join('\n'));
console.log(`blocos gravados: ${n}`);
