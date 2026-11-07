<script setup lang="ts">
import { computed } from 'vue'
import type { PontoMeta } from './tipos'

const props = defineProps<{
  serie: PontoMeta[]
  alvo: number
  rotuloAlvo: string
  formatar: (v: number) => string
}>()

const L = 620, A = 220, ME = 56, MD = 14, MT = 14, MB = 28

const max = computed(() => {
  const m = Math.max(props.alvo, ...props.serie.map((p) => p.acumulado ?? 0))
  return m > 0 ? m : 1
})
const n = computed(() => Math.max(props.serie.length - 1, 1))
const x = (i: number) => ME + ((L - ME - MD) * i) / n.value
const y = (v: number) => MT + (A - MT - MB) * (1 - v / max.value)

const pontosReal = computed(() => props.serie
  .map((p, i) => (p.acumulado === null ? null : { i, v: p.acumulado }))
  .filter((p): p is { i: number; v: number } => p !== null))
const linhaReal = computed(() => pontosReal.value.map((p) => `${x(p.i).toFixed(1)},${y(p.v).toFixed(1)}`).join(' '))
const areaReal = computed(() => {
  const ps = pontosReal.value
  if (!ps.length) return ''
  const base = y(0).toFixed(1)
  return `${x(ps[0]!.i).toFixed(1)},${base} ${linhaReal.value} ${x(ps[ps.length - 1]!.i).toFixed(1)},${base}`
})
const linhaAlvo = computed(() => props.serie.map((p, i) => `${x(i).toFixed(1)},${y(p.alvoLinear).toFixed(1)}`).join(' '))
const grades = computed(() => [0, 0.5, 1].map((f) => ({ y: y(max.value * f), rotulo: props.formatar(max.value * f) })))

function dataCurta(iso: string): string {
  const d = new Date(iso)
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: '2-digit', timeZone: 'UTC' })
}
const inicioRot = computed(() => (props.serie[0] ? dataCurta(props.serie[0].data) : ''))
const fimRot = computed(() => (props.serie.length ? dataCurta(props.serie[props.serie.length - 1]!.data) : ''))
const resumo = computed(() => {
  const ult = pontosReal.value[pontosReal.value.length - 1]
  return `Evolução acumulada: ${ult ? props.formatar(ult.v) : props.formatar(0)} de ${props.formatar(props.alvo)}`
})
</script>

<template>
  <figure class="gm">
    <svg :viewBox="`0 0 ${L} ${A}`" role="img" :aria-label="resumo">
      <g v-for="g in grades" :key="g.y">
        <line :x1="ME" :x2="L - MD" :y1="g.y" :y2="g.y" class="grade" />
        <text :x="ME - 6" :y="g.y + 3" text-anchor="end" class="eixo">{{ g.rotulo }}</text>
      </g>
      <polygon v-if="areaReal" :points="areaReal" class="area" />
      <polyline :points="linhaAlvo" class="alvo" />
      <polyline v-if="linhaReal" :points="linhaReal" class="real" />
      <circle v-if="pontosReal.length" :cx="x(pontosReal[pontosReal.length - 1]!.i)" :cy="y(pontosReal[pontosReal.length - 1]!.v)" r="3.5" class="ponto" />
      <text :x="ME" :y="A - 8" class="eixo">{{ inicioRot }}</text>
      <text :x="L - MD" :y="A - 8" text-anchor="end" class="eixo">{{ fimRot }}</text>
    </svg>
    <figcaption class="leg">
      <span><i class="tr real-l"></i> Realizado acumulado</span>
      <span><i class="tr alvo-l"></i> {{ rotuloAlvo }}</span>
    </figcaption>
  </figure>
</template>

<style scoped>
.gm { margin: 0; }
svg { width: 100%; height: auto; display: block; }
.grade { stroke: var(--border); stroke-width: 1; }
.eixo { fill: var(--text-muted); font-size: 10px; }
.area { fill: var(--accent); opacity: .12; }
.real { fill: none; stroke: var(--accent); stroke-width: 2.2; stroke-linejoin: round; vector-effect: non-scaling-stroke; }
.alvo { fill: none; stroke: var(--text-muted); stroke-width: 1.5; stroke-dasharray: 5 4; vector-effect: non-scaling-stroke; }
.ponto { fill: var(--accent); }
.leg { display: flex; gap: 16px; font-size: 11px; color: var(--text-secondary); margin-top: 4px; }
.tr { display: inline-block; width: 16px; height: 0; border-top: 2px solid; vertical-align: middle; margin-right: 5px; }
.real-l { border-color: var(--accent); }
.alvo-l { border-color: var(--text-muted); border-top-style: dashed; }
</style>
