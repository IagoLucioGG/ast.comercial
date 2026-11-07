<script setup lang="ts">
// Listas de apoio configuráveis (origens, ramos, portes, status de cliente, motivos de perda,
// status de negócio, famílias e grupos de produto). Backend: odata/AdminCadastros/{tipo}.
import { computed } from 'vue'
import CadastroLista from './comum/CadastroLista.vue'
import { apiCadastroAux, type CampoCadastro } from './comum/adminApi'

type Tipo = 'origens' | 'ramos' | 'portes' | 'status-cliente' | 'motivos-perda' | 'status-negocio' | 'familias-produto' | 'grupos-produto'
const props = defineProps<{ tipo: Tipo }>()

const nome = (rotulo = 'Nome'): CampoCadastro => ({ chave: 'nome', rotulo, tipo: 'texto', obrigatorio: true, maxLength: 150, lista: true })
const ordem: CampoCadastro = { chave: 'ordem', rotulo: 'Ordem', tipo: 'numero', lista: true, valorPadrao: 0, ajuda: 'Menor número aparece primeiro.' }
const cor: CampoCadastro = { chave: 'cor', rotulo: 'Cor', tipo: 'cor', lista: true, valorPadrao: '#06b6d4', ajuda: 'Formato #RRGGBB.' }
const padrao: CampoCadastro = { chave: 'padrao', rotulo: 'Padrão', tipo: 'checkbox', lista: true, ajuda: 'Só um registro pode ser o padrão.' }

interface Config { singular: string; artigo: 'o' | 'a'; vazio: string; campos: (itens: Record<string, unknown>[]) => CampoCadastro[] }

const configs: Record<Tipo, Config> = {
  origens: { singular: 'Origem', artigo: 'a', vazio: 'Nenhuma origem cadastrada.', campos: () => [nome(), { chave: 'descricao', rotulo: 'Descrição', tipo: 'textarea', maxLength: 300, lista: true }, ordem] },
  ramos: { singular: 'Ramo de atividade', artigo: 'o', vazio: 'Nenhum ramo cadastrado.', campos: () => [nome(), ordem] },
  portes: { singular: 'Porte', artigo: 'o', vazio: 'Nenhum porte cadastrado.', campos: () => [nome(), ordem] },
  'status-cliente': { singular: 'Status de cliente', artigo: 'o', vazio: 'Nenhum status cadastrado.', campos: () => [nome(), cor, ordem, padrao] },
  'motivos-perda': { singular: 'Motivo de perda', artigo: 'o', vazio: 'Nenhum motivo cadastrado.', campos: () => [nome(), ordem] },
  'status-negocio': {
    singular: 'Status de negócio', artigo: 'o', vazio: 'Nenhum status cadastrado.',
    campos: () => [
      nome(), cor,
      { chave: 'tipoBase', rotulo: 'Tipo base', tipo: 'select', lista: true, obrigatorio: true, valorPadrao: 'Aberto', opcoes: [{ valor: 'Aberto', rotulo: 'Aberto' }, { valor: 'Ganho', rotulo: 'Ganho' }, { valor: 'Perdido', rotulo: 'Perdido' }], ajuda: 'Define se o status representa um negócio aberto, ganho ou perdido.' },
      ordem, padrao,
    ],
  },
  'familias-produto': { singular: 'Família de produto', artigo: 'a', vazio: 'Nenhuma família cadastrada.', campos: () => [nome(), ordem] },
  'grupos-produto': {
    singular: 'Grupo de produto', artigo: 'o', vazio: 'Nenhum grupo cadastrado.',
    campos: () => [nome(), { chave: 'familiaProdutoId', rotulo: 'Família', tipo: 'select', lista: true, opcoes: familias.value }, ordem],
  },
}

// Famílias para o select de grupos (carregadas só nesse tipo).
import { ref, onMounted } from 'vue'
import { odataRequest } from '@/services/api'
const familias = ref<{ valor: number; rotulo: string }[]>([])
onMounted(async () => {
  if (props.tipo !== 'grupos-produto') return
  try {
    const r = await odataRequest<{ id: number; nome: string }[]>('GET', 'AdminCadastros/familias-produto')
    familias.value = r.map((f) => ({ valor: f.id, rotulo: f.nome }))
  } catch { /* o select fica vazio; a lista principal mostra o erro próprio */ }
})

const cfg = computed(() => configs[props.tipo])
const api = computed(() => apiCadastroAux(props.tipo))
</script>

<template>
  <CadastroLista
    :key="tipo"
    :singular="cfg.singular"
    :artigo="cfg.artigo"
    :api="api"
    :campos="cfg.campos"
    :vazio="cfg.vazio"
  />
</template>
