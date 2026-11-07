<script setup lang="ts">
// Gestão de usuários (login com senha). Usuários de integração ficam em "Usuários de integração".
import { ref, computed, onMounted } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import { odataQuery, odataRequest, odataAction, montarQuery } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { inicialDe } from '@/composables/useEntidadeDetalhe'
import { msgErro, nomePerfil, ehIntegracao, dataHoraBr, PERFIS } from './comum/adminApi'
import './comum/admin-comum.css'

interface Usuario {
  Id: number; Nome: string; Email: string; Tipo: number | string; Perfil: number | string
  FotoUrl: string | null; CargoId: number | null; DepartamentoId: number | null
  UltimoAcesso: string | null; Ativo: boolean
}
interface Opcao { Id: number; Nome: string }

const toast = useToast()
const usuarios = ref<Usuario[]>([])
const cargos = ref<Opcao[]>([])
const departamentos = ref<Opcao[]>([])
const carregando = ref(false)
const erro = ref('')
const busca = ref('')
const filtroStatus = ref<'ativos' | 'inativos' | 'todos'>('ativos')
const atingiuLimite = ref(false)

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({
      $orderby: 'Nome asc', $top: 100,
      $select: 'Id,Nome,Email,Tipo,Perfil,FotoUrl,CargoId,DepartamentoId,UltimoAcesso,Ativo',
    })
    // @Todos devolve também os inativos (a lista padrão da API os esconde).
    const lista = await odataRequest<Usuario[]>('GET', `Usuarios@Todos${q}`)
    atingiuLimite.value = lista.length >= 100
    usuarios.value = lista.filter((u) => !ehIntegracao(u.Tipo))
  } catch (e) {
    erro.value = msgErro(e, 'Não foi possível carregar os usuários.')
    toast.error(erro.value)
  } finally {
    carregando.value = false
  }
}

async function carregarApoio() {
  try {
    const [c, d] = await Promise.all([
      odataQuery<Opcao>(`Cargos${montarQuery({ $select: 'Id,Nome', $orderby: 'Nome asc', $top: 100 })}`),
      odataQuery<Opcao>(`Departamentos${montarQuery({ $select: 'Id,Nome', $orderby: 'Nome asc', $top: 100 })}`),
    ])
    cargos.value = c.value
    departamentos.value = d.value
  } catch (e) {
    toast.error(msgErro(e, 'Não foi possível carregar cargos e departamentos.'))
  }
}

onMounted(() => { void carregar(); void carregarApoio() })

const nomeCargo = (id: number | null) => cargos.value.find((c) => c.Id === id)?.Nome ?? '-'
const nomeDepartamento = (id: number | null) => departamentos.value.find((d) => d.Id === id)?.Nome ?? '-'

const filtrados = computed(() => {
  const q = busca.value.trim().toLowerCase()
  return usuarios.value.filter((u) => {
    if (filtroStatus.value === 'ativos' && !u.Ativo) return false
    if (filtroStatus.value === 'inativos' && u.Ativo) return false
    return !q || u.Nome.toLowerCase().includes(q) || u.Email.toLowerCase().includes(q)
  })
})

const tomPerfil = (p: unknown) => {
  const n = nomePerfil(p)
  return n === 'Administrador' ? 'destaque' : n === 'Gerente' ? 'info' : n === 'Visualizador' ? 'neutro' : 'sucesso'
}

// ---- criar / editar
const modalAberto = ref(false)
const editando = ref<Usuario | null>(null)
const form = ref({ Nome: '', Email: '', Perfil: 'Vendedor', CargoId: '' as number | '', DepartamentoId: '' as number | '', FotoUrl: '', Senha: '' })
const erros = ref<Record<string, string>>({})
const salvando = ref(false)
const mostrarSenha = ref(false)

function abrirNovo() {
  editando.value = null
  form.value = { Nome: '', Email: '', Perfil: 'Vendedor', CargoId: '', DepartamentoId: '', FotoUrl: '', Senha: '' }
  erros.value = {}
  mostrarSenha.value = false
  modalAberto.value = true
}
function abrirEdicao(u: Usuario) {
  editando.value = u
  form.value = { Nome: u.Nome, Email: u.Email, Perfil: nomePerfil(u.Perfil), CargoId: u.CargoId ?? '', DepartamentoId: u.DepartamentoId ?? '', FotoUrl: u.FotoUrl ?? '', Senha: '' }
  erros.value = {}
  modalAberto.value = true
}

