<script setup lang="ts">
export interface AbaConfig {
  chave: string
  rotulo: string
  icone?: string
  contador?: number
}

defineProps<{ abas: AbaConfig[]; modelValue: string }>()
defineEmits<{ 'update:modelValue': [chave: string] }>()
</script>

<template>
  <div class="ui-tabs" role="tablist">
    <button
      v-for="a in abas"
      :key="a.chave"
      class="ui-tab"
      :class="{ ativa: a.chave === modelValue }"
      role="tab"
      :aria-selected="a.chave === modelValue"
      @click="$emit('update:modelValue', a.chave)"
    >
      <i v-if="a.icone" class="mdi" :class="a.icone"></i>
      <span>{{ a.rotulo }}</span>
      <span v-if="a.contador !== undefined" class="ui-tab-n">{{ a.contador }}</span>
    </button>
  </div>
</template>

<style scoped>
.ui-tabs { display: flex; gap: 2px; border-bottom: 1px solid var(--border); overflow-x: auto; }
.ui-tab { display: inline-flex; align-items: center; gap: 6px; padding: 10px 14px; background: none; border: 0; border-bottom: 2px solid transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; white-space: nowrap; }
.ui-tab:hover { color: var(--text-primary); }
.ui-tab.ativa { color: var(--accent); border-bottom-color: var(--accent); }
.ui-tab-n { background: var(--bg-elevated); border: 1px solid var(--border); border-radius: 10px; padding: 0 7px; font-size: 11px; color: var(--text-secondary); }
</style>
