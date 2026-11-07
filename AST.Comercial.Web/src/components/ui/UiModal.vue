<script setup lang="ts">
import { onBeforeUnmount, watch } from 'vue'

const props = withDefaults(defineProps<{
  modelValue: boolean
  titulo?: string
  largura?: string
  fecharAoClicarFora?: boolean
}>(), { largura: '560px', fecharAoClicarFora: true })

const emit = defineEmits<{ 'update:modelValue': [v: boolean]; fechar: [] }>()

function fechar() {
  emit('update:modelValue', false)
  emit('fechar')
}

function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape') fechar()
}

watch(() => props.modelValue, (aberto) => {
  if (aberto) window.addEventListener('keydown', onKey)
  else window.removeEventListener('keydown', onKey)
}, { immediate: true })

onBeforeUnmount(() => window.removeEventListener('keydown', onKey))
</script>

<template>
  <Teleport to="body">
    <div v-if="modelValue" class="ui-modal-overlay" @mousedown.self="fecharAoClicarFora && fechar()">
      <div class="ui-modal" role="dialog" aria-modal="true" :aria-label="titulo" :style="{ width: largura }">
        <header v-if="titulo || $slots.cabecalho" class="ui-modal-head">
          <slot name="cabecalho"><h3>{{ titulo }}</h3></slot>
          <button class="ui-modal-x" aria-label="Fechar" @click="fechar"><i class="mdi mdi-close"></i></button>
        </header>
        <div class="ui-modal-body"><slot /></div>
        <footer v-if="$slots.rodape" class="ui-modal-foot"><slot name="rodape" /></footer>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
.ui-modal-overlay { position: fixed; inset: 0; background: rgba(0, 0, 0, .6); display: flex; align-items: center; justify-content: center; z-index: 2000; padding: 16px; }
.ui-modal { max-width: 100%; max-height: 90vh; display: flex; flex-direction: column; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 12px; box-shadow: 0 20px 60px rgba(0, 0, 0, .5); }
.ui-modal-head { display: flex; align-items: center; justify-content: space-between; padding: 14px 18px; border-bottom: 1px solid var(--border); }
.ui-modal-head h3 { font-size: 15px; font-weight: 600; }
.ui-modal-x { background: none; border: 0; color: var(--text-secondary); cursor: pointer; font-size: 18px; line-height: 1; }
.ui-modal-x:hover { color: var(--text-primary); }
.ui-modal-body { padding: 18px; overflow-y: auto; }
.ui-modal-foot { display: flex; justify-content: flex-end; gap: 8px; padding: 12px 18px; border-top: 1px solid var(--border); }
</style>
