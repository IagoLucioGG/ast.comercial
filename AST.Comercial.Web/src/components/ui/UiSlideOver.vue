<script setup lang="ts">
import { onBeforeUnmount, watch } from 'vue'

const props = withDefaults(defineProps<{
  modelValue: boolean
  titulo?: string
  largura?: string
}>(), { largura: '520px' })

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
    <Transition name="slide">
      <div v-if="modelValue" class="so-overlay" @mousedown.self="fechar">
        <aside class="so-panel" role="dialog" aria-modal="true" :aria-label="titulo" :style="{ width: largura }">
          <header class="so-head">
            <slot name="cabecalho"><h3>{{ titulo }}</h3></slot>
            <button class="so-x" aria-label="Fechar" @click="fechar"><i class="mdi mdi-close"></i></button>
          </header>
          <div class="so-body"><slot /></div>
          <footer v-if="$slots.rodape" class="so-foot"><slot name="rodape" /></footer>
        </aside>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.so-overlay { position: fixed; inset: 0; background: rgba(0, 0, 0, .5); z-index: 1900; display: flex; justify-content: flex-end; }
.so-panel { max-width: 100vw; height: 100%; display: flex; flex-direction: column; background: var(--bg-surface); border-left: 1px solid var(--border); box-shadow: -12px 0 40px rgba(0, 0, 0, .4); }
.so-head { display: flex; align-items: center; justify-content: space-between; padding: 14px 18px; border-bottom: 1px solid var(--border); }
.so-head h3 { font-size: 15px; font-weight: 600; }
.so-x { background: none; border: 0; color: var(--text-secondary); cursor: pointer; font-size: 18px; line-height: 1; }
.so-x:hover { color: var(--text-primary); }
.so-body { flex: 1; overflow-y: auto; padding: 18px; }
.so-foot { display: flex; justify-content: flex-end; gap: 8px; padding: 12px 18px; border-top: 1px solid var(--border); }
.slide-enter-active, .slide-leave-active { transition: opacity .18s ease; }
.slide-enter-active .so-panel, .slide-leave-active .so-panel { transition: transform .22s ease; }
.slide-enter-from, .slide-leave-to { opacity: 0; }
.slide-enter-from .so-panel, .slide-leave-to .so-panel { transform: translateX(100%); }
</style>
