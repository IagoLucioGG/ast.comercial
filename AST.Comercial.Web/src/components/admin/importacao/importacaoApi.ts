import { ApiError } from '@/services/api'

// Tipos e chamadas da importação de dados por CSV (api/importacoes).
// O arquivo é reenviado a cada passo (analisar, validar, executar): o servidor não guarda nada em disco.

export type EntidadeImportavel = 'Cliente' | 'PessoaContato' | 'Produto' | 'Negocio'
export type ModoDuplicidade = 'Permitir' | 'Ignorar' | 'Atualizar'

export const LIMITE_BYTES = 5 * 1024 * 1024
export const LIMITE_LINHAS = 10000

export interface CampoImportavel {
  chave: string
  rotulo: string
  tipo: string
  obrigatorio: boolean
  personalizado: boolean
  tamanho: number
  dica: string | null
}

export interface SugestaoMapeamento {
  coluna: number
  cabecalho: string
  campo: string
  pontuacao: number
}

export interface ChaveDuplicidade {
  chave: string
  rotulo: string
}

export interface AnaliseImportacao {
  entidade: string
  entidadeRotulo: string
  nomeArquivo: string
  codificacao: string
  separador: string
  totalLinhas: number
  cabecalhos: string[]
  amostra: string[][]
  campos: CampoImportavel[]
  sugestao: SugestaoMapeamento[]
  chavesDuplicidade: ChaveDuplicidade[]
}

export interface ErroLinha {
  linha: number
  campo: string
  mensagem: string
  valor: string | null
}

export interface ValidacaoImportacao {
  totalLinhas: number
  validas: number
  invalidas: number
  duplicadas: number
  totalErros: number
  errosTruncados: boolean
  erros: ErroLinha[]
}

export type StatusImportacao = 'Pendente' | 'Processando' | 'Concluida' | 'ConcluidaComErros' | 'Falhou'

export interface Importacao {
  id: number
  entidade: string
  entidadeRotulo: string
  nomeArquivo: string
  status: StatusImportacao
  totalLinhas: number
  sucesso: number
  falhas: number
  criadas: number
  atualizadas: number
  ignoradas: number
  processadas: number
  duplicidade: ModoDuplicidade
  chave: string | null
  temErros: boolean
  usuarioNome: string | null
  criadoEm: string
  concluidaEm: string | null
}

export interface PaginaImportacoes {
  itens: Importacao[]
  total: number
  pagina: number
  tamanho: number
}

export const ENTIDADES: { chave: EntidadeImportavel; rotulo: string; icone: string; descricao: string }[] = [
  { chave: 'Cliente', rotulo: 'Clientes', icone: 'mdi-domain', descricao: 'Empresas e pessoas com quem você negocia.' },
  { chave: 'PessoaContato', rotulo: 'Contatos', icone: 'mdi-account-multiple-outline', descricao: 'Pessoas ligadas a um cliente, pelo nome do cliente.' },
  { chave: 'Produto', rotulo: 'Produtos', icone: 'mdi-package-variant-closed', descricao: 'Produtos e serviços, com preço, família e grupo.' },
  { chave: 'Negocio', rotulo: 'Negócios', icone: 'mdi-handshake-outline', descricao: 'Negócios com cliente, funil e etapa pelo nome.' },
]

export const ROTULO_STATUS: Record<StatusImportacao, string> = {
  Pendente: 'Pendente',
  Processando: 'Processando',
  Concluida: 'Concluída',
  ConcluidaComErros: 'Concluída com erros',
  Falhou: 'Falhou',
}

export const TOM_STATUS: Record<StatusImportacao, 'neutro' | 'info' | 'sucesso' | 'aviso' | 'perigo'> = {
  Pendente: 'neutro',
  Processando: 'info',
  Concluida: 'sucesso',
  ConcluidaComErros: 'aviso',
  Falhou: 'perigo',
}

// ---------------------------------------------------------------- transporte

function cabecalhosAuth(): Record<string, string> {
  const token = localStorage.getItem('token')
  return token ? { Authorization: `Bearer ${token}` } : {}
}

async function renovarToken(): Promise<boolean> {
  const refreshToken = localStorage.getItem('refreshToken')
  if (!refreshToken) return false
  try {
    const res = await fetch('/api/autenticacao/refresh', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ refreshToken }),
    })
    if (!res.ok) return false
    const dados = await res.json()
    localStorage.setItem('token', dados.token)
    if (dados.refreshToken) localStorage.setItem('refreshToken', dados.refreshToken)
    return true
  } catch {
    return false
  }
}

