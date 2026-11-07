<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'

const props = defineProps<{
  modelValue: boolean
  modo: 'aprovar' | 'rejeitar'
  /** Comentário obrigatório (sempre na rejeição por fluxo). */
  obrigatorio?: boolean
  enviando?: boolean
  contexto?: string
}>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean]; confirmar: [comentario: string] }>()

const comentario = ref('')
const tentou = ref(false)
watch(() => props.modelValue, (a) => { if (a) { comentario.value = ''; tentou.value = false } })

const titulo = computed(() => (props.modo === 'aprovar' ? 'Aprovar' : 'Rejeitar'))
const invalido = computed(() => !!props.obrigatorio && comentario.value.trim().length === 0)

function confirmar() {
  tentou.value = true
  if (invalido.value) return
  emit('confirmar', comentario.value.trim())
}
</script>

<template>
  <UiModal :model-value="modelValue" :titulo="titulo" largura="480px" @update:model-value="emit('update:modelValue', $event)">
    <form class="form" @submit.prevent="confirmar">
      <p v-if="contexto" class="contexto">{{ contexto }}</p>
      <label class="campo">
        <span>Comentário <em v-if="obrigatorio">(obrigatório)</em><em v-else>(opcional)</em></span>
        <textarea
          v-model="comentario" rows="4" maxlength="1000" :aria-invalid="tentou && invalido"
          :placeholder="modo === 'rejeitar' ? 'Explique o motivo da rejeição' : 'Observação para quem solicitou'"
        ></textarea>
      </label>
      <p v-if="tentou && invalido" class="erro" role="alert">Informe o motivo da rejeição.</p>
    </form>
    <template #rodape>
      <button type="button" class="btn" :disabled="enviando" @click="emit('update:modelValue', false)">Cancelar</button>
      <button type="button" class="btn" :class="modo === 'aprovar' ? 'sucesso' : 'perigo'" :disabled="enviando" @click="confirmar">
        <i class="mdi" :class="modo === 'aprovar' ? 'mdi-check' : 'mdi-close'"></i>
        {{ enviando ? 'Enviando...' : (modo === 'aprovar' ? 'Aprovar' : 'Rejeitar') }}
      </button>
    </template>
  </UiModal>
</template>

<style scoped>
.form { display: flex; flex-direction: column; gap: 10px; }
.contexto { margin: 0; font-size: 13px; color: var(--text-secondary); overflow-wrap: anywhere; }
.campo { display: flex; flex-direction: column; gap: 5px; }
.campo > span { font-size: 12px; color: var(--text-secondary); }
.campo em { font-style: normal; color: var(--text-muted); }
textarea { padding: 8px 10px; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); font-size: 13px; font-family: inherit; resize: vertical; }
textarea:focus { outline: none; border-color: var(--accent); }
textarea[aria-invalid='true'] { border-color: var(--danger); }
.erro { margin: 0; font-size: 12px; color: var(--danger); }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; font-family: inherit; }
.btn:hover:not(:disabled) { border-color: var(--accent); }
.btn:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.btn:disabled { opacity: .5; cursor: default; }
.btn.sucesso { color: #22c55e; border-color: #22c55e; }
.btn.perigo { color: var(--danger); border-color: var(--danger); }
</style>
