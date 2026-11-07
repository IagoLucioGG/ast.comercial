<script setup lang="ts">
import CadastroLista from './comum/CadastroLista.vue'
import { apiOdata, type CampoCadastro } from './comum/adminApi'

const api = apiOdata('Departamentos')

// Departamento pai: lista os demais departamentos (a validação de ciclo é feita no servidor).
function campos(itens: Record<string, unknown>[]): CampoCadastro[] {
  return [
    { chave: 'nome', rotulo: 'Nome', tipo: 'texto', obrigatorio: true, maxLength: 150, lista: true },
    {
      chave: 'departamentoPaiId', rotulo: 'Departamento pai', tipo: 'select', lista: true,
      opcoes: itens.map((i) => ({ valor: Number(i.id), rotulo: String(i.nome) })),
    },
    { chave: 'descricao', rotulo: 'Descrição', tipo: 'textarea', maxLength: 300, lista: true },
  ]
}
</script>

<template>
  <CadastroLista singular="Departamento" :api="api" :campos="campos" vazio="Nenhum departamento cadastrado." />
</template>
