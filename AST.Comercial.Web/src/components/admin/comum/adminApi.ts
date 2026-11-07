import { ApiError, montarQuery, odataQuery, odataRequest } from '@/services/api'

/** Texto amigável para toast a partir de qualquer erro. */
export function msgErro(e: unknown, padrao = 'Não foi possível concluir a operação.'): string {
  if (e instanceof ApiError) return e.message || padrao
  if (e instanceof Error && e.message) return e.message
  return padrao
}

export type TipoCampo = 'texto' | 'textarea' | 'numero' | 'cor' | 'select' | 'checkbox'

export interface OpcaoCampo { valor: string | number; rotulo: string }

export interface CampoCadastro {
  chave: string
  rotulo: string
  tipo: TipoCampo
  obrigatorio?: boolean
  maxLength?: number
  ajuda?: string
  opcoes?: OpcaoCampo[]
  /** Aparece como coluna na lista. */
  lista?: boolean
  /** Formata o valor na lista. */
  formatar?: (valor: unknown, item: Record<string, unknown>) => string
  valorPadrao?: unknown
  /** Ocupa a linha inteira no formulário. */
  largo?: boolean
}

export interface ApiCadastro {
  carregar: () => Promise<Record<string, unknown>[]>
  criar?: (dados: Record<string, unknown>) => Promise<unknown>
  atualizar?: (id: number, dados: Record<string, unknown>) => Promise<unknown>
  remover?: (id: number) => Promise<unknown>
}

/** Iniciais para avatar (duas letras). */
export function iniciais(nome: string): string {
  const partes = nome.trim().split(/\s+/).filter(Boolean)
  const a = partes[0]?.[0] ?? '?'
  const b = partes.length > 1 ? (partes[partes.length - 1]?.[0] ?? '') : ''
  return (a + b).toUpperCase()
}

const minuscula = (s: string) => s.charAt(0).toLowerCase() + s.slice(1)
const maiuscula = (s: string) => s.charAt(0).toUpperCase() + s.slice(1)

function mapearChaves(o: Record<string, unknown>, f: (s: string) => string): Record<string, unknown> {
  const r: Record<string, unknown> = {}
  for (const [k, v] of Object.entries(o)) r[f(k)] = v
  return r
}

/** Adaptador CRUD para entidades OData "normais" (PascalCase na API, camelCase na tela genérica). */
export function apiOdata(entidade: string, orderby = 'Nome asc'): ApiCadastro {
  return {
    carregar: async () => {
      const r = await odataQuery<Record<string, unknown>>(`${entidade}${montarQuery({ $orderby: orderby, $top: 100 })}`)
      return r.value.map((i) => mapearChaves(i, minuscula))
    },
    criar: (d) => odataRequest('POST', entidade, mapearChaves(d, maiuscula)),
    atualizar: (id, d) => odataRequest('PATCH', `${entidade}(${id})`, mapearChaves(d, maiuscula)),
    remover: (id) => odataRequest('DELETE', `${entidade}(${id})`),
  }
}

/** Adaptador para odata/AdminCadastros/{tipo} (listas de apoio; JSON camelCase). */
export function apiCadastroAux(tipo: string): ApiCadastro {
  return {
    carregar: () => odataRequest<Record<string, unknown>[]>('GET', `AdminCadastros/${tipo}`),
    criar: (d) => odataRequest('POST', `AdminCadastros/${tipo}`, d),
    // No PATCH, null significa "não mexer"; 0 limpa a família do grupo.
    atualizar: (id, d) => odataRequest('PATCH', `AdminCadastros/${tipo}/${id}`, 'familiaProdutoId' in d && d.familiaProdutoId == null ? { ...d, familiaProdutoId: 0 } : d),
    remover: (id) => odataRequest('DELETE', `AdminCadastros/${tipo}/${id}`),
  }
}

export const PERFIS = ['Administrador', 'Gerente', 'Vendedor', 'Visualizador'] as const

/** O @Todos devolve enum numérico (JSON simples); as rotas OData devolvem texto. Normaliza para o nome. */
export function nomePerfil(v: unknown): string {
  if (typeof v === 'number') return PERFIS[v] ?? String(v)
  return String(v ?? '')
}

export function ehIntegracao(tipo: unknown): boolean {
  return tipo === 1 || tipo === 'Integracao'
}

export function dataBr(v: unknown): string {
  if (!v) return '-'
  const d = new Date(String(v))
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleDateString('pt-BR')
}

export function dataHoraBr(v: unknown): string {
  if (!v) return '-'
  const d = new Date(String(v))
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}
