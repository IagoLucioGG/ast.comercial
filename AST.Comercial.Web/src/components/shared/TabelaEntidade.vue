<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue'
import { odataGet, type ODataParams } from '@/services/api'
import TabsFiltro from '@/components/shared/TabsFiltro.vue'
import FormModal from '@/components/shared/FormModal.vue'
import ModalColunas from '@/components/shared/ModalColunas.vue'
import ModalFiltro from '@/components/shared/ModalFiltro.vue'

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

const emit = defineEmits<{ novo: []; editar: [id: number] }>()

const props = withDefaults(defineProps<{
  entidade: string
  endpoint: string
  titulo: string
  colunas: ColunaConfig[]
  ordenacaoPadrao?: string
  camposBusca?: string[]
  placeholderBusca?: string
  camposEstaticos?: CampoEstaticoConfig[]
  filtroBase?: string
  semFormInterno?: boolean
}>(), {
  ordenacaoPadrao: 'Nome asc',
  placeholderBusca: 'Buscar...',
  semFormInterno: false,
})

const itens = ref<any[]>([])
const total = ref(0)
const carregando = ref(false)
const pagina = ref(1)
const porPagina = ref(20)
const busca = ref('')
const ordenacaoColuna = ref('')
const ordenacaoDirecao = ref<'asc' | 'desc'>('asc')
const filtroAba = ref<string | null>(null)
const ordenacaoAba = ref<string | null>(null)
const formAberto = ref(false)
const formId = ref<number | null>(null)

// Seleção de colunas visíveis
const mostrarSeletorColunas = ref(false)
const mostrarModalColunas = ref(false)
const mostrarFiltroDropdown = ref(false)
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

const colunasAtivas = computed(() => {
  const resultado: ColunaConfig[] = []
  for (const campo of colunasVisiveis.value) {
    const existente = props.colunas.find(c => c.campo === campo)
    if (existente) {
      resultado.push(existente)
    } else {
      // Coluna de expansão adicionada dinamicamente
      const label = campo.includes('/') ? campo.split('/').pop()! : campo
      resultado.push({ campo, label })
    }
  }
  return resultado
})

const selectFields = computed(() => {
  const camposDiretos = new Set(['Id'])
  const expands = new Set<string>()
  
  for (const campo of colunasVisiveis.value) {
    if (campo.includes('/')) {
      expands.add(campo.split('/')[0])
    } else {
      camposDiretos.add(campo)
    }
  }
  if (props.colunas.some(c => c.tipo === 'status')) camposDiretos.add('Ativo')
  
  return { select: Array.from(camposDiretos).join(','), expand: Array.from(expands).join(',') }
})

