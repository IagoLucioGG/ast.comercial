<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { montarQuery, odataRequest } from '@/services/api'
import { campo, formatarValorMoeda, lista, mensagemErro, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import BuscaProduto, { type ProdutoBusca } from '@/components/negocio/BuscaProduto.vue'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'

const props = defineProps<{
  negocioId: number
  /** Símbolo/código da moeda do negócio (os itens são sempre na moeda do negócio). */
  simbolo?: string
  codigo?: string
  valorNegocio?: number | null
  fechado?: boolean
}>()
const emit = defineEmits<{ valorAtualizado: [] }>()

interface Linha {
  id: number
  produtoId: number | null
  descricao: string
  quantidade: number
  preco: number
  desconto: number
  total: number
  ordem: number
  salvando: boolean
}

const toast = useToast()
const linhas = ref<Linha[]>([])
const carregando = ref(false)
const erro = ref('')
const adicionando = ref(false)
const aplicando = ref(false)

const arred = (n: number) => Math.round((n + Number.EPSILON) * 100) / 100
/** Mesmo cálculo do servidor: qtd × preço × (1 − desc/100), 2 casas. */
const totalDe = (l: Pick<Linha, 'quantidade' | 'preco' | 'desconto'>) => arred(l.quantidade * l.preco * (1 - l.desconto / 100))
const dinheiro = (n: number | null | undefined) => formatarValorMoeda(n ?? 0, props.simbolo, props.codigo)

function paraLinha(r: Registro): Linha {
  return {
    id: Number(campo(r, 'Id')),
    produtoId: campo<number | null>(r, 'ProdutoId') ?? null,
    descricao: String(campo(r, 'Descricao') ?? ''),
    quantidade: Number(campo(r, 'Quantidade') ?? 1),
    preco: Number(campo(r, 'PrecoUnitario') ?? 0),
    desconto: Number(campo(r, 'Desconto') ?? 0),
    total: Number(campo(r, 'Total') ?? 0),
    ordem: Number(campo(r, 'Ordem') ?? 0),
    salvando: false,
  }
}

async function carregar(silencioso = false) {
  if (!silencioso) carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({ $filter: `NegocioId eq ${props.negocioId}`, $orderby: 'Ordem asc,Id asc', $top: 100 })
    linhas.value = lista<Registro>(await odataRequest<unknown>('GET', `ItensNegocio${q}`)).map(paraLinha)
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar os produtos do negócio')
  } finally {
    carregando.value = false
  }
}

const bruto = computed(() => arred(linhas.value.reduce((s, l) => s + l.quantidade * l.preco, 0)))
const total = computed(() => arred(linhas.value.reduce((s, l) => s + totalDe(l), 0)))
const descontos = computed(() => arred(bruto.value - total.value))
const igualAoValor = computed(() => props.valorNegocio != null && Math.abs(Number(props.valorNegocio) - total.value) < 0.005)

async function adicionar(p: ProdutoBusca) {
  adicionando.value = true
  try {
    // O servidor preenche descrição e preço (lista de preço padrão ou preço do produto)
    await odataRequest('POST', 'ItensNegocio', { NegocioId: props.negocioId, ProdutoId: p.id, Quantidade: 1 })
    await carregar(true)
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível adicionar o produto'))
  } finally {
    adicionando.value = false
  }
}

async function adicionarAvulso() {
  adicionando.value = true
  try {
    await odataRequest('POST', 'ItensNegocio', { NegocioId: props.negocioId, Descricao: 'Item avulso', Quantidade: 1, PrecoUnitario: 0, Desconto: 0 })
    await carregar(true)
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível adicionar o item'))
  } finally {
    adicionando.value = false
  }
}

/** Campos numéricos vazios/inválidos viram 0 (quantidade vazia vira 1 e é validada no servidor). */
function numero(v: unknown, padrao = 0): number {
  const n = typeof v === 'number' ? v : Number(String(v ?? '').replace(',', '.'))
  return Number.isFinite(n) ? n : padrao
}

async function salvar(l: Linha) {
  l.quantidade = numero(l.quantidade, 1)
  l.preco = numero(l.preco)
  l.desconto = numero(l.desconto)
  l.descricao = l.descricao.trim()
  if (!l.descricao) { toast.error('Informe a descrição do item.'); await carregar(true); return }
  l.salvando = true
  try {
    const r = await odataRequest<Registro>('PATCH', `ItensNegocio(${l.id})`, {
      Descricao: l.descricao, Quantidade: l.quantidade, PrecoUnitario: l.preco, Desconto: l.desconto,
    })
    l.total = Number(campo(r, 'Total') ?? totalDe(l))
  } catch (e) {
    // 422 (ex.: desconto fora de 0–100): mostra a mensagem e volta aos valores gravados
    toast.error(mensagemErro(e, 'Não foi possível salvar o item'))
    await carregar(true)
  } finally {
    l.salvando = false
  }
}

