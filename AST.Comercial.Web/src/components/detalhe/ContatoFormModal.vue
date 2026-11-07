<script setup lang="ts">
import { ref, watch } from 'vue'
import { odataRequest, montarQuery } from '@/services/api'
import { campo, lista, mensagemErro, lit, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import UiModal from '@/components/ui/UiModal.vue'

/**
 * Criar/editar pessoa de contato. Se `clienteId` vier preenchido o cliente é fixo;
 * caso contrário o usuário escolhe o cliente por busca.
 */
const props = defineProps<{
  modelValue: boolean
  contato?: Registro | null
  clienteId?: number | null
}>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean]; salvo: [] }>()

const toast = useToast()
const salvando = ref(false)
const form = ref({ Nome: '', Email: '', Telefone: '', Cargo: '', Documento: '', Observacoes: '', Decisor: false })
const clienteEscolhido = ref<{ id: number; nome: string } | null>(null)
const buscaCliente = ref('')
const resultados = ref<{ id: number; nome: string }[]>([])
const buscando = ref(false)
let timer: ReturnType<typeof setTimeout> | undefined

const editando = () => !!props.contato

watch(() => props.modelValue, async (aberto) => {
  if (!aberto) return
  const c = props.contato
  form.value = {
    Nome: campo(c, 'Nome') ?? '', Email: campo(c, 'Email') ?? '', Telefone: campo(c, 'Telefone') ?? '',
    Cargo: campo(c, 'Cargo') ?? '', Documento: campo(c, 'Documento') ?? '', Observacoes: campo(c, 'Observacoes') ?? '',
    Decisor: !!campo(c, 'Decisor'),
  }
  buscaCliente.value = ''
  resultados.value = []
  clienteEscolhido.value = null
  const idCliente = props.clienteId ?? campo<number>(c, 'ClienteId')
  if (idCliente) {
    try {
      const r = lista<Registro>(await odataRequest<unknown>('GET', `Clientes${montarQuery({ $filter: `Id eq ${idCliente}`, $select: 'Id,Nome', $top: 1 })}`))
      if (r[0]) clienteEscolhido.value = { id: Number(r[0].Id), nome: String(r[0].Nome) }
    } catch { /* o nome é só informativo */ }
  }
}, { immediate: true })

function buscar() {
  clearTimeout(timer)
  const termo = buscaCliente.value.trim()
  if (termo.length < 2) { resultados.value = []; return }
  timer = setTimeout(async () => {
    buscando.value = true
    try {
      const q = montarQuery({ $filter: `contains(tolower(Nome),'${lit(termo.toLowerCase())}')`, $select: 'Id,Nome', $orderby: 'Nome asc', $top: 8 })
      resultados.value = lista<Registro>(await odataRequest<unknown>('GET', `Clientes${q}`)).map((r) => ({ id: Number(r.Id), nome: String(r.Nome) }))
    } catch (e) {
      toast.error(mensagemErro(e, 'Não foi possível buscar clientes'))
    } finally {
      buscando.value = false
    }
  }, 250)
}

function escolher(c: { id: number; nome: string }) {
  clienteEscolhido.value = c
  resultados.value = []
  buscaCliente.value = ''
}

function fechar() { emit('update:modelValue', false) }

async function salvar() {
  const nome = form.value.Nome.trim()
  if (!nome) { toast.error('Informe o nome do contato.'); return }
  const cliente = clienteEscolhido.value?.id ?? props.clienteId
  if (!cliente) { toast.error('Escolha o cliente do contato.'); return }
  const vazioNulo = (s: string) => (s.trim() === '' ? null : s.trim())
  const corpo = {
    Nome: nome,
    Email: vazioNulo(form.value.Email),
    Telefone: vazioNulo(form.value.Telefone),
    Cargo: vazioNulo(form.value.Cargo),
    Documento: vazioNulo(form.value.Documento),
    Observacoes: vazioNulo(form.value.Observacoes),
    Decisor: form.value.Decisor,
    ClienteId: cliente,
  }
  salvando.value = true
  try {
    if (editando()) await odataRequest('PATCH', `PessoasContato(${campo(props.contato, 'Id')})`, corpo)
    else await odataRequest('POST', 'PessoasContato', corpo)
    toast.success(editando() ? 'Contato atualizado.' : 'Contato criado.')
    emit('salvo')
    fechar()
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível salvar o contato'))
  } finally {
    salvando.value = false
  }
}
</script>

