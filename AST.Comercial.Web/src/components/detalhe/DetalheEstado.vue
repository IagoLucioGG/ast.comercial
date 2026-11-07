<script setup lang="ts">
// Casca de estados (carregando, erro, vazio) usada pelas abas das telas de detalhe.
withDefaults(defineProps<{
  carregando?: boolean
  erro?: string
  vazio?: boolean
  textoVazio?: string
  icone?: string
}>(), { textoVazio: 'Nenhum registro encontrado.', icone: 'mdi-inbox-outline' })
defineEmits<{ tentar: [] }>()
</script>

<template>
  <div v-if="carregando" class="de-estado" role="status" aria-live="polite">
    <i class="mdi mdi-loading mdi-spin"></i> Carregando...
  </div>
  <div v-else-if="erro" class="de-estado de-erro" role="alert">
    <i class="mdi mdi-alert-circle-outline"></i>
    <span>{{ erro }}</span>
    <button class="de-btn" @click="$emit('tentar')">Tentar novamente</button>
  </div>
  <div v-else-if="vazio" class="de-estado">
    <i class="mdi" :class="icone"></i>
    <span>{{ textoVazio }}</span>
    <slot name="vazio" />
  </div>
  <slot v-else />
</template>

<style scoped>
.de-estado { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 36px 16px; color: var(--text-muted); font-size: 13px; text-align: center; }
.de-estado .mdi { font-size: 26px; }
.de-erro { color: var(--danger); }
.de-btn { margin-top: 4px; padding: 6px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-secondary); font-size: 12px; cursor: pointer; }
.de-btn:hover { background: var(--bg-elevated); color: var(--text-primary); }
</style>
