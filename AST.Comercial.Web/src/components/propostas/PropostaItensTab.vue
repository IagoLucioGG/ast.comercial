<script setup lang="ts">
import { ref } from 'vue'
import BuscaAssincrona, { type OpcaoBusca } from './BuscaAssincrona.vue'
import { odataRequest } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { buscarProdutos, type ItemProposta } from '@/composables/usePropostaApi'
import { totalLinha, formatarMoeda, arredondar } from '@/composables/usePropostaTotais'

const props = defineProps<{
  propostaId: number
  moeda: string
  somenteLeitura?: boolean
}>()
const itens = defineModel<ItemProposta[]>({ required: true })
const emit = defineEmits<{ mudou: [] }>()

const toast = useToast()
const salvandoIds = ref<Set<number>>(new Set())
const adicionando = ref(false)

function corpo(i: ItemProposta) {
  return {
    ProdutoId: i.ProdutoId ?? null,
    ProdutoNome: i.ProdutoNome ?? '',
    ProdutoCodigo: i.ProdutoCodigo ?? null,
    Quantidade: Number(i.Quantidade ?? 0),
    PrecoUnitario: arredondar(Number(i.PrecoUnitario ?? 0)),
    Desconto: Number(i.Desconto ?? 0),
    Total: totalLinha(i),
    Ordem: i.Ordem,
  }
}

async function adicionar() {
  adicionando.value = true
  try {
    const ordem = itens.value.reduce((m, i) => Math.max(m, i.Ordem), 0) + 1
    const criado = await odataRequest<ItemProposta>('POST', 'Propostas@Produtos', {
      PropostaId: props.propostaId,
      ProdutoNome: '',
      Quantidade: 1,
      PrecoUnitario: 0,
      Desconto: 0,
      Total: 0,
      Ordem: ordem,
    })
    itens.value = [...itens.value, { ...criado }]
    emit('mudou')
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível adicionar o item.')
  } finally {
    adicionando.value = false
  }
}

async function salvar(i: ItemProposta) {
  i.Total = totalLinha(i)
  salvandoIds.value.add(i.Id)
  try {
    await odataRequest('PATCH', `Propostas@Produtos(${i.Id})`, corpo(i))
    emit('mudou')
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível salvar o item.')
  } finally {
    salvandoIds.value.delete(i.Id)
  }
}

async function remover(i: ItemProposta) {
  try {
    await odataRequest('DELETE', `Propostas@Produtos(${i.Id})`)
    itens.value = itens.value.filter((x) => x.Id !== i.Id)
    emit('mudou')
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível remover o item.')
  }
}

function escolherProduto(i: ItemProposta, o: OpcaoBusca) {
  const p = o.extra as { Id: number; Nome: string; Codigo?: string | null; Preco: number }
  sujos.delete(i.Id)
  i.ProdutoId = p.Id
  i.ProdutoNome = p.Nome
  i.ProdutoCodigo = p.Codigo ?? null
  i.PrecoUnitario = arredondar(p.Preco)
  if (!i.Quantidade) i.Quantidade = 1
  salvar(i)
}

const sujos = new Set<number>()

function aoSairDescricao(i: ItemProposta) {
  if (sujos.delete(i.Id)) salvar(i)
}

function digitarDescricao(i: ItemProposta, texto: string) {
  sujos.add(i.Id)
  // texto livre: desvincula do produto do catálogo
  i.ProdutoNome = texto
  if (i.ProdutoId) { i.ProdutoId = null; i.ProdutoCodigo = null }
}

function numero(i: ItemProposta, campo: 'Quantidade' | 'PrecoUnitario' | 'Desconto', ev: Event) {
  const v = Number((ev.target as HTMLInputElement).value)
  i[campo] = Number.isFinite(v) && v >= 0 ? v : 0
  if (campo === 'Desconto' && (i.Desconto ?? 0) > 100) i.Desconto = 100
}
</script>

