<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ApiError } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { usePermissoes } from '@/composables/usePermissoes'
import { gerarPedidoDeProposta } from './pedidoApi'

const props = defineProps<{
  propostaId: number
  /** Proposta aprovada ou aceita pelo cliente. */
  liberada: boolean
}>()

const router = useRouter()
const toast = useToast()
const { pode } = usePermissoes()
const gerando = ref(false)

async function gerar() {
  gerando.value = true
  try {
    const r = await gerarPedidoDeProposta(props.propostaId)
    toast.success('Pedido gerado.')
    router.push(`/pedidos/${r.id}`)
  } catch (e) {
    toast.error(e instanceof ApiError ? e.message : 'Não foi possível gerar o pedido.')
  } finally {
    gerando.value = false
  }
}
</script>

<template>
  <button
    v-if="pode('Pedido', 'criar')"
    type="button" class="btn" :disabled="gerando || !liberada"
    :title="liberada ? 'Gerar um pedido com os itens desta proposta' : 'Só uma proposta aprovada ou aceita pelo cliente pode gerar pedido.'"
    @click="gerar"
  >
    <i class="mdi" :class="gerando ? 'mdi-loading mdi-spin' : 'mdi-cart-plus'"></i> Gerar pedido
  </button>
</template>
