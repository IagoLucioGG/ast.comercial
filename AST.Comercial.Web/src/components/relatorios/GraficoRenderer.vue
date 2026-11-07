<script setup lang="ts">
// Desenha um gráfico (número, barras, linhas, pizza/rosca ou tabela) em SVG próprio.
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import {
  cor, formatarCompacto, formatarInteiro, formatarValor, rotuloAgregacao,
  type DadosGrafico, type PontoDados, type SerieDados, type TipoGrafico,
} from './relatoriosApi'

const props = defineProps<{
  dados: DadosGrafico
  tipo: TipoGrafico
  rotuloCampo?: string
  rotuloAgrupamento?: string
  grande?: boolean
}>()

const raiz = ref<HTMLElement | null>(null)
const largura = ref(320)
let observador: ResizeObserver | null = null
onMounted(() => {
  if (!raiz.value) return
  largura.value = Math.max(160, raiz.value.clientWidth)
  observador = new ResizeObserver(() => { if (raiz.value) largura.value = Math.max(160, raiz.value.clientWidth) })
  observador.observe(raiz.value)
})
onBeforeUnmount(() => observador?.disconnect())

// Série selecionada (uma por moeda)
const indiceSerie = ref(0)
watch(() => props.dados, () => { indiceSerie.value = 0; dica.value = null })
const serie = computed<SerieDados>(() => props.dados.series[indiceSerie.value] ?? props.dados.series[0] ?? { pontos: [], quantidade: 0 })
const pontos = computed<PontoDados[]>(() => serie.value.pontos)
const moeda = computed(() => serie.value.moeda ?? null)
const ehMes = computed(() => props.dados.tipoAgrupamento === 'mes')
const fmt = (v: number, s?: SerieDados) => formatarValor(v, props.dados, (s ?? serie.value).moeda)
const fmtC = (v: number) => formatarCompacto(v, props.dados, moeda.value)
const legendaSerie = computed(() => {
  const base = rotuloAgregacao(props.dados, props.rotuloCampo)
  return moeda.value ? `${base} · ${moeda.value.codigo}` : base
})

// ---------- Tooltip (mouse e teclado) ----------
const dica = ref<{ x: number; y: number; texto: string } | null>(null)
function mostrarEm(ev: Event, texto: string) {
  const el = ev.currentTarget as Element | null
  const caixa = raiz.value?.getBoundingClientRect()
  if (!el || !caixa) return
  const r = el.getBoundingClientRect()
  dica.value = { x: r.left + r.width / 2 - caixa.left, y: r.top - caixa.top, texto }
}
function esconder() { dica.value = null }
function textoPonto(p: PontoDados): string {
  const total = serie.value.pontos.reduce((a, b) => a + Math.max(0, b.valor), 0)
  const pct = props.dados.agregacao !== 'Media' && total > 0 ? ` (${((p.valor / total) * 100).toFixed(1).replace('.', ',')}%)` : ''
  const qtd = props.dados.agregacao !== 'Contagem' ? ` · ${formatarInteiro(p.quantidade)} registro(s)` : ''
  return `${p.rotulo}: ${fmt(p.valor)}${pct}${qtd}`
}

// ---------- Eixos ----------
function maximoBonito(v: number): number {
  if (v <= 0) return 1
  const exp = Math.pow(10, Math.floor(Math.log10(v)))
  const f = v / exp
  const n = f <= 1 ? 1 : f <= 2 ? 2 : f <= 5 ? 5 : 10
  return n * exp
}
const maximo = computed(() => maximoBonito(Math.max(0, ...pontos.value.map((p) => p.valor))))
const ticks = computed(() => [0, 1, 2, 3, 4].map((i) => (maximo.value * i) / 4))

// ---------- Barras horizontais (categorias) ----------
const linhaH = 28
const rotuloW = computed(() => Math.min(150, Math.round(largura.value * 0.38)))
const alturaBarrasH = computed(() => Math.max(60, pontos.value.length * linhaH + 8))
function larguraBarra(v: number): number {
  const disponivel = largura.value - rotuloW.value - 90
  return Math.max(v > 0 ? 2 : 0, (Math.max(0, v) / maximo.value) * Math.max(40, disponivel))
}
function cortar(t: string, n: number): string { return t.length > n ? t.slice(0, n - 1) + '…' : t }
const maxChars = computed(() => Math.max(6, Math.floor(rotuloW.value / 6.3)))

