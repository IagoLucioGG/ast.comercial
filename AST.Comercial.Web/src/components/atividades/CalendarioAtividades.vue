<script setup lang="ts">
import { computed } from 'vue'
import {
  TIPO_META, chaveDia, formatarHora, intervaloCalendario, iniciais, nomeCurto,
  type Atividade, type ModoCalendario,
} from './atividades'

const props = defineProps<{
  modo: ModoCalendario
  dataRef: Date
  itens: Atividade[]
}>()

const emit = defineEmits<{
  abrir: [a: Atividade]
  novo: [data: Date]
}>()

const NOMES_DIA = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb']
const MAX_CHIPS_MES = 3

const dias = computed(() => intervaloCalendario(props.modo, props.dataRef).dias)
const hojeChave = chaveDia(new Date())

const porDia = computed(() => {
  const mapa = new Map<string, Atividade[]>()
  for (const a of props.itens) {
    if (!a.DataVencimento) continue
    const k = chaveDia(a.DataVencimento)
    const lista = mapa.get(k)
    if (lista) lista.push(a)
    else mapa.set(k, [a])
  }
  for (const lista of mapa.values()) {
    lista.sort((x, y) => (x.DataVencimento!.getTime() - y.DataVencimento!.getTime()))
  }
  return mapa
})

function doDia(d: Date): Atividade[] { return porDia.value.get(chaveDia(d)) ?? [] }
function foraDoMes(d: Date): boolean { return props.modo === 'mes' && d.getMonth() !== props.dataRef.getMonth() }
function ehHoje(d: Date): boolean { return chaveDia(d) === hojeChave }
function visiveis(d: Date): Atividade[] {
  const l = doDia(d)
  return props.modo === 'mes' ? l.slice(0, MAX_CHIPS_MES) : l
}
function extras(d: Date): number {
  return props.modo === 'mes' ? Math.max(0, doDia(d).length - MAX_CHIPS_MES) : 0
}

function criarNoDia(d: Date) {
  // Dia sem hora definida: sugere 09:00 locais.
  emit('novo', new Date(d.getFullYear(), d.getMonth(), d.getDate(), 9, 0))
}

function atrasada(a: Atividade): boolean {
  return !a.Concluida && !!a.DataVencimento && a.DataVencimento.getTime() < Date.now()
}
</script>

<template>
  <div class="cal" :class="modo">
    <div class="cal-cab" role="row">
      <div v-for="n in NOMES_DIA" :key="n" class="cab-dia" role="columnheader">{{ n }}</div>
    </div>
    <div class="cal-grade" role="grid">
      <div v-for="d in dias" :key="d.toISOString()" class="cel" role="gridcell"
        :class="{ fora: foraDoMes(d), hoje: ehHoje(d) }"
        :aria-label="d.toLocaleDateString('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })"
        tabindex="0"
        @click="criarNoDia(d)" @keydown.enter.self="criarNoDia(d)">
        <div class="num">
          <span>{{ d.getDate() }}</span>
        </div>
        <div class="chips">
          <button v-for="a in visiveis(d)" :key="a.Id" type="button" class="chip"
            :class="{ feita: a.Concluida, atrasada: atrasada(a) }"
            :style="{ '--cor': TIPO_META[a.Tipo].cor }"
            :title="`${TIPO_META[a.Tipo].rotulo}: ${a.Titulo}`"
            @click.stop="emit('abrir', a)">
            <i class="mdi" :class="TIPO_META[a.Tipo].icone"></i>
            <span class="hora">{{ formatarHora(a.DataVencimento!) }}</span>
            <span class="tit">{{ a.Titulo }}</span>
            <span v-if="a.ResponsavelNome" class="ini" :title="'Responsável: ' + nomeCurto(a.ResponsavelNome)">{{ iniciais(a.ResponsavelNome) }}</span>
          </button>
          <span v-if="extras(d)" class="mais">+{{ extras(d) }} mais</span>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.cal { display: flex; flex-direction: column; border: 1px solid var(--border); border-radius: 10px; overflow: hidden; background: var(--bg-surface); }
.cal-cab { display: grid; grid-template-columns: repeat(7, 1fr); background: var(--bg-secondary); border-bottom: 1px solid var(--border); }
.cab-dia { padding: 8px; text-align: center; font-size: 11px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: .04em; }
.cal-grade { display: grid; grid-template-columns: repeat(7, 1fr); }
.cel { min-height: 104px; padding: 4px 5px 6px; border-right: 1px solid var(--border); border-bottom: 1px solid var(--border); cursor: pointer; min-width: 0; outline: none; }
.cel:nth-child(7n) { border-right: 0; }
.cel:hover { background: var(--bg-elevated); }
.cel:focus-visible { box-shadow: inset 0 0 0 2px var(--accent); }
.cel.fora { background: var(--bg-secondary); }
.cel.fora .num { opacity: .45; }
.num { display: flex; justify-content: flex-end; font-size: 12px; color: var(--text-secondary); margin-bottom: 3px; }
.cel.hoje .num span { background: var(--accent); color: #000; font-weight: 700; border-radius: 50%; min-width: 22px; height: 22px; display: inline-flex; align-items: center; justify-content: center; }
.semana .cel { min-height: 420px; }
.chips { display: flex; flex-direction: column; gap: 3px; }
.chip {
  display: flex; align-items: center; gap: 4px; width: 100%; padding: 2px 6px; font-size: 11px;
  border: 0; border-left: 3px solid var(--cor); border-radius: 4px; cursor: pointer; text-align: left;
  background: color-mix(in srgb, var(--cor) 18%, transparent); color: var(--text-primary); min-width: 0;
}
.chip:hover { background: color-mix(in srgb, var(--cor) 32%, transparent); }
.chip .mdi { color: var(--cor); font-size: 13px; flex-shrink: 0; }
.chip .hora { color: var(--text-secondary); flex-shrink: 0; }
.chip .tit { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.chip .ini { margin-left: auto; flex-shrink: 0; font-size: 9px; font-weight: 700; color: var(--text-secondary); background: var(--bg-elevated); border-radius: 8px; padding: 0 4px; }
.chip.feita { opacity: .55; }
.chip.feita .tit { text-decoration: line-through; }
.chip.atrasada .hora { color: var(--danger); font-weight: 600; }
.mais { font-size: 11px; color: var(--text-muted); padding-left: 4px; }
</style>
