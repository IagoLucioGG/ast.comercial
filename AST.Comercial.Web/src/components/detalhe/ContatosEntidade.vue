<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { odataRequest, montarQuery } from '@/services/api'
import { campo, lista, mensagemErro, inicialDe, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import UiBadge from '@/components/ui/UiBadge.vue'
import DetalheEstado from './DetalheEstado.vue'
import ContatoFormModal from './ContatoFormModal.vue'

/** Contatos de um cliente. Com `editavel` permite criar, editar e excluir. */
const props = defineProps<{
  clienteId: number
  editavel?: boolean
  /** Contato principal do negócio (destacado). */
  principalId?: number | null
}>()
const emit = defineEmits<{ alterado: [] }>()

const toast = useToast()
const itens = ref<Registro[]>([])
const carregando = ref(false)
const erro = ref('')
const modal = ref(false)
const editando = ref<Registro | null>(null)

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({ $filter: `ClienteId eq ${props.clienteId}`, $orderby: 'Nome asc', $top: 100 })
    itens.value = lista<Registro>(await odataRequest<unknown>('GET', `PessoasContato${q}`))
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar os contatos')
  } finally {
    carregando.value = false
  }
}

function abrirNovo() { editando.value = null; modal.value = true }
function abrirEditar(c: Registro) { editando.value = c; modal.value = true }

async function excluir(c: Registro) {
  if (!window.confirm(`Excluir o contato "${campo(c, 'Nome')}"?`)) return
  try {
    await odataRequest('DELETE', `PessoasContato(${campo(c, 'Id')})`)
    toast.success('Contato excluído.')
    await carregar()
    emit('alterado')
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível excluir o contato'))
  }
}

async function aoSalvar() {
  await carregar()
  emit('alterado')
}

watch(() => props.clienteId, carregar)
onMounted(carregar)
defineExpose({ carregar })
</script>

<template>
  <div>
    <div v-if="editavel" class="ce-barra">
      <button class="ce-novo" @click="abrirNovo"><i class="mdi mdi-plus"></i> Novo contato</button>
    </div>
    <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!itens.length" texto-vazio="Nenhum contato cadastrado para este cliente." icone="mdi-account-multiple-outline" @tentar="carregar">
      <ul class="ce-lista">
        <li v-for="c in itens" :key="campo(c, 'Id')" class="ce-item">
          <div class="ce-avatar" aria-hidden="true">{{ inicialDe(campo(c, 'Nome')) }}</div>
          <div class="ce-info">
            <div class="ce-nome">
              {{ campo(c, 'Nome') }}
              <UiBadge v-if="campo(c, 'Decisor')" tom="destaque">Decisor</UiBadge>
              <UiBadge v-if="principalId && principalId === campo(c, 'Id')" tom="info">Contato do negócio</UiBadge>
            </div>
            <div class="ce-sub">
              <span v-if="campo(c, 'Cargo')">{{ campo(c, 'Cargo') }}</span>
              <a v-if="campo(c, 'Email')" :href="`mailto:${campo(c, 'Email')}`">{{ campo(c, 'Email') }}</a>
              <span v-if="campo(c, 'Telefone')">{{ campo(c, 'Telefone') }}</span>
            </div>
          </div>
          <div v-if="editavel" class="ce-acoes">
            <button :aria-label="`Editar ${campo(c, 'Nome')}`" @click="abrirEditar(c)"><i class="mdi mdi-pencil-outline"></i></button>
            <button :aria-label="`Excluir ${campo(c, 'Nome')}`" class="del" @click="excluir(c)"><i class="mdi mdi-delete-outline"></i></button>
          </div>
        </li>
      </ul>
    </DetalheEstado>
    <ContatoFormModal v-model="modal" :contato="editando" :cliente-id="clienteId" @salvo="aoSalvar" />
  </div>
</template>

<style scoped>
.ce-barra { display: flex; justify-content: flex-end; margin-bottom: 12px; }
.ce-novo { display: inline-flex; align-items: center; gap: 4px; padding: 7px 14px; border: 0; border-radius: 8px; background: var(--accent); color: #000; font-size: 13px; font-weight: 600; cursor: pointer; }
.ce-novo:hover { background: var(--accent-hover); }
.ce-lista { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 8px; }
.ce-item { display: flex; align-items: center; gap: 12px; padding: 12px 14px; border: 1px solid var(--border); border-radius: 10px; background: var(--bg-surface); }
.ce-avatar { flex: 0 0 36px; height: 36px; border-radius: 50%; display: flex; align-items: center; justify-content: center; background: var(--bg-elevated); border: 1px solid var(--border); color: var(--accent); font-weight: 600; }
.ce-info { flex: 1; min-width: 0; }
.ce-nome { display: flex; align-items: center; flex-wrap: wrap; gap: 6px; font-size: 14px; color: var(--text-primary); font-weight: 500; }
.ce-sub { display: flex; flex-wrap: wrap; gap: 4px 14px; margin-top: 3px; font-size: 12px; color: var(--text-muted); }
.ce-sub a { color: var(--accent); text-decoration: none; }
.ce-acoes { display: flex; gap: 4px; }
.ce-acoes button { width: 30px; height: 30px; border: 1px solid var(--border); border-radius: 8px; background: transparent; color: var(--text-secondary); cursor: pointer; }
.ce-acoes button:hover { background: var(--bg-elevated); color: var(--text-primary); }
.ce-acoes button.del:hover { color: var(--danger); }
</style>
