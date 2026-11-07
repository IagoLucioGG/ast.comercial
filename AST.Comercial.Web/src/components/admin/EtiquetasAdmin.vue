<script setup lang="ts">
import CadastroLista from './comum/CadastroLista.vue'
import { apiOdata, type CampoCadastro } from './comum/adminApi'

const api = apiOdata('Etiquetas')
const campos: CampoCadastro[] = [
  { chave: 'cor', rotulo: 'Cor', tipo: 'cor', lista: true, valorPadrao: '#06b6d4', ajuda: 'Formato #RRGGBB.' },
  { chave: 'nome', rotulo: 'Nome', tipo: 'texto', obrigatorio: true, maxLength: 100, lista: true },
]
</script>

<template>
  <CadastroLista singular="Etiqueta" artigo="a" :api="api" :campos="campos" vazio="Nenhuma etiqueta cadastrada." />
</template>
