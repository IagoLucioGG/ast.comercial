<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'
import { odataRequest, montarQuery } from '@/services/api'
import { useFunisStore } from '@/stores/funis'
import { extrairLista, lerCampo, literalOData } from '@/composables/useFunilFormato'
import { useToast } from '@/composables/useToast'

const props = defineProps<{ modelValue: boolean; funilId: number | null; etapaId?: number | null }>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean]; criado: [etapaId: number] }>()

const store = useFunisStore()
const toast = useToast()

const titulo = ref('')
const valor = ref<string>('')
const moedaId = ref<number | null>(null)
const etapaSel = ref<number | null>(null)
const cliente = ref<{ id: number; nome: string } | null>(null)
const buscaCliente = ref('')
const resultados = ref<{ id: number; nome: string }[]>([])
const buscando = ref(false)
const salvando = ref(false)
const tentou = ref(false)
const statusAbertoId = ref<number | null>(null)
let timer: ReturnType<typeof setTimeout> | undefined
let seq = 0

const etapas = computed(() => store.funilPorId(props.funilId)?.etapas ?? [])

watch(() => props.modelValue, async (aberto) => {
  if (!aberto) return
  titulo.value = ''
  valor.value = ''
  cliente.value = null
  buscaCliente.value = ''
  resultados.value = []
  tentou.value = false
  etapaSel.value = props.etapaId ?? etapas.value[0]?.id ?? null
  moedaId.value = (store.moedas.find((m) => m.padrao) ?? store.moedas.find((m) => m.codigo === 'BRL') ?? store.moedas[0])?.id ?? null
  if (statusAbertoId.value === null) {
    try {
      const r = await odataRequest<unknown>('GET', 'Negocios@Status')
      const statusAberto = extrairLista(r).find((s) => Number(lerCampo(s, 'TipoBase')) === 0)
      statusAbertoId.value = statusAberto ? Number(lerCampo(statusAberto, 'Id')) : null
    } catch { /* o negócio é criado mesmo sem status explícito */ }
  }
})

function aoDigitarCliente() {
  cliente.value = null
  clearTimeout(timer)
  const termo = buscaCliente.value.trim()
  if (termo.length < 2) { resultados.value = []; return }
  timer = setTimeout(() => buscar(termo), 300)
}

async function buscar(termo: string) {
  const minha = ++seq
  buscando.value = true
  try {
    const q = montarQuery({
      $select: 'Id,Nome',
      $top: 8,
      $orderby: 'Nome asc',
      $filter: `Ativo eq true and contains(tolower(Nome),'${literalOData(termo.toLowerCase())}')`,
    })
    const r = await odataRequest<unknown>('GET', `Clientes${q}`)
    if (minha !== seq) return
    resultados.value = extrairLista(r).map((c) => ({ id: Number(lerCampo(c, 'Id')), nome: String(lerCampo(c, 'Nome') ?? '') }))
  } catch {
    if (minha === seq) resultados.value = []
  } finally {
    if (minha === seq) buscando.value = false
  }
}

function escolher(c: { id: number; nome: string }) {
  cliente.value = c
  buscaCliente.value = c.nome
  resultados.value = []
}

const erros = computed(() => ({
  titulo: !titulo.value.trim(),
  cliente: !cliente.value,
  etapa: !etapaSel.value,
}))

async function salvar() {
  tentou.value = true
  if (erros.value.titulo || erros.value.cliente || erros.value.etapa || !props.funilId) return
  const numero = valor.value.trim() === '' ? null : Number(valor.value.replace(/\./g, '').replace(',', '.'))
  if (numero !== null && Number.isNaN(numero)) { toast.error('Valor inválido.'); return }
  salvando.value = true
  try {
    const corpo: Record<string, unknown> = {
      Titulo: titulo.value.trim(),
      ClienteId: cliente.value!.id,
      FunilId: props.funilId,
      EtapaId: etapaSel.value,
    }
    if (numero !== null) corpo.Valor = numero
    if (moedaId.value) corpo.MoedaId = moedaId.value
    if (statusAbertoId.value) corpo.StatusId = statusAbertoId.value
    await odataRequest('POST', 'Negocios', corpo)
    toast.success('Negócio criado.')
    emit('criado', etapaSel.value!)
    emit('update:modelValue', false)
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível criar o negócio.')
  } finally {
    salvando.value = false
  }
}

onBeforeUnmount(() => clearTimeout(timer))
</script>

