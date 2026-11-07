<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { odataRequest, montarQuery } from '@/services/api'
import { campo, lista, mensagemErro, formatarData, formatarValorMoeda, carregarMoedas, STATUS_APROVACAO, type Registro, type MoedaInfo } from '@/composables/useEntidadeDetalhe'
import UiBadge from '@/components/ui/UiBadge.vue'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'

/** Propostas que têm este produto (Propostas@Produtos). */
const props = defineProps<{ produtoId: number }>()

const itens = ref<Registro[]>([])
const moedas = ref<MoedaInfo[]>([])
const carregando = ref(false)
const erro = ref('')

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({
      $filter: `ProdutoId eq ${props.produtoId}`,
      $expand: 'Proposta($select=Id,Titulo,Numero,Revisao,Valor,MoedaId,Data,StatusAprovacao,UltimaRevisao)',
      $top: 100,
    })
    const [r, m] = await Promise.all([odataRequest<unknown>('GET', `Propostas@Produtos${q}`), carregarMoedas()])
    // Uma proposta pode ter o produto em mais de um item: mostra cada proposta uma vez.
    const vistos = new Set<unknown>()
    itens.value = lista<Registro>(r).filter((i) => {
      const p = campo(i, 'Proposta')
      if (!p || vistos.has(campo(p, 'Id'))) return false
      vistos.add(campo(p, 'Id'))
      return true
    })
    moedas.value = m
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar as propostas')
  } finally {
    carregando.value = false
  }
}

function dinheiro(p: Registro | undefined): string {
  const m = moedas.value.find((x) => x.id === campo(p, 'MoedaId'))
  return formatarValorMoeda(campo(p, 'Valor'), m?.simbolo, m?.codigo)
}
function status(p: Registro | undefined) {
  const s = campo<string | number>(p, 'StatusAprovacao')
  return s === undefined || s === null ? null : STATUS_APROVACAO[s]
}

watch(() => props.produtoId, carregar)
onMounted(carregar)
</script>

<template>
  <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!itens.length" texto-vazio="Nenhuma proposta com este produto." icone="mdi-file-document-outline" @tentar="carregar">
    <div class="pr-wrap">
      <table class="pr-tab">
        <thead><tr><th>Proposta</th><th>Aprovação</th><th class="num">Qtd.</th><th class="num">Valor da proposta</th><th>Data</th></tr></thead>
        <tbody>
          <tr v-for="i in itens" :key="campo(campo(i, 'Proposta'), 'Id')">
            <td>
              <RouterLink class="pr-link" :to="`/propostas/${campo(campo(i, 'Proposta'), 'Id')}`">
                <template v-if="campo(campo(i, 'Proposta'), 'Numero')">#{{ campo(campo(i, 'Proposta'), 'Numero') }} · </template>{{ campo(campo(i, 'Proposta'), 'Titulo') || 'Sem título' }}
              </RouterLink>
            </td>
            <td><UiBadge v-if="status(campo(i, 'Proposta'))" :tom="status(campo(i, 'Proposta'))!.tom">{{ status(campo(i, 'Proposta'))!.nome }}</UiBadge><span v-else>-</span></td>
            <td class="num">{{ campo(i, 'Quantidade') ?? '-' }}</td>
            <td class="num">{{ dinheiro(campo(i, 'Proposta')) }}</td>
            <td>{{ formatarData(campo(campo(i, 'Proposta'), 'Data')) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </DetalheEstado>
</template>

<style scoped>
.pr-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 10px; }
.pr-tab { width: 100%; border-collapse: collapse; font-size: 13px; }
.pr-tab th { text-align: left; padding: 10px 14px; background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: .5px; border-bottom: 1px solid var(--border); }
.pr-tab td { padding: 10px 14px; border-bottom: 1px solid var(--border); color: var(--text-secondary); }
.pr-tab tr:last-child td { border-bottom: 0; }
.pr-tab .num { text-align: right; white-space: nowrap; }
.pr-link { color: var(--accent); text-decoration: none; font-weight: 500; }
.pr-link:hover { text-decoration: underline; }
</style>
