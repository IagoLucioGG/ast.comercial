<script setup lang="ts">
import { useToast } from '@/composables/useToast'

const toast = useToast()
const icones = {
  success: 'mdi-check-circle-outline',
  error: 'mdi-alert-circle-outline',
  info: 'mdi-information-outline',
  warning: 'mdi-alert-outline',
} as const
</script>

<template>
  <Teleport to="body">
    <div class="toast-host" aria-live="polite">
      <div v-for="t in toast.itens" :key="t.id" class="toast" :class="`toast-${t.tipo}`" role="status">
        <i class="mdi" :class="icones[t.tipo]"></i>
        <span class="toast-msg">{{ t.mensagem }}</span>
        <button class="toast-x" aria-label="Fechar" @click="toast.remover(t.id)"><i class="mdi mdi-close"></i></button>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.toast-host { position: fixed; right: 16px; bottom: 16px; z-index: 3000; display: flex; flex-direction: column; gap: 8px; max-width: 380px; }
.toast { display: flex; align-items: flex-start; gap: 10px; padding: 10px 12px; border-radius: 8px; background: var(--bg-elevated); border: 1px solid var(--border); border-left-width: 3px; color: var(--text-primary); font-size: 13px; box-shadow: 0 6px 24px rgba(0, 0, 0, .35); }
.toast-msg { flex: 1; }
.toast-x { background: none; border: 0; color: var(--text-muted); cursor: pointer; padding: 0; line-height: 1; }
.toast-x:hover { color: var(--text-primary); }
.toast-success { border-left-color: #22c55e; }
.toast-success > .mdi:first-child { color: #22c55e; }
.toast-error { border-left-color: var(--danger); }
.toast-error > .mdi:first-child { color: var(--danger); }
.toast-info { border-left-color: var(--accent); }
.toast-info > .mdi:first-child { color: var(--accent); }
.toast-warning { border-left-color: #f59e0b; }
.toast-warning > .mdi:first-child { color: #f59e0b; }
</style>
