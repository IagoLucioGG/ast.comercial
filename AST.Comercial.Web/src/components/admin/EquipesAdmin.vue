<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'
import UiSlideOver from '@/components/ui/UiSlideOver.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import { odataQuery, odataRequest, montarQuery } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { msgErro, iniciais } from './comum/adminApi'
import './comum/admin-comum.css'

interface MembroApi { Id: number; UsuarioId: number; Papel: string | number; Usuario?: { Id: number; Nome: string } | null }
interface EquipeApi { Id: number; Nome: string; Descricao: string | null; Membros?: MembroApi[] }
interface UsuarioOpcao { Id: number; Nome: string; Email: string }

const toast = useToast()
const equipes = ref<EquipeApi[]>([])
const carregando = ref(false)
const erro = ref('')
const busca = ref('')

const ehLider = (m: MembroApi) => m.Papel === 'Lider' || m.Papel === 0

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({
      $orderby: 'Nome asc',
      $top: 100,
      $expand: 'Membros($select=Id,UsuarioId,Papel;$expand=Usuario($select=Id,Nome))',
    })
    equipes.value = (await odataQuery<EquipeApi>(`Equipes${q}`)).value
  } catch (e) {
    erro.value = msgErro(e, 'Não foi possível carregar as equipes.')
    toast.error(erro.value)
  } finally {
    carregando.value = false
  }
}
onMounted(carregar)

const filtradas = computed(() => {
  const q = busca.value.trim().toLowerCase()
  return q ? equipes.value.filter((e) => e.Nome.toLowerCase().includes(q)) : equipes.value
})

const responsaveis = (e: EquipeApi) =>
  (e.Membros ?? []).filter(ehLider).map((m) => m.Usuario?.Nome ?? `#${m.UsuarioId}`).join(', ')

// ---- criar / editar equipe
const modalAberto = ref(false)
const editandoId = ref<number | null>(null)
const form = ref({ Nome: '', Descricao: '' })
const erroNome = ref('')
const salvando = ref(false)

function abrirNova() {
  editandoId.value = null
  form.value = { Nome: '', Descricao: '' }
  erroNome.value = ''
  modalAberto.value = true
}
function abrirEdicao(e: EquipeApi) {
  editandoId.value = e.Id
  form.value = { Nome: e.Nome, Descricao: e.Descricao ?? '' }
  erroNome.value = ''
  modalAberto.value = true
}
async function salvar() {
  const nome = form.value.Nome.trim()
  if (!nome) { erroNome.value = 'O nome é obrigatório.'; return }
  if (salvando.value) return
  salvando.value = true
  try {
    const corpo = { Nome: nome, Descricao: form.value.Descricao.trim() }
    if (editandoId.value === null) await odataRequest('POST', 'Equipes', corpo)
    else await odataRequest('PATCH', `Equipes(${editandoId.value})`, corpo)
    toast.success(editandoId.value === null ? 'Equipe criada.' : 'Equipe atualizada.')
    modalAberto.value = false
    await carregar()
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    salvando.value = false
  }
}

// ---- excluir equipe
const excluindo = ref<EquipeApi | null>(null)
const confirmAberto = ref(false)
const removendo = ref(false)
function pedirExclusao(e: EquipeApi) { excluindo.value = e; confirmAberto.value = true }
async function confirmarExclusao() {
  if (!excluindo.value || removendo.value) return
  removendo.value = true
  try {
    await odataRequest('DELETE', `Equipes(${excluindo.value.Id})`)
    toast.success('Equipe excluída.')
    confirmAberto.value = false
    await carregar()
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    removendo.value = false
  }
}

// ---- membros
const membrosAberto = ref(false)
const equipeId = ref<number | null>(null)
const equipeAtual = computed(() => equipes.value.find((e) => e.Id === equipeId.value) ?? null)
const usuarios = ref<UsuarioOpcao[]>([])
const novoUsuarioId = ref<number | ''>('')
const novoPapel = ref<'Membro' | 'Lider'>('Membro')
const ocupado = ref(false)

const disponiveis = computed(() => {
  const jaNaEquipe = new Set((equipeAtual.value?.Membros ?? []).map((m) => m.UsuarioId))
  return usuarios.value.filter((u) => !jaNaEquipe.has(u.Id))
})

