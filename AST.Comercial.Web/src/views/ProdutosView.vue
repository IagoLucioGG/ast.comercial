<script setup lang="ts">
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'

const colunas: ColunaConfig[] = [
  { campo: 'Nome', label: 'Nome', tipo: 'avatar' },
  { campo: 'Codigo', label: 'Código' },
  { campo: 'Preco', label: 'Preço', tipo: 'moeda' },
  { campo: 'Ativo', label: 'Status', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]
</script>

<template>
  <TabelaEntidade
    entidade="Produto"
    endpoint="Produtos"
    titulo="Produto"
    :colunas="colunas"
    :campos-busca="['Nome', 'Codigo']"
    placeholder-busca="Buscar produto..."
    ordenacao-padrao="Nome asc"
  />
</template>
