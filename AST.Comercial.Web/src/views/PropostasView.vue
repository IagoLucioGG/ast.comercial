<script setup lang="ts">
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'
import NovaPropostaModal from '@/components/propostas/NovaPropostaModal.vue'

const route = useRoute()
const router = useRouter()

// O status de aprovação aparece como selo na grade (coluna "badge");
// os selos UiBadge (aprovação/aceite) ficam no detalhe e na página pública.
const colunas: ColunaConfig[] = [
  { campo: 'Numero', label: 'Nº' },
  { campo: 'Titulo', label: 'Título', tipo: 'avatar' },
  { campo: 'Valor', label: 'Valor', tipo: 'moeda' },
  { campo: 'StatusAprovacao', label: 'Status', tipo: 'badge' },
  { campo: 'StatusResposta', label: 'Resposta do cliente', tipo: 'badge' },
  { campo: 'Ativo', label: 'Situação', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]

const modalAberto = ref(false)

function numeroDaRota(v: unknown): number | null {
  const n = Number(Array.isArray(v) ? v[0] : v)
  return Number.isInteger(n) && n > 0 ? n : null
}
const negocioId = ref<number | null>(numeroDaRota(route.query.negocioId))
const clienteId = ref<number | null>(numeroDaRota(route.query.clienteId))

watch(
  () => route.query,
  (q) => {
    negocioId.value = numeroDaRota(q.negocioId)
    clienteId.value = numeroDaRota(q.clienteId)
    if (q.novo === '1') modalAberto.value = true
  },
  { immediate: true },
)

watch(modalAberto, (aberto) => {
  // limpa ?novo=1 ao fechar para não reabrir ao recarregar
  if (!aberto && route.query.novo) {
    const { novo: _novo, ...resto } = route.query
    router.replace({ query: resto })
  }
})

function aoCriar(id: number, modeloId: number | null) {
  router.push({ path: `/propostas/${id}`, query: modeloId ? { modelo: String(modeloId) } : {} })
}
</script>

<template>
  <TabelaEntidade
    entidade="Proposta"
    endpoint="Propostas"
    titulo="Proposta"
    :colunas="colunas"
    :campos-busca="['Titulo']"
    placeholder-busca="Buscar proposta..."
    ordenacao-padrao="CriadoEm desc"
    sem-form-interno
    @novo="modalAberto = true"
    @editar="(id: number) => router.push(`/propostas/${id}`)"
  />
  <NovaPropostaModal v-model="modalAberto" :negocio-id="negocioId" :cliente-id="clienteId" @criada="aoCriar" />
</template>
