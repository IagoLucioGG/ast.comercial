<script setup lang="ts">
// Usuários de integração: autenticam com token fixo (cabeçalho X-Api-Key). O token nunca vem nas listagens;
// só aparece uma vez, ao criar ou regenerar.
import { ref, computed, onMounted } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'
import { odataRequest, odataAction, montarQuery } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { msgErro, ehIntegracao, dataBr } from './comum/adminApi'
import './comum/admin-comum.css'

interface UsuarioInteg { Id: number; Nome: string; Email: string; Tipo: number | string; DescricaoIntegracao: string | null; Ativo: boolean; CriadoEm: string }

const toast = useToast()
const itens = ref<UsuarioInteg[]>([])
const carregando = ref(false)
const erro = ref('')
const busca = ref('')

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({ $orderby: 'Nome asc', $top: 100, $select: 'Id,Nome,Email,Tipo,DescricaoIntegracao,Ativo,CriadoEm' })
    const lista = await odataRequest<UsuarioInteg[]>('GET', `Usuarios@Todos${q}`)
    itens.value = lista.filter((u) => ehIntegracao(u.Tipo))
  } catch (e) {
    erro.value = msgErro(e, 'Não foi possível carregar os usuários de integração.')
    toast.error(erro.value)
  } finally {
    carregando.value = false
  }
}
onMounted(carregar)

const filtrados = computed(() => {
  const q = busca.value.trim().toLowerCase()
  return q ? itens.value.filter((u) => u.Nome.toLowerCase().includes(q) || u.Email.toLowerCase().includes(q) || (u.DescricaoIntegracao ?? '').toLowerCase().includes(q)) : itens.value
})

// ---- criar / editar
const modalAberto = ref(false)
const editando = ref<UsuarioInteg | null>(null)
const form = ref({ Nome: '', Email: '', DescricaoIntegracao: '' })
const erros = ref<Record<string, string>>({})
const salvando = ref(false)

function abrirNovo() {
  editando.value = null
  form.value = { Nome: '', Email: '', DescricaoIntegracao: '' }
  erros.value = {}
  modalAberto.value = true
}
function abrirEdicao(u: UsuarioInteg) {
  editando.value = u
  form.value = { Nome: u.Nome, Email: u.Email, DescricaoIntegracao: u.DescricaoIntegracao ?? '' }
  erros.value = {}
  modalAberto.value = true
}

// ---- token exibido uma única vez
const tokenAberto = ref(false)
const tokenExibido = ref('')
const tokenDe = ref('')
function mostrarToken(token: string, nome: string) {
  tokenExibido.value = token
  tokenDe.value = nome
  tokenAberto.value = true
}
async function copiarToken() {
  try { await navigator.clipboard.writeText(tokenExibido.value); toast.info('Token copiado.') } catch { toast.warning('Não foi possível copiar. Selecione o texto e copie manualmente.') }
}
function fecharToken() { tokenAberto.value = false; tokenExibido.value = '' }

async function gerarToken(u: { Id: number; Nome: string }) {
  const r = await odataAction<{ tokenIntegracao: string }>('Usuarios', u.Id, 'RegenerarToken')
  mostrarToken(r.tokenIntegracao, u.Nome)
}

async function salvar() {
  const e: Record<string, string> = {}
  if (!form.value.Nome.trim()) e.Nome = 'O nome é obrigatório.'
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.value.Email.trim())) e.Email = 'Informe um e-mail válido (usado só como identificador).'
  erros.value = e
  if (Object.keys(e).length || salvando.value) return
  salvando.value = true
  try {
    const corpo = { Nome: form.value.Nome.trim(), Email: form.value.Email.trim(), DescricaoIntegracao: form.value.DescricaoIntegracao.trim() || null }
    if (editando.value) {
      await odataRequest('PATCH', `Usuarios(${editando.value.Id})`, corpo)
      toast.success('Usuário de integração atualizado.')
      modalAberto.value = false
    } else {
      const criado = await odataRequest<{ Id: number; Nome: string }>('POST', 'Usuarios', { ...corpo, Tipo: 'Integracao' })
      toast.success('Usuário de integração criado.')
      modalAberto.value = false
      await gerarToken({ Id: criado.Id, Nome: criado.Nome })
    }
    await carregar()
  } catch (err) {
    toast.error(msgErro(err))
  } finally {
    salvando.value = false
  }
}

// ---- regenerar token
const regenAberto = ref(false)
const alvoRegen = ref<UsuarioInteg | null>(null)
const regenerando = ref(false)
function pedirRegenerar(u: UsuarioInteg) { alvoRegen.value = u; regenAberto.value = true }
async function confirmarRegenerar() {
  if (!alvoRegen.value || regenerando.value) return
  regenerando.value = true
  try {
    await gerarToken(alvoRegen.value)
    regenAberto.value = false
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    regenerando.value = false
  }
}

// ---- ativar / desativar
const ocupadoId = ref<number | null>(null)
async function alternarAtivo(u: UsuarioInteg) {
  if (ocupadoId.value !== null) return
  ocupadoId.value = u.Id
  try {
    await odataRequest('PATCH', `Usuarios(${u.Id})`, { Ativo: !u.Ativo })
    toast.success(u.Ativo ? 'Usuário desativado. O token deixa de funcionar.' : 'Usuário ativado.')
    await carregar()
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    ocupadoId.value = null
  }
}
</script>