function gerarSenha(): string {
  const alfabeto = 'ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789'
  const bytes = crypto.getRandomValues(new Uint32Array(14))
  return Array.from(bytes, (b) => alfabeto[b % alfabeto.length]).join('')
}

function validar(criando: boolean): boolean {
  const e: Record<string, string> = {}
  const f = form.value
  if (!f.Nome.trim()) e.Nome = 'O nome é obrigatório.'
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(f.Email.trim())) e.Email = 'Informe um e-mail válido.'
  if (criando && f.Senha.length < 8) e.Senha = 'A senha inicial deve ter ao menos 8 caracteres.'
  if (f.FotoUrl.trim() && !/^(https?:\/\/|\/)/.test(f.FotoUrl.trim())) e.FotoUrl = 'Use uma URL começando com http://, https:// ou /.'
  erros.value = e
  return Object.keys(e).length === 0
}

async function salvar() {
  const criando = editando.value === null
  if (salvando.value || !validar(criando)) return
  salvando.value = true
  try {
    const f = form.value
    const base = {
      Nome: f.Nome.trim(), Email: f.Email.trim(), Perfil: f.Perfil,
      CargoId: f.CargoId === '' ? null : Number(f.CargoId),
      DepartamentoId: f.DepartamentoId === '' ? null : Number(f.DepartamentoId),
      FotoUrl: f.FotoUrl.trim() || null,
    }
    if (criando) {
      await odataRequest('POST', 'Usuarios', { ...base, Senha: f.Senha })
      toast.success('Usuário criado.')
    } else {
      await odataRequest('PATCH', `Usuarios(${editando.value!.Id})`, base)
      toast.success('Usuário atualizado.')
    }
    modalAberto.value = false
    await carregar()
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    salvando.value = false
  }
}

// ---- ativar / desativar
const alvoStatus = ref<Usuario | null>(null)
const statusAberto = ref(false)
const mudandoStatus = ref(false)
function pedirStatus(u: Usuario) {
  if (u.Ativo) { alvoStatus.value = u; statusAberto.value = true } else { void aplicarStatus(u, true) }
}
async function aplicarStatus(u: Usuario, ativo: boolean) {
  if (mudandoStatus.value) return
  mudandoStatus.value = true
  try {
    await odataRequest('PATCH', `Usuarios(${u.Id})`, { Ativo: ativo })
    toast.success(ativo ? 'Usuário ativado.' : 'Usuário desativado.')
    statusAberto.value = false
    await carregar()
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    mudandoStatus.value = false
  }
}

// ---- redefinir senha
const senhaAberto = ref(false)
const alvoSenha = ref<Usuario | null>(null)
const novaSenha = ref('')
const erroSenha = ref('')
const redefinindo = ref(false)
function abrirSenha(u: Usuario) {
  alvoSenha.value = u
  novaSenha.value = ''
  erroSenha.value = ''
  senhaAberto.value = true
}
async function redefinirSenha() {
  if (!alvoSenha.value || redefinindo.value) return
  if (novaSenha.value.length < 8) { erroSenha.value = 'A senha deve ter ao menos 8 caracteres.'; return }
  redefinindo.value = true
  try {
    await odataAction('Usuarios', alvoSenha.value.Id, 'RedefinirSenha', { NovaSenha: novaSenha.value })
    toast.success('Senha redefinida. As sessões abertas desse usuário foram encerradas.')
    senhaAberto.value = false
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    redefinindo.value = false
  }
}
async function copiarSenha() {
  try { await navigator.clipboard.writeText(novaSenha.value); toast.info('Senha copiada.') } catch { toast.warning('Não foi possível copiar.') }
}
</script>

