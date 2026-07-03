<script setup lang="ts">
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'

const colunas: ColunaConfig[] = [
  { campo: 'Nome', label: 'Nome', tipo: 'avatar' },
  { campo: 'Descricao', label: 'Descrição' },
  { campo: 'Ativo', label: 'Status', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]
</script>

<template>
  <TabelaEntidade
    entidade="Equipe"
    endpoint="Equipes"
    titulo="Equipe"
    :colunas="colunas"
    :campos-busca="['Nome']"
    placeholder-busca="Buscar equipe..."
    ordenacao-padrao="Nome asc"
  />
</template>
