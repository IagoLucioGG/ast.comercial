<script setup lang="ts">
import GraficoConteudo from './GraficoConteudo.vue'
import type { Catalogo, Grafico } from './relatoriosApi'

defineProps<{
  grafico: Grafico
  catalogo: Catalogo | null
  periodoPainel: string | null
  podeEditar: boolean
  primeiro: boolean
  ultimo: boolean
  versao: number
}>()
defineEmits<{ editar: []; excluir: []; mover: [direcao: -1 | 1]; ampliar: [] }>()

const icones: Record<string, string> = { Numero: 'numeric', Barras: 'chart-bar', Linhas: 'chart-line', Pizza: 'chart-donut', Tabela: 'table' }
</script>

<template>
  <article class="card" :class="`w${grafico.largura}`">
    <header class="card-head">
      <i class="mdi" :class="`mdi-${icones[grafico.tipo] ?? 'chart-box'}`" aria-hidden="true"></i>
      <h3 :title="grafico.titulo">{{ grafico.titulo }}</h3>
      <div class="acoes">
        <button class="ic" aria-label="Ver em tela cheia" title="Ampliar" @click="$emit('ampliar')"><i class="mdi mdi-arrow-expand"></i></button>
        <template v-if="podeEditar">
          <button class="ic" aria-label="Mover para trás" title="Mover para trás" :disabled="primeiro" @click="$emit('mover', -1)"><i class="mdi mdi-arrow-left"></i></button>
          <button class="ic" aria-label="Mover para frente" title="Mover para frente" :disabled="ultimo" @click="$emit('mover', 1)"><i class="mdi mdi-arrow-right"></i></button>
          <button class="ic" aria-label="Editar gráfico" title="Editar" @click="$emit('editar')"><i class="mdi mdi-pencil-outline"></i></button>
          <button class="ic perigo" aria-label="Excluir gráfico" title="Excluir" @click="$emit('excluir')"><i class="mdi mdi-trash-can-outline"></i></button>
        </template>
      </div>
    </header>
    <GraficoConteudo
      :definicao="grafico" :tipo="grafico.tipo" :periodo-painel="periodoPainel" :catalogo="catalogo" :versao="versao"
    />
  </article>
</template>

<style scoped>
.card { grid-column: span 2; min-width: 0; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 12px; padding: 14px 16px 16px; display: flex; flex-direction: column; gap: 10px; }
.card.w1 { grid-column: span 1; }
.card.w2 { grid-column: span 2; }
.card.w3 { grid-column: span 3; }
.card.w4 { grid-column: span 4; }
.card-head { display: flex; align-items: center; gap: 8px; min-width: 0; }
.card-head > i { color: var(--accent); font-size: 18px; }
.card-head h3 { flex: 1; min-width: 0; font-size: 13px; font-weight: 600; color: var(--text-primary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.acoes { display: flex; gap: 2px; opacity: .55; transition: opacity .15s; }
.card:hover .acoes, .card:focus-within .acoes { opacity: 1; }
.ic { width: 26px; height: 26px; display: inline-flex; align-items: center; justify-content: center; border: 0; background: none; color: var(--text-secondary); border-radius: 6px; cursor: pointer; font-size: 15px; }
.ic:hover:not(:disabled) { background: var(--bg-elevated); color: var(--accent); }
.ic.perigo:hover:not(:disabled) { color: var(--danger); }
.ic:disabled { opacity: .3; cursor: default; }
/* Cartão estreito: o título ganha a linha inteira e as ações descem para a linha de baixo
   (antes os botões espremiam o título até sobrar só a primeira letra). */
.card.w1 .card-head { flex-wrap: wrap; row-gap: 4px; }
.card.w1 .card-head h3 { flex: 1 1 calc(100% - 30px); }
.card.w1 .acoes { flex: 1 1 100%; justify-content: flex-end; }
@media (max-width: 1180px) {
  .card.w3, .card.w4 { grid-column: span 2; }
}
@media (max-width: 720px) {
  .card, .card.w1, .card.w2, .card.w3, .card.w4 { grid-column: span 1; }
}
</style>
