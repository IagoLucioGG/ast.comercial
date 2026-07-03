<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { odataGet, type ODataParams } from '@/services/api'
import TabsFiltro from '@/components/shared/TabsFiltro.vue'
import FormModal from '@/components/shared/FormModal.vue'

export interface ColunaConfig {
  campo: string
  label: string
  tipo?: 'texto' | 'data' | 'badge' | 'status' | 'moeda' | 'avatar'
  largura?: string
}

export interface CampoEstaticoConfig {
  nome: string
  tipo: string
}

const props = withDefaults(defineProps<{
  entidade: string
  endpoint: string
  titulo: string
  colunas: ColunaConfig[]
  ordenacaoPadrao?: string
  camposBusca?: string[]
  placeholderBusca?: string
  camposEstaticos?: CampoEstaticoConfig[]
}>(), {
  ordenacaoPadrao: 'Nome asc',
  placeholderBusca: 'Buscar...',
})

const itens = ref<any[]>([])
const total = ref(0)
const carregando = ref(false)
const pagina = ref(1)
const porPagina = ref(20)
const busca = ref('')
const filtroAba = ref<string | null>(null)
const ordenacaoAba = ref<string | null>(null)
const formAberto = ref(false)
const formId = ref<number | null>(null)

// Seleção de colunas visíveis
const mostrarSeletorColunas = ref(false)
const colunasVisiveis = ref<string[]>([])
const STORAGE_KEY = computed(() => `colunas_${props.entidade}`)

function inicializarColunas() {
  const salvas = localStorage.getItem(STORAGE_KEY.value)
  if (salvas) {
    try {
      const parsed = JSON.parse(salvas)
      colunasVisiveis.value = parsed.filter((c: string) => props.colunas.some(col => col.campo === c))
      if (colunasVisiveis.value.length === 0) colunasVisiveis.value = props.colunas.map(c => c.campo)
    } catch { colunasVisiveis.value = props.colunas.map(c => c.campo) }
  } else {
    colunasVisiveis.value = props.colunas.map(c => c.campo)
  }
}

function salvarColunas() {
  localStorage.setItem(STORAGE_KEY.value, JSON.stringify(colunasVisiveis.value))
}

function toggleColuna(campo: string) {
  const idx = colunasVisiveis.value.indexOf(campo)
  if (idx >= 0) {
    if (colunasVisiveis.value.length > 1) colunasVisiveis.value.splice(idx, 1)
  } else {
    colunasVisiveis.value.push(campo)
  }
  salvarColunas()
}

const colunasAtivas = computed(() => props.colunas.filter(c => colunasVisiveis.value.includes(c.campo)))

const selectFields = computed(() => {
  const campos = new Set(['Id', ...colunasVisiveis.value])
  if (props.colunas.some(c => c.tipo === 'status')) campos.add('Ativo')
  return Array.from(campos).join(',')
})

async function carregar() {
  carregando.value = true
  try {
    const params: ODataParams = {
      top: porPagina.value,
      skip: (pagina.value - 1) * porPagina.value,
      orderby: ordenacaoAba.value ?? props.ordenacaoPadrao,
      count: true,
      select: selectFields.value,
    }
    const filtros: string[] = []
    if (filtroAba.value) filtros.push(filtroAba.value)
    if (busca.value && props.camposBusca?.length) {
      const buscaFiltros = props.camposBusca.map(c => `contains(${c},'${busca.value}')`)
      filtros.push(`(${buscaFiltros.join(' or ')})`)
    }
    if (filtros.length) params.filter = filtros.join(' and ')
    const res = await odataGet<any>(props.endpoint, params)
    itens.value = res.value
    total.value = res['@odata.count'] ?? 0
  } finally { carregando.value = false }
}

function onFiltro(f: string | null, o: string | null, pp: number) {
  filtroAba.value = f; ordenacaoAba.value = o; porPagina.value = pp; pagina.value = 1; carregar()
}
function pg(p: number) { pagina.value = p; carregar() }
const tp = () => Math.ceil(total.value / porPagina.value)
function pesquisar() { pagina.value = 1; carregar() }
function abrirNovo() { formId.value = null; formAberto.value = true }
function abrirEditar(id: number) { formId.value = id; formAberto.value = true }
function onSalvo() { formAberto.value = false; carregar() }

