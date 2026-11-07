<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'
import FormularioDinamico from '@/components/shared/FormularioDinamico.vue'

const route = useRoute()
const router = useRouter()

const colunas: ColunaConfig[] = [
  { campo: 'Nome', label: 'Nome', tipo: 'avatar' },
  { campo: 'Documento', label: 'Documento' },
  { campo: 'Email', label: 'Email' },
  { campo: 'Telefone', label: 'Telefone' },
  { campo: 'Cidade', label: 'Cidade' },
  { campo: 'Ativo', label: 'Status', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]

const formAberto = ref(false)
const tabelaRef = ref<InstanceType<typeof TabelaEntidade> | null>(null)

// O fluxo de criação continua no formulário atual; editar leva ao detalhe do cliente.
function abrirNovo() { formAberto.value = true }
function abrirDetalhe(id: number) { router.push(`/clientes/${id}`) }
function onSalvo() { formAberto.value = false; tabelaRef.value?.carregar?.() }

// ?novo=1 abre o formulário de novo cliente (e limpa o parâmetro).
function verificarNovo() {
  if (route.query.novo === '1') {
    formAberto.value = true
    const { novo: _novo, ...resto } = route.query
    router.replace({ query: resto })
  }
}

watch(() => route.query.novo, verificarNovo)
onMounted(verificarNovo)
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
    @editar="abrirDetalhe"
  />
  <FormularioDinamico
    v-if="formAberto"
    entidade="Cliente"
    endpoint="Clientes"
    :id="null"
    titulo="Cliente"
    @fechar="formAberto = false"
    @salvo="onSalvo"
  />
</template>
