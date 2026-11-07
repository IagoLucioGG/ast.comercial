<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { odataRequest, odataAction, montarQuery } from '@/services/api'
import { campo, lista, mensagemErro, formatarData, TIPOS_ATIVIDADE, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import DetalheEstado from './DetalheEstado.vue'

/** Atividades de um negócio ou cliente: lista, criação rápida e conclusão. */
const props = defineProps<{
  endpoint: 'Negocios' | 'Clientes'
  paiId: number
  /** Ids de vínculo gravados na nova atividade. */
  vinculo: { ClienteId?: number; NegocioId?: number }
}>()
const emit = defineEmits<{ alterado: [] }>()

const toast = useToast()
const itens = ref<Registro[]>([])
const carregando = ref(false)
const erro = ref('')
const criando = ref(false)
const concluindo = ref<number | null>(null)
const novo = ref({ titulo: '', tipo: 'Tarefa', vencimento: '' })

const TIPOS_CRIACAO = ['Tarefa', 'Ligacao', 'Email', 'Reuniao', 'Nota']

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({
      $filter: `Id eq ${props.paiId}`,
      $top: 1,
      $expand: 'Atividades($orderby=DataVencimento asc;$top=100)',
    })
    const res = lista<Registro>(await odataRequest<unknown>('GET', `${props.endpoint}${q}`))
    itens.value = lista<Registro>(res[0]?.Atividades ?? [])
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar as atividades')
  } finally {
    carregando.value = false
  }
}

const pendentes = computed(() => itens.value.filter((a) => !campo(a, 'Concluida')))
const concluidas = computed(() =>
  itens.value
    .filter((a) => !!campo(a, 'Concluida'))
    .sort((a, b) => new Date(String(campo(b, 'ConcluidaEm') ?? 0)).getTime() - new Date(String(campo(a, 'ConcluidaEm') ?? 0)).getTime()),
)

function atrasada(a: Registro): boolean {
  const v = campo<string>(a, 'DataVencimento')
  return !!v && !campo(a, 'Concluida') && new Date(v).getTime() < Date.now()
}

async function criar() {
  const titulo = novo.value.titulo.trim()
  if (!titulo) { toast.error('Informe o título da atividade.'); return }
  criando.value = true
  try {
    const corpo: Record<string, unknown> = { Titulo: titulo, Tipo: novo.value.tipo, ...props.vinculo }
    if (novo.value.vencimento) corpo.DataVencimento = `${novo.value.vencimento}T12:00:00Z`
    await odataRequest('POST', 'Atividades', corpo)
    novo.value = { titulo: '', tipo: novo.value.tipo, vencimento: '' }
    toast.success('Atividade criada.')
    await carregar()
    emit('alterado')
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível criar a atividade'))
  } finally {
    criando.value = false
  }
}

async function concluir(a: Registro) {
  const id = Number(campo(a, 'Id'))
  concluindo.value = id
  try {
    await odataAction('Atividades', id, 'Concluir')
    await carregar()
    emit('alterado')
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível concluir a atividade'))
  } finally {
    concluindo.value = null
  }
}

watch(() => [props.endpoint, props.paiId], carregar)
onMounted(carregar)
defineExpose({ carregar })
</script>

