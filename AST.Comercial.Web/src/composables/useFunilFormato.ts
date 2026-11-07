/** Utilitários de formatação e leitura de respostas OData usados pelo funil/kanban. */

export interface MoedaInfo {
  id: number
  codigo: string
  simbolo?: string | null
  padrao?: boolean
}

/** Lê uma propriedade tolerando PascalCase ou camelCase (o servidor serializa em camelCase). */
export function lerCampo<T = unknown>(obj: unknown, nome: string): T | undefined {
  if (!obj || typeof obj !== 'object') return undefined
  const o = obj as Record<string, unknown>
  const camel = nome.charAt(0).toLowerCase() + nome.slice(1)
  const v = nome in o ? o[nome] : o[camel]
  return v as T | undefined
}

/** Algumas rotas devolvem um array puro, outras `{ value: [...] }`. */
export function extrairLista<T = Record<string, unknown>>(resposta: unknown): T[] {
  if (Array.isArray(resposta)) return resposta as T[]
  if (resposta && typeof resposta === 'object') {
    const v = (resposta as Record<string, unknown>).value
    if (Array.isArray(v)) return v as T[]
  }
  return []
}

/** Escapa aspas simples para literais de string em $filter. */
export function literalOData(texto: string): string {
  return texto.replace(/'/g, "''")
}

/** Formata data ISO (ex.: data de fechamento) em dd/mm/aaaa; vazio quando inválida. */
export function formatarData(iso: string | null | undefined): string {
  if (!iso) return ''
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '' : d.toLocaleDateString('pt-BR')
}

/**
 * Formata valor monetário com o símbolo (ou o código, se não houver símbolo) da moeda do registro.
 * Nunca assume "R$": quando a moeda é desconhecida, mostra o código/identificador que tiver.
 */
export function formatarValor(valor: number | null | undefined, moeda: MoedaInfo): string {
  const num = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(valor ?? 0)
  return `${moeda.simbolo || moeda.codigo} ${num}`
}

/** Ordem estável dos totais: BRL primeiro, depois as demais moedas por código. */
export function compararMoedas(a: MoedaInfo, b: MoedaInfo): number {
  if (a.codigo === 'BRL' && b.codigo !== 'BRL') return -1
  if (b.codigo === 'BRL' && a.codigo !== 'BRL') return 1
  return a.codigo.localeCompare(b.codigo) || a.id - b.id
}

/** Iniciais: primeira LETRA da primeira e da última palavra, ignorando prefixos como "[Demo]". */
export function iniciais(nome: string | null | undefined): string {
  const limpo = (nome ?? '').replace(/\[[^\]]*\]/g, ' ').replace(/\([^)]*\)/g, ' ')
  const partes = limpo.split(/\s+/).map((p) => p.replace(/^[^\p{L}]+/u, '')).filter(Boolean)
  if (!partes.length) return '?'
  const primeira = partes[0]?.charAt(0) ?? ''
  const ultima = partes.length > 1 ? (partes[partes.length - 1]?.charAt(0) ?? '') : ''
  return (primeira + ultima).toUpperCase()
}

/** Nome para exibição sem o prefixo de marcação, ex.: "[Demo] Carla" vira "Carla". */
export function nomeSemPrefixo(nome: string | null | undefined): string {
  return (nome ?? '').replace(/^\s*\[[^\]]*\]\s*/, '').trim() || (nome ?? '')
}