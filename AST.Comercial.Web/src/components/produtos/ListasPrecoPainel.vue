<script setup lang="ts">
import { ref, reactive, computed, onMounted, onBeforeUnmount, watch } from 'vue'
import { odataRequest, odataTodos, montarQuery } from '@/services/api'
import { campo, lista, lit, mensagemErro, formatarData, formatarValorMoeda, carregarMoedas, type Registro, type MoedaInfo } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import UiBadge from '@/components/ui/UiBadge.vue'
import UiModal from '@/components/ui/UiModal.vue'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'
import { copiarPrecosBase } from './apiPrecos'
import { numeroOuNulo, paraInputData, deInputData } from './tiposProduto'

/** Seção "Listas de preço" de Produtos: listas, vigência, padrão e grade de itens. */
const toast = useToast()

const listas = ref<Registro[]>([])
const moedas = ref<MoedaInfo[]>([])
const carregando = ref(false)
const erro = ref('')
const selecionadaId = ref<number | 'nova' | null>(null)

// ----- Listas -----
async function carregarListas(manterSelecao = true) {
  carregando.value = true
  erro.value = ''
  try {
    const [r, m] = await Promise.all([
      odataTodos<Registro>('ListasPreco?$orderby=Padrao desc,Nome asc'),
      carregarMoedas(),
    ])
    listas.value = r
    moedas.value = m
    if (!manterSelecao || (typeof selecionadaId.value === 'number' && !r.some((l) => campo(l, 'Id') === selecionadaId.value))) {
      selecionadaId.value = null
    }
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar as listas de preço')
  } finally {
    carregando.value = false
  }
}

function moedaDe(id: number | null | undefined): MoedaInfo | undefined {
  return moedas.value.find((m) => m.id === id)
}

function vigencia(l: Registro): string {
  const i = campo<string>(l, 'VigenciaInicio'), f = campo<string>(l, 'VigenciaFim')
  if (!i && !f) return 'Sem prazo definido'
  return `${i ? formatarData(i) : 'Início aberto'} a ${f ? formatarData(f) : 'sem fim'}`
}

// ----- Formulário da lista -----
const form = reactive({ Nome: '', Descricao: '', MoedaId: '' as number | string, Padrao: false, VigenciaInicio: '', VigenciaFim: '' })
const errosForm = reactive<Record<string, string>>({})
const salvandoLista = ref(false)

const listaAtual = computed(() => typeof selecionadaId.value === 'number' ? listas.value.find((l) => campo(l, 'Id') === selecionadaId.value) ?? null : null)

function preencherForm() {
  const l = listaAtual.value
  form.Nome = l ? campo(l, 'Nome') ?? '' : ''
  form.Descricao = l ? campo(l, 'Descricao') ?? '' : ''
  form.MoedaId = l ? campo(l, 'MoedaId') ?? '' : (moedas.value.find((m) => m.codigo === 'BRL')?.id ?? '')
  form.Padrao = l ? campo(l, 'Padrao') === true : false
  form.VigenciaInicio = l ? paraInputData(campo(l, 'VigenciaInicio')) : ''
  form.VigenciaFim = l ? paraInputData(campo(l, 'VigenciaFim')) : ''
  Object.keys(errosForm).forEach((k) => delete errosForm[k])
}

function validarForm(): boolean {
  Object.keys(errosForm).forEach((k) => delete errosForm[k])
  if (!form.Nome.trim()) errosForm.Nome = 'Informe o nome da lista.'
  if (form.VigenciaInicio && form.VigenciaFim && form.VigenciaFim < form.VigenciaInicio) {
    errosForm.VigenciaFim = 'O fim da vigência deve ser igual ou posterior ao início.'
  }
  return Object.keys(errosForm).length === 0
}