// ---------- Barras verticais e linhas (séries por mês/ordem) ----------
const alt = computed(() => (props.grande ? 380 : 220))
// Margem esquerda comporta rótulos como "R$ 200 mil" sem cortar o símbolo da moeda
const margem = { e: 66, d: 12, t: 12, b: 28 }
const areaW = computed(() => Math.max(40, largura.value - margem.e - margem.d))
const areaH = computed(() => alt.value - margem.t - margem.b)
const passo = computed(() => areaW.value / Math.max(1, pontos.value.length))
function yDe(v: number): number { return margem.t + areaH.value - (Math.max(0, v) / maximo.value) * areaH.value }
function xCentro(i: number): number { return margem.e + passo.value * (i + 0.5) }
const saltoRotulo = computed(() => Math.max(1, Math.ceil(pontos.value.length / Math.max(2, Math.floor(areaW.value / 56)))))
const rotuloEixo = (p: PontoDados) => cortar(p.rotulo, 9)
const caminhoLinha = computed(() => pontos.value.map((p, i) => `${i === 0 ? 'M' : 'L'}${xCentro(i).toFixed(1)},${yDe(p.valor).toFixed(1)}`).join(' '))
const caminhoArea = computed(() => {
  if (pontos.value.length < 2) return ''
  const base = margem.t + areaH.value
  return `${caminhoLinha.value} L${xCentro(pontos.value.length - 1).toFixed(1)},${base} L${xCentro(0).toFixed(1)},${base} Z`
})

// ---------- Pizza / rosca ----------
const fatias = computed(() => {
  const itens = pontos.value.filter((p) => p.valor > 0)
  const total = itens.reduce((a, p) => a + p.valor, 0)
  let acc = 0
  return itens.map((p, i) => {
    const ini = acc / total
    acc += p.valor
    return { p, cor: cor(pontos.value.indexOf(p)), ini, fim: acc / total, pct: (p.valor / total) * 100, i }
  })
})
const lado = computed(() => Math.min(props.grande ? 340 : 190, Math.max(120, largura.value - (largura.value > 420 ? 200 : 0))))
const raio = computed(() => lado.value / 2 - 6)
function arco(ini: number, fim: number): string {
  const c = lado.value / 2
  const r = raio.value
  const ri = r * 0.58
  if (fim - ini >= 0.9999) {
    return `M${c},${c - r} A${r},${r} 0 1 1 ${c - 0.01},${c - r} L${c - 0.01},${c - ri} A${ri},${ri} 0 1 0 ${c},${c - ri} Z`
  }
  const a0 = ini * 2 * Math.PI - Math.PI / 2
  const a1 = fim * 2 * Math.PI - Math.PI / 2
  const g = fim - ini > 0.5 ? 1 : 0
  const p = (ang: number, rad: number) => `${(c + rad * Math.cos(ang)).toFixed(2)},${(c + rad * Math.sin(ang)).toFixed(2)}`
  return `M${p(a0, r)} A${r},${r} 0 ${g} 1 ${p(a1, r)} L${p(a1, ri)} A${ri},${ri} 0 ${g} 0 ${p(a0, ri)} Z`
}
const totalPizza = computed(() => serie.value.total != null ? fmt(serie.value.total) : '')

const resumoAria = computed(() => `${props.tipo}: ${pontos.value.map((p) => `${p.rotulo} ${fmt(p.valor)}`).join('; ')}`)
</script>

