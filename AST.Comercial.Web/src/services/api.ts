const BASE = ''

function headers(): Record<string, string> {
  const token = localStorage.getItem('token')
  const h: Record<string, string> = { 'Content-Type': 'application/json' }
  if (token) h['Authorization'] = `Bearer ${token}`
  return h
}

let refreshPromise: Promise<boolean> | null = null

async function tentarRefresh(): Promise<boolean> {
  const refreshToken = localStorage.getItem('refreshToken')
  if (!refreshToken) return false
  try {
    const res = await fetch(`${BASE}/api/autenticacao/refresh`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken })
    })
    if (!res.ok) return false
    const data = await res.json()
    localStorage.setItem('token', data.token)
    if (data.refreshToken) localStorage.setItem('refreshToken', data.refreshToken)
    return true
  } catch { return false }
}

async function fetchComRefresh(url: string, options: RequestInit): Promise<Response> {
  let res = await fetch(url, options)
  if (res.status === 401) {
    if (!refreshPromise) refreshPromise = tentarRefresh().finally(() => { refreshPromise = null })
    const sucesso = await refreshPromise
    if (sucesso) {
      const novoHeaders = { ...options.headers as Record<string, string>, 'Authorization': `Bearer ${localStorage.getItem('token')}` }
      res = await fetch(url, { ...options, headers: novoHeaders })
    }
  }
  return res
}

function tratarErro(res: Response, url: string): never {
  if (res.status === 401) {
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    window.location.href = '/login'
  }
  throw new Error(`${res.status} em ${url}`)
}

export interface ODataResponse<T> {
  value: T[]
  '@odata.count'?: number
}

export interface ODataParams {
  top?: number
  skip?: number
  filter?: string
  orderby?: string
  select?: string
  expand?: string
  count?: boolean
}

function buildQuery(params?: ODataParams): string {
  if (!params) return ''
  const parts: string[] = []
  if (params.top) parts.push(`$top=${params.top}`)
  if (params.skip) parts.push(`$skip=${params.skip}`)
  if (params.filter) parts.push(`$filter=${encodeURIComponent(params.filter)}`)
  if (params.orderby) parts.push(`$orderby=${params.orderby}`)
  if (params.select) parts.push(`$select=${params.select}`)
  if (params.expand) parts.push(`$expand=${params.expand}`)
  if (params.count) parts.push('$count=true')
  return parts.length ? '?' + parts.join('&') : ''
}

export async function odataGet<T>(endpoint: string, params?: ODataParams): Promise<ODataResponse<T>> {
  const url = `${BASE}/odata/${endpoint}${buildQuery(params)}`
  const res = await fetchComRefresh(url, { headers: headers() })
  if (!res.ok) tratarErro(res, url)
  return res.json()
}

export async function odataGetOne<T>(endpoint: string, id: number): Promise<T> {
  const url = `${BASE}/odata/${endpoint}(${id})`
  const res = await fetchComRefresh(url, { headers: headers() })
  if (!res.ok) tratarErro(res, url)
  return res.json()
}

export async function odataPost<T>(endpoint: string, data: Partial<T>): Promise<T> {
  const url = `${BASE}/odata/${endpoint}`
  const res = await fetchComRefresh(url, { method: 'POST', headers: headers(), body: JSON.stringify(data) })
  if (!res.ok) tratarErro(res, url)
  return res.json()
}

export async function odataPatch<T>(endpoint: string, id: number, data: Partial<T>): Promise<T> {
  const url = `${BASE}/odata/${endpoint}(${id})`
  const res = await fetchComRefresh(url, { method: 'PATCH', headers: headers(), body: JSON.stringify(data) })
  if (!res.ok) tratarErro(res, url)
  return res.json()
}

export async function odataDelete(endpoint: string, id: number): Promise<void> {
  const url = `${BASE}/odata/${endpoint}(${id})`
  const res = await fetchComRefresh(url, { method: 'DELETE', headers: headers() })
  if (!res.ok) tratarErro(res, url)
}

