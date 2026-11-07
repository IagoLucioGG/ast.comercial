import { odataRequest } from '@/services/api'

// ---------- Tipos (espelham os DTOs de api/relatorios) ----------
export type TipoGrafico = 'Numero' | 'Barras' | 'Linhas' | 'Pizza' | 'Tabela'
export type Agregacao = 'Contagem' | 'Soma' | 'Media'

export interface FiltroRel {
  campo: string
  operador: string
  valor?: string | number | boolean | null
}

export interface DefinicaoGrafico {
  entidade: string
  agregacao: Agregacao
  campoValor?: string | null
  campoAgrupamento?: string | null
  filtros: FiltroRel[]
  periodoCampo?: string | null
  periodoRelativo?: string | null
  limite?: number | null
}

export interface GraficoEntrada extends DefinicaoGrafico {
  titulo: string
  tipo: TipoGrafico
  largura: number
}

export interface Grafico extends GraficoEntrada {
  id: number
  painelId: number
  ordem: number
}

export interface Painel {
  id: number
  nome: string
  descricao?: string | null
  compartilhado: boolean
  ordem: number
  criadoPorUsuarioId?: number | null
  criadoPorNome?: string | null
  podeEditar: boolean
  quantidadeGraficos: number
  graficos?: Grafico[] | null
}

export interface MoedaSerie { id: number; codigo: string; simbolo?: string | null }
export interface PontoDados { chave: string; rotulo: string; valor: number; quantidade: number }
export interface SerieDados { moeda?: MoedaSerie | null; pontos: PontoDados[]; total?: number | null; quantidade: number }
export interface DadosGrafico {
  entidade: string
  agregacao: Agregacao
  campoValor?: string | null
  campoAgrupamento?: string | null
  tipoAgrupamento?: 'mes' | 'id' | 'texto' | null
  monetario: boolean
  truncado: boolean
  periodoInicio?: string | null
  periodoFim?: string | null
  series: SerieDados[]
}

export interface OpcaoRel { valor: string; rotulo: string }
export interface CampoCatalogo {
  chave: string
  rotulo: string
  tipo: 'id' | 'numero' | 'data' | 'texto' | 'bool' | 'enum' | 'mes'
  monetario: boolean
  referencia?: string | null
  operadores: string[]
  opcoes?: OpcaoRel[] | null
}
export interface EntidadeCatalogo {
  chave: string
  rotulo: string
  agregacoes: Agregacao[]
  camposValor: CampoCatalogo[]
  camposAgrupamento: CampoCatalogo[]
  camposFiltro: CampoCatalogo[]
  camposPeriodo: CampoCatalogo[]
  periodoCampoPadrao?: string | null
}
export interface Catalogo {
  entidades: EntidadeCatalogo[]
  periodos: OpcaoRel[]
  tipos: OpcaoRel[]
  operadores: OpcaoRel[]
}

export interface ConsultaDados extends DefinicaoGrafico {
  tipo?: TipoGrafico
  periodoPainel?: string | null
}

// ---------- Chamadas ----------
// `odataRequest` monta "/odata/<path>". O navegador normaliza "../" na URL, então
// "../api/relatorios/x" vira "/api/relatorios/x" e reaproveitamos refresh de token e ApiError.
const base = '../api/relatorios'

export const relatoriosApi = {
  catalogo: () => odataRequest<Catalogo>('GET', `${base}/catalogo`),
  opcoes: (referencia: string, busca?: string) =>
    odataRequest<OpcaoRel[]>('GET', `${base}/opcoes/${encodeURIComponent(referencia)}${busca ? `?busca=${encodeURIComponent(busca)}` : ''}`),
  dados: (consulta: ConsultaDados) => odataRequest<DadosGrafico>('POST', `${base}/dados`, consulta),
  listarPaineis: () => odataRequest<Painel[]>('GET', `${base}/paineis`),
  obterPainel: (id: number) => odataRequest<Painel>('GET', `${base}/paineis/${id}`),
  criarPainel: (corpo: { nome: string; descricao?: string; compartilhado?: boolean }) =>
    odataRequest<Painel>('POST', `${base}/paineis`, corpo),
  atualizarPainel: (id: number, corpo: { nome?: string; descricao?: string; compartilhado?: boolean }) =>
    odataRequest<Painel>('PUT', `${base}/paineis/${id}`, corpo),
  excluirPainel: (id: number) => odataRequest<null>('DELETE', `${base}/paineis/${id}`),
  criarGrafico: (painelId: number, corpo: GraficoEntrada) =>
    odataRequest<Grafico>('POST', `${base}/paineis/${painelId}/graficos`, corpo),
  atualizarGrafico: (id: number, corpo: GraficoEntrada) =>
    odataRequest<Grafico>('PUT', `${base}/graficos/${id}`, corpo),
  excluirGrafico: (id: number) => odataRequest<null>('DELETE', `${base}/graficos/${id}`),
  reordenar: (painelId: number, ids: number[]) =>
    odataRequest<null>('POST', `${base}/paineis/${painelId}/graficos/reordenar`, { ids }),
}

// ---------- Formatação ----------
const nf2 = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
const nf0 = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 0 })
const nfCompacto = new Intl.NumberFormat('pt-BR', { notation: 'compact', maximumFractionDigits: 1 })

/** Dinheiro sempre com o símbolo/código da moeda do registro (nunca "R$" fixo). */
export function formatarValor(v: number, dados: Pick<DadosGrafico, 'agregacao' | 'monetario'>, moeda?: MoedaSerie | null): string {
  if (dados.monetario && moeda) return `${moeda.simbolo || moeda.codigo} ${nf2.format(v)}`
  if (dados.agregacao === 'Contagem') return nf0.format(v)
  return nf2.format(v)
}

export function formatarCompacto(v: number, dados: Pick<DadosGrafico, 'agregacao' | 'monetario'>, moeda?: MoedaSerie | null): string {
  const n = nfCompacto.format(v)
  if (dados.monetario && moeda) return `${moeda.simbolo || moeda.codigo} ${n}`
  return n
}

export function formatarInteiro(v: number): string { return nf0.format(v) }

export function rotuloAgregacao(dados: Pick<DadosGrafico, 'agregacao' | 'campoValor'>, rotuloCampo?: string): string {
  if (dados.agregacao === 'Contagem') return 'Quantidade'
  return `${dados.agregacao === 'Soma' ? 'Soma' : 'Média'} de ${rotuloCampo ?? dados.campoValor ?? 'valor'}`
}

export const PALETA = ['var(--accent)', '#a78bfa', '#34d399', '#fbbf24', '#f87171', '#60a5fa', '#f472b6', '#2dd4bf', '#fb923c', '#94a3b8']
export function cor(i: number): string { return PALETA[i % PALETA.length] as string }

export const PERIODOS_FALLBACK: OpcaoRel[] = [
  { valor: '7d', rotulo: 'Últimos 7 dias' }, { valor: '30d', rotulo: 'Últimos 30 dias' }, { valor: '90d', rotulo: 'Últimos 90 dias' },
  { valor: 'mes', rotulo: 'Este mês' }, { valor: 'mes_anterior', rotulo: 'Mês anterior' }, { valor: 'ano', rotulo: 'Este ano' },
]
