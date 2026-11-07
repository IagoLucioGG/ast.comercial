<script setup lang="ts">
// Lista um valor por moeda (nunca soma moedas diferentes).
import { formatarValor, type Moeda, type TotalMoeda } from './dashboardDados'
defineProps<{ totais: TotalMoeda[]; moedas: Moeda[] }>()
</script>

<template>
  <ul class="valores">
    <li v-for="t in totais.filter((x) => x.total !== 0 || x.qtd > 0)" :key="t.moedaId ?? 'sem'">
      {{ formatarValor(t.total, t.moedaId, moedas) }}
    </li>
    <li v-if="!totais.some((x) => x.total !== 0)" class="zero">{{ formatarValor(0, null, moedas) }}</li>
  </ul>
</template>

<style scoped>
.valores { list-style: none; margin: 0; padding: 0; font-size: 13px; color: var(--text-secondary); }
.zero { color: var(--text-muted); }
</style>
