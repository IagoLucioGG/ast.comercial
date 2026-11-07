<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useRouter } from 'vue-router'
import { odataQuery } from '@/services/api'

const router = useRouter()

const MAX_POR_GRUPO = 5
const DEBOUNCE_MS = 250

interface Resultado {
  chave: string
  titulo: string
  detalhe?: string
  rota: string
}
interface Grupo {
  nome: string
  icone: string
  itens: Resultado[]
}

const entrada = ref<HTMLInputElement | null>(null)
const raiz = ref<HTMLElement | null>(null)
const termo = ref('')
const aberto = ref(false)
const carregando = ref(false)
const falhou = ref(false)
const grupos = ref<Grupo[]>([])
const ativo = ref(-1)

let timer: number | undefined
let seq = 0

const lista = computed(() => grupos.value.flatMap((g) => g.itens))
const semResultado = computed(() => !carregando.value && !falhou.value && termo.value.trim().length >= 2 && lista.value.length === 0)

defineExpose({ focar })
function focar() {
  entrada.value?.focus()
  entrada.value?.select()
}

watch(termo, (t) => {
  if (timer) clearTimeout(timer)
  ativo.value = -1
  if (t.trim().length < 2) {
    seq++ // descarta qualquer resposta em voo
    grupos.value = []
    carregando.value = false
    falhou.value = false
    return
  }
  carregando.value = true
  timer = window.setTimeout(() => buscar(t.trim()), DEBOUNCE_MS)
})

