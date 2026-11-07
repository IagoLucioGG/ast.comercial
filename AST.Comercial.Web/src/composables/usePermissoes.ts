import { computed, ref } from 'vue'

export type AcaoPermissao = 'ver' | 'criar' | 'editar' | 'excluir'
export type EscopoVisibilidade = 'Todos' | 'Equipe' | 'Proprios'

/** Chaves do catálogo de entidades do servidor (Domain/Autorizacao/CatalogoEntidades.cs). */
export type EntidadeChave =
  | 'Cliente' | 'PessoaContato' | 'Negocio' | 'Atividade' | 'Proposta' | 'Produto' | 'ListaPreco' | 'Meta'
  | 'PainelRelatorio' | 'Importacao' | 'Etiqueta' | 'Funil' | 'Automacao' | 'Integracao' | 'Usuario' | 'Equipe'
  | 'Departamento' | 'Cargo' | 'Campo' | 'Formulario' | 'ModeloDocumento' | 'FluxoAprovacao' | 'Empresa'
  | 'CadastroAuxiliar'

export interface PermissaoEntidade {
  entidade: string
  podeVer: boolean
  podeCriar: boolean
  podeEditar: boolean
  podeExcluir: boolean
  escopo: EscopoVisibilidade
}

interface MinhasPermissoes {
  usuarioId: number | null
  perfil: string
  administrador: boolean
  permissoes: PermissaoEntidade[]
}

// Estado único (módulo): todas as telas compartilham a mesma cópia das permissões do usuário logado.
const dados = ref<MinhasPermissoes | null>(null)
const carregando = ref(false)
const falhou = ref(false)
let emAndamento: Promise<void> | null = null

function cabecalho(): Record<string, string> {
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
    const corpo = await res.json()
    localStorage.setItem('token', corpo.token)
    if (corpo.refreshToken) localStorage.setItem('refreshToken', corpo.refreshToken)
    return true
  } catch {
    return false
  }
}

async function buscarMinhas(): Promise<MinhasPermissoes> {
  let res = await fetch('/api/permissoes/minhas', { headers: cabecalho() })
  if (res.status === 401 && (await renovarToken())) {
    res = await fetch('/api/permissoes/minhas', { headers: cabecalho() })
  }
  if (!res.ok) throw new Error(`${res.status} ao carregar permissões`)
  return (await res.json()) as MinhasPermissoes
}

/** Carrega (ou recarrega) as permissões do usuário logado. Chamadas simultâneas compartilham a mesma requisição. */
function carregar(forcar = false): Promise<void> {
  if (!forcar && dados.value) return Promise.resolve()
  if (emAndamento) return emAndamento
  carregando.value = true
  emAndamento = buscarMinhas()
    .then((r) => {
      dados.value = r
      falhou.value = false
    })
    .catch(() => {
      // O servidor continua sendo a barreira real; sem as permissões a interface não esconde nada.
      falhou.value = true
    })
    .finally(() => {
      carregando.value = false
      emAndamento = null
    })
  return emAndamento
}

function limpar() {
  dados.value = null
  falhou.value = false
}

const mapaPorEntidade = computed(() => {
  const m = new Map<string, PermissaoEntidade>()
  for (const p of dados.value?.permissoes ?? []) m.set(p.entidade, p)
  return m
})

/**
 * Permissões do usuário logado.
 * - pode(entidade, acao): enquanto as permissões não carregaram (ou se falharam) devolve true,
 *   porque quem barra de verdade é o servidor (403); aqui só se evita mostrar o que o perfil não pode usar.
 */
export function usePermissoes() {
  const carregado = computed(() => dados.value !== null)
  const administrador = computed(() => dados.value?.administrador === true)
  const perfil = computed(() => dados.value?.perfil ?? '')

  function pode(entidade: EntidadeChave | string, acao: AcaoPermissao = 'ver'): boolean {
    if (!dados.value) return true
    if (dados.value.administrador) return true
    const p = mapaPorEntidade.value.get(entidade)
    if (!p) return false
    switch (acao) {
      case 'ver': return p.podeVer
      case 'criar': return p.podeCriar
      case 'editar': return p.podeEditar
      case 'excluir': return p.podeExcluir
    }
  }

  function escopo(entidade: EntidadeChave | string): EscopoVisibilidade {
    if (!dados.value || dados.value.administrador) return 'Todos'
    return mapaPorEntidade.value.get(entidade)?.escopo ?? 'Todos'
  }

  return { carregar, limpar, pode, escopo, carregado, carregando, falhou, administrador, perfil }
}