async function salvarLista() {
  if (!validarForm()) return
  salvandoLista.value = true
  const corpo = {
    Nome: form.Nome.trim(),
    Descricao: form.Descricao.trim() || null,
    MoedaId: numeroOuNulo(form.MoedaId),
    Padrao: form.Padrao,
    VigenciaInicio: deInputData(form.VigenciaInicio),
    VigenciaFim: deInputData(form.VigenciaFim),
  }
  try {
    if (selecionadaId.value === 'nova') {
      const criada = await odataRequest<Registro>('POST', 'ListasPreco', corpo)
      toast.success('Lista criada.')
      selecionadaId.value = Number(campo(criada, 'Id'))
    } else {
      await odataRequest('PATCH', `ListasPreco(${selecionadaId.value})`, corpo)
      toast.success('Lista salva.')
    }
    await carregarListas()
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível salvar a lista'))
  } finally {
    salvandoLista.value = false
  }
}

const confirmarExcluir = ref(false)
async function excluirLista() {
  try {
    await odataRequest('DELETE', `ListasPreco(${selecionadaId.value})`)
    toast.success('Lista excluída.')
    confirmarExcluir.value = false
    selecionadaId.value = null
    await carregarListas()
  } catch (e) {
    confirmarExcluir.value = false
    toast.error(mensagemErro(e, 'Não foi possível excluir a lista'))
  }
}

// ----- Itens -----
const itens = ref<Registro[]>([])
const carregandoItens = ref(false)
const erroItens = ref('')
const rascunho = ref<Record<number, string>>({})
const salvandoItem = ref<number | null>(null)

async function carregarItens() {
  if (typeof selecionadaId.value !== 'number') { itens.value = []; return }
  carregandoItens.value = true
  erroItens.value = ''
  try {
    const q = montarQuery({ $filter: `ListaPrecoId eq ${selecionadaId.value}`, $expand: 'Produto($select=Id,Nome,Codigo,Preco,MoedaId)', $orderby: 'Id asc' })
    itens.value = await odataTodos<Registro>(`ItensListaPreco${q}`)
    rascunho.value = Object.fromEntries(itens.value.map((i) => [Number(campo(i, 'Id')), String(campo(i, 'Preco') ?? '')]))
  } catch (e) {
    erroItens.value = mensagemErro(e, 'Não foi possível carregar os itens da lista')
  } finally {
    carregandoItens.value = false
  }
}

async function salvarItem(i: Registro) {
  const id = Number(campo(i, 'Id'))
  const txt = (rascunho.value[id] ?? '').trim()
  const preco = Number(txt)
  if (txt === '' || !Number.isFinite(preco) || preco < 0) {
    toast.error('Informe um preço válido (zero ou maior).')
    rascunho.value[id] = String(campo(i, 'Preco') ?? '')
    return
  }
  if (preco === Number(campo(i, 'Preco'))) return
  salvandoItem.value = id
  try {
    await odataRequest('PATCH', `ItensListaPreco(${id})`, { Preco: preco })
    toast.success('Preço atualizado.')
    await carregarItens()
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível salvar o preço'))
    rascunho.value[id] = String(campo(i, 'Preco') ?? '')
  } finally {
    salvandoItem.value = null
  }
}

async function removerItem(i: Registro) {
  try {
    await odataRequest('DELETE', `ItensListaPreco(${campo(i, 'Id')})`)
    toast.success('Produto removido da lista.')
    await carregarItens()
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível remover o produto'))
  }
}

// ----- Adicionar produto (busca) -----
const busca = ref('')
const sugestoes = ref<Registro[]>([])
const buscando = ref(false)
const produtoEscolhido = ref<Registro | null>(null)
const precoNovo = ref('')
const adicionando = ref(false)
let timer: ReturnType<typeof setTimeout> | undefined

function aoDigitar() {
  produtoEscolhido.value = null
  clearTimeout(timer)
  const termo = busca.value.trim()
  if (termo.length < 2) { sugestoes.value = []; return }
  timer = setTimeout(async () => {
    buscando.value = true
    try {
      const t = lit(termo)
      const q = montarQuery({
        $filter: `contains(Nome,'${t}') or contains(Codigo,'${t}')`,
        $select: 'Id,Nome,Codigo,Preco,MoedaId', $orderby: 'Nome asc', $top: 8,
      })
      const jaNaLista = new Set(itens.value.map((i) => Number(campo(i, 'ProdutoId'))))
      sugestoes.value = lista<Registro>(await odataRequest<unknown>('GET', `Produtos${q}`)).filter((p) => !jaNaLista.has(Number(campo(p, 'Id'))))
    } catch (e) {
      sugestoes.value = []
      toast.error(mensagemErro(e, 'Não foi possível buscar produtos'))
    } finally {
      buscando.value = false
    }
  }, 250)
}