async function abrirMembros(e: EquipeApi) {
  equipeId.value = e.Id
  novoUsuarioId.value = ''
  novoPapel.value = 'Membro'
  membrosAberto.value = true
  if (!usuarios.value.length) {
    try {
      const q = montarQuery({ $filter: "Tipo eq 'Normal'", $select: 'Id,Nome,Email', $orderby: 'Nome asc', $top: 100 })
      usuarios.value = (await odataQuery<UsuarioOpcao>(`Usuarios${q}`)).value
    } catch (err) {
      toast.error(msgErro(err, 'Não foi possível carregar os usuários.'))
    }
  }
}

async function executar(fn: () => Promise<unknown>, sucesso: string) {
  if (ocupado.value) return
  ocupado.value = true
  try {
    await fn()
    toast.success(sucesso)
    await carregar()
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    ocupado.value = false
  }
}

function adicionar() {
  if (equipeId.value === null || novoUsuarioId.value === '') return
  const corpo = { EquipeId: equipeId.value, UsuarioId: Number(novoUsuarioId.value), Papel: novoPapel.value }
  void executar(async () => {
    await odataRequest('POST', 'Equipes@Membros', corpo)
    novoUsuarioId.value = ''
  }, 'Membro adicionado.')
}
function mudarPapel(m: MembroApi, papel: 'Lider' | 'Membro') {
  void executar(() => odataRequest('PATCH', `Equipes@Membros(${m.Id})`, { Papel: papel }), 'Papel atualizado.')
}
function remover(m: MembroApi) {
  void executar(() => odataRequest('DELETE', `Equipes@Membros(${m.Id})`), 'Membro removido.')
}
</script>

