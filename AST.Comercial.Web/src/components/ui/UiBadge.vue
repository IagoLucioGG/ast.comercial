<script setup lang="ts">
withDefaults(defineProps<{
  tom?: 'neutro' | 'sucesso' | 'perigo' | 'aviso' | 'info' | 'destaque'
}>(), { tom: 'neutro' })
</script>

<template>
  <span class="ui-badge" :class="`tom-${tom}`"><slot /></span>
</template>

<style scoped>
.ui-badge { display: inline-flex; align-items: center; gap: 4px; padding: 1px 8px; border-radius: 999px; font-size: 11px; font-weight: 600; border: 1px solid transparent; white-space: nowrap; }
.tom-neutro { background: var(--bg-elevated); color: var(--text-secondary); border-color: var(--border); }
.tom-sucesso { background: rgba(34, 197, 94, .14); color: #22c55e; }
.tom-perigo { background: rgba(239, 68, 68, .14); color: var(--danger); }
.tom-aviso { background: rgba(245, 158, 11, .14); color: #f59e0b; }
.tom-info { background: rgba(59, 130, 246, .14); color: #60a5fa; }
.tom-destaque { background: rgba(6, 182, 212, .14); color: var(--accent); }
</style>
