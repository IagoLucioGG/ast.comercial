<script setup lang="ts">
import { ref, watch } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'
import BuscaAssincrona from './BuscaAssincrona.vue'
import { odataRequest, montarQuery } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { buscarClientes, buscarNegocios, listar, type Proposta } from '@/composables/usePropostaApi'

const props = defineProps<{
  modelValue: boolean
  negocioId?: number | null
  clienteId?: number | null
}>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean]; criada: [id: number, modeloId: number | null] }>()

const toast = useToast()
const titulo = ref('')
const clienteId = ref<number | null>(null)
const clienteNome = ref('')
const negocioId = ref<number | null>(null)
const negocioNome = ref('')
const modeloId = ref<number | null>(null)
const modelos = ref<{ Id: number; Nome: string }[]>([])
const salvando = ref(false)
const erro = ref('')

async function carregarModelos() {
  try {
    const r = await listar<{ Id: number; Nome: string }>(`ModelosDocumento${montarQuery({ $select: 'Id,Nome', $orderby: 'Nome', $top: 100 })}`)
    modelos.value = r
  } catch {
    modelos.value = []
  }
}

async function preencherDaRota() {
  try {
    if (props.negocioId) {
      const n = await odataRequest<{ Id: number; Titulo: string; ClienteId: number }>('GET', `Negocios(${props.negocioId})${montarQuery({ $select: 'Id,Titulo,ClienteId' })}`)
      negocioId.value = n.Id
      negocioNome.value = n.Titulo
      if (!clienteId.value && n.ClienteId) await carregarCliente(n.ClienteId)
      if (!titulo.value) titulo.value = `Proposta - ${n.Titulo}`
    }
    if (props.clienteId && !clienteId.value) await carregarCliente(props.clienteId)
  } catch {
    toast.warning('Não foi possível carregar o negócio/cliente informado.')
  }
}

async function carregarCliente(id: number) {
  const c = await odataRequest<{ Id: number; Nome: string }>('GET', `Clientes(${id})${montarQuery({ $select: 'Id,Nome' })}`)
  clienteId.value = c.Id
  clienteNome.value = c.Nome
}

watch(() => props.modelValue, (aberto) => {
  if (!aberto) return
  titulo.value = ''
  clienteId.value = null; clienteNome.value = ''
  negocioId.value = null; negocioNome.value = ''
  modeloId.value = null
  erro.value = ''
  carregarModelos()
  preencherDaRota()
})

function fechar() { emit('update:modelValue', false) }

async function criar() {
  erro.value = ''
  if (!titulo.value.trim()) { erro.value = 'Informe o título.'; return }
  if (!clienteId.value) { erro.value = 'Selecione o cliente.'; return }
  salvando.value = true
  try {
    const nova = await odataRequest<Proposta>('POST', 'Propostas', {
      Titulo: titulo.value.trim(),
      ClienteId: clienteId.value,
      NegocioId: negocioId.value ?? undefined,
      StatusAprovacao: 'Pendente',
      Data: new Date().toISOString(),
    })
    // O servidor não gera o número: usa o Id (único por proposta) como Numero; exibido como PRO-000042
    if (!nova.Numero) {
      try { await odataRequest('PATCH', `Propostas(${nova.Id})`, { Numero: nova.Id }) } catch { /* o detalhe usa o Id como alternativa */ }
    }
    toast.success('Proposta criada.')
    emit('criada', nova.Id, modeloId.value)
    fechar()
  } catch (e) {
    erro.value = e instanceof Error ? e.message : 'Falha ao criar a proposta.'
    toast.error(erro.value)
  } finally {
    salvando.value = false
  }
}
</script>

<template>
  <UiModal :model-value="modelValue" titulo="Nova proposta" largura="480px" @update:model-value="emit('update:modelValue', $event)">
    <form class="form" @submit.prevent="criar">
      <label class="campo">
        <span>Título *</span>
        <input v-model="titulo" type="text" maxlength="200" placeholder="Ex.: Proposta comercial" />
      </label>
      <div class="campo">
        <span>Cliente *</span>
        <BuscaAssincrona rotulo="Buscar cliente" placeholder="Digite para buscar o cliente" :texto="clienteNome" :buscar="buscarClientes"
          @selecionar="(o) => { clienteId = o.id; clienteNome = o.texto }" />
      </div>
      <div class="campo">
        <span>Negócio (opcional)</span>
        <BuscaAssincrona rotulo="Buscar negócio" placeholder="Digite para buscar o negócio" :texto="negocioNome" :buscar="buscarNegocios"
          @selecionar="(o) => { negocioId = o.id; negocioNome = o.texto; const x = o.extra as { ClienteId?: number } | undefined; if (x?.ClienteId && !clienteId) carregarCliente(x.ClienteId) }" />
      </div>
      <label class="campo">
        <span>Modelo de documento (opcional)</span>
        <select v-model="modeloId">
          <option :value="null">Nenhum</option>
          <option v-for="m in modelos" :key="m.Id" :value="m.Id">{{ m.Nome }}</option>
        </select>
      </label>
      <p v-if="erro" class="erro" role="alert">{{ erro }}</p>
    </form>
    <template #rodape>
      <button type="button" class="btn" @click="fechar">Cancelar</button>
      <button type="button" class="btn primario" :disabled="salvando" @click="criar">{{ salvando ? 'Criando...' : 'Criar e abrir' }}</button>
    </template>
  </UiModal>
</template>

<style scoped>
.form { display: flex; flex-direction: column; gap: 14px; }
.campo { display: flex; flex-direction: column; gap: 5px; }
.campo > span { font-size: 12px; color: var(--text-secondary); }
.campo input, .campo select { padding: 7px 10px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); font-size: 13px; }
.campo input:focus, .campo select:focus { outline: none; border-color: var(--accent); }
.erro { color: var(--danger); font-size: 12px; margin: 0; }
.btn { padding: 7px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; }
.btn.primario { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.btn:disabled { opacity: .6; cursor: default; }
</style>
