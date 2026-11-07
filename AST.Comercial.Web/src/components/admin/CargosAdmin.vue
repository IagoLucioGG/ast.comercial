<script setup lang="ts">
import CadastroLista from './comum/CadastroLista.vue'
import { apiOdata, type CampoCadastro } from './comum/adminApi'

const api = apiOdata('Cargos')
const campos: CampoCadastro[] = [
  { chave: 'nome', rotulo: 'Nome', tipo: 'texto', obrigatorio: true, maxLength: 100, lista: true },
  { chave: 'nivel', rotulo: 'Nível', tipo: 'numero', lista: true, valorPadrao: 0, ajuda: 'Número de 0 a 100 para ordenar a hierarquia (maior = mais alto).' },
  { chave: 'descricao', rotulo: 'Descrição', tipo: 'textarea', maxLength: 300, lista: true },
]
</script>

<template>
  <CadastroLista singular="Cargo" :api="api" :campos="campos" vazio="Nenhum cargo cadastrado." />
</template>