<template>
  <div ref="raiz" class="gr" :class="{ grande }" @keydown.esc="esconder">
    <!-- Seletor de série (uma por moeda; valores de moedas diferentes nunca são somados) -->
    <div v-if="dados.series.length > 1 && tipo !== 'Numero'" class="gr-series" role="tablist" aria-label="Moeda">
      <button
        v-for="(s, i) in dados.series" :key="i" role="tab" :aria-selected="i === indiceSerie"
        class="gr-serie" :class="{ ativa: i === indiceSerie }" @click="indiceSerie = i"
      >{{ s.moeda?.codigo ?? 'Sem moeda' }}</button>
    </div>

    <!-- Número grande -->
    <div v-if="tipo === 'Numero'" class="gr-numeros">
      <div v-for="(s, i) in dados.series" :key="i" class="gr-numero">
        <div class="gr-numero-valor" :title="s.total != null ? fmt(s.total, s) : ''">
          {{ s.total != null ? fmt(s.total, s) : fmt(s.pontos[0]?.valor ?? 0, s) }}
        </div>
        <div class="gr-numero-leg">
          <span v-if="s.moeda" class="gr-moeda">{{ s.moeda.codigo }}</span>
          {{ rotuloAgregacao(dados, rotuloCampo) }} · {{ formatarInteiro(s.quantidade) }} registro(s)
        </div>
      </div>
    </div>

    <!-- Barras verticais (por mês) -->
    <div v-else-if="tipo === 'Barras' && ehMes" class="gr-scroll">
      <svg :width="largura" :height="alt" :viewBox="`0 0 ${largura} ${alt}`" role="img" :aria-label="resumoAria">
        <g v-for="t in ticks" :key="t">
          <line :x1="margem.e" :x2="largura - margem.d" :y1="yDe(t)" :y2="yDe(t)" class="grade" />
          <text :x="margem.e - 6" :y="yDe(t) + 4" text-anchor="end" class="eixo">{{ fmtC(t) }}</text>
        </g>
        <g v-for="(p, i) in pontos" :key="p.chave">
          <rect
            :x="xCentro(i) - Math.max(3, passo * 0.32)" :y="yDe(p.valor)" :width="Math.max(6, passo * 0.64)"
            :height="Math.max(p.valor > 0 ? 2 : 0, margem.t + areaH - yDe(p.valor))" rx="3" :fill="cor(0)" class="alvo"
            tabindex="0" role="img" :aria-label="textoPonto(p)"
            @mouseenter="mostrarEm($event, textoPonto(p))" @mouseleave="esconder" @focus="mostrarEm($event, textoPonto(p))" @blur="esconder"
          />
          <text v-if="i % saltoRotulo === 0" :x="xCentro(i)" :y="alt - 8" text-anchor="middle" class="eixo">{{ rotuloEixo(p) }}</text>
        </g>
      </svg>
    </div>

    <!-- Barras horizontais -->
    <div v-else-if="tipo === 'Barras'" class="gr-scroll">
      <svg :width="largura" :height="alturaBarrasH" :viewBox="`0 0 ${largura} ${alturaBarrasH}`" role="img" :aria-label="resumoAria">
        <g v-for="(p, i) in pontos" :key="p.chave + i">
          <text :x="rotuloW - 8" :y="i * linhaH + 19" text-anchor="end" class="eixo rot">{{ cortar(p.rotulo, maxChars) }}</text>
          <rect
            :x="rotuloW" :y="i * linhaH + 6" :width="larguraBarra(p.valor)" height="17" rx="4" :fill="cor(i)" class="alvo"
            tabindex="0" role="img" :aria-label="textoPonto(p)"
            @mouseenter="mostrarEm($event, textoPonto(p))" @mouseleave="esconder" @focus="mostrarEm($event, textoPonto(p))" @blur="esconder"
          />
          <text :x="rotuloW + larguraBarra(p.valor) + 6" :y="i * linhaH + 19" class="eixo val">{{ fmtC(p.valor) }}</text>
        </g>
      </svg>
    </div>

    <!-- Linhas -->
    <div v-else-if="tipo === 'Linhas'" class="gr-scroll">
      <svg :width="largura" :height="alt" :viewBox="`0 0 ${largura} ${alt}`" role="img" :aria-label="resumoAria">
        <g v-for="t in ticks" :key="t">
          <line :x1="margem.e" :x2="largura - margem.d" :y1="yDe(t)" :y2="yDe(t)" class="grade" />
          <text :x="margem.e - 6" :y="yDe(t) + 4" text-anchor="end" class="eixo">{{ fmtC(t) }}</text>
        </g>
        <path v-if="caminhoArea" :d="caminhoArea" :fill="cor(0)" opacity="0.12" />
        <path v-if="pontos.length > 1" :d="caminhoLinha" fill="none" :stroke="cor(0)" stroke-width="2.2" stroke-linejoin="round" />
        <g v-for="(p, i) in pontos" :key="p.chave + i">
          <circle :cx="xCentro(i)" :cy="yDe(p.valor)" r="4" :fill="cor(0)" class="ponto" />
          <circle
            :cx="xCentro(i)" :cy="yDe(p.valor)" r="10" fill="transparent" class="alvo" tabindex="0" role="img" :aria-label="textoPonto(p)"
            @mouseenter="mostrarEm($event, textoPonto(p))" @mouseleave="esconder" @focus="mostrarEm($event, textoPonto(p))" @blur="esconder"
          />
          <text v-if="i % saltoRotulo === 0" :x="xCentro(i)" :y="alt - 8" text-anchor="middle" class="eixo">{{ rotuloEixo(p) }}</text>
        </g>
      </svg>
    </div>

    <!-- Pizza / rosca -->
    <div v-else-if="tipo === 'Pizza'" class="gr-pizza">
      <svg :width="lado" :height="lado" :viewBox="`0 0 ${lado} ${lado}`" role="img" :aria-label="resumoAria">
        <path
          v-for="f in fatias" :key="f.p.chave + f.i" :d="arco(f.ini, f.fim)" :fill="f.cor" class="alvo fatia"
          tabindex="0" role="img" :aria-label="textoPonto(f.p)"
          @mouseenter="mostrarEm($event, textoPonto(f.p))" @mouseleave="esconder" @focus="mostrarEm($event, textoPonto(f.p))" @blur="esconder"
        />
        <text :x="lado / 2" :y="lado / 2 - 2" text-anchor="middle" class="centro">{{ totalPizza }}</text>
        <text :x="lado / 2" :y="lado / 2 + 16" text-anchor="middle" class="eixo">{{ dados.agregacao === 'Contagem' ? 'total' : 'soma' }}</text>
      </svg>
      <ul class="gr-legenda">
        <li v-for="(p, i) in pontos" :key="p.chave + i">
          <span class="sw" :style="{ background: cor(i) }"></span>
          <span class="nome" :title="p.rotulo">{{ p.rotulo }}</span>
          <span class="num">{{ fmt(p.valor) }}</span>
        </li>
      </ul>
    </div>

    <!-- Tabela -->
    <div v-else class="gr-scroll">
      <table class="gr-tabela">
        <thead>
          <tr>
            <th>{{ rotuloAgrupamento || 'Item' }}</th>
            <th class="num">{{ rotuloAgregacao(dados, rotuloCampo) }}</th>
            <th v-if="dados.agregacao !== 'Contagem'" class="num">Registros</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="(p, i) in pontos" :key="p.chave + i">
            <td :title="p.rotulo">{{ p.rotulo }}</td>
            <td class="num">{{ fmt(p.valor) }}</td>
            <td v-if="dados.agregacao !== 'Contagem'" class="num">{{ formatarInteiro(p.quantidade) }}</td>
          </tr>
        </tbody>
        <tfoot v-if="serie.total != null && pontos.length > 1">
          <tr>
            <td>Total{{ dados.truncado ? ' (todos)' : '' }}</td>
            <td class="num">{{ fmt(serie.total) }}</td>
            <td v-if="dados.agregacao !== 'Contagem'" class="num">{{ formatarInteiro(serie.quantidade) }}</td>
          </tr>
        </tfoot>
      </table>
    </div>

    <!-- Legenda geral -->
    <div v-if="tipo !== 'Numero' && tipo !== 'Pizza' && tipo !== 'Tabela'" class="gr-leg">
      <span class="sw" :style="{ background: cor(0) }"></span> {{ legendaSerie }}
      <span v-if="dados.truncado" class="trunc">· exibindo parte dos itens</span>
    </div>
    <div v-else-if="tipo === 'Pizza' && dados.truncado" class="gr-leg"><span class="trunc">Exibindo os maiores itens</span></div>

    <div v-if="dica" class="gr-dica" role="status" :style="{ left: dica.x + 'px', top: dica.y + 'px' }">{{ dica.texto }}</div>
  </div>
