<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { montarQuery, odataRequest } from '@/services/api'
import { campo, lista, mensagemErro, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import ChecklistEtapaEditor from '@/components/admin/funis/ChecklistEtapaEditor.vue'

export interface FunilReg { id: number; nome: string; descricao: string; proibirRetorno: boolean; exigirTodas: boolean }

const props = defineProps<{ funil: FunilReg }>()
const emit = defineEmits<{ alterado: []; arquivado: [] }>()

interface Etapa { id: number; nome: string; ordem: number; dias: number | null; salvando: boolean }

const toast = useToast()
const nome = ref('')
const descricao = ref('')
const proibirRetorno = ref(false)
const exigirTodas = ref(false)
const salvandoFunil = ref(false)

const etapas = ref<Etapa[]>([])
const carregando = ref(false)
const erro = ref('')
const novaEtapa = ref('')
const criando = ref(false)
const expandida = ref<number | null>(null)

const sujo = computed(() =>
  nome.value.trim() !== props.funil.nome || descricao.value.trim() !== props.funil.descricao
  || proibirRetorno.value !== props.funil.proibirRetorno || exigirTodas.value !== props.funil.exigirTodas)

function preencher() {
  nome.value = props.funil.nome
  descricao.value = props.funil.descricao
  proibirRetorno.value = props.funil.proibirRetorno
  exigirTodas.value = props.funil.exigirTodas
}

async function carregarEtapas() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({ $orderby: 'Ordem asc,Id asc', $top: 100 })
    etapas.value = lista<Registro>(await odataRequest<unknown>('GET', `Funis(${props.funil.id})/Etapas${q}`)).map((e) => ({
      id: Number(campo(e, 'Id')), nome: String(campo(e, 'Nome') ?? ''), ordem: Number(campo(e, 'Ordem') ?? 0),
      dias: campo<number | null>(e, 'DiasParaExpirar') ?? null, salvando: false,
    }))
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar as etapas')
  } finally {
    carregando.value = false
  }
}

async function salvarFunil() {
  const n = nome.value.trim()
  if (!n) { toast.error('Informe o nome do funil.'); return }
  salvandoFunil.value = true
  try {
    await odataRequest('PATCH', `Funis(${props.funil.id})`, {
      Nome: n, Descricao: descricao.value.trim() || null, ProibirRetornoEtapa: proibirRetorno.value, ExigirTodasEtapas: exigirTodas.value,
    })
    toast.success('Funil salvo.')
    emit('alterado')
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível salvar o funil'))
  } finally {
    salvandoFunil.value = false
  }
}

async function arquivar() {
  if (!window.confirm(`Arquivar o funil "${props.funil.nome}"? Ele deixa de aparecer para os usuários.`)) return
  try {
    await odataRequest('DELETE', `Funis(${props.funil.id})`)
    toast.success('Funil arquivado.')
    emit('arquivado')
  } catch (e) {
    // 422: ainda há negócios abertos no funil
    toast.error(mensagemErro(e, 'Não foi possível arquivar o funil'))
  }
}

async function adicionarEtapa() {
  const n = novaEtapa.value.trim()
  if (!n) { toast.error('Informe o nome da etapa.'); return }
  criando.value = true
  try {
    await odataRequest('POST', `Funis(${props.funil.id})/Etapas`, { Nome: n })
    novaEtapa.value = ''
    await carregarEtapas()
    emit('alterado')
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível criar a etapa'))
  } finally {
    criando.value = false
  }
}

async function salvarEtapa(et: Etapa) {
  const n = et.nome.trim()
  if (!n) { toast.error('Informe o nome da etapa.'); await carregarEtapas(); return }
  const dias = et.dias === null || (et.dias as unknown) === '' ? null : Number(et.dias)
  if (dias !== null && (!Number.isInteger(dias) || dias < 0)) { toast.error('Dias para expirar deve ser um número inteiro maior ou igual a zero.'); await carregarEtapas(); return }
  et.salvando = true
  try {
    await odataRequest('PATCH', `Funis(${props.funil.id})/Etapas(${et.id})`, { Nome: n, DiasParaExpirar: dias })
    et.nome = n
    et.dias = dias
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível salvar a etapa'))
    await carregarEtapas()
  } finally {
    et.salvando = false
  }
}

async function removerEtapa(et: Etapa) {
  if (!window.confirm(`Remover a etapa "${et.nome}"? Ela é inativada e some do funil.`)) return
  et.salvando = true
  try {
    await odataRequest('DELETE', `Funis(${props.funil.id})/Etapas(${et.id})`)
    toast.success('Etapa removida.')
    if (expandida.value === et.id) expandida.value = null
    await carregarEtapas()
    emit('alterado')
  } catch (e) {
    // 422: há negócios abertos na etapa
    et.salvando = false
    toast.error(mensagemErro(e, 'Não foi possível remover a etapa'))
  }
}