async function buscar(t: string) {
  const meu = ++seq
  falhou.value = false
  const q = t.replace(/'/g, "''")
  const f = (campo: string, extra = '') => encodeURIComponent(`contains(${campo},'${q}')${extra}`)
  try {
    const [cli, con, neg, pro] = await Promise.all([
      odataQuery<{ Id: number; Nome: string; Email?: string }>(`Clientes?$select=Id,Nome,Email&$filter=${f('Nome')}&$top=${MAX_POR_GRUPO}`),
      odataQuery<{ Id: number; Nome: string; ClienteId: number }>(`PessoasContato?$select=Id,Nome,ClienteId&$filter=${f('Nome')}&$top=${MAX_POR_GRUPO}`),
      odataQuery<{ Id: number; Titulo: string }>(`Negocios?$select=Id,Titulo&$filter=${f('Titulo')}&$top=${MAX_POR_GRUPO}`),
      odataQuery<{ Id: number; Titulo?: string; Numero?: number }>(`Propostas?$select=Id,Titulo,Numero&$filter=${f('Titulo', ' and IsTemplate eq false')}&$top=${MAX_POR_GRUPO}`),
    ])
    if (meu !== seq) return // resposta atrasada
    grupos.value = [
      { nome: 'Clientes', icone: 'mdi-domain', itens: cli.value.map((x) => ({ chave: `c${x.Id}`, titulo: x.Nome, detalhe: x.Email, rota: `/clientes/${x.Id}` })) },
      { nome: 'Contatos', icone: 'mdi-account-outline', itens: con.value.map((x) => ({ chave: `p${x.Id}`, titulo: x.Nome, rota: x.ClienteId ? `/clientes/${x.ClienteId}` : '/contatos' })) },
      { nome: 'Negócios', icone: 'mdi-handshake-outline', itens: neg.value.map((x) => ({ chave: `n${x.Id}`, titulo: x.Titulo, rota: `/negocios/${x.Id}` })) },
      { nome: 'Propostas', icone: 'mdi-file-document-outline', itens: pro.value.map((x) => ({ chave: `r${x.Id}`, titulo: x.Titulo || `Proposta #${x.Numero ?? x.Id}`, detalhe: x.Numero ? `Nº ${x.Numero}` : undefined, rota: `/propostas/${x.Id}` })) },
    ].filter((g) => g.itens.length)
  } catch {
    if (meu !== seq) return
    grupos.value = []
    falhou.value = true
  } finally {
    if (meu === seq) carregando.value = false
  }
}

function indiceDe(r: Resultado): number { return lista.value.findIndex((x) => x.chave === r.chave) }

function ir(r: Resultado) {
  fechar()
  router.push(r.rota)
}

function fechar() {
  aberto.value = false
  termo.value = ''
  ativo.value = -1
  entrada.value?.blur()
}

function onKeydown(e: KeyboardEvent) {
  if (e.key === 'Escape') { e.preventDefault(); fechar(); return }
  const n = lista.value.length
  if (e.key === 'ArrowDown') { e.preventDefault(); if (n) ativo.value = (ativo.value + 1) % n }
  else if (e.key === 'ArrowUp') { e.preventDefault(); if (n) ativo.value = ativo.value <= 0 ? n - 1 : ativo.value - 1 }
  else if (e.key === 'Enter') {
    const alvo = lista.value[ativo.value >= 0 ? ativo.value : 0]
    if (alvo) { e.preventDefault(); ir(alvo) }
  }
}

function onFocusOut(e: FocusEvent) {
  if (!raiz.value?.contains(e.relatedTarget as Node | null)) aberto.value = false
}

onBeforeUnmount(() => { if (timer) clearTimeout(timer); seq++ })
</script>

<template>
  <div ref="raiz" class="busca-global" @focusout="onFocusOut">
    <div class="caixa" :class="{ foco: aberto }">
      <i class="mdi mdi-magnify"></i>
      <input ref="entrada" v-model="termo" type="text" placeholder="Buscar… ( / )" role="combobox"
        aria-label="Busca global" aria-autocomplete="list" :aria-expanded="aberto && termo.trim().length >= 2"
        autocomplete="off" @focus="aberto = true" @keydown="onKeydown" />
      <i v-if="carregando" class="mdi mdi-loading mdi-spin"></i>
    </div>

    <div v-if="aberto && termo.trim().length >= 2" class="painel" role="listbox">
      <div v-if="falhou" class="msg">Não foi possível buscar agora.</div>
      <div v-else-if="semResultado" class="msg">Nada encontrado para “{{ termo.trim() }}”.</div>
      <div v-else-if="carregando && !lista.length" class="msg">Buscando…</div>
      <template v-else>
        <div v-for="g in grupos" :key="g.nome" class="grupo">
          <div class="grupo-nome"><i class="mdi" :class="g.icone"></i> {{ g.nome }}</div>
          <button v-for="r in g.itens" :key="r.chave" type="button" class="res" role="option"
            :class="{ ativo: indiceDe(r) === ativo }" :aria-selected="indiceDe(r) === ativo"
            @mouseenter="ativo = indiceDe(r)" @mousedown.prevent @click="ir(r)">
            <span class="res-tit">{{ r.titulo }}</span>
            <span v-if="r.detalhe" class="res-det">{{ r.detalhe }}</span>
          </button>
        </div>
      </template>
    </div>
  </div>
</template>

<style scoped>
.busca-global { position: relative; width: 320px; }
.caixa { display: flex; align-items: center; gap: 8px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 8px; padding: 6px 12px; }
.caixa.foco { border-color: var(--accent); }
.caixa .mdi { color: var(--text-muted); font-size: 18px; }
.caixa input { border: 0; background: transparent; color: var(--text-primary); font-size: 13px; outline: none; width: 100%; }
.caixa input::placeholder { color: var(--text-muted); }
.painel { position: absolute; top: 100%; left: 0; right: 0; margin-top: 6px; z-index: 300; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; box-shadow: 0 8px 24px rgba(0, 0, 0, .35); max-height: 70vh; overflow-y: auto; padding: 4px; min-width: 320px; }
.msg { padding: 14px; font-size: 13px; color: var(--text-muted); text-align: center; }
.grupo + .grupo { border-top: 1px solid var(--border); margin-top: 4px; padding-top: 4px; }
.grupo-nome { padding: 6px 10px 4px; font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: .04em; color: var(--text-muted); display: flex; gap: 6px; align-items: center; }
.res { display: flex; align-items: baseline; justify-content: space-between; gap: 8px; width: 100%; padding: 7px 10px; border: 0; background: none; border-radius: 6px; cursor: pointer; text-align: left; color: var(--text-primary); font-size: 13px; }
.res.ativo { background: var(--bg-elevated); }
.res-tit { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.res-det { font-size: 11px; color: var(--text-muted); flex-shrink: 0; max-width: 45%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
</style>
