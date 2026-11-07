<script setup lang="ts">
import { computed, ref } from 'vue'
import NegocioCard, { type ProgressoChecklist } from '@/components/funil/NegocioCard.vue'
import { useFunisStore, type EtapaItem } from '@/stores/funis'
import { formatarValor } from '@/composables/useFunilFormato'
import type { EstadoColuna, TotalMoeda } from '@/composables/useFunilKanban'

const props = defineProps<{
  etapa: EtapaItem
  etapas: EtapaItem[]
  estado: EstadoColuna
  totais: TotalMoeda[]
  parcial: boolean
  arrastandoId: number | null
  buscando: boolean
  /** Texto do estado vazio conforme o recorte (abertos, ganhos...). */
  vazioTexto?: string
  /** Progresso de checklist por id de negócio (uma consulta agregada por quadro). */
  progresso?: Record<number, ProgressoChecklist>
}>()

const emit = defineEmits<{
  mais: []
  novo: [etapaId: number]
  soltar: [etapaId: number]
  abrir: [id: number]
  mover: [id: number, etapaId: number]
  ganhar: [id: number]
  perder: [id: number]
  reabrir: [id: number]
  iniciarArraste: [id: number, ev: DragEvent]
  fimArraste: []
  recarregar: [etapaId: number]
}>()

const store = useFunisStore()
const sobre = ref(false)

const linhasTotais = computed(() =>
  props.totais.map((t) => ({
    chave: t.moedaId ?? 0,
    texto: formatarValor(t.total, store.moedaDe(t.moedaId)),
  })),
)

function aoRolar(e: Event) {
  const el = e.target as HTMLElement
  if (el.scrollTop + el.clientHeight >= el.scrollHeight - 120) emit('mais')
}

function aoSoltar() {
  sobre.value = false
  emit('soltar', props.etapa.id)
}
</script>

<template>
  <section class="kc" :class="{ 'kc-sobre': sobre }" :aria-label="`Etapa ${etapa.nome}`"
    @dragover.prevent="sobre = true" @dragleave.self="sobre = false" @drop.prevent="aoSoltar">
    <header class="kc-cab">
      <div class="kc-linha">
        <h3 class="kc-nome">{{ etapa.nome }}</h3>
        <span class="kc-qtd" :aria-label="`${estado.qtd} negócios`">{{ estado.qtd }}</span>
        <button class="kc-add" type="button" :aria-label="`Novo negócio em ${etapa.nome}`" @click="emit('novo', etapa.id)">
          <i class="mdi mdi-plus" aria-hidden="true"></i>
        </button>
      </div>
      <div class="kc-totais">
        <template v-if="estado.carregando && !totais.length"><span class="kc-sk-total"></span></template>
        <template v-else-if="linhasTotais.length">
          <span v-for="l in linhasTotais" :key="l.chave" class="kc-total">{{ l.texto }}</span>
          <span v-if="parcial" class="kc-parcial" title="Soma apenas dos cartões carregados">parcial</span>
        </template>
        <span v-else class="kc-total kc-zero">—</span>
      </div>
    </header>

    <div class="kc-corpo" @scroll.passive="aoRolar">
      <template v-if="estado.carregando">
        <div v-for="n in 3" :key="n" class="kc-sk" aria-hidden="true"></div>
      </template>
      <div v-else-if="estado.erro" class="kc-estado" role="alert">
        <i class="mdi mdi-alert-circle-outline" aria-hidden="true"></i>
        <span>{{ estado.erro }}</span>
        <button type="button" class="kc-link" @click="emit('recarregar', etapa.id)">Tentar de novo</button>
      </div>
      <template v-else>
        <NegocioCard
          v-for="c in estado.cards" :key="c.id" :card="c" :etapas="etapas" :arrastando="arrastandoId === c.id" :checklist="progresso?.[c.id]"
          @abrir="emit('abrir', $event)" @mover="(id, e) => emit('mover', id, e)" @ganhar="emit('ganhar', $event)"
          @perder="emit('perder', $event)" @reabrir="emit('reabrir', $event)"
          @dragstart="(id, ev) => emit('iniciarArraste', id, ev)" @dragend="emit('fimArraste')"
        />
        <p v-if="!estado.cards.length" class="kc-vazio">{{ buscando ? 'Nenhum resultado.' : (vazioTexto ?? 'Nenhum negócio nesta etapa.') }}</p>
        <div v-if="estado.carregandoMais" class="kc-sk" aria-hidden="true"></div>
      </template>
    </div>
  </section>
</template>

<style scoped>
.kc { flex: 0 0 300px; width: 300px; display: flex; flex-direction: column; min-height: 0; background: var(--bg-secondary); border: 1px solid var(--border); border-radius: 12px; transition: border-color .15s, background .15s; }
.kc-sobre { border-color: var(--accent); background: var(--bg-elevated); }
.kc-cab { padding: 12px 12px 10px; border-bottom: 1px solid var(--border); flex-shrink: 0; }
.kc-linha { display: flex; align-items: center; gap: 8px; }
.kc-nome { flex: 1; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .5px; color: var(--text-primary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.kc-qtd { font-size: 11px; font-weight: 600; padding: 1px 8px; border-radius: 10px; background: var(--bg-elevated); border: 1px solid var(--border); color: var(--text-secondary); }
.kc-add { width: 24px; height: 24px; border: 1px solid var(--border); border-radius: 6px; background: var(--bg-surface); color: var(--text-secondary); cursor: pointer; display: flex; align-items: center; justify-content: center; }
.kc-add:hover, .kc-add:focus-visible { color: var(--accent); border-color: var(--accent); outline: none; }
.kc-totais { margin-top: 6px; display: flex; flex-wrap: wrap; gap: 2px 10px; align-items: baseline; min-height: 18px; }
.kc-total { font-size: 12px; font-weight: 600; color: var(--accent); }
.kc-zero { color: var(--text-muted); }
.kc-parcial { font-size: 10px; color: var(--text-muted); border: 1px dashed var(--border); border-radius: 4px; padding: 0 5px; }
.kc-corpo { flex: 1; overflow-y: auto; padding: 10px; display: flex; flex-direction: column; gap: 8px; min-height: 80px; }
.kc-vazio { text-align: center; font-size: 12px; color: var(--text-muted); padding: 24px 8px; }
.kc-estado { display: flex; flex-direction: column; align-items: center; gap: 6px; padding: 20px 8px; font-size: 12px; color: var(--danger); text-align: center; }
.kc-link { background: none; border: 0; color: var(--accent); font-size: 12px; cursor: pointer; text-decoration: underline; }
.kc-sk, .kc-sk-total { background: linear-gradient(90deg, var(--bg-surface) 25%, var(--bg-elevated) 50%, var(--bg-surface) 75%); background-size: 200% 100%; animation: kc-brilho 1.2s infinite linear; border-radius: 10px; }
.kc-sk { height: 74px; flex-shrink: 0; }
.kc-sk-total { display: inline-block; width: 90px; height: 14px; border-radius: 4px; }
@keyframes kc-brilho { from { background-position: 200% 0; } to { background-position: -200% 0; } }
@media (prefers-reduced-motion: reduce) { .kc-sk, .kc-sk-total { animation: none; } }
</style>
