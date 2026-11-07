<script setup lang="ts">
import { reactive, ref, watch, computed, onBeforeUnmount } from 'vue'
import UiSlideOver from '@/components/ui/UiSlideOver.vue'
import UiModal from '@/components/ui/UiModal.vue'
import { odataRequest, odataQuery, ApiError } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { TIPOS, TIPO_META, paraInputData, paraInputHora, type Atividade, type TipoAtividade } from './atividades'

const props = defineProps<{
  modelValue: boolean
  atividade: Atividade | null
  /** Data pré-selecionada ao criar a partir de um dia do calendário. */
  dataInicial?: Date | null
}>()

const emit = defineEmits<{
  'update:modelValue': [v: boolean]
  salvo: [a: Atividade]
  excluido: [id: number]
}>()

const toast = useToast()

type Vinculo = 'nenhum' | 'cliente' | 'contato' | 'negocio'

interface FormState {
  titulo: string
  tipo: TipoAtividade
  data: string
  hora: string
  vinculo: Vinculo
  vinculoId: number | null
  vinculoRotulo: string
  descricao: string
}

const form = reactive<FormState>(estadoVazio())
const salvando = ref(false)
const confirmarExclusao = ref(false)
const excluindo = ref(false)
const erroTitulo = ref(false)

const editando = computed(() => !!props.atividade)

function estadoVazio(): FormState {
  return {
    titulo: '', tipo: 'Tarefa', data: '', hora: '',
    vinculo: 'nenhum', vinculoId: null, vinculoRotulo: '', descricao: '',
  }
}

watch(() => props.modelValue, (aberto) => {
  if (!aberto) return
  erroTitulo.value = false
  confirmarExclusao.value = false
  if (props.atividade) carregarDe(props.atividade)
  else {
    Object.assign(form, estadoVazio())
    if (props.dataInicial) {
      form.data = paraInputData(props.dataInicial)
      form.hora = paraInputHora(props.dataInicial)
    }
  }
})

function carregarDe(a: Atividade) {
  Object.assign(form, estadoVazio())
  form.titulo = a.Titulo
  form.tipo = a.Tipo
  if (a.DataVencimento) {
    form.data = paraInputData(a.DataVencimento)
    form.hora = paraInputHora(a.DataVencimento)
  }
  form.descricao = a.Descricao ?? ''
  if (a.ClienteId) { form.vinculo = 'cliente'; form.vinculoId = a.ClienteId; form.vinculoRotulo = a.ClienteNome ?? `Cliente #${a.ClienteId}` }
  else if (a.PessoaContatoId) { form.vinculo = 'contato'; form.vinculoId = a.PessoaContatoId; form.vinculoRotulo = a.ContatoNome ?? `Contato #${a.PessoaContatoId}` }
  else if (a.NegocioId) { form.vinculo = 'negocio'; form.vinculoId = a.NegocioId; form.vinculoRotulo = a.NegocioTitulo ?? `Negócio #${a.NegocioId}` }
}

interface OpcaoSimples { id: number; nome: string }
// --- Busca assíncrona de vínculo ---------------------------------------
const buscaVinculo = ref('')
const resultados = ref<OpcaoSimples[]>([])
const buscandoVinculo = ref(false)
const mostrarResultados = ref(false)
let timer: number | undefined
let ctrl: AbortController | null = null

watch(() => form.vinculo, () => {
  form.vinculoId = null
  form.vinculoRotulo = ''
  buscaVinculo.value = ''
  resultados.value = []
})

watch(buscaVinculo, (q) => {
  if (timer) clearTimeout(timer)
  if (form.vinculo === 'nenhum' || q.trim().length < 2) { resultados.value = []; mostrarResultados.value = false; return }
  timer = window.setTimeout(() => buscarVinculo(q.trim()), 250)
})