async function moverEtapa(idx: number, delta: -1 | 1) {
  const j = idx + delta
  const a = etapas.value[idx]
  const b = etapas.value[j]
  if (!a || !b) return
  const antes = [...etapas.value]
  const nova = [...etapas.value]
  nova[idx] = b
  nova[j] = a
  etapas.value = nova
  try {
    await odataRequest('POST', `Funis(${props.funil.id})/ReordenarEtapas`, { Ids: nova.map((x) => x.id) })
    emit('alterado')
  } catch (e) {
    etapas.value = antes
    toast.error(mensagemErro(e, 'Não foi possível reordenar as etapas'))
  }
}

function alternarChecklist(id: number) { expandida.value = expandida.value === id ? null : id }

watch(() => props.funil.id, () => { expandida.value = null; preencher(); carregarEtapas() })
// Só reenche o formulário se os valores gravados mudarem (um refresh da lista não apaga edição em andamento)
watch(() => [props.funil.nome, props.funil.descricao, props.funil.proibirRetorno, props.funil.exigirTodas], preencher)
onMounted(() => { preencher(); carregarEtapas() })
</script>

<template>
  <div class="fe">
    <section class="fe-card" aria-labelledby="fe-t-dados">
      <h3 id="fe-t-dados">Dados do funil</h3>
      <div class="fe-grid">
        <label class="fe-campo">
          <span>Nome</span>
          <input v-model="nome" type="text" maxlength="200" @keydown.enter.prevent="salvarFunil" />
        </label>
        <label class="fe-campo fe-largo">
          <span>Descrição</span>
          <input v-model="descricao" type="text" maxlength="500" />
        </label>
      </div>
      <fieldset class="fe-regras">
        <legend>Regras de movimentação</legend>
        <label class="fe-check">
          <input v-model="proibirRetorno" type="checkbox" />
          <span><strong>Proibir retorno de etapa</strong><small>O negócio não pode voltar para uma etapa anterior.</small></span>
        </label>
        <label class="fe-check">
          <input v-model="exigirTodas" type="checkbox" />
          <span><strong>Exigir passar por todas as etapas</strong><small>O negócio só avança para a etapa seguinte, sem pular.</small></span>
        </label>
      </fieldset>
      <div class="fe-rodape">
        <button type="button" class="fe-btn fe-primario" :disabled="!sujo || salvandoFunil" @click="salvarFunil">
          <i class="mdi" :class="salvandoFunil ? 'mdi-loading mdi-spin' : 'mdi-content-save-outline'" aria-hidden="true"></i> Salvar
        </button>
        <button type="button" class="fe-btn fe-perigo" @click="arquivar"><i class="mdi mdi-archive-outline" aria-hidden="true"></i> Arquivar funil</button>
      </div>
    </section>

    <section class="fe-card" aria-labelledby="fe-t-etapas">
      <h3 id="fe-t-etapas">Etapas</h3>
      <p v-if="carregando" class="fe-msg" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando etapas...</p>
      <p v-else-if="erro" class="fe-msg fe-erro" role="alert">{{ erro }} <button type="button" class="fe-link" @click="carregarEtapas">Tentar novamente</button></p>
      <template v-else>
        <p v-if="!etapas.length" class="fe-msg">Este funil ainda não tem etapas. Adicione a primeira abaixo.</p>
        <ol class="fe-etapas">
          <li v-for="(et, idx) in etapas" :key="et.id" :class="{ ocupada: et.salvando }">
            <div class="fe-linha">
              <span class="fe-num" aria-hidden="true">{{ idx + 1 }}</span>
              <input v-model="et.nome" class="fe-in fe-nome" type="text" maxlength="200" :disabled="et.salvando" :aria-label="`Nome da etapa ${idx + 1}`" @change="salvarEtapa(et)" />
              <label class="fe-dias" :title="'Dias para o negócio expirar nesta etapa (opcional)'">
                <span>Expira em</span>
                <input v-model.number="et.dias" class="fe-in fe-dias-in" type="number" min="0" step="1" placeholder="—" :disabled="et.salvando" :aria-label="`Dias para expirar na etapa ${et.nome}`" @change="salvarEtapa(et)" />
                <span>dias</span>
              </label>
              <button type="button" class="fe-ic" :class="{ on: expandida === et.id }" :aria-expanded="expandida === et.id" :aria-label="`Checklists da etapa ${et.nome}`" title="Checklists da etapa" @click="alternarChecklist(et.id)"><i class="mdi mdi-checkbox-marked-outline" aria-hidden="true"></i></button>
              <button type="button" class="fe-ic" :disabled="idx === 0 || et.salvando" :aria-label="`Mover ${et.nome} para cima`" title="Subir" @click="moverEtapa(idx, -1)"><i class="mdi mdi-arrow-up" aria-hidden="true"></i></button>
              <button type="button" class="fe-ic" :disabled="idx === etapas.length - 1 || et.salvando" :aria-label="`Mover ${et.nome} para baixo`" title="Descer" @click="moverEtapa(idx, 1)"><i class="mdi mdi-arrow-down" aria-hidden="true"></i></button>
              <button type="button" class="fe-ic fe-del" :disabled="et.salvando" :aria-label="`Remover etapa ${et.nome}`" title="Remover etapa" @click="removerEtapa(et)"><i class="mdi mdi-delete-outline" aria-hidden="true"></i></button>
            </div>
            <ChecklistEtapaEditor v-if="expandida === et.id" :etapa-id="et.id" :etapa-nome="et.nome" />
          </li>
        </ol>
        <form class="fe-nova" @submit.prevent="adicionarEtapa">
          <input v-model="novaEtapa" class="fe-in fe-nome" type="text" maxlength="200" placeholder="Nome da nova etapa" aria-label="Nome da nova etapa" />
          <button type="submit" class="fe-btn" :disabled="criando"><i class="mdi mdi-plus" aria-hidden="true"></i> Adicionar etapa</button>
        </form>
      </template>
    </section>
  </div>
