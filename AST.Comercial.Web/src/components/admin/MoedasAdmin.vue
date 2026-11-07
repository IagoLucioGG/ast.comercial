<script setup lang="ts">
import CadastroLista from './comum/CadastroLista.vue'
import { apiOdata, type ApiCadastro, type CampoCadastro } from './comum/adminApi'

// Somente leitura: a API de moedas só expõe GET.
const completa = apiOdata('Moedas')
const api: ApiCadastro = { carregar: completa.carregar }
const campos: CampoCadastro[] = [
  { chave: 'codigo', rotulo: 'Código', tipo: 'texto', lista: true },
  { chave: 'nome', rotulo: 'Nome', tipo: 'texto', lista: true },
  { chave: 'simbolo', rotulo: 'Símbolo', tipo: 'texto', lista: true },
  { chave: 'padrao', rotulo: 'Padrão', tipo: 'checkbox', lista: true },
]
</script>

<template>
  <CadastroLista
    singular="Moeda"
    artigo="a"
    :api="api"
    :campos="campos"
    vazio="Nenhuma moeda cadastrada."
    aviso="Somente leitura: a API de moedas ainda não permite criar nem editar."
  />
</template>