function formatarValor(item: any, col: ColunaConfig): string {
  const val = item[col.campo]
  if (val === null || val === undefined) return '-'
  switch (col.tipo) {
    case 'data': return new Date(val).toLocaleDateString('pt-BR')
    case 'moeda': return `R$ ${Number(val).toLocaleString('pt-BR')}`
    case 'status': return val ? 'Ativo' : 'Inativo'
    default: return String(val)
  }
}

watch(() => props.colunas, inicializarColunas, { immediate: true })
onMounted(carregar)
</script>

<template>
  <div class="te-container" @click="mostrarSeletorColunas = false">
    <TabsFiltro :entidade="entidade" :campos-estaticos="camposEstaticos" @filtro-alterado="onFiltro" />

    <div class="te-toolbar">
      <div class="search-inline">
        <i class="mdi mdi-magnify"></i>
        <input v-model="busca" :placeholder="placeholderBusca" @keyup.enter="pesquisar" />
      </div>
      <div class="te-toolbar-right">
        <div class="col-selector-wrap">
          <button class="btn-colunas" @click.stop="mostrarSeletorColunas = !mostrarSeletorColunas">
            <i class="mdi mdi-view-column-outline"></i> Colunas
          </button>
          <div v-if="mostrarSeletorColunas" class="col-dropdown" @click.stop>
            <p class="col-dropdown-title">Colunas visíveis</p>
            <label v-for="col in colunas" :key="col.campo" class="col-check" :class="{ active: colunasVisiveis.includes(col.campo) }">
              <input type="checkbox" :checked="colunasVisiveis.includes(col.campo)" @change="toggleColuna(col.campo)" />
              <span>{{ col.label }}</span>
            </label>
          </div>
        </div>
        <button class="btn-novo" @click="abrirNovo"><i class="mdi mdi-plus"></i> Novo</button>
        <span class="total">{{ total }} registros</span>
      </div>
    </div>

    <div class="te-table-wrap">
      <table>
        <thead>
          <tr>
            <th v-for="col in colunasAtivas" :key="col.campo" :style="col.largura ? { width: col.largura } : {}">{{ col.label }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="carregando"><td :colspan="colunasAtivas.length" class="msg">Carregando...</td></tr>
          <tr v-else-if="!itens.length"><td :colspan="colunasAtivas.length" class="msg">Nenhum registro encontrado</td></tr>
          <tr v-for="item in itens" :key="item.Id" class="row-click" @click="abrirEditar(item.Id)">
            <td v-for="col in colunasAtivas" :key="col.campo">
              <div v-if="col.tipo === 'avatar'" class="cell-avatar">
                <div class="avatar-mini">{{ (item[col.campo] ?? '?')[0] }}</div>
                <span>{{ item[col.campo] ?? '-' }}</span>
              </div>
              <span v-else-if="col.tipo === 'status'"><span class="dot" :class="{ on: item[col.campo] }"></span>{{ item[col.campo] ? 'Ativo' : 'Inativo' }}</span>
              <span v-else-if="col.tipo === 'badge'" class="badge">{{ item[col.campo] ?? '-' }}</span>
              <span v-else>{{ formatarValor(item, col) }}</span>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="tp() > 1" class="paginacao">
      <button :disabled="pagina <= 1" @click="pg(pagina - 1)"><i class="mdi mdi-chevron-left"></i></button>
      <span>{{ pagina }} / {{ tp() }}</span>
      <button :disabled="pagina >= tp()" @click="pg(pagina + 1)"><i class="mdi mdi-chevron-right"></i></button>
    </div>

    <FormModal v-if="formAberto" :entidade="entidade" :endpoint="endpoint" :id="formId" :titulo="titulo" @fechar="formAberto = false" @salvo="onSalvo" />
  </div>
</template>

<style scoped>
.te-container{display:flex;flex-direction:column;height:100%}
.te-toolbar{display:flex;align-items:center;justify-content:space-between;margin-bottom:16px;gap:12px}
.search-inline{display:flex;align-items:center;gap:8px;background:var(--bg-surface);border:1px solid var(--border);border-radius:8px;padding:8px 12px;width:300px}
.search-inline .mdi{color:var(--text-muted);font-size:18px}
.search-inline input{border:none;background:transparent;color:var(--text-primary);font-size:13px;outline:none;width:100%}
.search-inline input::placeholder{color:var(--text-muted)}
.te-toolbar-right{display:flex;align-items:center;gap:10px}
.col-selector-wrap{position:relative}
.btn-colunas{display:flex;align-items:center;gap:6px;padding:6px 12px;border:1px solid var(--border);border-radius:6px;background:var(--bg-surface);color:var(--text-secondary);font-size:12px;cursor:pointer}
.btn-colunas:hover{background:var(--bg-elevated);color:var(--text-primary)}
.col-dropdown{position:absolute;top:100%;right:0;margin-top:6px;z-index:50;background:var(--bg-primary);border:1px solid var(--border);border-radius:10px;box-shadow:0 8px 24px rgba(0,0,0,.35);padding:12px;min-width:200px;animation:fadeIn .12s ease}
@keyframes fadeIn{from{opacity:0;transform:translateY(-4px)}to{opacity:1;transform:translateY(0)}}
.col-dropdown-title{font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.5px;margin-bottom:10px}
.col-check{display:flex;align-items:center;gap:8px;padding:6px 8px;border-radius:6px;cursor:pointer;font-size:13px;color:var(--text-secondary);transition:background .1s}
.col-check:hover{background:var(--bg-elevated)}
.col-check.active{color:var(--text-primary)}
.col-check input[type="checkbox"]{accent-color:var(--accent);width:14px;height:14px}
.btn-novo{display:flex;align-items:center;gap:4px;padding:6px 14px;border:none;border-radius:6px;background:var(--accent);color:#000;font-size:12px;font-weight:600;cursor:pointer}
.btn-novo:hover{background:var(--accent-hover)}
.total{color:var(--text-muted);font-size:12px;white-space:nowrap}
.te-table-wrap{flex:1;overflow:auto;border:1px solid var(--border);border-radius:10px}
table{width:100%;border-collapse:collapse;font-size:13px}
thead{position:sticky;top:0;z-index:1}
th{background:var(--bg-secondary);color:var(--text-muted);font-weight:600;text-transform:uppercase;font-size:11px;letter-spacing:.5px;padding:12px 14px;text-align:left;border-bottom:1px solid var(--border)}
td{padding:10px 14px;border-bottom:1px solid var(--border);color:var(--text-secondary)}
tr:hover td{background:var(--bg-elevated)}
.msg{text-align:center;padding:40px;color:var(--text-muted)}
.cell-avatar{display:flex;align-items:center;gap:10px;color:var(--text-primary);font-weight:500}
.avatar-mini{width:28px;height:28px;border-radius:50%;background:var(--accent);color:#000;display:flex;align-items:center;justify-content:center;font-size:11px;font-weight:700;flex-shrink:0}
.badge{background:var(--bg-elevated);border:1px solid var(--border);padding:2px 8px;border-radius:4px;font-size:11px}
.dot{display:inline-block;width:8px;height:8px;border-radius:50%;background:#ef4444;margin-right:4px}.dot.on{background:#22c55e}
.row-click{cursor:pointer}
.paginacao{display:flex;align-items:center;justify-content:center;gap:12px;padding:12px 0}
.paginacao button{width:32px;height:32px;border-radius:6px;border:1px solid var(--border);background:var(--bg-surface);color:var(--text-secondary);cursor:pointer;display:flex;align-items:center;justify-content:center}
.paginacao button:hover:not(:disabled){background:var(--bg-elevated)}
.paginacao button:disabled{opacity:.3;cursor:not-allowed}
.paginacao span{color:var(--text-muted);font-size:13px}
</style>
