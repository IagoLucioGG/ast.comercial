<script setup lang="ts">
import { formatarMoeda } from '@/composables/usePropostaTotais'
import type { ItemPedido } from './pedidoApi'

defineProps<{ itens: ItemPedido[]; moeda?: string | null }>()
</script>

<template>
  <p v-if="!itens.length" class="vazio">Este pedido não tem itens.</p>
  <table v-else class="itens">
    <caption class="sr-only">Itens do pedido</caption>
    <thead>
      <tr>
        <th scope="col">Produto</th>
        <th scope="col" class="num">Qtd.</th>
        <th scope="col" class="num">Preço</th>
        <th scope="col" class="num">Desc. %</th>
        <th scope="col" class="num">Total</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="i in itens" :key="i.Id">
        <td>{{ i.ProdutoNome || '-' }}<small v-if="i.ProdutoCodigo"> · {{ i.ProdutoCodigo }}</small></td>
        <td class="num">{{ i.Quantidade ?? 0 }}</td>
        <td class="num">{{ formatarMoeda(i.PrecoUnitario, moeda) }}</td>
        <td class="num">{{ i.Desconto ?? 0 }}</td>
        <td class="num">{{ formatarMoeda(i.Total, moeda) }}</td>
      </tr>
    </tbody>
  </table>
</template>

<style scoped>
.vazio { color: var(--text-muted); font-size: 13px; }
.itens { width: 100%; border-collapse: collapse; font-size: 13px; }
.itens th { text-align: left; font-size: 11px; text-transform: uppercase; letter-spacing: .04em; color: var(--text-muted); padding: 8px 10px; border-bottom: 1px solid var(--border); }
.itens td { padding: 9px 10px; border-bottom: 1px solid var(--border); color: var(--text-primary); }
.itens small { color: var(--text-muted); }
.num { text-align: right; font-variant-numeric: tabular-nums; }
.sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
</style>
