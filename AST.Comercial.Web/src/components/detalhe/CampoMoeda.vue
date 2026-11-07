<script setup lang="ts">
import { computed } from 'vue'

/**
 * Campo monetário com máscara numérica: os dígitos digitados entram pelos centavos
 * (1 → 0,01; 12345 → 123,45) e o valor é exibido em pt-BR com separador de milhar.
 * O modelo é um número (ex.: 123.45) ou '' quando vazio.
 */
const props = defineProps<{
  modelValue: number | string | null
  simbolo?: string
  id?: string
  ariaLabel?: string
}>()
const emit = defineEmits<{ 'update:modelValue': [v: number | ''] }>()

const texto = computed(() => {
  if (props.modelValue === '' || props.modelValue === null || props.modelValue === undefined) return ''
  return Number(props.modelValue).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
})

function aoDigitar(e: Event) {
  const el = e.target as HTMLInputElement
  const digitos = el.value.replace(/\D/g, '').slice(0, 15)
  const valor: number | '' = digitos === '' ? '' : Number(digitos) / 100
  emit('update:modelValue', valor)
  // Reaplica a máscara no próprio input (o valor do modelo pode não mudar quando o texto é inválido).
  el.value = valor === '' ? '' : valor.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
}
</script>

<template>
  <div class="cm">
    <span v-if="simbolo" class="cm-simbolo" aria-hidden="true">{{ simbolo }}</span>
    <input :id="id" class="cm-input" type="text" inputmode="numeric" autocomplete="off" :aria-label="ariaLabel" :value="texto" placeholder="0,00" @input="aoDigitar" />
  </div>
</template>

<style scoped>
.cm { display: flex; align-items: stretch; width: 100%; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); overflow: hidden; }
.cm:focus-within { border-color: var(--accent); }
.cm-simbolo { display: flex; align-items: center; padding: 0 10px; background: var(--bg-elevated); color: var(--text-secondary); font-size: 13px; border-right: 1px solid var(--border); }
.cm-input { flex: 1; min-width: 0; padding: 9px 11px; border: 0; background: transparent; color: var(--text-primary); font-size: 14px; font-family: inherit; outline: none; text-align: right; }
</style>
