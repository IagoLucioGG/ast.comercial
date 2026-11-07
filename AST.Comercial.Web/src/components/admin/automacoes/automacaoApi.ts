// Tipos e acesso à API de automações (contrato: mapeamento/analise/07-contrato-automacoes.md)
import { ApiError } from '@/services/api'

export interface CampoCatalogo { nome: string; tipo: string; opcoes?: string[] | null }
export interface EntidadeCatalogo { valor: string; nome: string; gatilhos: string[]; campos: CampoCatalogo[] }
export interface GatilhoCatalogo { valor: string; nome: string; implementado: boolean; entidades: string[]; observacao?: string | null }
export interface ParamAcao { chave: string; tipo: string; obrigatorio: boolean; descricao?: string | null }
export interface AcaoCatalogo { valor: string; nome: string; implementado: boolean; entidades: string[] | null; configuracao: ParamAcao[] }
export interface OperadorCatalogo { valor: string; nome: string; implementado: boolean; precisaValor: boolean }
export interface Catalogo {
  entidades: EntidadeCatalogo[]; gatilhos: GatilhoCatalogo[]; acoes: AcaoCatalogo[]; operadores: OperadorCatalogo[]
}

export interface FiltroDto { id?: number; grupo: number; ordem?: number; campoReferencia: string; operador: string; valor: string | null }
export interface AcaoDto { id?: number; ordem?: number; tipo: string; configuracao: Record<string, unknown> }
export interface AutomacaoDto {
  id: number; nome: string; descricao: string | null; entidadeAlvo: string; gatilho: string
  ordem: number; impedirCascata: boolean; ativo: boolean; horarioExecucao: string | null
  proximaExecucao: string | null; ultimaExecucao: string | null; criadoEm: string; atualizadoEm: string | null
  filtros: FiltroDto[]; acoes: AcaoDto[]
  criadoPor?: string | null
}
export interface AutomacaoEntrada {
  nome: string; descricao: string | null; entidadeAlvo: string; gatilho: string; ordem: number
  impedirCascata: boolean; ativo: boolean; horarioExecucao: string | null
  filtros: FiltroDto[]; acoes: AcaoDto[]
}
export interface ExecucaoDto {
  id: number; automacaoId: number; registroId: number | null; entidadeAlvo: string; status: string
  iniciadaEm: string; finalizadaEm: string | null; duracaoMs: number | null
  detalhes: string | null; erro: string | null; disparadoPor: string | null
}
export interface Pagina<T> { total: number; itens: T[] }

/** Erro de validação do servidor com a lista completa de mensagens. */
export function mensagensDoErro(e: unknown): string[] {
  if (e instanceof ApiError) {
    const corpo = e.body as { erros?: unknown } | null
    if (corpo && Array.isArray(corpo.erros) && corpo.erros.length) return corpo.erros.map(String)
    return [e.message]
  }
  return [e instanceof Error ? e.message : 'Erro inesperado.']
}

// Chamada direta a /api/automacoes (controller MVC, fora de /odata), com Authorization e renovação do token.
let refreshEmAndamento: Promise<boolean> | null = null
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
  const h: Record<string, string> = { 'Content-Type': 'application/json' }
  const t = localStorage.getItem('token')
  if (t) h['Authorization'] = `Bearer ${t}`
  return h
}

async function api<T>(metodo: 'GET' | 'POST' | 'PUT' | 'DELETE', caminho: string, corpo?: unknown): Promise<T> {
  const url = `/api/automacoes${caminho}`
  const init = (): RequestInit => ({ method: metodo, headers: cabecalhos(), body: corpo === undefined ? undefined : JSON.stringify(corpo) })
  let res = await fetch(url, init())
  if (res.status === 401) {
    if (!refreshEmAndamento) refreshEmAndamento = renovarToken().finally(() => { refreshEmAndamento = null })
    if (await refreshEmAndamento) res = await fetch(url, init())
  }
  if (res.status === 401) {
    localStorage.removeItem('token'); localStorage.removeItem('refreshToken')
    window.location.href = '/login'
    throw new ApiError(401, 'Sessão expirada')
  }
  const txt = await res.text()
  let dados: unknown = null
  if (txt) { try { dados = JSON.parse(txt) } catch { dados = txt } }
  if (!res.ok) {
    const msg = dados && typeof dados === 'object' && typeof (dados as { erro?: unknown }).erro === 'string'
      ? (dados as { erro: string }).erro : `${res.status} em ${url}`
    throw new ApiError(res.status, msg, dados)
  }
  return dados as T
}

function qs(o: Record<string, string | number | boolean | null | undefined>): string {
  const p = Object.entries(o).filter(([, v]) => v !== undefined && v !== null && v !== '')
    .map(([k, v]) => `${k}=${encodeURIComponent(String(v))}`)
  return p.length ? '?' + p.join('&') : ''
}

export const automacaoApi = {
  catalogo: () => api<Catalogo>('GET', '/catalogo'),
  listar: (f: { entidadeAlvo?: string; busca?: string; skip?: number; top?: number }) =>
    api<Pagina<AutomacaoDto>>('GET', qs({ ...f, top: f.top ?? 100 })),
  obter: (id: number) => api<AutomacaoDto>('GET', `/${id}`),
  criar: (e: AutomacaoEntrada) => api<AutomacaoDto>('POST', '', e),
  atualizar: (id: number, e: AutomacaoEntrada) => api<AutomacaoDto>('PUT', `/${id}`, e),
  remover: (id: number) => api<null>('DELETE', `/${id}`),
  ativar: (id: number) => api<AutomacaoDto>('POST', `/${id}/ativar`, {}),
  desativar: (id: number) => api<AutomacaoDto>('POST', `/${id}/desativar`, {}),
  execucoes: (id: number, f: { status?: string; skip?: number; top?: number }) =>
    api<Pagina<ExecucaoDto>>('GET', `/${id}/execucoes${qs(f)}`),
}

export function formatarData(iso: string | null | undefined): string {
  if (!iso) return '—'
  const d = new Date(iso.endsWith('Z') || /[+-]\d\d:\d\d$/.test(iso) ? iso : iso + 'Z')
  return isNaN(d.getTime()) ? '—' : d.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}