function escolher(p: Registro) {
  produtoEscolhido.value = p
  busca.value = String(campo(p, 'Nome') ?? '')
  precoNovo.value = String(campo(p, 'Preco') ?? '')
  sugestoes.value = []
}

async function adicionar() {
  const p = produtoEscolhido.value
  const preco = Number(precoNovo.value)
  if (!p) { toast.error('Escolha um produto na busca.'); return }
  if (precoNovo.value.trim() === '' || !Number.isFinite(preco) || preco < 0) { toast.error('Informe um preço válido (zero ou maior).'); return }
  adicionando.value = true
  try {
    await odataRequest('POST', 'ItensListaPreco', { ListaPrecoId: selecionadaId.value, ProdutoId: campo(p, 'Id'), Preco: preco })
    toast.success('Produto adicionado à lista.')
    busca.value = ''
    produtoEscolhido.value = null
    precoNovo.value = ''
    await carregarItens()
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível adicionar o produto'))
  } finally {
    adicionando.value = false
  }
}

// ----- Copiar preços base -----
const copiarAberto = ref(false)
const sobrescrever = ref(false)
const copiando = ref(false)

async function copiar() {
  copiando.value = true
  try {
    const r = await copiarPrecosBase(selecionadaId.value as number, sobrescrever.value)
    toast.success(`Preços copiados: ${r.criados} novos, ${r.atualizados} atualizados, ${r.ignorados} ignorados.`)
    copiarAberto.value = false
    await carregarItens()
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível copiar os preços'))
  } finally {
    copiando.value = false
  }
}

function moedaDaLista() { return moedaDe(numeroOuNulo(form.MoedaId)) }
function dinheiroBase(p: Registro | undefined): string {
  const m = moedaDe(campo(p, 'MoedaId'))
  return formatarValorMoeda(campo(p, 'Preco'), m?.simbolo, m?.codigo)
}

function nova() { selecionadaId.value = 'nova' }
watch(selecionadaId, () => { preencherForm(); busca.value = ''; sugestoes.value = []; produtoEscolhido.value = null; carregarItens() })
onMounted(() => carregarListas(false))
onBeforeUnmount(() => clearTimeout(timer))
</script>

