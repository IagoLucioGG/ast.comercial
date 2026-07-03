<script setup lang="ts">
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'

const colunas: ColunaConfig[] = [
  { campo: 'Titulo', label: 'Título', tipo: 'avatar' },
  { campo: 'Valor', label: 'Valor', tipo: 'moeda' },
  { campo: 'Status', label: 'Status', tipo: 'badge' },
  { campo: 'Ativo', label: 'Situação', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]
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
  />
</template>
