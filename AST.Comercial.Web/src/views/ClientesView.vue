<script setup lang="ts">
import { ref } from 'vue'
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'
import FormularioDinamico from '@/components/shared/FormularioDinamico.vue'

const colunas: ColunaConfig[] = [
  { campo: 'Nome', label: 'Nome', tipo: 'avatar' },
  { campo: 'Email', label: 'Email' },
  { campo: 'Telefone', label: 'Telefone' },
  { campo: 'Tipo', label: 'Tipo', tipo: 'badge' },
  { campo: 'Ativo', label: 'Status', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]

const formAberto = ref(false)
const formId = ref<number | null>(null)
const tabelaRef = ref<InstanceType<typeof TabelaEntidade> | null>(null)

function abrirNovo() { formId.value = null; formAberto.value = true }
function abrirEditar(id: number) { formId.value = id; formAberto.value = true }
function onSalvo() { formAberto.value = false; tabelaRef.value?.carregar?.() }
</script>

<template>
  <TabelaEntidade
    ref="tabelaRef"
    entidade="Cliente"
    endpoint="Clientes"
    titulo="Cliente"
    :colunas="colunas"
    :campos-busca="['Nome', 'Email']"
    placeholder-busca="Buscar por nome ou email..."
    ordenacao-padrao="Nome asc"
    sem-form-interno
    @novo="abrirNovo"
    @editar="abrirEditar"
  />
  <FormularioDinamico
    v-if="formAberto"
    entidade="Cliente"
    endpoint="Clientes"
    :id="formId"
    titulo="Cliente"
    @fechar="formAberto = false"
    @salvo="onSalvo"
  />
</template>
