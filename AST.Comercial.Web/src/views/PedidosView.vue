<script setup lang="ts">
import { useRouter } from 'vue-router'
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'
import { useToast } from '@/composables/useToast'

const router = useRouter()
const toast = useToast()

const colunas: ColunaConfig[] = [
  { campo: 'Numero', label: 'Nº' },
  { campo: 'Titulo', label: 'Título', tipo: 'avatar' },
  { campo: 'Cliente/Nome', label: 'Cliente' },
  { campo: 'Etapa/Nome', label: 'Etapa' },
  { campo: 'Valor', label: 'Valor', tipo: 'moeda' },
  { campo: 'Data', label: 'Data', tipo: 'data' },
]

// Pedido nasce de uma proposta aprovada ou aceita; não há cadastro manual.
function novo() {
  toast.info('Pedidos são gerados a partir de uma proposta aprovada ou aceita.')
  router.push('/propostas')
}
</script>

<template>
  <TabelaEntidade
    entidade="Pedido"
    endpoint="Pedidos"
    titulo="Pedido"
    :colunas="colunas"
    :campos-busca="['Titulo']"
    placeholder-busca="Buscar pedido..."
    ordenacao-padrao="Numero desc"
    campo-moeda="MoedaId"
    sem-form-interno
    @novo="novo"
    @editar="(id: number) => router.push(`/pedidos/${id}`)"
  />
</template>