async function remover(l: Linha) {
  if (!window.confirm(`Remover "${l.descricao}" do negócio?`)) return
  l.salvando = true
  try {
    await odataRequest('DELETE', `ItensNegocio(${l.id})`)
    linhas.value = linhas.value.filter((x) => x.id !== l.id)
  } catch (e) {
    l.salvando = false
    toast.error(mensagemErro(e, 'Não foi possível remover o item'))
  }
}

async function mover(i: number, delta: -1 | 1) {
  const j = i + delta
  const a = linhas.value[i]
  const b = linhas.value[j]
  if (!a || !b) return
  const antes = [...linhas.value]
  const nova = [...linhas.value]
  nova[i] = b
  nova[j] = a
  linhas.value = nova
  try {
    await odataRequest('POST', `Negocios(${props.negocioId})/ReordenarItens`, { Ids: nova.map((x) => x.id) })
  } catch (e) {
    linhas.value = antes
    toast.error(mensagemErro(e, 'Não foi possível reordenar os itens'))
  }
}

async function usarSoma() {
  aplicando.value = true
  try {
    await odataRequest('POST', `Negocios(${props.negocioId})/AtualizarValorPelosItens`, {})
    toast.success('Valor do negócio atualizado pela soma dos produtos.')
    emit('valorAtualizado')
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível atualizar o valor do negócio'))
  } finally {
    aplicando.value = false
  }
}

watch(() => props.negocioId, () => carregar())
onMounted(() => carregar())
</script>

<template>
  <div class="pn">
    <div v-if="!fechado" class="pn-barra">
      <BuscaProduto :desabilitado="adicionando" @escolher="adicionar" />
      <button type="button" class="pn-btn" :disabled="adicionando" @click="adicionarAvulso">
        <i class="mdi mdi-plus" aria-hidden="true"></i> Item avulso
      </button>
    </div>

    <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!linhas.length" :texto-vazio="fechado ? 'Nenhum produto neste negócio. Reabra o negócio para adicionar produtos.' : 'Nenhum produto neste negócio. Busque um produto acima para adicionar.'" icone="mdi-package-variant-closed" @tentar="carregar()">
      <div class="pn-wrap">
        <table class="pn-tab">
          <caption class="pn-sr">Produtos do negócio</caption>
          <thead>
            <tr>
              <th scope="col">Produto</th>
              <th scope="col" class="num">Qtd.</th>
              <th scope="col" class="num">Preço unit.</th>
              <th scope="col" class="num">Desc. %</th>
              <th scope="col" class="num">Total</th>
              <th scope="col" class="pn-acoes-th"><span class="pn-sr">Ações</span></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(l, i) in linhas" :key="l.id" :class="{ ocupada: l.salvando }">
              <td>
                <input v-model="l.descricao" class="pn-in pn-desc" type="text" maxlength="300" :disabled="fechado || l.salvando" :aria-label="`Descrição do item ${i + 1}`" @change="salvar(l)" />
              </td>
              <td class="num"><input v-model.number="l.quantidade" class="pn-in pn-num" type="number" min="0" step="any" :disabled="fechado || l.salvando" :aria-label="`Quantidade de ${l.descricao}`" @change="salvar(l)" /></td>
              <td class="num"><input v-model.number="l.preco" class="pn-in pn-num" type="number" min="0" step="0.01" :disabled="fechado || l.salvando" :aria-label="`Preço unitário de ${l.descricao}`" @change="salvar(l)" /></td>
              <td class="num"><input v-model.number="l.desconto" class="pn-in pn-num pn-curto" type="number" min="0" max="100" step="0.01" :disabled="fechado || l.salvando" :aria-label="`Desconto em porcentagem de ${l.descricao}`" @change="salvar(l)" /></td>
              <td class="num pn-total">{{ dinheiro(totalDe(l)) }}</td>
              <td class="pn-acoes">
                <template v-if="!fechado">
                  <button type="button" class="pn-ic" :disabled="i === 0 || l.salvando" :aria-label="`Mover ${l.descricao} para cima`" title="Subir" @click="mover(i, -1)"><i class="mdi mdi-arrow-up" aria-hidden="true"></i></button>
                  <button type="button" class="pn-ic" :disabled="i === linhas.length - 1 || l.salvando" :aria-label="`Mover ${l.descricao} para baixo`" title="Descer" @click="mover(i, 1)"><i class="mdi mdi-arrow-down" aria-hidden="true"></i></button>
                  <button type="button" class="pn-ic pn-del" :disabled="l.salvando" :aria-label="`Remover ${l.descricao}`" title="Remover" @click="remover(l)"><i class="mdi mdi-delete-outline" aria-hidden="true"></i></button>
                </template>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pn-resumo" aria-live="polite">
        <dl class="pn-totais">
          <div><dt>Subtotal</dt><dd>{{ dinheiro(bruto) }}</dd></div>
          <div v-if="descontos > 0"><dt>Descontos</dt><dd>− {{ dinheiro(descontos) }}</dd></div>
          <div class="pn-geral"><dt>Total ({{ codigo ?? 'moeda do negócio' }})</dt><dd>{{ dinheiro(total) }}</dd></div>
          <div><dt>Valor do negócio</dt><dd>{{ dinheiro(valorNegocio) }}</dd></div>
        </dl>
        <div v-if="!fechado" class="pn-usar">
          <button type="button" class="pn-btn pn-primario" :disabled="aplicando || igualAoValor || !linhas.length" @click="usarSoma">
            <i class="mdi" :class="aplicando ? 'mdi-loading mdi-spin' : 'mdi-sigma'" aria-hidden="true"></i> Usar soma como valor do negócio
          </button>
          <p v-if="igualAoValor" class="pn-dica"><i class="mdi mdi-check" aria-hidden="true"></i> O valor do negócio já corresponde à soma.</p>
        </div>
      </div>
    </DetalheEstado>
  </div>