<template>
  <div class="adm-tela">
    <div class="adm-aviso"><strong>Atenção:</strong> o token (API Key) de um usuário de integração é exibido apenas na criação ou ao regenerar. Regenerar invalida o token anterior.</div>
    <div class="adm-barra">
      <div class="adm-busca">
        <i class="mdi mdi-magnify"></i>
        <input v-model="busca" type="search" placeholder="Buscar..." aria-label="Buscar usuário de integração" />
      </div>
      <button class="adm-btn primario" @click="abrirNovo"><i class="mdi mdi-plus"></i> Novo usuário de integração</button>
      <span class="adm-total">{{ filtrados.length }} usuário(s)</span>
    </div>

    <div class="adm-tabela-wrap">
      <table class="adm-tabela">
        <thead><tr><th>Nome</th><th>E-mail (identificador)</th><th>Descrição</th><th>Criado em</th><th>Status</th><th></th></tr></thead>
        <tbody>
          <tr v-if="carregando"><td colspan="6" class="adm-vazio">Carregando...</td></tr>
          <tr v-else-if="erro"><td colspan="6" class="adm-erro">{{ erro }}<br /><button class="adm-btn" @click="carregar">Tentar novamente</button></td></tr>
          <tr v-else-if="!filtrados.length"><td colspan="6" class="adm-vazio">Nenhum usuário de integração.</td></tr>
          <template v-else>
            <tr v-for="u in filtrados" :key="u.Id">
              <td class="adm-principal">{{ u.Nome }}</td>
              <td>{{ u.Email }}</td>
              <td>{{ u.DescricaoIntegracao || '-' }}</td>
              <td>{{ dataBr(u.CriadoEm) }}</td>
              <td><span class="adm-dot" :class="{ on: u.Ativo }"></span>{{ u.Ativo ? 'Ativo' : 'Inativo' }}</td>
              <td class="adm-acoes">
                <button class="adm-icone" :aria-label="`Editar ${u.Nome}`" title="Editar" @click="abrirEdicao(u)"><i class="mdi mdi-pencil-outline"></i></button>
                <button class="adm-icone" :disabled="!u.Ativo" :aria-label="`Regenerar token de ${u.Nome}`" title="Regenerar token" @click="pedirRegenerar(u)"><i class="mdi mdi-key-change"></i></button>
                <button class="adm-icone" :class="{ perigo: u.Ativo }" :disabled="ocupadoId === u.Id" :aria-label="u.Ativo ? `Desativar ${u.Nome}` : `Ativar ${u.Nome}`" :title="u.Ativo ? 'Desativar' : 'Ativar'" @click="alternarAtivo(u)"><i class="mdi" :class="u.Ativo ? 'mdi-account-off-outline' : 'mdi-account-check-outline'"></i></button>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <UiModal v-model="modalAberto" :titulo="editando ? 'Editar usuário de integração' : 'Novo usuário de integração'" largura="500px">
      <form id="form-integ" class="adm-form" @submit.prevent="salvar">
        <div class="adm-campo">
          <label for="ig-nome">Nome <span class="adm-obrig" aria-hidden="true">*</span></label>
          <input id="ig-nome" v-model="form.Nome" class="adm-input" maxlength="150" />
          <span v-if="erros.Nome" class="adm-invalido" role="alert">{{ erros.Nome }}</span>
        </div>
        <div class="adm-campo">
          <label for="ig-email">E-mail (identificador) <span class="adm-obrig" aria-hidden="true">*</span></label>
          <input id="ig-email" v-model="form.Email" class="adm-input" type="email" maxlength="150" />
          <span v-if="erros.Email" class="adm-invalido" role="alert">{{ erros.Email }}</span>
        </div>
        <div class="adm-campo">
          <label for="ig-desc">Descrição</label>
          <textarea id="ig-desc" v-model="form.DescricaoIntegracao" class="adm-textarea" maxlength="300" placeholder="Ex.: integração com o ERP"></textarea>
        </div>
      </form>
      <template #rodape>
        <button class="adm-btn" type="button" @click="modalAberto = false">Cancelar</button>
        <button class="adm-btn primario" type="submit" form="form-integ" :disabled="salvando">{{ salvando ? 'Salvando...' : 'Salvar' }}</button>
      </template>
    </UiModal>

    <UiModal v-model="regenAberto" titulo="Regenerar token" largura="460px">
      <p class="txt">Gerar um novo token para <strong>{{ alvoRegen?.Nome }}</strong>? O token atual deixa de funcionar imediatamente e qualquer integração que o use vai falhar até ser atualizada.</p>
      <template #rodape>
        <button class="adm-btn" @click="regenAberto = false">Cancelar</button>
        <button class="adm-btn perigo" :disabled="regenerando" @click="confirmarRegenerar">{{ regenerando ? 'Gerando...' : 'Regenerar' }}</button>
      </template>
    </UiModal>

    <UiModal v-model="tokenAberto" :titulo="`Token de ${tokenDe}`" largura="560px" :fechar-ao-clicar-fora="false" @fechar="fecharToken">
      <p class="txt">Copie e guarde o token agora. Ele não será exibido novamente. Envie no cabeçalho <code>X-Api-Key</code>.</p>
      <div class="adm-token" style="margin-top: 12px">{{ tokenExibido }}</div>
      <template #rodape>
        <button class="adm-btn" @click="copiarToken"><i class="mdi mdi-content-copy"></i> Copiar</button>
        <button class="adm-btn primario" @click="fecharToken">Já guardei</button>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.txt { font-size: 14px; color: var(--text-secondary); line-height: 1.5; }
code { background: var(--bg-secondary); padding: 1px 5px; border-radius: 4px; font-size: 12px; }
</style>