<template>
  <div class="lp">
    <aside class="lp-lateral" aria-label="Listas de preço">
      <button class="lp-nova" type="button" @click="nova"><i class="mdi mdi-plus"></i> Nova lista</button>
      <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!listas.length" texto-vazio="Nenhuma lista de preço ainda." icone="mdi-tag-multiple-outline" @tentar="carregarListas()">
        <ul class="lp-lista">
          <li v-for="l in listas" :key="campo(l, 'Id')">
            <button type="button" class="lp-item" :class="{ ativa: selecionadaId === campo(l, 'Id') }" :aria-current="selecionadaId === campo(l, 'Id')" @click="selecionadaId = Number(campo(l, 'Id'))">
              <span class="lp-nome">{{ campo(l, 'Nome') }}</span>
              <span class="lp-sub">{{ vigencia(l) }}</span>
              <span class="lp-tags">
                <UiBadge v-if="campo(l, 'Padrao')" tom="destaque">Padrão</UiBadge>
                <UiBadge v-if="moedaDe(campo(l, 'MoedaId'))" tom="neutro">{{ moedaDe(campo(l, 'MoedaId'))!.codigo }}</UiBadge>
              </span>
            </button>
          </li>
        </ul>
      </DetalheEstado>
    </aside>

    <section class="lp-principal">
      <div v-if="selecionadaId === null" class="lp-vazio">
        <i class="mdi mdi-tag-multiple-outline"></i>
        <p>Selecione uma lista ou crie uma nova para definir preços por produto.</p>
      </div>

      <template v-else>
        <form class="lp-card" novalidate @submit.prevent="salvarLista">
          <h3>{{ selecionadaId === 'nova' ? 'Nova lista de preço' : 'Dados da lista' }}</h3>
          <div class="lp-grade">
            <label class="lp-campo lp-largo">
              <span>Nome *</span>
              <input v-model="form.Nome" maxlength="200" :aria-invalid="!!errosForm.Nome" />
              <small v-if="errosForm.Nome" class="lp-erro" role="alert">{{ errosForm.Nome }}</small>
            </label>
            <label class="lp-campo">
              <span>Moeda</span>
              <select v-model="form.MoedaId">
                <option value="">Sem moeda</option>
                <option v-for="m in moedas" :key="m.id" :value="m.id">{{ m.simbolo }} · {{ m.codigo }}</option>
              </select>
            </label>
            <label class="lp-campo"><span>Vigência: início</span><input v-model="form.VigenciaInicio" type="date" /></label>
            <label class="lp-campo">
              <span>Vigência: fim</span>
              <input v-model="form.VigenciaFim" type="date" :min="form.VigenciaInicio || undefined" :aria-invalid="!!errosForm.VigenciaFim" />
              <small v-if="errosForm.VigenciaFim" class="lp-erro" role="alert">{{ errosForm.VigenciaFim }}</small>
            </label>
            <label class="lp-campo lp-check"><input v-model="form.Padrao" type="checkbox" /> <span>Lista padrão (desmarca a anterior)</span></label>
            <label class="lp-campo lp-total"><span>Descrição</span><textarea v-model="form.Descricao" rows="2" maxlength="1000"></textarea></label>
          </div>
          <div class="lp-acoes">
            <button v-if="selecionadaId !== 'nova'" type="button" class="lp-btn lp-perigo" @click="confirmarExcluir = true"><i class="mdi mdi-delete-outline"></i> Excluir</button>
            <span class="lp-espaco"></span>
            <button v-if="selecionadaId === 'nova'" type="button" class="lp-btn" @click="selecionadaId = null">Cancelar</button>
            <button type="submit" class="lp-btn lp-prim" :disabled="salvandoLista">
              <i class="mdi" :class="salvandoLista ? 'mdi-loading mdi-spin' : 'mdi-content-save-outline'"></i> Salvar lista
            </button>
          </div>
        </form>

        <div v-if="selecionadaId !== 'nova'" class="lp-card">
          <div class="lp-cab-itens">
            <h3>Produtos e preços <small>({{ itens.length }})</small></h3>
            <button type="button" class="lp-btn" @click="copiarAberto = true"><i class="mdi mdi-content-copy"></i> Copiar preços base</button>
          </div>

          <div class="lp-add">
            <div class="lp-busca">
              <input v-model="busca" placeholder="Buscar produto por nome ou código..." aria-label="Buscar produto para adicionar" autocomplete="off" @input="aoDigitar" />
              <ul v-if="sugestoes.length || buscando" class="lp-sugestoes" role="listbox">
                <li v-if="buscando" class="lp-sug-info">Buscando...</li>
                <li v-for="p in sugestoes" :key="campo(p, 'Id')" role="option">
                  <button type="button" @click="escolher(p)">
                    <span>{{ campo(p, 'Nome') }}<small v-if="campo(p, 'Codigo')"> · {{ campo(p, 'Codigo') }}</small></span>
                    <em>{{ dinheiroBase(p) }}</em>
                  </button>
                </li>
              </ul>
            </div>
            <span class="lp-simb">{{ moedaDaLista()?.simbolo }}</span>
            <input v-model="precoNovo" type="number" min="0" step="0.01" class="lp-preco" placeholder="Preço" aria-label="Preço do produto na lista" @keyup.enter="adicionar" />
            <button type="button" class="lp-btn lp-prim" :disabled="adicionando || !produtoEscolhido" @click="adicionar"><i class="mdi mdi-plus"></i> Adicionar</button>
          </div>

          <DetalheEstado :carregando="carregandoItens" :erro="erroItens" :vazio="!itens.length" texto-vazio="Nenhum produto nesta lista. Busque acima ou copie os preços base." icone="mdi-package-variant" @tentar="carregarItens">
            <div class="lp-wrap">
              <table class="lp-tab">
                <thead><tr><th>Produto</th><th>Código</th><th class="num">Preço base</th><th class="num">Preço na lista</th><th class="acao"></th></tr></thead>
                <tbody>
                  <tr v-for="i in itens" :key="campo(i, 'Id')">
                    <td>{{ campo(campo(i, 'Produto'), 'Nome') ?? '-' }}</td>
                    <td>{{ campo(campo(i, 'Produto'), 'Codigo') ?? '-' }}</td>
                    <td class="num">{{ dinheiroBase(campo(i, 'Produto')) }}</td>
                    <td class="num">
                      <span class="lp-simb">{{ moedaDaLista()?.simbolo }}</span>
                      <input
                        v-model="rascunho[Number(campo(i, 'Id'))]" type="number" min="0" step="0.01" class="lp-preco"
                        :disabled="salvandoItem === campo(i, 'Id')" :aria-label="`Preço de ${campo(campo(i, 'Produto'), 'Nome')}`"
                        @change="salvarItem(i)" @keyup.enter="($event.target as HTMLInputElement).blur()"
                      />
                    </td>
                    <td class="acao"><button type="button" class="lp-x" :aria-label="`Remover ${campo(campo(i, 'Produto'), 'Nome')} da lista`" @click="removerItem(i)"><i class="mdi mdi-close"></i></button></td>
                  </tr>
                </tbody>
              </table>
            </div>
          </DetalheEstado>
        </div>
      </template>
    </section>

    <UiModal v-model="confirmarExcluir" titulo="Excluir lista de preço" largura="420px">
      <p class="lp-texto">Excluir a lista <strong>{{ campo(listaAtual, 'Nome') }}</strong>? Os preços dela deixam de valer.</p>
      <template #rodape>
        <button class="lp-btn" @click="confirmarExcluir = false">Cancelar</button>
        <button class="lp-btn lp-perigo" @click="excluirLista">Excluir</button>
      </template>
    </UiModal>

    <UiModal v-model="copiarAberto" titulo="Copiar preços base" largura="440px">
      <p class="lp-texto">Cria um item na lista para cada produto, com o preço base atual. Produtos de outra moeda não são copiados.</p>
      <label class="lp-check lp-bloco"><input v-model="sobrescrever" type="checkbox" /> <span>Sobrescrever preços já existentes na lista</span></label>
      <template #rodape>
        <button class="lp-btn" @click="copiarAberto = false">Cancelar</button>
        <button class="lp-btn lp-prim" :disabled="copiando" @click="copiar"><i class="mdi" :class="copiando ? 'mdi-loading mdi-spin' : 'mdi-content-copy'"></i> Copiar</button>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.lp { display: grid; grid-template-columns: 260px 1fr; gap: 16px; align-items: start; }