</template>

<style scoped>
.pn-barra { display: flex; align-items: flex-end; gap: 12px; flex-wrap: wrap; margin-bottom: 14px; }
.pn-btn { display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-secondary); font-size: 13px; font-weight: 600; cursor: pointer; }
.pn-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.pn-btn:disabled { opacity: .5; cursor: not-allowed; }
.pn-primario { background: var(--accent); border-color: var(--accent); color: #000; }
.pn-primario:hover:not(:disabled) { background: var(--accent-hover); color: #000; }
.pn-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 10px; }
.pn-tab { width: 100%; border-collapse: collapse; font-size: 13px; }
.pn-tab th { text-align: left; padding: 10px 12px; background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: .5px; border-bottom: 1px solid var(--border); white-space: nowrap; }
.pn-tab td { padding: 6px 12px; border-bottom: 1px solid var(--border); color: var(--text-secondary); vertical-align: middle; }
.pn-tab tr:last-child td { border-bottom: 0; }
.pn-tab .num { text-align: right; }
.pn-tab tr.ocupada { opacity: .6; }
.pn-in { width: 100%; padding: 6px 8px; border: 1px solid transparent; border-radius: 6px; background: transparent; color: var(--text-primary); font-size: 13px; }
.pn-in:hover:not(:disabled) { border-color: var(--border); }
.pn-in:focus { outline: none; border-color: var(--accent); background: var(--bg-primary); }
.pn-in:disabled { color: var(--text-secondary); }
.pn-desc { min-width: 200px; }
.pn-num { width: 104px; text-align: right; }
.pn-curto { width: 80px; }
.pn-total { font-weight: 600; color: var(--text-primary) !important; white-space: nowrap; }
.pn-acoes { white-space: nowrap; text-align: right; }
.pn-acoes-th { width: 1%; }
.pn-ic { width: 28px; height: 28px; border: 1px solid transparent; border-radius: 6px; background: transparent; color: var(--text-muted); cursor: pointer; font-size: 16px; }
.pn-ic:hover:not(:disabled), .pn-ic:focus-visible { background: var(--bg-elevated); color: var(--text-primary); border-color: var(--border); outline: none; }
.pn-ic:disabled { opacity: .35; cursor: not-allowed; }
.pn-del:hover:not(:disabled) { color: var(--danger); }
.pn-resumo { display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: 16px; margin-top: 16px; }
.pn-totais { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, max-content)); gap: 10px 28px; margin: 0; }
.pn-totais dt { font-size: 11px; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); margin-bottom: 2px; }
.pn-totais dd { margin: 0; font-size: 14px; color: var(--text-primary); }
.pn-geral dd { font-size: 18px; font-weight: 700; color: var(--accent); }
.pn-usar { display: flex; flex-direction: column; align-items: flex-end; gap: 6px; }
.pn-dica { font-size: 12px; color: var(--text-muted); }
.pn-sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
</style>
