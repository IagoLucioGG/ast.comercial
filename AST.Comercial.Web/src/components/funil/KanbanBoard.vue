<script setup lang="ts">
import { computed, ref, toRef } from 'vue'
import { useRouter } from 'vue-router'
import KanbanColuna from '@/components/funil/KanbanColuna.vue'
import PerderNegocioModal from '@/components/negocio/PerderNegocioModal.vue'
import { useFunisStore } from '@/stores/funis'
import { useFunilKanban, type FiltroStatus } from '@/composables/useFunilKanban'
import { useAcoesNegocio } from '@/composables/useAcoesNegocio'

const props = defineProps<{ funilId: number | null; busca: string; status: FiltroStatus; responsavelId: number | null }>()
const emit = defineEmits<{ novo: [etapaId: number | null] }>()

const router = useRouter()
const store = useFunisStore()
const acoes = useAcoesNegocio()

const funilIdRef = toRef(props, 'funilId')
const buscaRef = toRef(props, 'busca')
const etapas = computed(() => store.funilPorId(props.funilId)?.etapas ?? [])

const statusRef = toRef(props, 'status')
const responsavelRef = toRef(props, 'responsavelId')
const kanban = useFunilKanban(funilIdRef, etapas, buscaRef, statusRef, responsavelRef)

const vazioTexto = computed(() => ({
  abertos: 'Nenhum negócio aberto nesta etapa.',
  ganhos: 'Nenhum negócio ganho nesta etapa.',
  perdidos: 'Nenhum negócio perdido nesta etapa.',
  todos: 'Nenhum negócio nesta etapa.',
})[props.status])

const perdendo = ref<{ id: number; titulo: string } | null>(null)
const perderAberto = computed({
  get: () => perdendo.value !== null,
  set: (v: boolean) => { if (!v) perdendo.value = null },
})

function abrir(id: number) { router.push(`/negocios/${id}`) }

function iniciarArraste(id: number, ev: DragEvent) {
  const de = Object.entries(kanban.colunas).find(([, c]) => c.cards.some((x) => x.id === id))
  kanban.arrastando.value = { id, deEtapaId: Number(de?.[0]) }
  if (ev.dataTransfer) {
    ev.dataTransfer.effectAllowed = 'move'
    ev.dataTransfer.setData('text/plain', String(id))
  }
}

function fimArraste() { kanban.arrastando.value = null }

async function soltar(etapaId: number) {
  const a = kanban.arrastando.value
  kanban.arrastando.value = null
  if (a && a.deEtapaId !== etapaId) await kanban.mover(a.id, etapaId)
}

async function ganhar(id: number) {
  if (await acoes.ganhar(id)) await kanban.aposFechamento(id)
}

function perder(id: number) {
  for (const c of Object.values(kanban.colunas)) {
    const card = c.cards.find((x) => x.id === id)
    if (card) { perdendo.value = { id, titulo: card.titulo }; return }
  }
}

async function reabrir(id: number) {
  if (await acoes.reabrir(id)) await kanban.carregarTudo()
}

defineExpose({
  recarregar: kanban.carregarTudo,
  recarregarEtapa: (etapaId: number) => kanban.recarregarColunas(etapaId),
})
</script>

<template>
  <div v-if="!etapas.length" class="kb-vazio">
    <i class="mdi mdi-view-column-outline" aria-hidden="true"></i>
    <p>Este funil ainda não tem etapas.</p>
  </div>
  <div v-else class="kb" role="region" aria-label="Quadro do funil">
    <KanbanColuna
      v-for="e in etapas" :key="e.id" :etapa="e" :etapas="etapas"
      :estado="kanban.colunas[e.id] ?? { cards: [], qtd: 0, carregando: true, carregandoMais: false, fim: false, erro: null, totaisServidor: null }"
      :totais="kanban.totaisDaColuna(e.id)" :parcial="kanban.totaisParciais.value"
      :arrastando-id="kanban.arrastando.value?.id ?? null" :buscando="!!busca.trim()" :vazio-texto="vazioTexto"
      @mais="kanban.carregarMais(e.id)" @novo="emit('novo', $event)" @soltar="soltar"
      @abrir="abrir" @mover="(id, et) => kanban.mover(id, et)" @ganhar="ganhar" @perder="perder" @reabrir="reabrir"
      @iniciar-arraste="iniciarArraste" @fim-arraste="fimArraste" @recarregar="(id) => kanban.recarregarColunas(id)"
    />
  </div>
  <PerderNegocioModal v-model="perderAberto" :negocio-id="perdendo?.id ?? null" :titulo="perdendo?.titulo" @perdido="kanban.aposFechamento" />
</template>

<style scoped>
.kb { flex: 1; min-height: 0; display: flex; gap: 12px; padding: 16px 20px; overflow-x: auto; overflow-y: hidden; align-items: stretch; }
.kb-vazio { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px; color: var(--text-muted); font-size: 13px; }
.kb-vazio i { font-size: 36px; }
</style>
