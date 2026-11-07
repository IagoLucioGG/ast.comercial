import { ApiError } from '@/services/api'

export type PerfilChave = 'Administrador' | 'Gerente' | 'Vendedor' | 'Visualizador'
export type EscopoChave = 'Todos' | 'Equipe' | 'Proprios'

export interface EntidadeCatalogo {
  chave: string
  rotulo: string
  grupo: string
  /** Escopo (próprios/equipe/todos) aplicado pelo servidor nesta entidade. */
  suportaEscopo: boolean
}

export interface LinhaPermissao {
  perfil: PerfilChave
  entidade: string
  podeVer: boolean
  podeCriar: boolean
  podeEditar: boolean
  podeExcluir: boolean
  escopo: EscopoChave
}

export const PERFIS: { chave: PerfilChave; rotulo: string; descricao: string; icone: string }[] = [
  { chave: 'Administrador', rotulo: 'Administrador', icone: 'mdi-shield-crown-outline', descricao: 'Acesso total, sempre. Não pode ser restringido.' },
  { chave: 'Gerente', rotulo: 'Gerente', icone: 'mdi-account-tie-outline', descricao: 'Coordena a operação comercial e consulta a administração.' },
  { chave: 'Vendedor', rotulo: 'Vendedor', icone: 'mdi-account-cash-outline', descricao: 'Trabalha os próprios negócios e atividades.' },
  { chave: 'Visualizador', rotulo: 'Visualizador', icone: 'mdi-eye-outline', descricao: 'Somente consulta.' },
]

export const ESCOPOS: { chave: EscopoChave; rotulo: string }[] = [
  { chave: 'Todos', rotulo: 'Todos os registros' },
  { chave: 'Equipe', rotulo: 'Registros da equipe' },
  { chave: 'Proprios', rotulo: 'Somente os próprios' },
]

async function chamar<T>(metodo: 'GET' | 'PUT', url: string, corpo?: unknown): Promise<T> {
  const token = localStorage.getItem('token')
  const res = await fetch(url, {
    method: metodo,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    body: corpo === undefined ? undefined : JSON.stringify(corpo),
  })
  const texto = await res.text()
  let dados: unknown = null
  try { dados = texto ? JSON.parse(texto) : null } catch { dados = texto }
  if (!res.ok) {
    const msg = dados && typeof dados === 'object' && typeof (dados as { erro?: unknown }).erro === 'string'
      ? (dados as { erro: string }).erro
      : `Erro ${res.status} ao acessar as permissões.`
    throw new ApiError(res.status, msg, dados)
  }
  return dados as T
}

export const carregarCatalogo = () => chamar<EntidadeCatalogo[]>('GET', '/api/permissoes/catalogo')

export async function carregarMatriz(): Promise<LinhaPermissao[]> {
  const r = await chamar<{ permissoes: LinhaPermissao[] }>('GET', '/api/permissoes')
  return r.permissoes
}

export async function salvarMatriz(linhas: LinhaPermissao[]): Promise<LinhaPermissao[]> {
  const r = await chamar<{ permissoes: LinhaPermissao[] }>('PUT', '/api/permissoes', { permissoes: linhas })
  return r.permissoes
}