.lp-lateral { display: flex; flex-direction: column; gap: 10px; }
.lp-nova { display: inline-flex; align-items: center; justify-content: center; gap: 6px; padding: 9px 14px; border: 0; border-radius: 8px; background: var(--accent); color: #000; font-size: 13px; font-weight: 600; cursor: pointer; }
.lp-nova:hover { background: var(--accent-hover); }
.lp-lista { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.lp-item { width: 100%; text-align: left; display: flex; flex-direction: column; gap: 4px; padding: 10px 12px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; color: var(--text-primary); cursor: pointer; }
.lp-item:hover { border-color: var(--text-muted); }
.lp-item.ativa { border-color: var(--accent); }
.lp-nome { font-size: 13px; font-weight: 600; overflow-wrap: anywhere; }
.lp-sub { font-size: 11px; color: var(--text-muted); }
.lp-tags { display: flex; gap: 6px; flex-wrap: wrap; }
.lp-principal { display: flex; flex-direction: column; gap: 14px; min-width: 0; }
.lp-vazio { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 60px 16px; color: var(--text-muted); font-size: 13px; text-align: center; background: var(--bg-surface); border: 1px dashed var(--border); border-radius: 12px; }
.lp-vazio .mdi { font-size: 32px; }
.lp-card { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 12px; padding: 16px 18px; }
.lp-card h3 { font-size: 14px; font-weight: 600; color: var(--text-primary); margin: 0 0 12px; }
.lp-card h3 small { color: var(--text-muted); font-weight: 400; }
.lp-grade { display: grid; grid-template-columns: repeat(auto-fill, minmax(200px, 1fr)); gap: 12px; }
.lp-campo { display: flex; flex-direction: column; gap: 5px; font-size: 12px; color: var(--text-muted); }
.lp-campo input, .lp-campo select, .lp-campo textarea { padding: 8px 10px; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 13px; font-family: inherit; }
.lp-campo input:focus, .lp-campo select:focus, .lp-campo textarea:focus { outline: none; border-color: var(--accent); }
.lp-campo [aria-invalid='true'] { border-color: var(--danger); }
.lp-largo { grid-column: span 2; }
.lp-total { grid-column: 1 / -1; }
.lp-check { display: flex; flex-direction: row; align-items: center; gap: 8px; color: var(--text-secondary); font-size: 13px; }
.lp-check input { width: 16px; height: 16px; accent-color: var(--accent); }
.lp-bloco { margin-top: 12px; }
.lp-erro { color: var(--danger); font-size: 11px; }
.lp-acoes { display: flex; align-items: center; gap: 8px; margin-top: 14px; }
.lp-espaco { flex: 1; }
.lp-btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.lp-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.lp-btn:disabled { opacity: .55; cursor: default; }
.lp-prim { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.lp-prim:hover:not(:disabled) { background: var(--accent-hover); color: #000; }
.lp-perigo { color: var(--danger); }
.lp-perigo:hover:not(:disabled) { background: var(--danger); color: #fff; border-color: var(--danger); }
.lp-cab-itens { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-bottom: 12px; }
.lp-cab-itens h3 { margin: 0; }
.lp-add { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; margin-bottom: 12px; }
.lp-busca { position: relative; flex: 1; min-width: 220px; }
.lp-busca input { width: 100%; padding: 8px 10px; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 13px; }
.lp-busca input:focus { outline: none; border-color: var(--accent); }
.lp-sugestoes { position: absolute; z-index: 20; left: 0; right: 0; top: calc(100% + 4px); list-style: none; margin: 0; padding: 4px; background: var(--bg-elevated); border: 1px solid var(--border); border-radius: 8px; box-shadow: 0 8px 24px rgba(0, 0, 0, .35); }
.lp-sugestoes button { width: 100%; display: flex; justify-content: space-between; gap: 10px; padding: 7px 10px; background: none; border: 0; border-radius: 6px; color: var(--text-primary); font-size: 13px; cursor: pointer; text-align: left; }
.lp-sugestoes button:hover, .lp-sugestoes button:focus-visible { background: var(--bg-surface); }
.lp-sugestoes small, .lp-sugestoes em { color: var(--text-muted); font-style: normal; }
.lp-sug-info { padding: 7px 10px; font-size: 12px; color: var(--text-muted); }
.lp-simb { color: var(--text-muted); font-size: 13px; }
.lp-preco { width: 110px; padding: 7px 9px; text-align: right; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); font-size: 13px; }
.lp-preco:focus { outline: none; border-color: var(--accent); }
.lp-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 10px; }
.lp-tab { width: 100%; border-collapse: collapse; font-size: 13px; }
.lp-tab th { text-align: left; padding: 10px 14px; background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: .5px; border-bottom: 1px solid var(--border); }
.lp-tab td { padding: 7px 14px; border-bottom: 1px solid var(--border); color: var(--text-secondary); }
.lp-tab tr:last-child td { border-bottom: 0; }
.lp-tab .num { text-align: right; white-space: nowrap; }
.lp-tab .acao { width: 1%; }
.lp-x { background: none; border: 0; color: var(--text-muted); cursor: pointer; font-size: 16px; padding: 4px; border-radius: 6px; }
.lp-x:hover { color: var(--danger); background: var(--bg-elevated); }
.lp-texto { font-size: 13px; color: var(--text-secondary); margin: 0; }
@media (max-width: 800px) { .lp { grid-template-columns: 1fr; } .lp-largo { grid-column: 1 / -1; } }
</style>