<template>
  <UiModal :model-value="modelValue" titulo="Novo negócio" largura="480px" @update:model-value="emit('update:modelValue', $event)">
    <form class="nn-form" @submit.prevent="salvar">
      <label class="nn-campo">
        <span>Título *</span>
        <input v-model="titulo" type="text" maxlength="200" :aria-invalid="tentou && erros.titulo" autofocus />
        <small v-if="tentou && erros.titulo" class="nn-erro" role="alert">Informe o título.</small>
      </label>

      <div class="nn-campo nn-cliente">
        <label for="nn-cliente">Cliente *</label>
        <input id="nn-cliente" v-model="buscaCliente" type="text" autocomplete="off" placeholder="Digite para buscar..." role="combobox"
          aria-autocomplete="list" :aria-expanded="resultados.length > 0" :aria-invalid="tentou && erros.cliente" @input="aoDigitarCliente" />
        <ul v-if="resultados.length" class="nn-lista" role="listbox">
          <li v-for="c in resultados" :key="c.id" role="option">
            <button type="button" @click="escolher(c)">{{ c.nome }}</button>
          </li>
        </ul>
        <small v-else-if="buscando" class="nn-dica">Buscando...</small>
        <small v-else-if="buscaCliente.trim().length >= 2 && !cliente" class="nn-dica">Nenhum cliente encontrado.</small>
        <small v-if="tentou && erros.cliente" class="nn-erro" role="alert">Selecione um cliente da lista.</small>
      </div>

      <div class="nn-linha">
        <label class="nn-campo">
          <span>Valor</span>
          <input v-model="valor" type="text" inputmode="decimal" placeholder="0,00" />
        </label>
        <label class="nn-campo">
          <span>Moeda</span>
          <select v-model="moedaId">
            <option v-if="!store.moedas.length" :value="null">Padrão</option>
            <option v-for="m in store.moedas" :key="m.id" :value="m.id">{{ m.codigo }}{{ m.simbolo ? ` (${m.simbolo})` : '' }}</option>
          </select>
        </label>
      </div>

      <label class="nn-campo">
        <span>Etapa *</span>
        <select v-model="etapaSel" :aria-invalid="tentou && erros.etapa">
          <option v-for="e in etapas" :key="e.id" :value="e.id">{{ e.nome }}</option>
        </select>
      </label>
      <button type="submit" hidden>Salvar</button>
    </form>
    <template #rodape>
      <button class="nn-btn" type="button" @click="emit('update:modelValue', false)">Cancelar</button>
      <button class="nn-btn nn-primario" type="button" :disabled="salvando" @click="salvar">{{ salvando ? 'Salvando...' : 'Criar negócio' }}</button>
    </template>
  </UiModal>
</template>

<style scoped>
.nn-form { display: flex; flex-direction: column; gap: 14px; }
.nn-campo { display: flex; flex-direction: column; gap: 6px; position: relative; font-size: 12px; color: var(--text-secondary); }
.nn-campo input, .nn-campo select { padding: 9px 12px; border-radius: 8px; border: 1px solid var(--border); background: var(--bg-primary); color: var(--text-primary); font-size: 13px; }
.nn-campo input:focus, .nn-campo select:focus { outline: none; border-color: var(--accent); }
.nn-campo [aria-invalid='true'] { border-color: var(--danger); }
.nn-linha { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.nn-erro { color: var(--danger); font-size: 11px; }
.nn-dica { color: var(--text-muted); font-size: 11px; }
.nn-lista { position: absolute; top: 100%; left: 0; right: 0; z-index: 5; margin-top: 4px; list-style: none; padding: 4px; background: var(--bg-elevated); border: 1px solid var(--border); border-radius: 8px; box-shadow: 0 8px 24px rgba(0, 0, 0, .35); max-height: 220px; overflow-y: auto; }
.nn-lista button { width: 100%; text-align: left; padding: 8px 10px; border: 0; background: none; border-radius: 6px; color: var(--text-primary); font-size: 13px; cursor: pointer; }
.nn-lista button:hover, .nn-lista button:focus-visible { background: var(--bg-surface); outline: none; color: var(--accent); }
.nn-btn { padding: 8px 16px; border-radius: 8px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); font-size: 12px; font-weight: 600; cursor: pointer; }
.nn-btn:hover { background: var(--bg-elevated); color: var(--text-primary); }
.nn-primario { background: var(--accent); border-color: var(--accent); color: #000; }
.nn-primario:hover { background: var(--accent-hover); color: #000; }
.nn-btn:disabled { opacity: .6; cursor: default; }
</style>
