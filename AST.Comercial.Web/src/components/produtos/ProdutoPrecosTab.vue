<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { odataRequest } from '@/services/api'
import { mensagemErro, formatarData, formatarValorMoeda } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import UiBadge from '@/components/ui/UiBadge.vue'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'
import { obterPrecosProduto, type PrecosProduto } from './apiPrecos'

/** Aba "Preços": preço base e preço em cada lista, com edição inline. */
const props = defineProps<{ produtoId: number }>()
const toast = useToast()

const dados = ref<PrecosProduto | null>(null)
const carregando = ref(false)
const erro = ref('')
const rascunho = ref<Record<number, string>>({})
const salvandoLista = ref<number | null>(null)

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const r = await obterPrecosProduto(props.produtoId, true)
    dados.value = r
    rascunho.value = Object.fromEntries(r.listas.map((l) => [l.listaPrecoId, l.preco === null ? '' : String(l.preco)]))
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar os preços')
  } finally {
    carregando.value = false
  }
}

function alterado(listaId: number, preco: number | null): boolean {
  const txt = (rascunho.value[listaId] ?? '').trim()
  if (txt === '') return preco !== null
  return Number(txt) !== preco
}

async function salvar(listaId: number) {
  const l = dados.value?.listas.find((x) => x.listaPrecoId === listaId)
  if (!l) return
  const txt = (rascunho.value[listaId] ?? '').trim()
  salvandoLista.value = listaId
  try {
    if (txt === '') {
      if (l.itemId) await odataRequest('DELETE', `ItensListaPreco(${l.itemId})`)
    } else {
      const preco = Number(txt)
      if (!Number.isFinite(preco) || preco < 0) { toast.error('Informe um preço válido (zero ou maior).'); return }
      if (l.itemId) await odataRequest('PATCH', `ItensListaPreco(${l.itemId})`, { Preco: preco })
      else await odataRequest('POST', 'ItensListaPreco', { ListaPrecoId: listaId, ProdutoId: props.produtoId, Preco: preco })
    }
    toast.success('Preço atualizado.')
    await carregar()
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível salvar o preço'))
  } finally {
    salvandoLista.value = null
  }
}

watch(() => props.produtoId, carregar)
onMounted(carregar)
</script>

<template>
  <DetalheEstado :carregando="carregando" :erro="erro" @tentar="carregar">
    <div v-if="dados">
      <div class="pp-base">
        <span class="pp-rot">Preço base</span>
        <strong>{{ formatarValorMoeda(dados.preco, dados.moedaSimbolo, dados.moedaCodigo) }}</strong>
        <small>Edite o preço base na aba Dados.</small>
      </div>
      <DetalheEstado :vazio="!dados.listas.length" texto-vazio="Nenhuma lista de preço cadastrada." icone="mdi-tag-multiple-outline">
        <div class="pp-wrap">
          <table class="pp-tab">
            <thead><tr><th>Lista</th><th>Vigência</th><th class="num">Preço na lista</th><th class="acao"></th></tr></thead>
            <tbody>
              <tr v-for="l in dados.listas" :key="l.listaPrecoId">
                <td>
                  {{ l.listaNome }}
                  <UiBadge v-if="l.padrao" tom="destaque">Padrão</UiBadge>
                  <UiBadge v-if="!l.vigente" tom="neutro">Fora de vigência</UiBadge>
                </td>
                <td>
                  <template v-if="l.vigenciaInicio || l.vigenciaFim">{{ formatarData(l.vigenciaInicio) }} a {{ l.vigenciaFim ? formatarData(l.vigenciaFim) : 'sem fim' }}</template>
                  <template v-else>Sempre</template>
                </td>
                <td class="num">
                  <span class="pp-simb">{{ l.moedaSimbolo || dados.moedaSimbolo || '' }}</span>
                  <input
                    v-model="rascunho[l.listaPrecoId]" type="number" min="0" step="0.01" placeholder="Sem preço"
                    :aria-label="`Preço em ${l.listaNome}`" @keyup.enter="salvar(l.listaPrecoId)"
                  />
                </td>
                <td class="acao">
                  <button
                    class="pp-salvar" :disabled="salvandoLista === l.listaPrecoId || !alterado(l.listaPrecoId, l.preco)"
                    :aria-label="`Salvar preço em ${l.listaNome}`" @click="salvar(l.listaPrecoId)"
                  >
                    <i class="mdi" :class="salvandoLista === l.listaPrecoId ? 'mdi-loading mdi-spin' : 'mdi-check'"></i> Salvar
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </DetalheEstado>
    </div>
  </DetalheEstado>
</template>

<style scoped>
.pp-base { display: flex; align-items: baseline; gap: 12px; flex-wrap: wrap; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 14px 18px; margin-bottom: 14px; }
.pp-rot { font-size: 11px; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); }
.pp-base strong { font-size: 20px; color: var(--text-primary); }
.pp-base small { color: var(--text-muted); font-size: 12px; }
.pp-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 10px; }
.pp-tab { width: 100%; border-collapse: collapse; font-size: 13px; }
.pp-tab th { text-align: left; padding: 10px 14px; background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: .5px; border-bottom: 1px solid var(--border); }
.pp-tab td { padding: 8px 14px; border-bottom: 1px solid var(--border); color: var(--text-secondary); }
.pp-tab tr:last-child td { border-bottom: 0; }
.pp-tab .num { text-align: right; white-space: nowrap; }
.pp-tab .acao { width: 1%; text-align: right; }
.pp-simb { margin-right: 6px; color: var(--text-muted); }
.pp-tab input { width: 120px; padding: 6px 8px; text-align: right; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); font-size: 13px; }
.pp-tab input:focus { outline: none; border-color: var(--accent); }
.pp-salvar { display: inline-flex; align-items: center; gap: 4px; padding: 6px 12px; border: 1px solid var(--border); border-radius: 6px; background: var(--bg-primary); color: var(--text-secondary); font-size: 12px; cursor: pointer; }
.pp-salvar:hover:not(:disabled) { border-color: var(--accent); color: var(--accent); }
.pp-salvar:disabled { opacity: .45; cursor: default; }
</style>
