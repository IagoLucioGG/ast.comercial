<script setup lang="ts">
import type { FunilItem } from '@/stores/funis'

defineProps<{ funis: FunilItem[]; modelValue: number | null }>()
const emit = defineEmits<{ 'update:modelValue': [id: number] }>()
</script>

<template>
  <label class="fs-wrap">
    <i class="mdi mdi-filter-variant" aria-hidden="true"></i>
    <span class="fs-sr">Funil</span>
    <select class="fs-select" :value="modelValue ?? ''" aria-label="Funil" @change="emit('update:modelValue', Number(($event.target as HTMLSelectElement).value))">
      <option v-for="f in funis" :key="f.id" :value="f.id">{{ f.nome }}</option>
    </select>
  </label>
</template>

<style scoped>
.fs-wrap { display: flex; align-items: center; gap: 8px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 8px; padding: 0 10px; color: var(--text-muted); }
.fs-wrap:focus-within { border-color: var(--accent); }
.fs-sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
.fs-select { border: 0; background: transparent; color: var(--text-primary); font-size: 13px; font-weight: 600; padding: 8px 4px; outline: none; cursor: pointer; min-width: 140px; }
.fs-select option { background: var(--bg-surface); color: var(--text-primary); }
</style>
