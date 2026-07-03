<script setup lang="ts">
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'

const colunas: ColunaConfig[] = [
  { campo: 'Nome', label: 'Nome', tipo: 'avatar' },
  { campo: 'Email', label: 'Email' },
  { campo: 'Telefone', label: 'Telefone' },
  { campo: 'Tipo', label: 'Tipo', tipo: 'badge' },
  { campo: 'Ativo', label: 'Status', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]
</script>

<template>
  <TabelaEntidade
    entidade="Cliente"
    endpoint="Clientes"
    titulo="Cliente"
    :colunas="colunas"
    :campos-busca="['Nome', 'Email']"
    placeholder-busca="Buscar por nome ou email..."
    ordenacao-padrao="Nome asc"
  />
</template>
