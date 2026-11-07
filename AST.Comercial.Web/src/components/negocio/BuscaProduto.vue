<script setup lang="ts">
import { computed, onBeforeUnmount, ref } from 'vue'
import { montarQuery, odataRequest } from '@/services/api'
import { campo, lista, lit, mensagemErro, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'

export interface ProdutoBusca { id: number; nome: string; codigo: string | null; preco: number }

defineProps<{ desabilitado?: boolean; placeholder?: string; rotulo?: string }>()
const emit = defineEmits<{ escolher: [produto: ProdutoBusca] }>()

const toast = useToast()
const termo = ref('')
const aberto = ref(false)
const buscando = ref(false)
const resultados = ref<ProdutoBusca[]>([])
const ativo = ref(-1)
const idLista = `bp-${Math.random().toString(36).slice(2, 8)}`
let timer: ReturnType<typeof setTimeout> | null = null
let geracao = 0

const descendente = computed(() => (ativo.value >= 0 ? `${idLista}-${ativo.value}` : undefined))

async function buscar() {
  const minha = ++geracao
  buscando.value = true
  try {
    const t = termo.value.trim().toLowerCase()
    const filtro = ['Disponivel eq true']
    if (t) filtro.push(`(contains(tolower(Nome),'${lit(t)}') or contains(tolower(Codigo),'${lit(t)}'))`)
    const q = montarQuery({ $filter: filtro.join(' and '), $select: 'Id,Nome,Codigo,Preco', $orderby: 'Nome asc', $top: 8 })
    const r = lista<Registro>(await odataRequest<unknown>('GET', `Produtos${q}`))
    if (minha !== geracao) return
    resultados.value = r.map((p) => ({
      id: Number(campo(p, 'Id')), nome: String(campo(p, 'Nome') ?? ''), codigo: (campo<string | null>(p, 'Codigo') ?? null), preco: Number(campo(p, 'Preco') ?? 0),
    }))
    ativo.value = resultados.value.length ? 0 : -1
  } catch (e) {
    if (minha === geracao) { resultados.value = []; toast.error(mensagemErro(e, 'Não foi possível buscar produtos')) }
  } finally {
    if (minha === geracao) buscando.value = false
  }
}

function aoDigitar() {
  aberto.value = true
  if (timer) clearTimeout(timer)
  timer = setTimeout(buscar, 250)
}

function aoFocar() {
  aberto.value = true
  if (!resultados.value.length) buscar()
}

function escolher(p: ProdutoBusca) {
  emit('escolher', p)
  termo.value = ''
  resultados.value = []
  aberto.value = false
  ativo.value = -1
}

function teclado(e: KeyboardEvent) {
  if (e.key === 'ArrowDown') {
    e.preventDefault()
    aberto.value = true
    if (resultados.value.length) ativo.value = (ativo.value + 1) % resultados.value.length
  } else if (e.key === 'ArrowUp') {
    e.preventDefault()
    if (resultados.value.length) ativo.value = (ativo.value - 1 + resultados.value.length) % resultados.value.length
  } else if (e.key === 'Enter') {
    const p = resultados.value[ativo.value]
    if (aberto.value && p) { e.preventDefault(); escolher(p) }
  } else if (e.key === 'Escape' && aberto.value) {
    e.stopPropagation()
    aberto.value = false
  }
}

function aoSair() {
  // Atraso para o clique numa opção ser processado antes de fechar a lista
  setTimeout(() => { aberto.value = false }, 150)
}

onBeforeUnmount(() => { if (timer) clearTimeout(timer) })
</script>

<template>
  <div class="bp">
    <label class="bp-rotulo" :for="`${idLista}-in`">{{ rotulo ?? 'Adicionar produto' }}</label>
    <div class="bp-campo">
      <i class="mdi mdi-magnify" aria-hidden="true"></i>
      <input
        :id="`${idLista}-in`" v-model="termo" class="bp-input" type="text" autocomplete="off" role="combobox"
        :placeholder="placeholder ?? 'Buscar produto por nome ou código'" :disabled="desabilitado"
        :aria-expanded="aberto" aria-autocomplete="list" :aria-controls="idLista" :aria-activedescendant="descendente"
        @input="aoDigitar" @focus="aoFocar" @blur="aoSair" @keydown="teclado"
      />
      <i v-if="buscando" class="mdi mdi-loading mdi-spin bp-spin" aria-hidden="true"></i>
    </div>
    <ul v-show="aberto" :id="idLista" class="bp-lista" role="listbox" :aria-label="rotulo ?? 'Produtos encontrados'">
      <li
        v-for="(p, i) in resultados" :id="`${idLista}-${i}`" :key="p.id" class="bp-opcao" :class="{ ativa: i === ativo }"
        role="option" :aria-selected="i === ativo" @mousedown.prevent="escolher(p)" @mouseenter="ativo = i"
      >
        <span class="bp-nome">{{ p.nome }}</span>
        <span v-if="p.codigo" class="bp-cod">{{ p.codigo }}</span>
      </li>
      <li v-if="!buscando && !resultados.length" class="bp-vazio" role="presentation">Nenhum produto encontrado.</li>
    </ul>
  </div>
</template>

<style scoped>
.bp { position: relative; min-width: 240px; flex: 1; max-width: 420px; }
.bp-rotulo { display: block; font-size: 11px; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); margin-bottom: 4px; }
.bp-campo { position: relative; display: flex; align-items: center; }
.bp-campo > .mdi:first-child { position: absolute; left: 10px; color: var(--text-muted); font-size: 16px; }
.bp-input { width: 100%; padding: 8px 30px 8px 32px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-primary); font-size: 13px; }
.bp-input:focus { outline: none; border-color: var(--accent); }
.bp-input:disabled { opacity: .5; }
.bp-spin { position: absolute; right: 10px; color: var(--text-muted); }
.bp-lista { position: absolute; z-index: 30; top: 100%; left: 0; right: 0; margin-top: 4px; padding: 4px; list-style: none; background: var(--bg-elevated); border: 1px solid var(--border); border-radius: 8px; box-shadow: 0 12px 28px rgba(0, 0, 0, .35); max-height: 260px; overflow-y: auto; }
.bp-opcao { display: flex; justify-content: space-between; gap: 8px; padding: 7px 8px; border-radius: 6px; font-size: 13px; color: var(--text-primary); cursor: pointer; }
.bp-opcao.ativa { background: var(--bg-surface); color: var(--accent); }
.bp-cod { color: var(--text-muted); font-size: 11px; white-space: nowrap; }
.bp-vazio { padding: 10px 8px; font-size: 12px; color: var(--text-muted); text-align: center; }
</style>
