// Tipos e acesso à API de aprovações (rotas /api/aprovacoes e /api/fluxos-aprovacao, JSON em camelCase).
// Contrato: mapeamento/analise/12-aprovacoes-onda3.md
import { ApiError } from '@/services/api'
import { formatarMoeda, registrarSimbolos } from '@/composables/usePropostaTotais'

export type Situacao = 'EmAndamento' | 'Aprovada' | 'Rejeitada' | 'Cancelada'
export type EstadoPasso = 'Aprovado' | 'Rejeitado' | 'Atual' | 'Aguardando' | 'Cancelado'

export interface PassoAprovacao {
  nivel: number
  descricao: string | null
  aprovadores: string[]
  estado: EstadoPasso
  decididoPorId: number | null
  decididoPorNome: string | null
  decididoEm: string | null
  comentario: string | null
  foraDaAlcada: boolean
}

export interface PropostaResumo {
  id: number
  numero: number | null
  titulo: string | null
  clienteId: number | null
  clienteNome: string | null
  valor: number | null
  moedaId: number | null
  moedaSimbolo: string | null
  moedaCodigo: string | null
  descontoPercentual: number
  statusAprovacao: string | null
}

export interface Solicitacao {
  id: number
  fluxoId: number
  fluxoNome: string | null
  entidade: string
  registroId: number
  status: string
  situacao: Situacao
  nivelAtual: number
  totalNiveis: number
  solicitadoPorId: number | null
  solicitadoPorNome: string | null
  solicitadoEm: string
  observacao: string | null
  decididoPorId: number | null
  decididoPorNome: string | null
  decididoEm: string | null
  comentarioDecisao: string | null
  podeDecidir: boolean
  proposta: PropostaResumo | null
  passos: PassoAprovacao[]
}

export interface FiltroFluxo { grupo: number; campoReferencia: string; operador: string; valor: string | null }
export interface AprovadorFluxo { usuarioId: number | null; usuarioNome?: string | null; perfil: string | null }
export interface NivelFluxo { nivel?: number; descricao: string | null; aprovadores: AprovadorFluxo[] }
export interface Fluxo {
  id: number | null
  nome: string
  tipo: 'Alerta' | 'Bloqueio' | 'ControleAlcada'
  mensagem: string | null
  ordem: number
  ativo: boolean | null
  filtros: FiltroFluxo[]
  niveis: NivelFluxo[]
}
export interface CampoFiltro { campo: string; rotulo: string; tipo: string; operadores: string[]; ajuda: string }

// ---- acesso HTTP (com renovação do token) ----
let renovando: Promise<boolean> | null = null

async function renovarToken(): Promise<boolean> {
  const refreshToken = localStorage.getItem('refreshToken')
  if (!refreshToken) return false
  try {
    const res = await fetch('/api/autenticacao/refresh', {
      method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ refreshToken }),
    })
    if (!res.ok) return false
    const d = await res.json()
    localStorage.setItem('token', d.token)
    if (d.refreshToken) localStorage.setItem('refreshToken', d.refreshToken)
    return true
  } catch { return false }
}

function cabecalhos(): Record<string, string> {
  const t = localStorage.getItem('token')
  return { 'Content-Type': 'application/json', ...(t ? { Authorization: `Bearer ${t}` } : {}) }
}

/** Chama `path` (ex.: "/api/aprovacoes/minhas"). Erros viram ApiError com a mensagem do servidor (403/422 legíveis). */
export async function apiAprov<T>(method: 'GET' | 'POST' | 'PUT' | 'DELETE', path: string, body?: unknown): Promise<T> {
  const enviar = () => fetch(path, { method, headers: cabecalhos(), body: body === undefined ? undefined : JSON.stringify(body) })
  let res = await enviar()
  if (res.status === 401) {
    if (!renovando) renovando = renovarToken().finally(() => { renovando = null })
    if (await renovando) res = await enviar()
  }
  if (res.status === 401) {
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    window.location.href = '/login'
    throw new ApiError(401, 'Sessão expirada')
  }
  const txt = await res.text()
  let corpo: unknown = null
  if (txt) { try { corpo = JSON.parse(txt) } catch { corpo = txt } }
  if (!res.ok) {
    const o = corpo && typeof corpo === 'object' ? (corpo as Record<string, unknown>) : null
    const msg = o && typeof o.erro === 'string' ? o.erro : mensagemPadrao(res.status)
    throw new ApiError(res.status, msg, corpo)
  }
  return corpo as T
}

function mensagemPadrao(status: number): string {
  if (status === 403) return 'Você não tem permissão para esta ação.'
  if (status === 404) return 'Registro não encontrado.'
  if (status === 422) return 'A operação não atende às regras de negócio.'
  return `Erro ${status} ao falar com o servidor.`
}

// ---- helpers de exibição ----
export function dinheiro(r: PropostaResumo | null): string {
  if (!r || r.valor === null) return '-'
  if (r.moedaCodigo && r.moedaSimbolo) registrarSimbolos([{ Codigo: r.moedaCodigo, Simbolo: r.moedaSimbolo }])
  return formatarMoeda(r.valor, r.moedaCodigo)
}

export function numeroProposta(r: { id: number; numero: number | null }): string {
  return `PRO-${String(r.numero ?? r.id).padStart(6, '0')}`
}

export function dataHora(iso: string | null | undefined): string {
  if (!iso) return '-'
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

export function situacaoVisual(s: Situacao): { rotulo: string; tom: 'neutro' | 'sucesso' | 'perigo' | 'aviso' } {
  switch (s) {
    case 'Aprovada': return { rotulo: 'Aprovada', tom: 'sucesso' }
    case 'Rejeitada': return { rotulo: 'Rejeitada', tom: 'perigo' }
    case 'Cancelada': return { rotulo: 'Cancelada', tom: 'neutro' }
    default: return { rotulo: 'Em andamento', tom: 'aviso' }
  }
}

/** Iniciais pela primeira LETRA, ignorando prefixo entre colchetes (ex.: "[Demo] Carla Vendedora" → "CV"). */
export function iniciaisDe(nome: string | null | undefined): string {
  const limpo = (nome ?? '').replace(/\[[^\]]*\]/g, ' ').trim()
  const partes = limpo.split(/\s+/).filter(Boolean)
  const a = partes[0]?.match(/\p{L}/u)?.[0] ?? '?'
  const b = partes.length > 1 ? (partes[partes.length - 1]?.match(/\p{L}/u)?.[0] ?? '') : ''
  return (a + b).toUpperCase()
}
