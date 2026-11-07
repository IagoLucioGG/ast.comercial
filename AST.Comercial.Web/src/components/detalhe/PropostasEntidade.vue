<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { odataRequest, montarQuery } from '@/services/api'
import { campo, lista, mensagemErro, formatarData, formatarMoeda, STATUS_APROVACAO, type Registro } from '@/composables/useEntidadeDetalhe'
import UiBadge from '@/components/ui/UiBadge.vue'
import DetalheEstado from './DetalheEstado.vue'

/** Propostas de um negócio ou de um cliente, com link para o detalhe e criação de nova proposta. */
const props = defineProps<{
  filtroCampo: 'NegocioId' | 'ClienteId'
  paiId: number
  /** Ids extras enviados na rota "Nova proposta". */
  novaQuery: Record<string, string | number>
}>()

const itens = ref<Registro[]>([])
const carregando = ref(false)
const erro = ref('')

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({
      $filter: `${props.filtroCampo} eq ${props.paiId} and IsTemplate eq false`,
      $orderby: 'Data desc',
      $top: 100,
    })
    itens.value = lista<Registro>(await odataRequest<unknown>('GET', `Propostas${q}`))
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar as propostas')
  } finally {
    carregando.value = false
  }
}

const rotaNova = computed(() => ({ path: '/propostas', query: { novo: '1', ...Object.fromEntries(Object.entries(props.novaQuery).map(([k, v]) => [k, String(v)])) } }))

function status(p: Registro) {
  const s = campo(p, 'StatusAprovacao')
  return s === null || s === undefined ? null : STATUS_APROVACAO[s] ?? null
}

watch(() => [props.filtroCampo, props.paiId], carregar)
onMounted(carregar)
</script>

<template>
  <div>
    <div class="pe-barra">
      <RouterLink class="pe-novo" :to="rotaNova"><i class="mdi mdi-plus"></i> Nova proposta</RouterLink>
    </div>
    <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!itens.length" texto-vazio="Nenhuma proposta vinculada." icone="mdi-file-document-outline" @tentar="carregar">
      <div class="pe-wrap">
        <table class="pe-tab">
          <thead>
            <tr><th>Nº</th><th>Título</th><th>Data</th><th class="num">Valor</th><th>Aprovação</th></tr>
          </thead>
          <tbody>
            <tr v-for="p in itens" :key="campo(p, 'Id')">
              <td>{{ campo(p, 'Numero') ?? campo(p, 'Id') }}<span v-if="campo(p, 'Revisao')" class="pe-rev"> rev. {{ campo(p, 'Revisao') }}</span></td>
              <td><RouterLink class="pe-link" :to="`/propostas/${campo(p, 'Id')}`">{{ campo(p, 'Titulo') || `Proposta #${campo(p, 'Id')}` }}</RouterLink></td>
              <td>{{ formatarData(campo(p, 'Data')) }}</td>
              <td class="num">{{ formatarMoeda(campo(p, 'Valor')) }}</td>
              <td><UiBadge v-if="status(p)" :tom="status(p)!.tom">{{ status(p)!.nome }}</UiBadge><span v-else>-</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </DetalheEstado>
  </div>
</template>

<style scoped>
.pe-barra { display: flex; justify-content: flex-end; margin-bottom: 12px; }
.pe-novo { display: inline-flex; align-items: center; gap: 4px; padding: 7px 14px; border-radius: 8px; background: var(--accent); color: #000; font-size: 13px; font-weight: 600; text-decoration: none; }
.pe-novo:hover { background: var(--accent-hover); }
.pe-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 10px; }
.pe-tab { width: 100%; border-collapse: collapse; font-size: 13px; }
.pe-tab th { text-align: left; padding: 10px 14px; background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: .5px; border-bottom: 1px solid var(--border); }
.pe-tab td { padding: 10px 14px; border-bottom: 1px solid var(--border); color: var(--text-secondary); }
.pe-tab tr:last-child td { border-bottom: 0; }
.pe-tab .num { text-align: right; }
.pe-link { color: var(--accent); text-decoration: none; font-weight: 500; }
.pe-link:hover { text-decoration: underline; }
.pe-rev { font-size: 11px; color: var(--text-muted); }
</style>