// ---------------------------------------------------------------------------
// API v2 (usada pelas telas novas): erro tipado, corpo vazio tolerado, consulta
// com query bruta ($apply, $expand aninhado) e ações OData.
// As funções acima continuam valendo para o código existente.
// ---------------------------------------------------------------------------

export class ApiError extends Error {
  status: number
  body: unknown
  constructor(status: number, message: string, body?: unknown) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.body = body
  }
}

async function lerCorpo(res: Response): Promise<unknown> {
  const txt = await res.text()
  if (!txt) return null
  try { return JSON.parse(txt) } catch { return txt }
}

function mensagemDoCorpo(corpo: unknown, fallback: string): string {
  if (corpo && typeof corpo === 'object') {
    const o = corpo as Record<string, unknown>
    if (typeof o.erro === 'string') return o.erro
    if (typeof o.message === 'string') return o.message
    const err = o.error as Record<string, unknown> | undefined
    if (err && typeof err.message === 'string') return err.message
  }
  return fallback
}

/**
 * Requisição OData com erro tipado (ApiError). `path` é relativo a /odata/,
 * ex.: "Negocios(5)" ou "Negocios?$top=15&$filter=...". Já deve vir com a query codificada.
 */
export async function odataRequest<T>(
  method: 'GET' | 'POST' | 'PATCH' | 'PUT' | 'DELETE',
  path: string,
  body?: unknown,
): Promise<T> {
  const url = `${BASE}/odata/${path}`
  const res = await fetchComRefresh(url, {
    method,
    headers: headers(),
    body: body === undefined ? undefined : JSON.stringify(body),
  })
  if (res.status === 401) {
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    window.location.href = '/login'
    throw new ApiError(401, 'Sessão expirada')
  }
  const corpo = await lerCorpo(res)
  if (!res.ok) throw new ApiError(res.status, mensagemDoCorpo(corpo, `${res.status} em ${url}`), corpo)
  return corpo as T
}

/** GET de coleção com query montada à mão (use encodeURIComponent nos valores). */
export function odataQuery<T>(path: string): Promise<ODataResponse<T>> {
  return odataRequest<ODataResponse<T>>('GET', path)
}

/** POST de ação OData: odata/Entidade(id)/Acao */
export function odataAction<T = unknown>(endpoint: string, id: number, acao: string, body?: unknown): Promise<T> {
  return odataRequest<T>('POST', `${endpoint}(${id})/${acao}`, body ?? {})
}

/** Monta "?$a=1&$b=2" codificando só os valores; ignora undefined/vazio. */
export function montarQuery(opcoes: Record<string, string | number | boolean | undefined | null>): string {
  const partes: string[] = []
  for (const [k, v] of Object.entries(opcoes)) {
    if (v === undefined || v === null || v === '') continue
    partes.push(`${k}=${encodeURIComponent(String(v))}`)
  }
  return partes.length ? '?' + partes.join('&') : ''
}

/**
 * Busca TODOS os itens de uma coleção paginando de 100 em 100 (o servidor limita $top a 100).
 * `path` NÃO deve conter $top nem $skip e DEVE ter $orderby estável (senão a paginação pode repetir ou perder itens).
 * Aceita tanto a resposta OData ({ value: [...] }) quanto as rotas "@" que devolvem array puro.
 * `maximo` limita o total de itens (padrão 2000) para evitar carregar tabelas enormes por engano.
 */
export async function odataTodos<T>(path: string, maximo = 2000): Promise<T[]> {
  const sep = path.includes('?') ? '&' : '?'
  const itens: T[] = []
  for (let skip = 0; skip < maximo; skip += 100) {
    const r = await odataRequest<unknown>('GET', `${path}${sep}$top=100&$skip=${skip}`)
    const pagina: T[] = Array.isArray(r) ? (r as T[]) : (((r as { value?: T[] } | null)?.value) ?? [])
    itens.push(...pagina)
    if (pagina.length < 100) break
  }
  return itens
}