<template>
  <div>
    <form class="ae-nova" @submit.prevent="criar">
      <select v-model="novo.tipo" aria-label="Tipo da atividade">
        <option v-for="t in TIPOS_CRIACAO" :key="t" :value="t">{{ TIPOS_ATIVIDADE[t]?.nome }}</option>
      </select>
      <input v-model="novo.titulo" class="ae-titulo" type="text" placeholder="Nova atividade..." aria-label="Título da nova atividade" maxlength="200" />
      <input v-model="novo.vencimento" type="date" aria-label="Vencimento" />
      <button class="ae-add" type="submit" :disabled="criando"><i class="mdi mdi-plus"></i> {{ criando ? 'Criando...' : 'Adicionar' }}</button>
    </form>

    <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!itens.length" texto-vazio="Nenhuma atividade ainda. Crie a primeira acima." icone="mdi-calendar-check-outline" @tentar="carregar">
      <h4 v-if="pendentes.length" class="ae-sub">Pendentes ({{ pendentes.length }})</h4>
      <ul class="ae-lista">
        <li v-for="a in pendentes" :key="campo(a, 'Id')" class="ae-item" :class="{ atrasada: atrasada(a) }">
          <button class="ae-check" :disabled="concluindo === campo(a, 'Id')" :aria-label="`Concluir ${campo(a, 'Titulo')}`" @click="concluir(a)">
            <i class="mdi mdi-checkbox-blank-circle-outline"></i>
          </button>
          <i class="mdi ae-tipo" :class="TIPOS_ATIVIDADE[campo(a, 'Tipo')]?.icone ?? 'mdi-checkbox-marked-circle-outline'"></i>
          <div class="ae-corpo">
            <div class="ae-t">{{ campo(a, 'Titulo') }}</div>
            <div v-if="campo(a, 'Descricao')" class="ae-d">{{ campo(a, 'Descricao') }}</div>
          </div>
          <span class="ae-data" :class="{ atrasada: atrasada(a) }">
            <i v-if="atrasada(a)" class="mdi mdi-alert-outline"></i>{{ formatarData(campo(a, 'DataVencimento')) }}
          </span>
        </li>
      </ul>
      <h4 v-if="concluidas.length" class="ae-sub">Concluídas ({{ concluidas.length }})</h4>
      <ul class="ae-lista">
        <li v-for="a in concluidas" :key="campo(a, 'Id')" class="ae-item feita">
          <span class="ae-check ok" aria-hidden="true"><i class="mdi mdi-check-circle"></i></span>
          <i class="mdi ae-tipo" :class="TIPOS_ATIVIDADE[campo(a, 'Tipo')]?.icone ?? 'mdi-checkbox-marked-circle-outline'"></i>
          <div class="ae-corpo"><div class="ae-t">{{ campo(a, 'Titulo') }}</div></div>
          <span class="ae-data">{{ formatarData(campo(a, 'ConcluidaEm')) }}</span>
        </li>
      </ul>
    </DetalheEstado>
  </div>
</template>

<style scoped>
.ae-nova { display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 16px; }
.ae-nova select, .ae-nova input { padding: 8px 10px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-primary); font-size: 13px; outline: none; }
.ae-nova select:focus, .ae-nova input:focus { border-color: var(--accent); }
.ae-titulo { flex: 1; min-width: 180px; }
.ae-add { display: inline-flex; align-items: center; gap: 4px; padding: 8px 14px; border: 0; border-radius: 8px; background: var(--accent); color: #000; font-size: 13px; font-weight: 600; cursor: pointer; }
.ae-add:hover:not(:disabled) { background: var(--accent-hover); }
.ae-add:disabled { opacity: .5; cursor: not-allowed; }
.ae-sub { margin: 14px 0 8px; font-size: 12px; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); }
.ae-lista { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.ae-item { display: flex; align-items: center; gap: 10px; padding: 10px 12px; border: 1px solid var(--border); border-radius: 10px; background: var(--bg-surface); }
.ae-item.atrasada { border-color: rgba(239, 68, 68, .4); }
.ae-item.feita { opacity: .7; }
.ae-item.feita .ae-t { text-decoration: line-through; }
.ae-check { background: none; border: 0; padding: 0; font-size: 20px; line-height: 1; color: var(--text-muted); cursor: pointer; }
.ae-check:hover:not(:disabled) { color: #22c55e; }
.ae-check.ok { color: #22c55e; cursor: default; }
.ae-tipo { color: var(--text-secondary); font-size: 18px; }
.ae-corpo { flex: 1; min-width: 0; }
.ae-t { font-size: 13px; color: var(--text-primary); }
.ae-d { font-size: 12px; color: var(--text-muted); margin-top: 2px; }
.ae-data { font-size: 12px; color: var(--text-muted); white-space: nowrap; display: inline-flex; align-items: center; gap: 3px; }
.ae-data.atrasada { color: var(--danger); }
</style>
