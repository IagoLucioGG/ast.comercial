export type Metrica = 'ValorGanho' | 'QuantidadeGanhos' | 'NegociosCriados' | 'AtividadesConcluidas' | 'PropostasAprovadas'

export const METRICAS: { valor: Metrica; rotulo: string; monetaria: boolean; unidade: string }[] = [
  { valor: 'ValorGanho', rotulo: 'Valor ganho', monetaria: true, unidade: '' },
  { valor: 'QuantidadeGanhos', rotulo: 'Negócios ganhos', monetaria: false, unidade: 'negócios' },
  { valor: 'NegociosCriados', rotulo: 'Negócios criados', monetaria: false, unidade: 'negócios' },
  { valor: 'AtividadesConcluidas', rotulo: 'Atividades concluídas', monetaria: false, unidade: 'atividades' },
  { valor: 'PropostasAprovadas', rotulo: 'Propostas aprovadas', monetaria: false, unidade: 'propostas' },
]

export interface PontoMeta { data: string; acumulado: number | null; alvoLinear: number }
export interface RankingMeta { usuarioId: number; nome: string; realizado: number }

export interface ProgressoMeta {
  id: number
  nome: string
  metrica: Metrica
  escopo: 'usuario' | 'equipe' | 'empresa'
  alvo: number
  realizado: number
  percentual: number
  projecao: number | null
  percentualProjetado: number | null
  noRitmo: boolean
  moedaId: number | null
  moedaSimbolo: string | null
  moedaCodigo: string | null
  usuarioId: number | null
  usuarioNome: string | null
  equipeId: number | null
  equipeNome: string | null
  funilId: number | null
  funilNome: string | null
  inicio: string
  fim: string
  diasTotais: number
  diasDecorridos: number
  serie: PontoMeta[]
  ranking: RankingMeta[]
}

export function infoMetrica(m: Metrica) {
  return METRICAS.find((x) => x.valor === m) ?? METRICAS[0]!
}

/** Formata um número da meta: com símbolo da moeda quando monetária, inteiro/decimal simples nas demais. */
export function formatarValor(valor: number, p: Pick<ProgressoMeta, 'metrica' | 'moedaSimbolo' | 'moedaCodigo'>): string {
  if (infoMetrica(p.metrica).monetaria) {
    const n = valor.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
    return `${p.moedaSimbolo ?? p.moedaCodigo ?? ''} ${n}`.trim()
  }
  return valor.toLocaleString('pt-BR', { maximumFractionDigits: 1 })
}

/** Iniciais pela primeira letra do nome, ignorando prefixo entre colchetes. */
export function iniciais(nome: string): string {
  const limpo = nome.replace(/^\s*\[[^\]]*\]\s*/, '').trim()
  const partes = limpo.split(/\s+/).filter(Boolean)
  if (!partes.length) return '?'
  const a = partes[0]![0] ?? ''
  const b = partes.length > 1 ? (partes[partes.length - 1]![0] ?? '') : ''
  return (a + b).toUpperCase()
}
