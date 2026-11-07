import { odataRequest, montarQuery } from '@/services/api'
import type { OpcaoBusca } from '@/components/propostas/BuscaAssincrona.vue'

/** Campos da entidade Proposta (nomes reais do backend). */
export interface Proposta {
  Id: number
  Titulo?: string | null
  Descricao?: string | null
  Observacoes?: string | null
  Numero?: number | null
  Revisao?: number | null
  Data?: string | null
  DataExpiracao?: string | null
  Valor?: number | null
  Desconto?: number | null
  CustoFrete?: number | null
  ModalFrete?: string | null
  NumeroParcelas?: number | null
  PrazoEntrega?: string | null
  FormaPagamento?: string | null
  MoedaId?: number | null
  StatusAprovacao?: string | number | null
  ClienteId?: number | null
  NegocioId?: number | null
  Chave?: string | null
  Compartilhada?: boolean
  AceitaExternamente?: boolean
  StatusResposta?: string | number | null
  RespondidaEm?: string | null
  RespondidaPorNome?: string | null
  RespondidaPorEmail?: string | null
  MotivoRecusa?: string | null
}

export interface ItemProposta {
  Id: number
  PropostaId: number
  ProdutoId?: number | null
  ProdutoNome?: string | null
  ProdutoCodigo?: string | null
  Quantidade?: number | null
  PrecoUnitario?: number | null
  Desconto?: number | null
  Total?: number | null
  Ordem: number
}

export interface ParcelaProposta {
  Id: number
  PropostaId: number
  Numero: number
  DataVencimento?: string | null
  Valor?: number | null
  Percentual?: number | null
  Descricao?: string | null
}

const NOMES_STATUS = ['Pendente', 'Aprovada', 'Rejeitada', 'EmRevisao']

/** O backend pode serializar o enum como texto ou número; normaliza para o nome. */
export function statusAprovacaoNome(v: string | number | null | undefined): string {
  // Nulo = nenhum fluxo exige aprovação (neutro), não "Pendente".
  if (v === null || v === undefined || v === '') return 'SemAprovacao'
  if (typeof v === 'number') return NOMES_STATUS[v] ?? 'SemAprovacao'
  return /^\d+$/.test(v) ? (NOMES_STATUS[Number(v)] ?? 'SemAprovacao') : v
}

export function statusAprovacaoVisual(v: string | number | null | undefined): { rotulo: string; tom: 'neutro' | 'sucesso' | 'perigo' | 'aviso' | 'info' } {
  switch (statusAprovacaoNome(v)) {
    case 'Aprovada': return { rotulo: 'Aprovada', tom: 'sucesso' }
    case 'Rejeitada': return { rotulo: 'Rejeitada', tom: 'perigo' }
    case 'EmRevisao': return { rotulo: 'Em revisão', tom: 'info' }
    case 'Pendente': return { rotulo: 'Pendente', tom: 'aviso' }
    default: return { rotulo: 'Sem aprovação', tom: 'neutro' }
  }
}

/** Os GET das rotas Entidade@Sub saem em camelCase (fora do OData); o front usa PascalCase. */
function paraPascal<T>(linha: unknown): T {
  if (!linha || typeof linha !== 'object' || Array.isArray(linha)) return linha as T
  const saida: Record<string, unknown> = {}
  for (const [k, v] of Object.entries(linha as Record<string, unknown>)) {
    saida[k.charAt(0).toUpperCase() + k.slice(1)] = v
  }
  return saida as T
}

/** Lista de uma rota; as rotas Entidade@Sub devolvem array puro (camelCase) e as demais { value: [] }. */
export async function listar<T>(path: string): Promise<T[]> {
  const r = await odataRequest<{ value?: T[] } | T[] | null>('GET', path)
  if (Array.isArray(r)) return r.map((x) => paraPascal<T>(x))
  return r?.value ?? []
}

/**
 * Lista TODAS as linhas de uma rota paginando de 100 em 100 (o servidor limita $top a 100).
 * `path` não pode ter $top/$skip e precisa de $orderby estável.
 */
export async function listarTodos<T>(path: string, maximo = 5000): Promise<T[]> {
  const sep = path.includes('?') ? '&' : '?'
  const todas: T[] = []
  for (let skip = 0; skip < maximo; skip += 100) {
    const pagina = await listar<T>(`${path}${sep}$top=100&$skip=${skip}`)
    todas.push(...pagina)
    if (pagina.length < 100) break
  }
  return todas
}

export interface Moeda { Id: number; Codigo: string; Simbolo?: string | null; Padrao?: boolean }

/** Número exibido da proposta: o Numero do servidor ou, se faltar, PRO-000042 (a partir do Id). */
export function numeroDaProposta(p: { Id: number; Numero?: number | null }): string {
  return p.Numero ? `PRO-${String(p.Numero).padStart(6, '0')}` : `PRO-${String(p.Id).padStart(6, '0')}`
}

function aspas(termo: string): string {
  return encodeURIComponent(termo.replace(/'/g, "''"))
}

export async function buscarClientes(termo: string): Promise<OpcaoBusca[]> {
  const filtro = termo ? `&$filter=contains(tolower(Nome),'${aspas(termo.toLowerCase())}')` : ''
  const r = await listar<{ Id: number; Nome: string }>(`Clientes${montarQuery({ $top: 15, $select: 'Id,Nome', $orderby: 'Nome' })}${filtro}`)
  return r.map((c) => ({ id: c.Id, texto: c.Nome }))
}

export async function buscarProdutos(termo: string): Promise<OpcaoBusca[]> {
  const filtro = termo ? `&$filter=contains(tolower(Nome),'${aspas(termo.toLowerCase())}')` : ''
  const r = await listar<{ Id: number; Nome: string; Codigo?: string | null; Preco: number }>(
    `Produtos${montarQuery({ $top: 15, $select: 'Id,Nome,Codigo,Preco', $orderby: 'Nome' })}${filtro}`,
  )
  return r.map((p) => ({ id: p.Id, texto: p.Nome, extra: p }))
}

export async function buscarNegocios(termo: string): Promise<OpcaoBusca[]> {
  const filtro = termo ? `&$filter=contains(tolower(Titulo),'${aspas(termo.toLowerCase())}')` : ''
  const r = await listar<{ Id: number; Titulo: string; ClienteId: number }>(
    `Negocios${montarQuery({ $top: 15, $select: 'Id,Titulo,ClienteId', $orderby: 'Titulo' })}${filtro}`,
  )
  return r.map((n) => ({ id: n.Id, texto: n.Titulo, extra: n }))
}

/** Converte uma data ISO (ou null) para o valor de <input type="date">. */
export function paraInputData(iso: string | null | undefined): string {
  return iso ? iso.slice(0, 10) : ''
}

/** Converte o valor de <input type="date"> para ISO UTC (ou null). */
export function deInputData(v: string): string | null {
  return v ? `${v}T12:00:00Z` : null
}