async function buscarVinculo(q: string) {
  ctrl?.abort()
  ctrl = new AbortController()
  buscandoVinculo.value = true
  mostrarResultados.value = true
  const termo = q.replace(/'/g, "''")
  const conf = {
    cliente: { ent: 'Clientes', campo: 'Nome', sel: 'Id,Nome' },
    contato: { ent: 'PessoasContato', campo: 'Nome', sel: 'Id,Nome' },
    negocio: { ent: 'Negocios', campo: 'Titulo', sel: 'Id,Titulo' },
  }[form.vinculo as Exclude<Vinculo, 'nenhum'>]
  const filtro = encodeURIComponent(`contains(${conf.campo},'${termo}')`)
  try {
    const r = await odataRequest<{ value: Record<string, any>[] }>(
      'GET', `${conf.ent}?$select=${conf.sel}&$filter=${filtro}&$top=8`,
    )
    resultados.value = r.value.map((x) => ({ id: x.Id, nome: x.Nome ?? x.Titulo ?? `#${x.Id}` }))
  } catch (e) {
    if (e instanceof ApiError) resultados.value = []
  } finally {
    buscandoVinculo.value = false
  }
}

function escolherVinculo(o: OpcaoSimples) {
  form.vinculoId = o.id
  form.vinculoRotulo = o.nome
  buscaVinculo.value = o.nome
  mostrarResultados.value = false
}

function limparVinculo() {
  form.vinculoId = null
  form.vinculoRotulo = ''
  buscaVinculo.value = ''
  resultados.value = []
}

onBeforeUnmount(() => { if (timer) clearTimeout(timer); ctrl?.abort() })

// --- Salvar / excluir ---------------------------------------------------
function montarVencimento(): string | null {
  if (!form.data) return null
  const hora = form.hora || '09:00'
  const [a, m, d] = form.data.split('-').map(Number)
  const [hh, mm] = hora.split(':').map(Number)
  return new Date(a!, (m! - 1), d!, hh!, mm!).toISOString()
}

async function salvar() {
  if (!form.titulo.trim()) { erroTitulo.value = true; return }
  salvando.value = true
  const corpo: Record<string, unknown> = {
    Titulo: form.titulo.trim(),
    Tipo: form.tipo,
    DataVencimento: montarVencimento(),
    Descricao: form.descricao.trim() || null,
    ClienteId: form.vinculo === 'cliente' ? form.vinculoId : null,
    PessoaContatoId: form.vinculo === 'contato' ? form.vinculoId : null,
    NegocioId: form.vinculo === 'negocio' ? form.vinculoId : null,
  }
  try {
    const r = props.atividade
      ? await odataRequest<Record<string, any>>('PATCH', `Atividades(${props.atividade.Id})`, corpo)
      : await odataRequest<Record<string, any>>('POST', 'Atividades', corpo)
    toast.success(props.atividade ? 'Atividade atualizada.' : 'Atividade criada.')
    const salva: Atividade = {
      Id: r.Id ?? props.atividade?.Id ?? 0,
      Titulo: corpo.Titulo as string,
      Descricao: corpo.Descricao as string | null,
      Tipo: form.tipo,
      DataVencimento: corpo.DataVencimento ? new Date(corpo.DataVencimento as string) : null,
      ConcluidaEm: props.atividade?.ConcluidaEm ?? null,
      Concluida: props.atividade?.Concluida ?? false,
      ClienteId: corpo.ClienteId as number | null,
      PessoaContatoId: corpo.PessoaContatoId as number | null,
      NegocioId: corpo.NegocioId as number | null,
      ClienteNome: form.vinculo === 'cliente' ? form.vinculoRotulo : null,
      ContatoNome: form.vinculo === 'contato' ? form.vinculoRotulo : null,
      NegocioTitulo: form.vinculo === 'negocio' ? form.vinculoRotulo : null,
    }
    emit('salvo', salva)
    fechar()
  } catch (e) {
    toast.error(e instanceof ApiError ? e.message : 'Não foi possível salvar a atividade.')
  } finally {
    salvando.value = false
  }
}

async function excluir() {
  if (!props.atividade) return
  excluindo.value = true
  try {
    await odataRequest('DELETE', `Atividades(${props.atividade.Id})`)
    toast.success('Atividade excluída.')
    emit('excluido', props.atividade.Id)
    confirmarExclusao.value = false
    fechar()
  } catch (e) {
    toast.error(e instanceof ApiError ? e.message : 'Não foi possível excluir.')
  } finally {
    excluindo.value = false
  }
}

function fechar() { emit('update:modelValue', false) }

const placeholderVinculo = computed(() => ({
  nenhum: '', cliente: 'Buscar cliente…', contato: 'Buscar contato…', negocio: 'Buscar negócio…',
}[form.vinculo]))
</script>

<template>
  <UiSlideOver :model-value="modelValue" @update:model-value="fechar"
    :titulo="editando ? 'Editar atividade' : 'Nova atividade'" largura="480px">
    <form class="af" @submit.prevent="salvar">
      <label class="campo">
        <span>Título *</span>
        <input v-model="form.titulo" :class="{ erro: erroTitulo }" @input="erroTitulo = false"
          placeholder="Ex.: Ligar para o cliente" autofocus />
        <small v-if="erroTitulo" class="msg-erro">Informe um título.</small>
      </label>

      <label class="campo">
        <span>Tipo</span>
        <div class="tipos">
          <button v-for="t in TIPOS" :key="t" type="button" class="chip-tipo"
            :class="{ ativo: form.tipo === t }" :style="form.tipo === t ? { borderColor: TIPO_META[t].cor, color: TIPO_META[t].cor } : {}"
            @click="form.tipo = t">
            <i class="mdi" :class="TIPO_META[t].icone"></i> {{ TIPO_META[t].rotulo }}
          </button>
        </div>
      </label>

      <div class="linha">
        <label class="campo">
          <span>Vencimento</span>
          <input type="date" v-model="form.data" />
        </label>
        <label class="campo">
          <span>Hora</span>
          <input type="time" v-model="form.hora" :disabled="!form.data" />
        </label>
      </div>

      <label class="campo">
        <span>Vínculo</span>
        <select v-model="form.vinculo">
          <option value="nenhum">— Sem vínculo —</option>
          <option value="cliente">Cliente</option>
          <option value="contato">Contato</option>
          <option value="negocio">Negócio</option>
        </select>
      </label>

      <div class="campo busca-vinc" v-if="form.vinculo !== 'nenhum'">
        <div class="vinc-input">
          <input v-model="buscaVinculo" :placeholder="placeholderVinculo"
            @focus="mostrarResultados = resultados.length > 0" autocomplete="off" />
          <button v-if="form.vinculoId" type="button" class="limpar" aria-label="Limpar vínculo" @click="limparVinculo">
            <i class="mdi mdi-close"></i>
          </button>
        </div>
        <div v-if="mostrarResultados" class="vinc-lista">
          <div v-if="buscandoVinculo" class="vinc-item muted"><i class="mdi mdi-loading mdi-spin"></i> Buscando…</div>
          <div v-else-if="!resultados.length" class="vinc-item muted">Nada encontrado.</div>
          <button v-for="o in resultados" :key="o.id" type="button" class="vinc-item" @click="escolherVinculo(o)">
            {{ o.nome }}
          </button>
        </div>
        <small v-if="form.vinculoId" class="sel-vinc"><i class="mdi mdi-check"></i> {{ form.vinculoRotulo }}</small>
      </div>

      <label class="campo">
        <span>Descrição</span>
        <textarea v-model="form.descricao" rows="4" placeholder="Detalhes da atividade…"></textarea>
      </label>
    </form>

    <template #rodape>
      <button v-if="editando" type="button" class="btn-txt danger" @click="confirmarExclusao = true">
        <i class="mdi mdi-trash-can-outline"></i> Excluir
      </button>
      <span class="espaco"></span>
      <button type="button" class="btn-txt" @click="fechar">Cancelar</button>
      <button type="button" class="btn-primario" :disabled="salvando" @click="salvar">
        <i v-if="salvando" class="mdi mdi-loading mdi-spin"></i>
        {{ editando ? 'Salvar' : 'Criar' }}
      </button>
    </template>
  </UiSlideOver>

  <UiModal v-model="confirmarExclusao" titulo="Excluir atividade" largura="420px">
    <p>Deseja excluir <strong>{{ atividade?.Titulo }}</strong>? Esta ação não pode ser desfeita.</p>
    <template #rodape>
      <button type="button" class="btn-txt" @click="confirmarExclusao = false">Cancelar</button>
      <button type="button" class="btn-primario danger" :disabled="excluindo" @click="excluir">
        <i v-if="excluindo" class="mdi mdi-loading mdi-spin"></i> Excluir
      </button>
    </template>
  </UiModal>
</template>

<style scoped>
.af { display: flex; flex-direction: column; gap: 16px; }
.campo { display: flex; flex-direction: column; gap: 6px; }
.campo > span { font-size: 12px; color: var(--text-secondary); font-weight: 600; }
.linha { display: grid; grid-template-columns: 1fr 120px; gap: 12px; }
input, select, textarea {
  background: var(--bg-primary); border: 1px solid var(--border); border-radius: 8px;
  padding: 9px 11px; color: var(--text-primary); font-size: 13px; outline: none; width: 100%;
  font-family: inherit;
}
input:focus, select:focus, textarea:focus { border-color: var(--accent); }
input.erro { border-color: var(--danger); }
textarea { resize: vertical; }
.msg-erro { color: var(--danger); font-size: 11px; }
.nota { color: var(--text-muted); font-size: 11px; }
.tipos { display: flex; flex-wrap: wrap; gap: 6px; }
.chip-tipo {
  display: inline-flex; align-items: center; gap: 5px; padding: 6px 10px; font-size: 12px;
  border: 1px solid var(--border); border-radius: 20px; background: var(--bg-primary);
  color: var(--text-secondary); cursor: pointer; transition: all .15s;
}
.chip-tipo:hover { background: var(--bg-elevated); }
.chip-tipo.ativo { background: var(--bg-elevated); font-weight: 600; }
.busca-vinc { position: relative; }
.vinc-input { position: relative; display: flex; align-items: center; }
.vinc-input .limpar {
  position: absolute; right: 6px; background: none; border: 0; color: var(--text-muted);
  cursor: pointer; font-size: 16px; display: flex;
}
.vinc-lista {
  position: absolute; top: 100%; left: 0; right: 0; z-index: 20; margin-top: 4px;
  background: var(--bg-elevated); border: 1px solid var(--border); border-radius: 8px;
  max-height: 220px; overflow-y: auto; box-shadow: 0 8px 24px rgba(0, 0, 0, .3);
}
.vinc-item {
  display: block; width: 100%; text-align: left; padding: 8px 11px; font-size: 13px;
  border: 0; background: none; color: var(--text-primary); cursor: pointer;
}
.vinc-item:hover { background: var(--bg-surface); }
.vinc-item.muted { color: var(--text-muted); cursor: default; }
.sel-vinc { color: #22c55e; font-size: 12px; display: inline-flex; gap: 4px; align-items: center; }
.espaco { flex: 1; }
.btn-txt {
  background: none; border: 0; color: var(--text-secondary); font-size: 13px; cursor: pointer;
  padding: 8px 12px; border-radius: 8px; display: inline-flex; align-items: center; gap: 5px;
}
.btn-txt:hover { background: var(--bg-elevated); color: var(--text-primary); }
.btn-txt.danger { color: var(--danger); }
.btn-txt.danger:hover { background: rgba(239, 68, 68, .1); }
.btn-primario {
  background: var(--accent); color: #000; border: 0; border-radius: 8px; padding: 8px 16px;
  font-size: 13px; font-weight: 600; cursor: pointer; display: inline-flex; align-items: center; gap: 5px;
}
.btn-primario:hover { background: var(--accent-hover); }
.btn-primario:disabled { opacity: .6; cursor: default; }
.btn-primario.danger { background: var(--danger); color: #fff; }
</style>