</template>

<style scoped>
.fe { display: flex; flex-direction: column; gap: 16px; }
.fe-card { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 12px; padding: 16px 18px; }
.fe-card h3 { font-size: 13px; font-weight: 700; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); margin: 0 0 12px; }
.fe-grid { display: grid; grid-template-columns: minmax(200px, 1fr) minmax(200px, 2fr); gap: 12px; }
.fe-campo { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--text-secondary); }
.fe-campo input, .fe-in { padding: 8px 10px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-primary); font-size: 13px; min-width: 0; }
.fe-campo input:focus, .fe-in:focus { outline: none; border-color: var(--accent); }
.fe-regras { border: 1px solid var(--border); border-radius: 8px; margin: 14px 0 0; padding: 10px 12px; display: flex; flex-direction: column; gap: 10px; }
.fe-regras legend { padding: 0 6px; font-size: 11px; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); }
.fe-check { display: flex; align-items: flex-start; gap: 10px; cursor: pointer; }
.fe-check input { margin-top: 3px; width: 16px; height: 16px; accent-color: var(--accent); }
.fe-check span { display: flex; flex-direction: column; font-size: 13px; color: var(--text-primary); }
.fe-check small { color: var(--text-muted); font-size: 12px; }
.fe-rodape { display: flex; justify-content: space-between; gap: 8px; margin-top: 14px; flex-wrap: wrap; }
.fe-btn { display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-secondary); font-size: 13px; font-weight: 600; cursor: pointer; white-space: nowrap; }
.fe-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.fe-btn:disabled { opacity: .5; cursor: not-allowed; }
.fe-primario { background: var(--accent); border-color: var(--accent); color: #000; }
.fe-primario:hover:not(:disabled) { background: var(--accent-hover); color: #000; }
.fe-perigo { color: var(--danger); border-color: rgba(239, 68, 68, .5); }
.fe-msg { font-size: 13px; color: var(--text-muted); margin: 0 0 10px; }
.fe-erro { color: var(--danger); }
.fe-link { background: none; border: 0; color: var(--accent); cursor: pointer; text-decoration: underline; font-size: 12px; }
.fe-etapas { list-style: none; margin: 0 0 12px; padding: 0; display: flex; flex-direction: column; gap: 8px; }
.fe-etapas li { border: 1px solid var(--border); border-radius: 10px; padding: 8px 10px; background: var(--bg-secondary); }
.fe-etapas li.ocupada { opacity: .6; }
.fe-linha, .fe-nova { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.fe-num { width: 22px; height: 22px; border-radius: 50%; background: var(--bg-elevated); color: var(--text-secondary); font-size: 11px; font-weight: 700; display: inline-flex; align-items: center; justify-content: center; flex-shrink: 0; }
.fe-nome { flex: 1; min-width: 160px; }
.fe-dias { display: inline-flex; align-items: center; gap: 4px; font-size: 11px; color: var(--text-muted); }
.fe-dias-in { width: 64px; padding: 6px 8px; }
.fe-ic { width: 30px; height: 30px; border: 1px solid transparent; border-radius: 6px; background: transparent; color: var(--text-muted); cursor: pointer; font-size: 16px; }
.fe-ic:hover:not(:disabled), .fe-ic:focus-visible { background: var(--bg-elevated); color: var(--text-primary); border-color: var(--border); outline: none; }
.fe-ic.on { color: var(--accent); border-color: var(--accent); }
.fe-ic:disabled { opacity: .35; cursor: not-allowed; }
.fe-del:hover:not(:disabled) { color: var(--danger); }
@media (max-width: 720px) { .fe-grid { grid-template-columns: 1fr; } }
</style>
