<script setup lang="ts">
// Modo apresentação: tela cheia, um gráfico por vez, setas navegam e Esc sai.
import { computed, nextTick, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import GraficoConteudo from './GraficoConteudo.vue'
import type { Catalogo, Grafico } from './relatoriosApi'

const props = defineProps<{
  graficos: Grafico[]
  inicio: number
  catalogo: Catalogo | null
  periodoPainel: string | null
  titulo: string
  rotuloPeriodo: string
}>()
const emit = defineEmits<{ sair: [] }>()

const indice = ref(Math.min(Math.max(0, props.inicio), Math.max(0, props.graficos.length - 1)))
const atual = computed(() => props.graficos[indice.value])
const raiz = ref<HTMLElement | null>(null)

function ir(delta: number) {
  const n = props.graficos.length
  if (!n) return
  indice.value = (indice.value + delta + n) % n
}
function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') { e.preventDefault(); sair() }
  else if (e.key === 'ArrowRight' || e.key === 'PageDown') { e.preventDefault(); ir(1) }
  else if (e.key === 'ArrowLeft' || e.key === 'PageUp') { e.preventDefault(); ir(-1) }
}
let saindo = false
function sair() {
  if (saindo) return
  saindo = true
  if (document.fullscreenElement) document.exitFullscreen().catch(() => {})
  emit('sair')
}
// Se o navegador sair da tela cheia sozinho (Esc consumido por ele), fecha também a apresentação.
function aoMudarTelaCheia() { if (!document.fullscreenElement) sair() }

onMounted(async () => {
  window.addEventListener('keydown', onKey)
  document.addEventListener('fullscreenchange', aoMudarTelaCheia)
  await nextTick()
  raiz.value?.focus()
  try { await raiz.value?.requestFullscreen?.() } catch { /* sem tela cheia: segue como sobreposição */ }
})
onBeforeUnmount(() => {
  window.removeEventListener('keydown', onKey)
  document.removeEventListener('fullscreenchange', aoMudarTelaCheia)
  if (document.fullscreenElement) document.exitFullscreen().catch(() => {})
})
watch(() => props.graficos.length, (n) => { if (indice.value >= n) indice.value = Math.max(0, n - 1) })
</script>

<template>
  <Teleport to="body">
    <div ref="raiz" class="ap" tabindex="-1" role="dialog" aria-modal="true" aria-label="Modo apresentação">
      <header class="ap-topo">
        <div>
          <div class="ap-painel">{{ titulo }}</div>
          <div v-if="rotuloPeriodo" class="ap-per"><i class="mdi mdi-calendar-range"></i> {{ rotuloPeriodo }}</div>
        </div>
        <div class="ap-ctrl">
          <span class="ap-cont" aria-live="polite">{{ graficos.length ? indice + 1 : 0 }} / {{ graficos.length }}</span>
          <button class="ap-sair" aria-label="Sair da apresentação (Esc)" @click="sair"><i class="mdi mdi-close"></i> Sair (Esc)</button>
        </div>
      </header>

      <main class="ap-meio">
        <button class="ap-seta" aria-label="Gráfico anterior" :disabled="graficos.length < 2" @click="ir(-1)"><i class="mdi mdi-chevron-left"></i></button>
        <section v-if="atual" class="ap-grafico" :aria-label="atual.titulo">
          <h2>{{ atual.titulo }}</h2>
          <GraficoConteudo
            :key="atual.id" :definicao="atual" :tipo="atual.tipo" :periodo-painel="periodoPainel" :catalogo="catalogo" grande
          />
        </section>
        <div v-else class="ap-vazio">Este painel ainda não tem gráficos.</div>
        <button class="ap-seta" aria-label="Próximo gráfico" :disabled="graficos.length < 2" @click="ir(1)"><i class="mdi mdi-chevron-right"></i></button>
      </main>

      <footer class="ap-pontos" aria-hidden="true">
        <span v-for="(g, i) in graficos" :key="g.id" class="ponto" :class="{ ativo: i === indice }"></span>
      </footer>
    </div>
  </Teleport>
</template>

<style scoped>
.ap { position: fixed; inset: 0; z-index: 3000; background: var(--bg-primary); color: var(--text-primary); display: flex; flex-direction: column; padding: 22px 28px; outline: none; }
.ap-topo { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; }
.ap-painel { font-size: 14px; color: var(--text-secondary); font-weight: 600; }
.ap-per { font-size: 12px; color: var(--text-muted); margin-top: 3px; display: flex; align-items: center; gap: 5px; }
.ap-ctrl { display: flex; align-items: center; gap: 14px; }
.ap-cont { font-size: 13px; color: var(--text-secondary); font-variant-numeric: tabular-nums; }
.ap-sair { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-primary); font-size: 12px; cursor: pointer; }
.ap-sair:hover { border-color: var(--accent); color: var(--accent); }
.ap-meio { flex: 1; min-height: 0; display: flex; align-items: center; gap: 18px; }
.ap-grafico { flex: 1; min-width: 0; max-height: 100%; overflow: auto; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 16px; padding: 28px 34px; }
.ap-grafico h2 { font-size: 26px; font-weight: 700; margin-bottom: 20px; }
.ap-vazio { flex: 1; text-align: center; color: var(--text-muted); font-size: 16px; }
.ap-seta { width: 48px; height: 48px; flex: none; border-radius: 50%; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-primary); font-size: 28px; cursor: pointer; display: inline-flex; align-items: center; justify-content: center; }
.ap-seta:hover:not(:disabled) { border-color: var(--accent); color: var(--accent); }
.ap-seta:disabled { opacity: .3; cursor: default; }
.ap-pontos { display: flex; justify-content: center; gap: 8px; padding-top: 16px; }
.ponto { width: 8px; height: 8px; border-radius: 50%; background: var(--border); }
.ponto.ativo { background: var(--accent); width: 22px; border-radius: 4px; }
@media (max-width: 720px) { .ap { padding: 14px; } .ap-grafico { padding: 16px; } .ap-grafico h2 { font-size: 18px; } .ap-seta { width: 38px; height: 38px; font-size: 22px; } }
</style>
