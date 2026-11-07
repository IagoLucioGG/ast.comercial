<script setup lang="ts">
// Alternância "Minhas | Todas" das listas do resumo.
import type { Escopo } from './dashboardDados'

defineProps<{ modelValue: Escopo; rotulo: string }>()
defineEmits<{ (e: 'update:modelValue', v: Escopo): void }>()
</script>

<template>
  <div class="escopo" role="group" :aria-label="rotulo">
    <button type="button" :class="{ on: modelValue === 'minhas' }" :aria-pressed="modelValue === 'minhas'"
      @click="$emit('update:modelValue', 'minhas')">Minhas</button>
    <button type="button" :class="{ on: modelValue === 'todas' }" :aria-pressed="modelValue === 'todas'"
      @click="$emit('update:modelValue', 'todas')">Todas</button>
  </div>
</template>

<style scoped>
.escopo { display: inline-flex; border: 1px solid var(--border); border-radius: 6px; overflow: hidden; }
button { background: none; border: none; color: var(--text-secondary); font-size: 12px; padding: 3px 10px; cursor: pointer; }
button + button { border-left: 1px solid var(--border); }
button:hover { background: var(--bg-elevated); }
button:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
button.on { background: var(--accent); color: #fff; }
</style>
