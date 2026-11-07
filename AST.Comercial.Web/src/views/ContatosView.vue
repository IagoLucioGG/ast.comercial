<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { odataRequest, montarQuery } from '@/services/api'
import { campo, lista, mensagemErro, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'
import ContatoFormModal from '@/components/detalhe/ContatoFormModal.vue'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const colunas: ColunaConfig[] = [
  { campo: 'Nome', label: 'Nome', tipo: 'avatar' },
  { campo: 'Cliente/Nome', label: 'Cliente' },
  { campo: 'Cargo', label: 'Cargo' },
  { campo: 'Email', label: 'E-mail' },
  { campo: 'Telefone', label: 'Telefone' },
  { campo: 'Decisor', label: 'Decisor', tipo: 'booleano' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]

const formAberto = ref(false)
const tabelaRef = ref<InstanceType<typeof TabelaEntidade> | null>(null)

function abrirNovo() { formAberto.value = true }

// Abrir o contato leva ao cliente ao qual ele pertence.
async function abrirContato(id: number) {
  try {
    const q = montarQuery({ $filter: `Id eq ${id}`, $select: 'Id,ClienteId', $top: 1 })
    const c = lista<Registro>(await odataRequest<unknown>('GET', `PessoasContato${q}`))[0]
    const clienteId = campo<number>(c, 'ClienteId')
    if (!clienteId) { toast.error('Este contato não está vinculado a um cliente.'); return }
    router.push(`/clientes/${clienteId}`)
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível abrir o contato'))
  }
}

function aoSalvar() { tabelaRef.value?.carregar?.() }

// ?novo=1 abre o formulário de novo contato (e limpa o parâmetro para não reabrir ao voltar).
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
    entidade="PessoaContato"
    endpoint="PessoasContato"
    titulo="Contato"
    :colunas="colunas"
    :campos-busca="['Nome', 'Email', 'Cargo']"
    placeholder-busca="Buscar por nome, e-mail ou cargo..."
    ordenacao-padrao="Nome asc"
    sem-form-interno
    @novo="abrirNovo"
    @editar="abrirContato"
  />
  <ContatoFormModal v-model="formAberto" @salvo="aoSalvar" />
</template>