<template>
  <div class="adm-tela">
    <div class="adm-barra">
      <div class="adm-busca">
        <i class="mdi mdi-magnify"></i>
        <input v-model="busca" type="search" placeholder="Buscar por nome ou e-mail..." aria-label="Buscar usuário" />
      </div>
      <select v-model="filtroStatus" class="adm-select" style="width: 140px" aria-label="Filtrar por status">
        <option value="ativos">Ativos</option>
        <option value="inativos">Inativos</option>
        <option value="todos">Todos</option>
      </select>
      <button class="adm-btn primario" @click="abrirNovo"><i class="mdi mdi-plus"></i> Novo usuário</button>
      <span class="adm-total">{{ filtrados.length }} usuário(s)</span>
    </div>
    <div v-if="atingiuLimite" class="adm-aviso">Mostrando os 100 primeiros usuários. Use a busca para localizar os demais.</div>

    <div class="adm-tabela-wrap">
      <table class="adm-tabela">
        <thead><tr><th>Usuário</th><th>Perfil</th><th>Cargo</th><th>Departamento</th><th>Último acesso</th><th>Status</th><th></th></tr></thead>
        <tbody>
          <tr v-if="carregando"><td colspan="7" class="adm-vazio">Carregando...</td></tr>
          <tr v-else-if="erro"><td colspan="7" class="adm-erro">{{ erro }}<br /><button class="adm-btn" @click="carregar">Tentar novamente</button></td></tr>
          <tr v-else-if="!filtrados.length"><td colspan="7" class="adm-vazio">Nenhum usuário encontrado.</td></tr>
          <template v-else>
            <tr v-for="u in filtrados" :key="u.Id">
              <td>
                <div class="adm-celula-pessoa">
                  <span class="adm-avatar"><img v-if="u.FotoUrl" :src="u.FotoUrl" :alt="u.Nome" /><template v-else>{{ inicialDe(u.Nome) }}</template></span>
                  <div><div class="adm-principal">{{ u.Nome }}</div><div class="adm-sub">{{ u.Email }}</div></div>
                </div>
              </td>
              <td><UiBadge :tom="tomPerfil(u.Perfil)">{{ nomePerfil(u.Perfil) }}</UiBadge></td>
              <td>{{ nomeCargo(u.CargoId) }}</td>
              <td>{{ nomeDepartamento(u.DepartamentoId) }}</td>
              <td>{{ dataHoraBr(u.UltimoAcesso) }}</td>
              <td><span class="adm-dot" :class="{ on: u.Ativo }"></span>{{ u.Ativo ? 'Ativo' : 'Inativo' }}</td>
              <td class="adm-acoes">
                <button class="adm-icone" :aria-label="`Editar ${u.Nome}`" title="Editar" @click="abrirEdicao(u)"><i class="mdi mdi-pencil-outline"></i></button>
                <button class="adm-icone" :aria-label="`Redefinir senha de ${u.Nome}`" title="Redefinir senha" :disabled="!u.Ativo" @click="abrirSenha(u)"><i class="mdi mdi-lock-reset"></i></button>
                <button class="adm-icone" :class="{ perigo: u.Ativo }" :aria-label="u.Ativo ? `Desativar ${u.Nome}` : `Ativar ${u.Nome}`" :title="u.Ativo ? 'Desativar' : 'Ativar'" @click="pedirStatus(u)"><i class="mdi" :class="u.Ativo ? 'mdi-account-off-outline' : 'mdi-account-check-outline'"></i></button>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <UiModal v-model="modalAberto" :titulo="editando ? 'Editar usuário' : 'Novo usuário'" largura="560px">
      <form id="form-usuario" class="adm-form adm-form-grade" @submit.prevent="salvar">
        <div class="adm-campo largo">
          <label for="us-nome">Nome <span class="adm-obrig" aria-hidden="true">*</span></label>
          <input id="us-nome" v-model="form.Nome" class="adm-input" maxlength="150" />
          <span v-if="erros.Nome" class="adm-invalido" role="alert">{{ erros.Nome }}</span>
        </div>
        <div class="adm-campo largo">
          <label for="us-email">E-mail <span class="adm-obrig" aria-hidden="true">*</span></label>
          <input id="us-email" v-model="form.Email" class="adm-input" type="email" maxlength="150" autocomplete="off" />
          <span v-if="erros.Email" class="adm-invalido" role="alert">{{ erros.Email }}</span>
        </div>
        <div class="adm-campo">
          <label for="us-perfil">Perfil</label>
          <select id="us-perfil" v-model="form.Perfil" class="adm-select"><option v-for="p in PERFIS" :key="p" :value="p">{{ p }}</option></select>
        </div>
        <div class="adm-campo">
          <label for="us-cargo">Cargo</label>
          <select id="us-cargo" v-model="form.CargoId" class="adm-select"><option value="">(nenhum)</option><option v-for="c in cargos" :key="c.Id" :value="c.Id">{{ c.Nome }}</option></select>
        </div>
        <div class="adm-campo">
          <label for="us-dep">Departamento</label>
          <select id="us-dep" v-model="form.DepartamentoId" class="adm-select"><option value="">(nenhum)</option><option v-for="d in departamentos" :key="d.Id" :value="d.Id">{{ d.Nome }}</option></select>
        </div>
        <div class="adm-campo">
          <label for="us-foto">URL da foto</label>
          <input id="us-foto" v-model="form.FotoUrl" class="adm-input" maxlength="500" placeholder="https://..." />
          <span v-if="erros.FotoUrl" class="adm-invalido" role="alert">{{ erros.FotoUrl }}</span>
        </div>
        <div v-if="!editando" class="adm-campo largo">
          <label for="us-senha">Senha inicial <span class="adm-obrig" aria-hidden="true">*</span></label>
          <div class="linha-senha">
            <input id="us-senha" v-model="form.Senha" class="adm-input" :type="mostrarSenha ? 'text' : 'password'" autocomplete="new-password" maxlength="100" />
            <button type="button" class="adm-icone" :aria-label="mostrarSenha ? 'Ocultar senha' : 'Mostrar senha'" @click="mostrarSenha = !mostrarSenha"><i class="mdi" :class="mostrarSenha ? 'mdi-eye-off-outline' : 'mdi-eye-outline'"></i></button>
            <button type="button" class="adm-btn" @click="form.Senha = gerarSenha(); mostrarSenha = true">Gerar</button>
          </div>
          <span class="adm-ajuda">Mínimo de 8 caracteres. Repasse ao usuário por um canal seguro.</span>
          <span v-if="erros.Senha" class="adm-invalido" role="alert">{{ erros.Senha }}</span>
        </div>
      </form>
      <template #rodape>
        <button class="adm-btn" type="button" @click="modalAberto = false">Cancelar</button>
        <button class="adm-btn primario" type="submit" form="form-usuario" :disabled="salvando">{{ salvando ? 'Salvando...' : 'Salvar' }}</button>
      </template>
    </UiModal>

    <UiModal v-model="statusAberto" titulo="Desativar usuário" largura="440px">
      <p class="txt">Desativar <strong>{{ alvoStatus?.Nome }}</strong>? O usuário perde o acesso e sai das listas de seleção. Dá para reativar depois.</p>
      <template #rodape>
        <button class="adm-btn" @click="statusAberto = false">Cancelar</button>
        <button class="adm-btn perigo" :disabled="mudandoStatus" @click="alvoStatus && aplicarStatus(alvoStatus, false)">{{ mudandoStatus ? 'Desativando...' : 'Desativar' }}</button>
      </template>
    </UiModal>

    <UiModal v-model="senhaAberto" :titulo="`Redefinir senha: ${alvoSenha?.Nome ?? ''}`" largura="480px">
      <form id="form-senha" class="adm-form" @submit.prevent="redefinirSenha">
        <div class="adm-campo">
          <label for="rs-senha">Nova senha</label>
          <div class="linha-senha">
            <input id="rs-senha" v-model="novaSenha" class="adm-input" type="text" autocomplete="off" maxlength="100" />
            <button type="button" class="adm-btn" @click="novaSenha = gerarSenha()">Gerar</button>
            <button type="button" class="adm-icone" aria-label="Copiar senha" :disabled="!novaSenha" @click="copiarSenha"><i class="mdi mdi-content-copy"></i></button>
          </div>
          <span class="adm-ajuda">Mínimo de 8 caracteres. As sessões abertas do usuário serão encerradas.</span>
          <span v-if="erroSenha" class="adm-invalido" role="alert">{{ erroSenha }}</span>
        </div>
      </form>
      <template #rodape>
        <button class="adm-btn" type="button" @click="senhaAberto = false">Cancelar</button>
        <button class="adm-btn primario" type="submit" form="form-senha" :disabled="redefinindo">{{ redefinindo ? 'Salvando...' : 'Redefinir' }}</button>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.txt { font-size: 14px; color: var(--text-secondary); line-height: 1.5; }
.linha-senha { display: flex; gap: 6px; align-items: center; }
</style>