<template>
  <div class="adm-tela">
    <div class="adm-barra">
      <div class="adm-busca">
        <i class="mdi mdi-magnify"></i>
        <input v-model="busca" type="search" placeholder="Buscar equipe..." aria-label="Buscar equipe" />
      </div>
      <button class="adm-btn primario" @click="abrirNova"><i class="mdi mdi-plus"></i> Nova equipe</button>
      <span class="adm-total">{{ filtradas.length }} equipe(s)</span>
    </div>

    <div class="adm-tabela-wrap">
      <table class="adm-tabela">
        <thead><tr><th>Equipe</th><th>Responsável</th><th>Membros</th><th>Descrição</th><th></th></tr></thead>
        <tbody>
          <tr v-if="carregando"><td colspan="5" class="adm-vazio">Carregando...</td></tr>
          <tr v-else-if="erro"><td colspan="5" class="adm-erro">{{ erro }}<br /><button class="adm-btn" @click="carregar">Tentar novamente</button></td></tr>
          <tr v-else-if="!filtradas.length"><td colspan="5" class="adm-vazio">{{ busca ? 'Nenhuma equipe encontrada.' : 'Nenhuma equipe cadastrada.' }}</td></tr>
          <template v-else>
            <tr v-for="e in filtradas" :key="e.Id">
              <td class="adm-principal">{{ e.Nome }}</td>
              <td>{{ responsaveis(e) || '-' }}</td>
              <td><UiBadge tom="info">{{ e.Membros?.length ?? 0 }}</UiBadge></td>
              <td>{{ e.Descricao || '-' }}</td>
              <td class="adm-acoes">
                <button class="adm-icone" :aria-label="`Gerir membros de ${e.Nome}`" title="Gerir membros" @click="abrirMembros(e)"><i class="mdi mdi-account-multiple-plus-outline"></i></button>
                <button class="adm-icone" :aria-label="`Editar ${e.Nome}`" @click="abrirEdicao(e)"><i class="mdi mdi-pencil-outline"></i></button>
                <button class="adm-icone perigo" :aria-label="`Excluir ${e.Nome}`" @click="pedirExclusao(e)"><i class="mdi mdi-trash-can-outline"></i></button>
              </td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <UiModal v-model="modalAberto" :titulo="editandoId === null ? 'Nova equipe' : 'Editar equipe'" largura="480px">
      <form id="form-equipe" class="adm-form" @submit.prevent="salvar">
        <div class="adm-campo">
          <label for="eq-nome">Nome <span class="adm-obrig" aria-hidden="true">*</span></label>
          <input id="eq-nome" v-model="form.Nome" class="adm-input" maxlength="150" />
          <span v-if="erroNome" class="adm-invalido" role="alert">{{ erroNome }}</span>
        </div>
        <div class="adm-campo">
          <label for="eq-desc">Descrição</label>
          <textarea id="eq-desc" v-model="form.Descricao" class="adm-textarea" maxlength="300"></textarea>
        </div>
      </form>
      <template #rodape>
        <button class="adm-btn" type="button" @click="modalAberto = false">Cancelar</button>
        <button class="adm-btn primario" type="submit" form="form-equipe" :disabled="salvando">{{ salvando ? 'Salvando...' : 'Salvar' }}</button>
      </template>
    </UiModal>

    <UiModal v-model="confirmAberto" titulo="Excluir equipe" largura="440px">
      <p class="txt">Excluir a equipe <strong>{{ excluindo?.Nome }}</strong>? Os vínculos dos membros também serão desfeitos (os usuários continuam ativos).</p>
      <template #rodape>
        <button class="adm-btn" @click="confirmAberto = false">Cancelar</button>
        <button class="adm-btn perigo" :disabled="removendo" @click="confirmarExclusao">{{ removendo ? 'Excluindo...' : 'Excluir' }}</button>
      </template>
    </UiModal>

    <UiSlideOver v-model="membrosAberto" :titulo="`Membros: ${equipeAtual?.Nome ?? ''}`" largura="480px">
      <div v-if="equipeAtual" class="membros">
        <div class="adm-form-grade add-linha">
          <div class="adm-campo largo">
            <label for="mb-usuario">Adicionar usuário</label>
            <select id="mb-usuario" v-model="novoUsuarioId" class="adm-select">
              <option value="">Selecione...</option>
              <option v-for="u in disponiveis" :key="u.Id" :value="u.Id">{{ u.Nome }} ({{ u.Email }})</option>
            </select>
          </div>
          <div class="adm-campo">
            <label for="mb-papel">Papel</label>
            <select id="mb-papel" v-model="novoPapel" class="adm-select">
              <option value="Membro">Membro</option>
              <option value="Lider">Responsável (líder)</option>
            </select>
          </div>
          <div class="adm-campo" style="justify-content: flex-end">
            <button class="adm-btn primario" :disabled="novoUsuarioId === '' || ocupado" @click="adicionar"><i class="mdi mdi-plus"></i> Adicionar</button>
          </div>
        </div>

        <p class="adm-sub">{{ equipeAtual.Membros?.length ?? 0 }} membro(s)</p>
        <p v-if="!equipeAtual.Membros?.length" class="adm-vazio">Esta equipe ainda não tem membros.</p>
        <ul v-else class="lista-membros">
          <li v-for="m in equipeAtual.Membros" :key="m.Id">
            <div class="adm-celula-pessoa">
              <span class="adm-avatar">{{ iniciais(m.Usuario?.Nome ?? '?') }}</span>
              <span class="adm-principal">{{ m.Usuario?.Nome ?? `Usuário #${m.UsuarioId}` }}</span>
              <UiBadge v-if="ehLider(m)" tom="destaque">Responsável</UiBadge>
            </div>
            <div class="linha-acoes">
              <select class="adm-select papel" :value="ehLider(m) ? 'Lider' : 'Membro'" :disabled="ocupado" :aria-label="`Papel de ${m.Usuario?.Nome ?? 'membro'}`" @change="mudarPapel(m, ($event.target as HTMLSelectElement).value as 'Lider' | 'Membro')">
                <option value="Membro">Membro</option>
                <option value="Lider">Responsável</option>
              </select>
              <button class="adm-icone perigo" :disabled="ocupado" :aria-label="`Remover ${m.Usuario?.Nome ?? 'membro'} da equipe`" @click="remover(m)"><i class="mdi mdi-account-remove-outline"></i></button>
            </div>
          </li>
        </ul>
      </div>
      <template #rodape><button class="adm-btn" @click="membrosAberto = false">Fechar</button></template>
    </UiSlideOver>
  </div>
</template>

<style scoped>
.txt { font-size: 14px; color: var(--text-secondary); line-height: 1.5; }
.membros { display: flex; flex-direction: column; gap: 14px; }
.add-linha { align-items: end; }
.lista-membros { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 8px; }
.lista-membros li { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 8px 10px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-secondary); }
.linha-acoes { display: flex; align-items: center; gap: 6px; }
.papel { width: 130px; padding: 5px 8px; }
</style>
