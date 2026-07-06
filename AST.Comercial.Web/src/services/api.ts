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
