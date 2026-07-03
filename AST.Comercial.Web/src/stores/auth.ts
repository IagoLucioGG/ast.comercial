import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { useRouter } from 'vue-router'

interface UsuarioLogado {
  usuarioId: number
  empresaId: number
  nome: string
  perfil: string
  token: string
  refreshToken?: string
}

export const useAuthStore = defineStore('auth', () => {
  const router = useRouter()
  const usuario = ref<UsuarioLogado | null>(carregarUsuario())

  const logado = computed(() => !!usuario.value)
  const nome = computed(() => usuario.value?.nome ?? '')
  const iniciais = computed(() => {
    if (!usuario.value?.nome) return '?'
    const partes = usuario.value.nome.split(' ')
    return partes.length > 1
      ? (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
      : partes[0][0].toUpperCase()
  })

  function carregarUsuario(): UsuarioLogado | null {
    const token = localStorage.getItem('token')
    const dados = localStorage.getItem('usuario')
    if (!token || !dados) return null
    try {
      return { ...JSON.parse(dados), token }
    } catch {
      return null
    }
  }

  function login(dados: UsuarioLogado) {
    usuario.value = dados
    localStorage.setItem('token', dados.token)
    if (dados.refreshToken) localStorage.setItem('refreshToken', dados.refreshToken)
    localStorage.setItem('usuario', JSON.stringify({
      usuarioId: dados.usuarioId,
      empresaId: dados.empresaId,
      nome: dados.nome,
      perfil: dados.perfil,
    }))
  }

  function logout() {
    usuario.value = null
    localStorage.removeItem('token')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('usuario')
    router.push({ name: 'login' })
  }

  return { usuario, logado, nome, iniciais, login, logout }
})