<template>
  <UiModal :model-value="modelValue" :titulo="editando() ? 'Editar contato' : 'Novo contato'" largura="560px" @update:model-value="emit('update:modelValue', $event)">
    <form id="form-contato" class="cf" @submit.prevent="salvar">
      <div class="cf-campo cf-largo">
        <label for="cf-cliente">Cliente</label>
        <div v-if="clienteEscolhido" class="cf-cliente">
          <i class="mdi mdi-domain"></i> <span>{{ clienteEscolhido.nome }}</span>
          <button v-if="!clienteId" type="button" class="cf-trocar" @click="clienteEscolhido = null">Trocar</button>
        </div>
        <template v-else>
          <input id="cf-cliente" v-model="buscaCliente" type="text" placeholder="Buscar cliente pelo nome..." autocomplete="off" @input="buscar" />
          <ul v-if="resultados.length || buscando" class="cf-res" role="listbox">
            <li v-if="buscando" class="cf-vazio">Buscando...</li>
            <li v-for="r in resultados" :key="r.id" role="option"><button type="button" @click="escolher(r)">{{ r.nome }}</button></li>
          </ul>
        </template>
      </div>
      <div class="cf-campo cf-largo"><label for="cf-nome" class="obrig">Nome</label><input id="cf-nome" v-model="form.Nome" type="text" maxlength="200" required /></div>
      <div class="cf-campo"><label for="cf-email">E-mail</label><input id="cf-email" v-model="form.Email" type="email" /></div>
      <div class="cf-campo"><label for="cf-tel">Telefone</label><input id="cf-tel" v-model="form.Telefone" type="text" /></div>
      <div class="cf-campo"><label for="cf-cargo">Cargo</label><input id="cf-cargo" v-model="form.Cargo" type="text" /></div>
      <div class="cf-campo"><label for="cf-doc">Documento (CPF)</label><input id="cf-doc" v-model="form.Documento" type="text" /></div>
      <label class="cf-check cf-largo"><input v-model="form.Decisor" type="checkbox" /> Decisor</label>
      <div class="cf-campo cf-largo"><label for="cf-obs">Observações</label><textarea id="cf-obs" v-model="form.Observacoes" rows="3"></textarea></div>
    </form>
    <template #rodape>
      <button class="cf-btn" type="button" :disabled="salvando" @click="fechar">Cancelar</button>
      <button class="cf-btn cf-primario" type="submit" form="form-contato" :disabled="salvando">{{ salvando ? 'Salvando...' : 'Salvar' }}</button>
    </template>
  </UiModal>
</template>

<style scoped>
.cf { display: grid; grid-template-columns: 1fr 1fr; gap: 14px; }
.cf-largo { grid-column: 1 / -1; }
.cf-campo { display: flex; flex-direction: column; gap: 5px; position: relative; }
.cf-campo label { font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: .4px; color: var(--text-muted); }
.cf-campo label.obrig::after { content: ' *'; color: var(--danger); }
.cf input[type='text'], .cf input[type='email'], .cf textarea { width: 100%; box-sizing: border-box; padding: 9px 11px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-primary); font-size: 14px; font-family: inherit; outline: none; }
.cf input:focus, .cf textarea:focus { border-color: var(--accent); }
.cf-check { display: flex; align-items: center; gap: 8px; font-size: 14px; color: var(--text-primary); }
.cf-cliente { display: flex; align-items: center; gap: 8px; padding: 9px 11px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-primary); font-size: 14px; }
.cf-trocar { margin-left: auto; background: none; border: 0; color: var(--accent); font-size: 12px; cursor: pointer; }
.cf-res { list-style: none; margin: 0; padding: 4px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-elevated); }
.cf-res button { width: 100%; text-align: left; padding: 7px 10px; background: none; border: 0; border-radius: 6px; color: var(--text-primary); font-size: 13px; cursor: pointer; }
.cf-res button:hover { background: var(--bg-surface); }
.cf-vazio { padding: 7px 10px; font-size: 12px; color: var(--text-muted); }
.cf-btn { padding: 8px 16px; border: 1px solid var(--border); border-radius: 8px; background: transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.cf-btn:hover:not(:disabled) { background: var(--bg-elevated); }
.cf-btn:disabled { opacity: .5; cursor: not-allowed; }
.cf-primario { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.cf-primario:hover:not(:disabled) { background: var(--accent-hover); }
@media (max-width: 560px) { .cf { grid-template-columns: 1fr; } }
</style>