<template>
  <div class="itens">
    <div class="tabela-wrap">
      <table class="tabela">
        <thead>
          <tr>
            <th class="c-prod">Produto / descrição</th>
            <th class="c-num">Qtd.</th>
            <th class="c-num">Preço</th>
            <th class="c-num">Desc. %</th>
            <th class="c-tot">Total</th>
            <th class="c-acao"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!itens.length">
            <td colspan="6" class="vazio">Nenhum item. Adicione o primeiro produto.</td>
          </tr>
          <tr v-for="i in itens" :key="i.Id">
            <td class="c-prod">
              <BuscaAssincrona
                :texto="i.ProdutoNome ?? ''"
                rotulo="Produto ou descrição"
                placeholder="Buscar produto ou digitar descrição"
                :buscar="buscarProdutos"
                :desabilitado="somenteLeitura"
                @selecionar="(o) => escolherProduto(i, o)"
                @digitar="(t) => digitarDescricao(i, t)"
                @focusout="aoSairDescricao(i)"
              />
              <small v-if="i.ProdutoCodigo" class="cod">{{ i.ProdutoCodigo }}</small>
            </td>
            <td class="c-num">
              <input type="number" min="0" step="any" aria-label="Quantidade" :value="i.Quantidade ?? 0" :disabled="somenteLeitura"
                @input="numero(i, 'Quantidade', $event)" @change="salvar(i)" />
            </td>
            <td class="c-num">
              <input type="number" min="0" step="0.01" aria-label="Preço unitário" :value="i.PrecoUnitario ?? 0" :disabled="somenteLeitura"
                @input="numero(i, 'PrecoUnitario', $event)" @change="salvar(i)" />
            </td>
            <td class="c-num">
              <input type="number" min="0" max="100" step="0.01" aria-label="Desconto em porcentagem" :value="i.Desconto ?? 0" :disabled="somenteLeitura"
                @input="numero(i, 'Desconto', $event)" @change="salvar(i)" />
            </td>
            <td class="c-tot">{{ formatarMoeda(totalLinha(i), moeda) }}</td>
            <td class="c-acao">
              <button v-if="!somenteLeitura" type="button" class="icone" aria-label="Remover item" :disabled="salvandoIds.has(i.Id)" @click="remover(i)">
                <i class="mdi mdi-delete-outline"></i>
              </button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <button v-if="!somenteLeitura" type="button" class="btn" :disabled="adicionando" @click="adicionar">
      <i class="mdi mdi-plus"></i> Adicionar item
    </button>
  </div>
</template>

<style scoped>
.itens { display: flex; flex-direction: column; gap: 12px; }
.tabela-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 8px; }
.tabela { width: 100%; border-collapse: collapse; min-width: 720px; }
th { text-align: left; font-size: 11px; text-transform: uppercase; letter-spacing: .04em; color: var(--text-muted); padding: 8px 10px; background: var(--bg-secondary); }
td { padding: 6px 10px; border-top: 1px solid var(--border); vertical-align: middle; }
.c-prod { min-width: 260px; }
.c-num { width: 110px; }
.c-tot { width: 130px; text-align: right; font-variant-numeric: tabular-nums; color: var(--text-primary); }
th.c-tot { text-align: right; }
.c-acao { width: 44px; text-align: center; }
.c-num input { width: 100%; padding: 6px 8px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); font-size: 13px; text-align: right; }
.c-num input:focus { outline: none; border-color: var(--accent); }
.cod { display: block; margin-top: 2px; font-size: 11px; color: var(--text-muted); }
.vazio { text-align: center; color: var(--text-muted); font-size: 13px; padding: 24px; }
.icone { background: none; border: 0; color: var(--text-secondary); cursor: pointer; font-size: 18px; padding: 4px; border-radius: 4px; }
.icone:hover { color: var(--danger); }
.btn { align-self: flex-start; display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; }
.btn:hover { border-color: var(--accent); color: var(--accent); }
.btn:disabled { opacity: .6; cursor: default; }
</style>