async function carregar() {
  carregando.value = true
  try {
    const { select, expand } = selectFields.value
    const params: ODataParams = {
      top: porPagina.value,
      skip: (pagina.value - 1) * porPagina.value,
      orderby: ordenacaoColuna.value ? `${ordenacaoColuna.value} ${ordenacaoDirecao.value}` : (ordenacaoAba.value ?? props.ordenacaoPadrao),
      count: true,
      select,
    }
    if (expand) params.expand = expand
    const filtros: string[] = []
    if (props.filtroBase) filtros.push(props.filtroBase)
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

const abaAtivaId = ref<number | null>(null)

function onFiltro(f: string | null, o: string | null, pp: number, colunas: string | null, abaId: number | null) {
  filtroAba.value = f; ordenacaoAba.value = o; porPagina.value = pp; abaAtivaId.value = abaId; pagina.value = 1
  if (colunas) {
    colunasVisiveis.value = colunas.split(',').map(c => c.trim()).filter(c => c)
  } else {
    colunasVisiveis.value = props.colunas.map(c => c.campo)
  }
  carregar()
}
function pg(p: number) { pagina.value = p; carregar() }
const tp = () => Math.ceil(total.value / porPagina.value)
function pesquisar() { pagina.value = 1; carregar() }

function ordenarPor(campo: string) {
  if (campo.includes('/')) return
  if (ordenacaoColuna.value === campo) {
    ordenacaoDirecao.value = ordenacaoDirecao.value === 'asc' ? 'desc' : 'asc'
  } else {
    ordenacaoColuna.value = campo
    ordenacaoDirecao.value = 'asc'
  }
  pagina.value = 1
  carregar()
}
function abrirNovo() { if (props.semFormInterno) { emit('novo') } else { formId.value = null; formAberto.value = true } }
function abrirEditar(id: number) { if (props.semFormInterno) { emit('editar', id) } else { formId.value = id; formAberto.value = true } }
function onSalvo() { formAberto.value = false; carregar() }

defineExpose({ carregar })

function onSalvarColunas(novasColunas: string[]) {
  colunasVisiveis.value = novasColunas
  salvarColunas()
  mostrarModalColunas.value = false
  // Salvar na aba ativa se houver
  if (abaAtivaId.value) {
    import('@/services/api').then(({ odataPatch }) => {
      odataPatch('Visualizacoes', abaAtivaId.value!, { Colunas: novasColunas.join(',') })
    })
  }
  carregar()
}

function onAplicarFiltro(filtro: string | null) {
  filtroAba.value = filtro
  pagina.value = 1
  carregar()
}

function formatarValor(item: any, col: ColunaConfig): string {
  let val: any
  if (col.campo.includes('/')) {
    const partes = col.campo.split('/')
    val = item
    for (const p of partes) {
      val = val?.[p]
      if (val === undefined || val === null) break
    }
  } else {
    val = item[col.campo]
  }
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
    <div class="te-header">
      <div class="search-inline">
        <i class="mdi mdi-magnify"></i>
        <input v-model="busca" :placeholder="placeholderBusca" @keyup.enter="pesquisar" />
      </div>
      <div class="te-header-right">
        <div class="te-filtro-wrap">
          <button class="btn-icon" title="Filtros" @click.stop="mostrarFiltroDropdown = !mostrarFiltroDropdown"><i class="mdi mdi-filter-variant"></i></button>
          <ModalFiltro :aberto="mostrarFiltroDropdown" :entidade="entidade" :filtros-salvos="[]" @fechar="mostrarFiltroDropdown = false" @aplicar="onAplicarFiltro" />
        </div>
        <button class="btn-colunas" @click="mostrarModalColunas = true"><i class="mdi mdi-view-column-outline"></i> Colunas</button>
        <span class="total">{{ total }} registros</span>
        <button class="btn-novo" @click="abrirNovo"><i class="mdi mdi-plus"></i> Novo</button>
      </div>
    </div>

    <div class="te-body">
      <aside class="te-sidebar">
        <TabsFiltro :entidade="entidade" :campos-estaticos="camposEstaticos" vertical @filtro-alterado="onFiltro" />
      </aside>
      <div class="te-main">
        <div class="te-table-wrap">
          <table>
            <thead>
              <tr>
                <th v-for="col in colunasAtivas" :key="col.campo" :style="col.largura ? { width: col.largura } : {}" @click="ordenarPor(col.campo)">
                  {{ col.label }}<i v-if="ordenacaoColuna === col.campo" class="mdi" :class="ordenacaoDirecao === 'asc' ? 'mdi-arrow-up' : 'mdi-arrow-down'"></i>
                </th>
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
          <span class="pag-info">Mostrando {{ (pagina-1)*porPagina + 1 }} - {{ Math.min(pagina*porPagina, total) }} de {{ total }}</span>
          <button :disabled="pagina <= 1" @click="pg(pagina - 1)"><i class="mdi mdi-chevron-left"></i></button>
          <span>{{ pagina }} / {{ tp() }}</span>
          <button :disabled="pagina >= tp()" @click="pg(pagina + 1)"><i class="mdi mdi-chevron-right"></i></button>
        </div>
      </div>
    </div>

    <FormModal v-if="formAberto" :entidade="entidade" :endpoint="endpoint" :id="formId" :titulo="titulo" @fechar="formAberto = false" @salvo="onSalvo" />
    <ModalColunas v-if="mostrarModalColunas" :colunas="colunas" :colunas-visiveis="colunasVisiveis" :entidade="entidade" @fechar="mostrarModalColunas = false" @salvar="onSalvarColunas" />
  </div>
</template>

<style scoped>
.te-container{display:flex;flex-direction:column;height:100%;overflow:hidden}
.te-header{display:flex;align-items:center;gap:16px;flex-shrink:0;padding:12px 20px;background:var(--bg-primary);border-bottom:1px solid var(--border)}
.search-inline{display:flex;align-items:center;gap:8px;background:var(--bg-surface);border:1px solid var(--border);border-radius:8px;padding:8px 14px;width:280px;transition:border-color .15s}
.search-inline:focus-within{border-color:var(--accent)}
.search-inline .mdi{color:var(--text-muted);font-size:16px}
.search-inline input{border:none;background:transparent;color:var(--text-primary);font-size:13px;outline:none;width:100%}
.search-inline input::placeholder{color:var(--text-muted)}
.te-header-right{display:flex;align-items:center;gap:12px;margin-left:auto}
.btn-icon{display:flex;align-items:center;justify-content:center;width:34px;height:34px;border:1px solid var(--border);border-radius:8px;background:var(--bg-surface);color:var(--text-secondary);font-size:16px;cursor:pointer;transition:all .15s}
.btn-icon:hover{background:var(--bg-elevated);color:var(--accent);border-color:var(--accent)}
.te-filtro-wrap{position:relative}
.btn-colunas{display:flex;align-items:center;gap:6px;padding:7px 14px;border:1px solid var(--border);border-radius:8px;background:var(--bg-surface);color:var(--text-secondary);font-size:12px;cursor:pointer;transition:all .15s}
.btn-colunas:hover{background:var(--bg-elevated);color:var(--text-primary);border-color:var(--text-muted)}
.btn-novo{display:flex;align-items:center;gap:5px;padding:8px 16px;border:none;border-radius:8px;background:var(--accent);color:#000;font-size:12px;font-weight:600;cursor:pointer;transition:background .15s}
.btn-novo:hover{background:var(--accent-hover)}
.total{color:var(--text-muted);font-size:12px;white-space:nowrap}
.te-body{display:flex;flex:1;overflow:hidden}
.te-sidebar{width:200px;border-right:1px solid var(--border);background:var(--bg-secondary);padding:16px;overflow:visible;flex-shrink:0;display:flex;flex-direction:column}
.te-sidebar :deep(.tabs-filtro){margin-bottom:0}
.te-main{flex:1;display:flex;flex-direction:column;overflow:hidden}
.te-table-wrap{flex:1;overflow:auto;background:var(--bg-surface);border-left:1px solid var(--border)}
table{width:100%;border-collapse:collapse;font-size:13px}
thead{position:sticky;top:0;z-index:1}
th{background:var(--bg-secondary);color:var(--text-muted);font-weight:600;text-transform:uppercase;font-size:10px;letter-spacing:.6px;padding:12px 16px;text-align:left;border-bottom:1px solid var(--border);cursor:pointer;user-select:none;white-space:nowrap}
th:hover{color:var(--text-primary)}
th .mdi{font-size:12px;color:var(--accent);margin-left:4px}
td{padding:11px 16px;border-bottom:1px solid var(--border);color:var(--text-secondary)}
tr:hover td{background:var(--bg-elevated)}
.msg{text-align:center;padding:60px 20px;color:var(--text-muted);font-size:14px}
.cell-avatar{display:flex;align-items:center;gap:10px;color:var(--text-primary);font-weight:500}
.avatar-mini{width:30px;height:30px;border-radius:50%;background:var(--accent);color:#000;display:flex;align-items:center;justify-content:center;font-size:12px;font-weight:700;flex-shrink:0}
.badge{background:var(--bg-elevated);border:1px solid var(--border);padding:3px 10px;border-radius:4px;font-size:11px;color:var(--text-secondary)}
.dot{display:inline-block;width:8px;height:8px;border-radius:50%;background:#ef4444;margin-right:6px}.dot.on{background:#22c55e}
.row-click{cursor:pointer;transition:background .1s}
.paginacao{display:flex;align-items:center;justify-content:center;gap:12px;padding:12px 16px;flex-shrink:0;border-top:1px solid var(--border);background:var(--bg-primary)}
.paginacao button{width:32px;height:32px;border-radius:6px;border:1px solid var(--border);background:var(--bg-surface);color:var(--text-secondary);cursor:pointer;display:flex;align-items:center;justify-content:center;transition:all .15s}
.paginacao button:hover:not(:disabled){background:var(--bg-elevated);border-color:var(--text-muted)}
.paginacao button:disabled{opacity:.3;cursor:not-allowed}
.paginacao span{color:var(--text-muted);font-size:12px}
.pag-info{font-size:11px;color:var(--text-muted);margin-right:auto}

@media (max-width: 768px) {
  .te-body{flex-direction:column}
  .te-sidebar{width:100%;border-right:none;border-bottom:1px solid var(--border);padding:12px;max-height:140px}
  .te-main{padding-left:0}
  .search-inline{width:100%}
  .te-header{flex-wrap:wrap}
}
</style>