</template>

<style scoped>
.gr { position: relative; width: 100%; min-width: 0; display: flex; flex-direction: column; gap: 8px; }
.gr-scroll { max-height: 320px; overflow: auto; }
.grande .gr-scroll { max-height: none; }
.gr-series { display: flex; gap: 4px; }
.gr-serie { padding: 3px 10px; border-radius: 999px; border: 1px solid var(--border); background: transparent; color: var(--text-secondary); font-size: 11px; cursor: pointer; }
.gr-serie:hover { color: var(--text-primary); }
.gr-serie.ativa { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.gr-numeros { display: flex; flex-direction: column; gap: 10px; container-type: inline-size; }
/* A fonte acompanha a largura do cartão (cqi) para o valor caber numa linha só, sem quebrar "R$". */
.gr-numero-valor { font-size: clamp(18px, 11cqi, 30px); font-weight: 700; color: var(--text-primary); line-height: 1.15; letter-spacing: -.5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.grande .gr-numero-valor { font-size: 64px; }
.gr-numero-leg { font-size: 12px; color: var(--text-muted); margin-top: 2px; }
.gr-moeda { display: inline-block; padding: 1px 6px; border-radius: 4px; background: var(--bg-elevated); color: var(--text-secondary); font-size: 10px; font-weight: 600; margin-right: 4px; }
svg { display: block; max-width: 100%; }
.grade { stroke: var(--border); stroke-width: 1; stroke-dasharray: 3 3; }
.eixo { fill: var(--text-muted); font-size: 11px; }
.eixo.rot { fill: var(--text-secondary); }
.eixo.val { fill: var(--text-primary); font-weight: 600; }
.centro { fill: var(--text-primary); font-size: 15px; font-weight: 700; }
.alvo { cursor: pointer; outline: none; transition: opacity .12s; }
.alvo:hover, .alvo:focus { opacity: .8; stroke: var(--text-primary); stroke-width: 1.5; }
.ponto { pointer-events: none; }
.gr-pizza { display: flex; flex-wrap: wrap; align-items: center; gap: 14px; }
.gr-legenda { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; flex: 1; min-width: 150px; max-height: 300px; overflow: auto; }
.gr-legenda li { display: flex; align-items: center; gap: 8px; font-size: 12px; color: var(--text-secondary); }
.gr-legenda .nome { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.gr-legenda .num { color: var(--text-primary); font-weight: 600; font-variant-numeric: tabular-nums; }
.sw { display: inline-block; width: 10px; height: 10px; border-radius: 3px; flex: none; }
.gr-leg { display: flex; align-items: center; gap: 6px; font-size: 11px; color: var(--text-muted); }
.trunc { color: var(--text-muted); }
.gr-tabela { width: 100%; border-collapse: collapse; font-size: 12px; }
.gr-tabela th { text-align: left; color: var(--text-muted); font-size: 10px; text-transform: uppercase; letter-spacing: .5px; padding: 6px 8px; border-bottom: 1px solid var(--border); position: sticky; top: 0; background: var(--bg-surface); }
.gr-tabela td { padding: 7px 8px; border-bottom: 1px solid var(--border); color: var(--text-secondary); max-width: 220px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.gr-tabela td:first-child { color: var(--text-primary); }
.gr-tabela .num { text-align: right; font-variant-numeric: tabular-nums; }
.gr-tabela tfoot td { font-weight: 700; color: var(--text-primary); border-bottom: 0; }
.grande .gr-tabela { font-size: 15px; }
.gr-dica { position: absolute; transform: translate(-50%, calc(-100% - 8px)); background: var(--bg-elevated); color: var(--text-primary); border: 1px solid var(--border); border-radius: 6px; padding: 6px 10px; font-size: 12px; line-height: 1.35; box-shadow: 0 6px 20px rgba(0, 0, 0, .35); pointer-events: none; z-index: 5; max-width: 280px; }
</style>