/** `montar` é chamado a cada tentativa: o corpo (FormData) é recriado se for preciso repetir após renovar o token. */
async function enviar(url: string, montar: () => RequestInit): Promise<Response> {
  const tentar = () => {
    const init = montar()
    return fetch(url, { ...init, headers: { ...cabecalhosAuth(), ...(init.headers as Record<string, string> | undefined) } })
  }
  let res = await tentar()
  if (res.status === 401 && (await renovarToken())) res = await tentar()
  if (res.status === 401) {
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    window.location.href = '/login'
    throw new ApiError(401, 'Sessão expirada')
  }
  return res
}

async function lerJson<T>(res: Response, url: string): Promise<T> {
  const txt = await res.text()
  let corpo: unknown = null
  if (txt) {
    try { corpo = JSON.parse(txt) } catch { corpo = txt }
  }
  if (!res.ok) {
    const msg = corpo && typeof corpo === 'object' && typeof (corpo as { erro?: unknown }).erro === 'string'
      ? (corpo as { erro: string }).erro
      : res.status === 413 ? 'O arquivo excede o limite de 5 MB.' : `Falha na requisição (${res.status}).`
    throw new ApiError(res.status, msg, corpo)
  }
  return corpo as T
}

function formulario(arquivo: File, entidade: EntidadeImportavel, extras: Record<string, string | undefined>): FormData {
  const f = new FormData()
  f.append('entidade', entidade)
  for (const [k, v] of Object.entries(extras)) if (v) f.append(k, v)
  f.append('arquivo', arquivo, arquivo.name)
  return f
}

async function postar<T>(rota: string, arquivo: File, entidade: EntidadeImportavel, extras: Record<string, string | undefined> = {}): Promise<T> {
  const url = `/api/importacoes/${rota}`
  const res = await enviar(url, () => ({ method: 'POST', body: formulario(arquivo, entidade, extras) }))
  return lerJson<T>(res, url)
}

async function obter<T>(caminho: string): Promise<T> {
  const url = `/api/importacoes${caminho}`
  const res = await enviar(url, () => ({ method: 'GET' }))
  return lerJson<T>(res, url)
}

/** Mapeamento enviado ao servidor: { "índice da coluna": "chave do campo" }, sem colunas ignoradas. */
export function serializarMapeamento(mapa: Record<number, string>): string {
  const limpo: Record<string, string> = {}
  for (const [coluna, campo] of Object.entries(mapa)) if (campo) limpo[coluna] = campo
  return JSON.stringify(limpo)
}

export const analisar = (arquivo: File, entidade: EntidadeImportavel) =>
  postar<AnaliseImportacao>('analisar', arquivo, entidade)

export const validar = (
  arquivo: File, entidade: EntidadeImportavel, mapa: Record<number, string>, chave?: string,
) => postar<ValidacaoImportacao>('validar', arquivo, entidade, { mapeamento: serializarMapeamento(mapa), chave })

export const executar = (
  arquivo: File, entidade: EntidadeImportavel, mapa: Record<number, string>, duplicidade: ModoDuplicidade, chave?: string,
) => postar<Importacao>('executar', arquivo, entidade, {
  mapeamento: serializarMapeamento(mapa), duplicidade, chave,
})

export const listar = (pagina: number, tamanho = 20) =>
  obter<PaginaImportacoes>(`?pagina=${pagina}&tamanho=${tamanho}`)

export const buscar = (id: number) => obter<Importacao>(`/${id}`)

/** Baixa um CSV protegido por token e dispara o "salvar como" do navegador. */
export async function baixarArquivo(caminho: string, nomeSugerido: string): Promise<void> {
  const url = `/api/importacoes${caminho}`
  const res = await enviar(url, () => ({ method: 'GET' }))
  if (!res.ok) {
    await lerJson<unknown>(res, url)
    return
  }
  const blob = await res.blob()
  const link = document.createElement('a')
  const objeto = URL.createObjectURL(blob)
  link.href = objeto
  link.download = nomeSugerido
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(objeto), 2000)
}

const NOME_MODELO: Record<EntidadeImportavel, string> = {
  Cliente: 'clientes', PessoaContato: 'contatos', Produto: 'produtos', Negocio: 'negocios',
}

export const baixarModelo = (entidade: EntidadeImportavel) =>
  baixarArquivo(`/modelo/${NOME_MODELO[entidade]}.csv`, `modelo-${NOME_MODELO[entidade]}.csv`)

export const baixarErros = (id: number) => baixarArquivo(`/${id}/erros.csv`, `erros-importacao-${id}.csv`)

export function formatarBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1).replace('.', ',')} KB`
  return `${(bytes / 1024 / 1024).toFixed(1).replace('.', ',')} MB`
}

export function formatarDataHora(iso: string | null): string {
  if (!iso) return '—'
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '—' : d.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}
