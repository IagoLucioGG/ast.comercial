<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { odataRequest } from '@/services/api'
import { campo, formatarDataHora, lista, mensagemErro, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'

const props = defineProps<{ negocioId: number; etapaId?: number; fechado?: boolean }>()
const emit = defineEmits<{ alterado: [] }>()

interface Item { id: number; texto: string; obrigatorio: boolean; concluido: boolean; por: string | null; em: string | null; salvando: boolean }
interface Checklist { id: number; nome: string; itens: Item[] }

const toast = useToast()
const carregando = ref(false)
const erro = ref('')
const etapaNome = ref('')
const checklists = ref<Checklist[]>([])

function aplicar(r: unknown) {
  etapaNome.value = String(campo(r, 'EtapaNome') ?? '')
  checklists.value = lista<Registro>(campo(r, 'Checklists')).map((c) => ({
    id: Number(campo(c, 'ChecklistId')),
    nome: String(campo(c, 'Nome') ?? ''),
    itens: lista<Registro>(campo(c, 'Itens')).map((i) => ({
      id: Number(campo(i, 'ItemId')),
      texto: String(campo(i, 'Texto') ?? ''),
      obrigatorio: !!campo(i, 'Obrigatorio'),
      concluido: !!campo(i, 'Concluido'),
      por: campo<string | null>(i, 'ConcluidoPorNome') ?? null,
      em: campo<string | null>(i, 'ConcluidoEm') ?? null,
      salvando: false,
    })),
  }))
}

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    aplicar(await odataRequest<unknown>('GET', `Negocios(${props.negocioId})/Checklist`))
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar o checklist')
  } finally {
    carregando.value = false
  }
}

const todos = computed(() => checklists.value.flatMap((c) => c.itens))
const total = computed(() => todos.value.length)
const feitos = computed(() => todos.value.filter((i) => i.concluido).length)
const pendentesObrig = computed(() => todos.value.filter((i) => i.obrigatorio && !i.concluido).length)
const percentual = computed(() => (total.value ? Math.round((feitos.value / total.value) * 100) : 0))

async function alternar(i: Item) {
  if (i.salvando || props.fechado) return
  const novo = !i.concluido
  i.salvando = true
  try {
    aplicar(await odataRequest<unknown>('POST', `Negocios(${props.negocioId})/MarcarItemChecklist`, { ItemId: i.id, Concluido: novo }))
    emit('alterado')
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível atualizar o item'))
    await carregar()
  }
}

watch(() => [props.negocioId, props.etapaId], carregar)
onMounted(carregar)
</script>

<template>
  <div class="ck">
    <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!total" :texto-vazio="`A etapa ${etapaNome ? `“${etapaNome}” ` : ''}não tem itens de checklist.`" icone="mdi-checkbox-marked-outline" @tentar="carregar">
      <div class="ck-topo">
        <div class="ck-prog">
          <div class="ck-prog-texto">
            <strong>{{ feitos }} de {{ total }}</strong> concluído{{ feitos === 1 ? '' : 's' }} na etapa <em>{{ etapaNome }}</em>
          </div>
          <div class="ck-barra" role="progressbar" :aria-valuenow="percentual" aria-valuemin="0" aria-valuemax="100" :aria-label="`Progresso do checklist: ${percentual}%`">
            <span :style="{ width: percentual + '%' }"></span>
          </div>
        </div>
        <p v-if="pendentesObrig" class="ck-aviso" role="status"><i class="mdi mdi-lock-outline" aria-hidden="true"></i> {{ pendentesObrig }} obrigatório{{ pendentesObrig === 1 ? '' : 's' }} pendente{{ pendentesObrig === 1 ? '' : 's' }}: impede avançar de etapa.</p>
        <p v-else class="ck-ok" role="status"><i class="mdi mdi-check-circle-outline" aria-hidden="true"></i> Nenhum item obrigatório pendente.</p>
      </div>

      <section v-for="c in checklists" :key="c.id" class="ck-grupo" :aria-label="c.nome">
        <h3>{{ c.nome }}</h3>
        <ul class="ck-itens">
          <li v-for="i in c.itens" :key="i.id" :class="{ feito: i.concluido }">
            <label class="ck-item">
              <input type="checkbox" :checked="i.concluido" :disabled="fechado || i.salvando" @change="alternar(i)" />
              <span class="ck-texto">{{ i.texto }}</span>
              <span v-if="i.obrigatorio" class="ck-tag">Obrigatório</span>
            </label>
            <p v-if="i.concluido && i.em" class="ck-meta">Concluído{{ i.por ? ` por ${i.por}` : '' }} em {{ formatarDataHora(i.em) }}</p>
          </li>
        </ul>
      </section>
    </DetalheEstado>
  </div>
</template>

<style scoped>
.ck-topo { display: flex; flex-direction: column; gap: 10px; margin-bottom: 16px; }
.ck-prog-texto { font-size: 13px; color: var(--text-secondary); margin-bottom: 6px; }
.ck-prog-texto strong { color: var(--text-primary); }
.ck-barra { height: 8px; border-radius: 4px; background: var(--bg-elevated); overflow: hidden; }
.ck-barra span { display: block; height: 100%; background: var(--accent); transition: width .2s; }
.ck-aviso, .ck-ok { display: flex; align-items: center; gap: 6px; font-size: 12px; }
.ck-aviso { color: #f59e0b; }
.ck-ok { color: #22c55e; }
.ck-grupo { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 12px; padding: 14px 16px; margin-bottom: 12px; }
.ck-grupo h3 { font-size: 13px; font-weight: 700; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); margin: 0 0 10px; }
.ck-itens { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.ck-item { display: flex; align-items: center; gap: 10px; padding: 6px 4px; cursor: pointer; font-size: 14px; color: var(--text-primary); }
.ck-item input { width: 17px; height: 17px; accent-color: var(--accent); cursor: pointer; flex-shrink: 0; }
.ck-item input:disabled { cursor: not-allowed; }
.feito .ck-texto { color: var(--text-muted); text-decoration: line-through; }
.ck-tag { font-size: 10px; padding: 1px 7px; border-radius: 4px; border: 1px solid rgba(245, 158, 11, .5); color: #f59e0b; white-space: nowrap; }
.ck-meta { margin: 0 0 0 31px; font-size: 11px; color: var(--text-muted); }
</style>
