<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { odataRequest, montarQuery } from '@/services/api'
import { campo, lista, mensagemErro, formatarData, formatarValorMoeda, carregarMoedas, type Registro, type MoedaInfo } from '@/composables/useEntidadeDetalhe'
import UiBadge from '@/components/ui/UiBadge.vue'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'

/** Negócios que têm este produto em ItensNegocio. */
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
      $expand: 'Negocio($select=Id,Titulo,Valor,MoedaId,FechadoEm,CriadoEm)',
      $orderby: 'CriadoEm desc',
      $top: 100,
    })
    const [r, m] = await Promise.all([odataRequest<unknown>('GET', `ItensNegocio${q}`), carregarMoedas()])
    itens.value = lista<Registro>(r).filter((i) => campo(i, 'Negocio'))
    moedas.value = m
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar os negócios')
  } finally {
    carregando.value = false
  }
}

function moedaDe(i: Registro): MoedaInfo | undefined {
  const id = campo<number>(campo(i, 'Negocio'), 'MoedaId')
  return moedas.value.find((m) => m.id === id)
}
function dinheiro(i: Registro, valor: number | undefined): string {
  const m = moedaDe(i)
  return formatarValorMoeda(valor, m?.simbolo, m?.codigo)
}

watch(() => props.produtoId, carregar)
onMounted(carregar)
</script>

<template>
  <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!itens.length" texto-vazio="Nenhum negócio com este produto." icone="mdi-handshake-outline" @tentar="carregar">
    <div class="pn-wrap">
      <table class="pn-tab">
        <thead><tr><th>Negócio</th><th>Situação</th><th class="num">Qtd.</th><th class="num">Total do item</th><th class="num">Valor do negócio</th><th>Criado em</th></tr></thead>
        <tbody>
          <tr v-for="i in itens" :key="campo(i, 'Id')">
            <td><RouterLink class="pn-link" :to="`/negocios/${campo(campo(i, 'Negocio'), 'Id')}`">{{ campo(campo(i, 'Negocio'), 'Titulo') }}</RouterLink></td>
            <td><UiBadge :tom="campo(campo(i, 'Negocio'), 'FechadoEm') ? 'neutro' : 'info'">{{ campo(campo(i, 'Negocio'), 'FechadoEm') ? 'Fechado' : 'Aberto' }}</UiBadge></td>
            <td class="num">{{ campo(i, 'Quantidade') }}</td>
            <td class="num">{{ dinheiro(i, campo(i, 'Total')) }}</td>
            <td class="num">{{ dinheiro(i, campo(campo(i, 'Negocio'), 'Valor')) }}</td>
            <td>{{ formatarData(campo(campo(i, 'Negocio'), 'CriadoEm')) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </DetalheEstado>
</template>

<style scoped>
.pn-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 10px; }
.pn-tab { width: 100%; border-collapse: collapse; font-size: 13px; }
.pn-tab th { text-align: left; padding: 10px 14px; background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: .5px; border-bottom: 1px solid var(--border); }
.pn-tab td { padding: 10px 14px; border-bottom: 1px solid var(--border); color: var(--text-secondary); }
.pn-tab tr:last-child td { border-bottom: 0; }
.pn-tab .num { text-align: right; white-space: nowrap; }
.pn-link { color: var(--accent); text-decoration: none; font-weight: 500; }
.pn-link:hover { text-decoration: underline; }
</style>
