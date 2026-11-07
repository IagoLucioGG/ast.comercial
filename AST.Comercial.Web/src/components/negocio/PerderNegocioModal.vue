<script setup lang="ts">
import { ref, watch } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'
import { useAcoesNegocio } from '@/composables/useAcoesNegocio'

const props = defineProps<{ modelValue: boolean; negocioId: number | null; titulo?: string }>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean]; perdido: [id: number] }>()

const { motivos, carregandoMotivos, processando, carregarMotivos, perder } = useAcoesNegocio()
const motivoId = ref<number | null>(null)
const tentou = ref(false)

watch(() => props.modelValue, (aberto) => {
  if (!aberto) return
  motivoId.value = null
  tentou.value = false
  carregarMotivos()
})

async function confirmar() {
  tentou.value = true
  if (props.negocioId === null) return
  if (motivos.value.length && !motivoId.value) return
  const ok = await perder(props.negocioId, motivoId.value)
  if (ok) {
    emit('perdido', props.negocioId)
    emit('update:modelValue', false)
  }
}
</script>

<template>
  <UiModal :model-value="modelValue" titulo="Perder negócio" largura="440px" @update:model-value="emit('update:modelValue', $event)">
    <p v-if="titulo" class="pn-nome">{{ titulo }}</p>
    <label class="pn-label" for="pn-motivo">Motivo da perda</label>
    <select id="pn-motivo" v-model="motivoId" class="pn-select" :disabled="carregandoMotivos" :aria-invalid="tentou && motivos.length > 0 && !motivoId">
      <option :value="null">{{ carregandoMotivos ? 'Carregando...' : 'Selecione um motivo' }}</option>
      <option v-for="m in motivos" :key="m.id" :value="m.id">{{ m.nome }}</option>
    </select>
    <p v-if="tentou && motivos.length > 0 && !motivoId" class="pn-erro" role="alert">Escolha o motivo da perda.</p>
    <p v-if="!carregandoMotivos && !motivos.length" class="pn-dica">Nenhum motivo de perda cadastrado. O negócio será marcado como perdido sem motivo.</p>
    <template #rodape>
      <button class="pn-btn" type="button" @click="emit('update:modelValue', false)">Cancelar</button>
      <button class="pn-btn pn-perigo" type="button" :disabled="processando" @click="confirmar">
        {{ processando ? 'Salvando...' : 'Marcar como perdido' }}
      </button>
    </template>
  </UiModal>
</template>

<style scoped>
.pn-nome { font-size: 13px; color: var(--text-secondary); margin-bottom: 14px; }
.pn-label { display: block; font-size: 12px; color: var(--text-secondary); margin-bottom: 6px; }
.pn-select { width: 100%; padding: 9px 12px; border-radius: 8px; border: 1px solid var(--border); background: var(--bg-primary); color: var(--text-primary); font-size: 13px; }
.pn-select:focus { outline: none; border-color: var(--accent); }
.pn-select[aria-invalid='true'] { border-color: var(--danger); }
.pn-erro { color: var(--danger); font-size: 12px; margin-top: 6px; }
.pn-dica { color: var(--text-muted); font-size: 12px; margin-top: 8px; }
.pn-btn { padding: 8px 16px; border-radius: 8px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); font-size: 12px; font-weight: 600; cursor: pointer; }
.pn-btn:hover { background: var(--bg-elevated); color: var(--text-primary); }
.pn-perigo { background: var(--danger); border-color: var(--danger); color: #fff; }
.pn-perigo:hover { background: var(--danger); color: #fff; filter: brightness(1.1); }
.pn-btn:disabled { opacity: .6; cursor: default; }
</style>
