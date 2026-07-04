<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { odataGet, odataPost, odataPatch, odataDelete } from '@/services/api'

interface Visualizacao { Id: number; Nome: string; Filtro: string | null; Ordenacao: string | null; ItensPorPagina: number; Padrao: boolean; Ordem: number; UsuariosVisiveis: string | null; Colunas: string | null }
interface NodoSchema { nome: string; tipo: string; filhos?: NodoSchema[] }
interface Condicao { id: number; caminho: string; label: string; operador: string; valor: string; tipoValor: string }
interface UsuarioOpcao { Id: number; Nome: string }

interface CampoEstatico { nome: string; tipo: string }

const props = defineProps<{ entidade: string; camposEstaticos?: CampoEstatico[]; vertical?: boolean }>()
const emit = defineEmits<{ 'filtro-alterado': [filtro: string | null, ordenacao: string | null, itensPorPagina: number, colunas: string | null, abaId: number | null] }>()

const abas = ref<Visualizacao[]>([])
const abaAtiva = ref<number | null>(null)
const mostrarModal = ref(false)
const modoEdicao = ref(false)
const abaEditandoId = ref<number | null>(null)
const mostrarFiltroRapido = ref(false)
const arvore = ref<NodoSchema[]>([])
const navegacao = ref<NodoSchema[][]>([])
const buscaCampo = ref('')
const caminhoNavegacao = ref<string[]>([])

const novaAbaNome = ref('')
const novaAbaFiltros = ref<Condicao[]>([])
const filtroRapidoCampo = ref('')
const filtroRapidoOperador = ref('eq')
const filtroRapidoValor = ref('')

// Visibilidade de usuários
const usuariosDisponiveis = ref<UsuarioOpcao[]>([])
const usuariosSelecionados = ref<number[]>([])
const visibilidadeTodos = ref(true)
const buscaUsuario = ref('')

const menuAbaAberta = ref<number | null>(null)

const operadores = [
  { value: 'eq', label: 'Igual a' },
  { value: 'ne', label: 'Diferente de' },
  { value: 'contains', label: 'Contém' },
  { value: 'gt', label: 'Maior que' },
  { value: 'lt', label: 'Menor que' },
  { value: 'ge', label: 'Maior ou igual' },
  { value: 'le', label: 'Menor ou igual' },
]

const nivelAtual = computed(() => {
  if (navegacao.value.length === 0) {
    if (!buscaCampo.value) return arvore.value
    const b = buscaCampo.value.toLowerCase()
    return arvore.value.filter(n => n.nome.toLowerCase().includes(b))
  }
  return navegacao.value[navegacao.value.length - 1]
})

const usuariosFiltrados = computed(() => {
  if (!buscaUsuario.value) return usuariosDisponiveis.value
  const b = buscaUsuario.value.toLowerCase()
  return usuariosDisponiveis.value.filter(u => u.Nome.toLowerCase().includes(b))
})

async function carregarSchema() {
  // Se foram passados campos estáticos, usar direto
  if (props.camposEstaticos?.length) {
    arvore.value = props.camposEstaticos.map(c => ({ nome: c.nome, tipo: c.tipo }))
    return
  }
  try {
    const res = await fetch(`/odata/Campos@Esquema(${props.entidade})`, {
      headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` }
    })
    if (!res.ok) return
    const schema = await res.json()
    arvore.value = parsearSchema(schema['$registro']?.propriedades ?? {})
  } catch { /* */ }
}

function parsearSchema(propriedades: any): NodoSchema[] {
  const resultado: NodoSchema[] = []
  for (const [nome, info] of Object.entries(propriedades) as any[]) {
    if (nome === 'SenhaHash' || nome === 'ChaveExterna') continue
    const nodo: NodoSchema = { nome, tipo: info.tipo }
    if (info.propriedades) nodo.filhos = parsearSchema(info.propriedades)
    resultado.push(nodo)
  }
  return resultado
}

function abrirNavegacao(nodo: NodoSchema) {
  if (nodo.filhos) { navegacao.value.push(nodo.filhos); caminhoNavegacao.value.push(nodo.nome) }
}

function voltarNavegacao() { navegacao.value.pop(); caminhoNavegacao.value.pop() }

function selecionarCampo(nodo: NodoSchema) {
  const caminho = nodo.filhos
    ? [...caminhoNavegacao.value, nodo.nome + 'Id'].join('/')
    : [...caminhoNavegacao.value, nodo.nome].join('/')
  const label = caminhoNavegacao.value.length > 0
    ? `${caminhoNavegacao.value.join(' → ')} → ${nodo.nome}`
    : nodo.nome
  const tipoValor = nodo.filhos ? `fk:${nodo.nome}` : nodo.tipo
  const id = Date.now() + Math.random()
  const condicao: Condicao = { id, caminho, label, operador: nodo.tipo.startsWith('enum') ? 'in' : 'eq', valor: '', tipoValor }
  novaAbaFiltros.value.push(condicao)
  if (nodo.filhos) carregarOpcoesFk(id, nodo.nome)
  navegacao.value = []; caminhoNavegacao.value = []
}

const opcoesFk = ref<Record<number, { id: number; nome: string }[]>>({})
const fkSelecionados = ref<Record<number, number[]>>({})

function resolverEndpoint(entidade: string): string {
  const irregulares: Record<string, string> = {
    Funil: 'Funis', EtapaFunil: 'EtapasFunil', Negocio: 'Negocios',
    Atividade: 'Atividades', PessoaContato: 'PessoasContato',
    ItemProposta: 'ItensProposta', SecaoProposta: 'SecoesProposta',
    ParcelaProposta: 'ParcelasProposta',
  }
  return irregulares[entidade] ?? entidade + 's'
}

async function carregarOpcoesFk(indice: number, entidade: string) {
  const endpoint = resolverEndpoint(entidade)
  try {
    const res = await odataGet<{ Id: number; Nome: string }>(endpoint, { select: 'Id,Nome', top: 50, orderby: 'Nome asc' })
    opcoesFk.value[indice] = res.value.map(r => ({ id: r.Id, nome: r.Nome }))
  } catch { opcoesFk.value[indice] = [] }
  fkSelecionados.value[indice] = []
}

function toggleFkValor(condicao: Condicao, id: number) {
  const sel = fkSelecionados.value[condicao.id] ?? []
  const idx = sel.indexOf(id)
  if (idx >= 0) sel.splice(idx, 1)
  else sel.push(id)
  fkSelecionados.value[condicao.id] = [...sel]
  condicao.valor = sel.join(',')
  condicao.operador = 'in'
}

function toggleEnumValor(condicao: Condicao, opt: string) {
  const valores = condicao.valor ? condicao.valor.split(',') : []
  const idx = valores.indexOf(opt)
  if (idx >= 0) valores.splice(idx, 1)
  else valores.push(opt)
  condicao.valor = valores.join(',')
}

function removerCondicao(condicaoId: number) {
  novaAbaFiltros.value = novaAbaFiltros.value.filter(c => c.id !== condicaoId)
  delete opcoesFk.value[condicaoId]
  delete fkSelecionados.value[condicaoId]
}

function montarFiltroOData(filtros: Condicao[]): string | null {
  const partes = filtros.filter(f => f.caminho && f.valor).map(f => {
    if (f.operador === 'contains') return `contains(${f.caminho},'${f.valor}')`
    if (f.operador === 'in') {
      const vals = f.valor.split(',').filter(v => v)
      if (vals.length === 0) return ''
      if (vals.length === 1) return `${f.caminho} eq '${vals[0]}'`
      return '(' + vals.map(v => `${f.caminho} eq '${v}'`).join(' or ') + ')'
    }
    return `${f.caminho} ${f.operador} '${f.valor}'`
  }).filter(p => p)
  return partes.length > 0 ? partes.join(' and ') : null
}

function parsearFiltroParaCondicoes(filtro: string): Condicao[] {
  if (!filtro) return []
  const condicoes: Condicao[] = []
  const partes = filtro.split(' and ')
  for (const parte of partes) {
    const id = Date.now() + Math.random()
    const containsMatch = parte.match(/contains\(([^,]+),'([^']+)'\)/)
    if (containsMatch) {
      condicoes.push({ id, caminho: containsMatch[1]!, label: containsMatch[1]!, operador: 'contains', valor: containsMatch[2]!, tipoValor: 'string' })
      continue
    }
    const simpleMatch = parte.trim().match(/^([^\s]+)\s+(eq|ne|gt|lt|ge|le)\s+'([^']*)'$/)
    if (simpleMatch) {
      condicoes.push({ id, caminho: simpleMatch[1]!, label: simpleMatch[1]!, operador: simpleMatch[2]!, valor: simpleMatch[3]!, tipoValor: 'string' })
    }
  }
  return condicoes
}

async function carregarUsuarios() {
  try {
    const res = await odataGet<UsuarioOpcao>('Usuarios', { select: 'Id,Nome', top: 100, orderby: 'Nome asc' })
    usuariosDisponiveis.value = res.value
  } catch { usuariosDisponiveis.value = [] }
}

function toggleUsuario(id: number) {
  const idx = usuariosSelecionados.value.indexOf(id)
  if (idx >= 0) usuariosSelecionados.value.splice(idx, 1)
  else usuariosSelecionados.value.push(id)
}

async function carregarAbas() {
  try {
    const res = await odataGet<Visualizacao>('Visualizacoes', { filter: `EntidadeAlvo eq '${props.entidade}'`, orderby: 'Ordem asc' })
    abas.value = res.value
    const primeira = abas.value[0]
    if (abas.value.length > 0 && abaAtiva.value === null && primeira) selecionarAba(primeira)
  } catch { abas.value = [] }
}

function selecionarAba(aba: Visualizacao) { abaAtiva.value = aba.Id; emit('filtro-alterado', aba.Filtro, aba.Ordenacao, aba.ItensPorPagina, aba.Colunas, aba.Id) }
function verTodos() { abaAtiva.value = null; emit('filtro-alterado', null, null, 20, null, null) }

function abrirModalNova() {
  modoEdicao.value = false
  abaEditandoId.value = null
  novaAbaNome.value = ''
  novaAbaFiltros.value = []
  visibilidadeTodos.value = true
  usuariosSelecionados.value = []
  navegacao.value = []; caminhoNavegacao.value = []; buscaCampo.value = ''
  mostrarModal.value = true
  carregarUsuarios()
}

function abrirModalEditar(aba: Visualizacao) {
  modoEdicao.value = true
  abaEditandoId.value = aba.Id
  novaAbaNome.value = aba.Nome
  novaAbaFiltros.value = aba.Filtro ? parsearFiltroParaCondicoes(aba.Filtro) : []
  visibilidadeTodos.value = !aba.UsuariosVisiveis
  usuariosSelecionados.value = aba.UsuariosVisiveis ? aba.UsuariosVisiveis.split(',').map(Number).filter(n => !isNaN(n)) : []
  navegacao.value = []; caminhoNavegacao.value = []; buscaCampo.value = ''
  menuAbaAberta.value = null
  mostrarModal.value = true
  carregarUsuarios()
}

async function salvarAba() {
  if (!novaAbaNome.value.trim()) return
  const payload: any = {
    Nome: novaAbaNome.value,
    EntidadeAlvo: props.entidade,
    Filtro: montarFiltroOData(novaAbaFiltros.value),
    ItensPorPagina: 20,
    UsuariosVisiveis: visibilidadeTodos.value ? null : usuariosSelecionados.value.join(',') || null,
  }
  if (modoEdicao.value && abaEditandoId.value) {
    await odataPatch('Visualizacoes', abaEditandoId.value, payload)
  } else {
    payload.Ordem = abas.value.length + 1
    await odataPost<Visualizacao>('Visualizacoes', payload)
  }
  novaAbaNome.value = ''; novaAbaFiltros.value = []; mostrarModal.value = false
  await carregarAbas()
}

async function excluirAba(id: number) {
  if (!confirm('Deseja realmente excluir esta aba?')) return
  await odataDelete('Visualizacoes', id)
  if (abaAtiva.value === id) verTodos()
  menuAbaAberta.value = null
  mostrarModal.value = false
  await carregarAbas()
}

function aplicarFiltroRapido() {
  if (!filtroRapidoCampo.value || !filtroRapidoValor.value) return
  const filtro = montarFiltroOData([{ id: 0, caminho: filtroRapidoCampo.value, label: '', operador: filtroRapidoOperador.value, valor: filtroRapidoValor.value, tipoValor: 'string' }])
  abaAtiva.value = null; emit('filtro-alterado', filtro, null, 20, null, null); mostrarFiltroRapido.value = false
}

function limparFiltroRapido() { filtroRapidoCampo.value = ''; filtroRapidoValor.value = ''; mostrarFiltroRapido.value = false; verTodos() }

function toggleMenuAba(id: number, ev: Event) {
  ev.stopPropagation()
  menuAbaAberta.value = menuAbaAberta.value === id ? null : id
}

function fecharMenus() { menuAbaAberta.value = null }

onMounted(() => { carregarAbas(); carregarSchema() })
</script>

<template>
  <div class="tabs-filtro" :class="{ vertical: props.vertical }" @click="fecharMenus">
    <div class="tabs-bar">
      <div class="tabs-bar-header">
        <button class="tab" :class="{ active: abaAtiva === null }" @click="verTodos">Todos</button>
        <button class="tab tab-add" @click="abrirModalNova" title="Nova aba"><i class="mdi mdi-plus"></i></button>
      </div>
      <div v-for="aba in abas" :key="aba.Id" class="tab-wrapper">
        <button class="tab" :class="{ active: abaAtiva === aba.Id }" @click="selecionarAba(aba)">
          {{ aba.Nome }}
          <span class="tab-menu-trigger" @click.stop="toggleMenuAba(aba.Id, $event)"><i class="mdi mdi-dots-vertical"></i></span>
        </button>
        <div v-if="menuAbaAberta === aba.Id" class="tab-dropdown">
          <p class="tab-dropdown-title">Ações da aba</p>
          <button @click.stop="abrirModalEditar(aba)"><i class="mdi mdi-pencil-outline"></i> Editar aba</button>
          <button class="del" @click.stop="excluirAba(aba.Id)"><i class="mdi mdi-delete-outline"></i> Excluir aba</button>
        </div>
      </div>
      <div class="tabs-actions">
        <button class="btn-filtro" @click.stop="mostrarFiltroRapido = !mostrarFiltroRapido">
          <i class="mdi mdi-filter-outline"></i> Filtros
        </button>
      </div>
    </div>

    <div v-if="mostrarFiltroRapido" class="filtro-rapido">
      <div class="filtro-row">
        <input v-model="filtroRapidoCampo" placeholder="Campo (ex: Nome, Cargo/Nome)" class="filtro-input" />
        <select v-model="filtroRapidoOperador" class="filtro-select">
          <option v-for="op in operadores" :key="op.value" :value="op.value">{{ op.label }}</option>
        </select>
        <input v-model="filtroRapidoValor" placeholder="Valor" class="filtro-input" @keyup.enter="aplicarFiltroRapido" />
        <button class="btn-acao btn-ok" @click="aplicarFiltroRapido"><i class="mdi mdi-check"></i></button>
        <button class="btn-acao btn-x" @click="limparFiltroRapido"><i class="mdi mdi-close"></i></button>
      </div>
    </div>

    <!-- Modal Criar/Editar Aba -->
    <div v-if="mostrarModal" class="modal-overlay" @click.self="mostrarModal = false">
      <div class="modal">
        <div class="modal-header">
          <h3>{{ modoEdicao ? 'Editar aba' : 'Nova aba' }}</h3>
          <button class="btn-fechar" @click="mostrarModal = false"><i class="mdi mdi-close"></i></button>
        </div>
        <div class="modal-body">
          <div class="painel-campos">
            <div class="painel-campos-header">
              <button v-if="caminhoNavegacao.length > 0" class="btn-voltar-nav" @click="voltarNavegacao"><i class="mdi mdi-arrow-left"></i></button>
              <span class="nav-breadcrumb">
                <span class="nav-root">{{ entidade }}</span>
                <template v-for="(p, i) in caminhoNavegacao" :key="i"><i class="mdi mdi-chevron-right nav-sep"></i><span>{{ p }}</span></template>
              </span>
            </div>
            <input v-if="navegacao.length === 0" v-model="buscaCampo" placeholder="Buscar campo..." class="campos-busca" />
            <div class="campos-lista">
              <div v-for="nodo in nivelAtual" :key="nodo.nome" class="campo-item-wrap">
                <button class="campo-item" @click="selecionarCampo(nodo)">
                  <i class="mdi" :class="nodo.filhos ? 'mdi-table-arrow-right' : (nodo.tipo === 'string' ? 'mdi-format-text' : nodo.tipo === 'number' ? 'mdi-numeric' : nodo.tipo === 'datetime' ? 'mdi-calendar-clock' : nodo.tipo === 'boolean' ? 'mdi-toggle-switch-outline' : nodo.tipo.startsWith('enum') ? 'mdi-format-list-bulleted' : 'mdi-code-braces')"></i>
                  <span class="campo-item-nome">{{ nodo.nome }}</span>
                  <span v-if="!nodo.filhos" class="campo-item-tipo">{{ nodo.tipo.startsWith('enum') ? 'lista' : nodo.tipo }}</span>
                </button>
                <button v-if="nodo.filhos" class="campo-item-seta" @click.stop="abrirNavegacao(nodo)"><i class="mdi mdi-chevron-right"></i></button>
              </div>
            </div>
          </div>
          <div class="painel-condicoes">
            <label>Nome da aba</label>
            <input v-model="novaAbaNome" placeholder="Ex: Administradores ativos" class="input-nome" />

            <!-- Seção de Visibilidade -->
            <div class="visibilidade-section">
              <div class="condicoes-header"><i class="mdi mdi-eye-outline"></i><span>Quem pode ver esta aba</span></div>
              <div class="visibilidade-toggle">
                <button class="vis-opt" :class="{ active: visibilidadeTodos }" @click="visibilidadeTodos = true">Todos</button>
                <button class="vis-opt" :class="{ active: !visibilidadeTodos }" @click="visibilidadeTodos = false">Usuários específicos</button>
              </div>
              <div v-if="!visibilidadeTodos" class="usuarios-selector">
                <input v-model="buscaUsuario" placeholder="Buscar usuário..." class="input-busca-usuario" />
                <div class="usuarios-lista">
                  <label v-for="u in usuariosFiltrados" :key="u.Id" class="usuario-check" :class="{ selected: usuariosSelecionados.includes(u.Id) }">
                    <input type="checkbox" :checked="usuariosSelecionados.includes(u.Id)" @change="toggleUsuario(u.Id)" />
                    <span class="usuario-avatar">{{ u.Nome?.[0] ?? '?' }}</span>
                    <span>{{ u.Nome }}</span>
                  </label>
                  <p v-if="usuariosFiltrados.length === 0" class="sem-usuarios">Nenhum usuário encontrado</p>
                </div>
                <p v-if="usuariosSelecionados.length > 0" class="selecionados-count">{{ usuariosSelecionados.length }} selecionado(s)</p>
              </div>
            </div>

            <div class="condicoes-header" style="margin-top:16px"><i class="mdi mdi-filter"></i><span>Condições</span><span v-if="novaAbaFiltros.length" class="badge-count">{{ novaAbaFiltros.length }}</span></div>
            <div v-if="novaAbaFiltros.length === 0" class="condicoes-vazio"><i class="mdi mdi-cursor-default-click-outline"></i><p>Clique num campo ao lado</p></div>
            <div class="condicoes-lista">
              <div v-for="c in novaAbaFiltros" :key="c.id" class="condicao-card">
                <div class="condicao-top"><span class="condicao-label">{{ c.label }}</span><button class="condicao-del" @click="removerCondicao(c.id)"><i class="mdi mdi-close-circle"></i></button></div>
                <div class="condicao-fields">
                  <select v-if="!c.tipoValor.startsWith('enum') && !c.tipoValor.startsWith('fk') && c.tipoValor !== 'boolean'" v-model="c.operador"><option v-for="op in operadores" :key="op.value" :value="op.value">{{ op.label }}</option></select>
                  <input v-if="c.tipoValor === 'string' || c.tipoValor === 'number'" v-model="c.valor" :type="c.tipoValor === 'number' ? 'number' : 'text'" :placeholder="c.tipoValor === 'number' ? 'Número...' : 'Valor...'" />
                  <input v-if="c.tipoValor === 'datetime'" v-model="c.valor" type="date" />
                  <div v-if="c.tipoValor === 'boolean'" class="bool-toggle">
                    <button class="bool-opt" :class="{ active: c.valor === 'true' }" @click="c.valor = 'true'">Sim</button>
                    <button class="bool-opt" :class="{ active: c.valor === 'false' }" @click="c.valor = 'false'">Não</button>
                  </div>
                  <div v-if="c.tipoValor.startsWith('enum')" class="enum-multi">
                    <button v-for="opt in c.tipoValor.split(':')[1]?.split('|') ?? []" :key="opt" class="enum-chip" :class="{ selected: c.valor.split(',').includes(opt) }" @click="toggleEnumValor(c, opt)">{{ opt }}</button>
                  </div>
                  <div v-if="c.tipoValor.startsWith('fk')" class="enum-multi">
                    <span v-if="opcoesFk[c.id] && opcoesFk[c.id]!.length === 0" class="fk-vazio">Nenhum registro encontrado</span>
                    <span v-else-if="!opcoesFk[c.id]" class="fk-loading">Carregando...</span>
                    <button v-for="opt in opcoesFk[c.id] ?? []" :key="opt.id" class="enum-chip" :class="{ selected: (fkSelecionados[c.id] ?? []).includes(opt.id) }" @click="toggleFkValor(c, opt.id)">{{ opt.nome }}</button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button v-if="modoEdicao" class="btn-excluir" @click="excluirAba(abaEditandoId!)"><i class="mdi mdi-delete-outline"></i> Excluir</button>
          <div class="footer-spacer"></div>
          <button class="btn-cancelar" @click="mostrarModal = false">Cancelar</button>
          <button class="btn-criar" :disabled="!novaAbaNome.trim()" @click="salvarAba"><i class="mdi mdi-check"></i> {{ modoEdicao ? 'Salvar' : 'Criar aba' }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.tabs-filtro { margin-bottom: 16px; }
.tabs-bar { display: flex; align-items: center; gap: 4px; border-bottom: 1px solid var(--border); overflow-x: auto; }
.tabs-bar-header { display: flex; align-items: center; gap: 4px; }
.tab { padding: 8px 14px; border: none; background: transparent; color: var(--text-muted); font-size: 13px; font-weight: 500; cursor: pointer; border-bottom: 2px solid transparent; display: flex; align-items: center; gap: 6px; white-space: nowrap; transition: all 0.15s; border-radius: 6px 6px 0 0; }
.tab:hover { color: var(--text-primary); background: var(--bg-elevated); }
.tab.active { color: var(--accent); border-bottom-color: var(--accent); }
.tab-wrapper { position: relative; }
.tab-menu-trigger { opacity: 0; margin-left: 4px; font-size: 12px; transition: opacity 0.15s; padding: 2px; border-radius: 3px; }
.tab:hover .tab-menu-trigger { opacity: 1; }
.tab-menu-trigger:hover { background: var(--bg-elevated); }
.tab-dropdown { position: absolute; top: 100%; left: 0; z-index: 100; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 10px; box-shadow: 0 8px 24px rgba(0,0,0,0.3); padding: 6px; min-width: 160px; animation: fadeIn 0.12s ease; }
.tab-dropdown-title { font-size: 11px; font-weight: 600; color: var(--text-muted); padding: 6px 12px 4px; margin: 0; }
@keyframes fadeIn { from { opacity: 0; transform: translateY(-4px); } to { opacity: 1; transform: translateY(0); } }
.tab-dropdown button { display: flex; align-items: center; gap: 8px; width: 100%; padding: 8px 12px; border: none; background: transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; border-radius: 6px; }
.tab-dropdown button:hover { background: var(--bg-elevated); color: var(--text-primary); }
.tab-dropdown button.del:hover { background: rgba(239,68,68,0.1); color: #ef4444; }
.tabs-actions { margin-left: auto; padding: 0 8px; }
.btn-filtro { display: flex; align-items: center; gap: 6px; padding: 6px 12px; border: 1px solid var(--border); border-radius: 6px; background: var(--bg-surface); color: var(--text-secondary); font-size: 12px; cursor: pointer; }
.btn-filtro:hover { background: var(--bg-elevated); color: var(--text-primary); }
.filtro-rapido { padding: 12px 0; }
.filtro-row { display: flex; align-items: center; gap: 8px; }
.filtro-input { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 6px; padding: 8px 10px; color: var(--text-primary); font-size: 13px; outline: none; flex: 1; }
.filtro-input:focus { border-color: var(--accent); }
.filtro-input::placeholder { color: var(--text-muted); }
.filtro-select { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 6px; padding: 8px 10px; color: var(--text-primary); font-size: 13px; outline: none; }
.btn-acao { width: 32px; height: 32px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); cursor: pointer; display: flex; align-items: center; justify-content: center; }
.btn-ok:hover { background: var(--accent); color: #000; border-color: var(--accent); }
.btn-x:hover { background: rgba(239,68,68,0.1); color: #ef4444; }
.modal-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.7); z-index: 1000; display: flex; align-items: center; justify-content: center; }
.modal { background: var(--bg-primary); border: 1px solid var(--border); border-radius: 16px; width: 820px; max-width: 95vw; max-height: 85vh; display: flex; flex-direction: column; box-shadow: 0 24px 64px rgba(0,0,0,0.5); resize: both; overflow: hidden; min-width: 600px; min-height: 400px; }
.modal-header { display: flex; align-items: center; justify-content: space-between; padding: 18px 24px; border-bottom: 1px solid var(--border); }
.modal-header h3 { font-size: 17px; font-weight: 600; color: var(--text-primary); }
.btn-fechar { background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 20px; }
.btn-fechar:hover { color: var(--text-primary); }
.modal-body { display: flex; flex: 1; overflow: hidden; }
.painel-campos { width: 260px; border-right: 1px solid var(--border); display: flex; flex-direction: column; flex-shrink: 0; }
.painel-campos-header { display: flex; align-items: center; gap: 8px; padding: 12px 14px; border-bottom: 1px solid var(--border); min-height: 44px; }
.btn-voltar-nav { width: 26px; height: 26px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); cursor: pointer; display: flex; align-items: center; justify-content: center; font-size: 14px; }
.btn-voltar-nav:hover { background: var(--bg-elevated); color: var(--text-primary); }
.nav-breadcrumb { font-size: 12px; color: var(--text-muted); display: flex; align-items: center; gap: 2px; overflow: hidden; }
.nav-root { color: var(--accent); font-weight: 600; }
.nav-sep { font-size: 12px; color: var(--text-muted); }
.campos-busca { border: none; border-bottom: 1px solid var(--border); background: transparent; padding: 10px 14px; color: var(--text-primary); font-size: 13px; outline: none; }
.campos-busca::placeholder { color: var(--text-muted); }
.campos-lista { flex: 1; overflow-y: auto; padding: 6px; }
.campo-item { display: flex; align-items: center; gap: 8px; width: 100%; padding: 9px 10px; border: none; background: transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; border-radius: 6px; text-align: left; transition: background 0.1s; flex: 1; }
.campo-item:hover { background: var(--bg-elevated); color: var(--text-primary); }
.campo-item .mdi { font-size: 16px; color: var(--accent); opacity: 0.7; width: 20px; text-align: center; }
.campo-item-nome { flex: 1; }
.campo-item-wrap { display: flex; align-items: center; }
.campo-item-seta { width: 28px; height: 28px; border: none; background: transparent; color: var(--text-muted); cursor: pointer; display: flex; align-items: center; justify-content: center; border-radius: 4px; font-size: 16px; flex-shrink: 0; }
.campo-item-seta:hover { background: var(--accent); color: #000; }
.campo-item-tipo { font-size: 10px; color: var(--text-muted); background: var(--bg-surface); padding: 1px 6px; border-radius: 3px; }
.painel-condicoes { flex: 1; display: flex; flex-direction: column; padding: 20px; overflow-y: auto; }
.painel-condicoes > label { font-size: 11px; font-weight: 600; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 6px; }
.input-nome { width: 100%; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 8px; padding: 10px 14px; color: var(--text-primary); font-size: 14px; outline: none; margin-bottom: 16px; }
.input-nome:focus { border-color: var(--accent); }
.visibilidade-section { margin-bottom: 4px; }
.visibilidade-toggle { display: flex; gap: 0; border: 1px solid var(--border); border-radius: 8px; overflow: hidden; margin-top: 8px; }
.vis-opt { flex: 1; padding: 8px 12px; border: none; background: transparent; color: var(--text-muted); font-size: 13px; cursor: pointer; transition: all 0.15s; }
.vis-opt:first-child { border-right: 1px solid var(--border); }
.vis-opt:hover { color: var(--text-primary); background: var(--bg-elevated); }
.vis-opt.active { background: var(--accent); color: #000; font-weight: 600; }
.usuarios-selector { margin-top: 10px; }
.input-busca-usuario { width: 100%; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 6px; padding: 8px 10px; color: var(--text-primary); font-size: 13px; outline: none; margin-bottom: 8px; }
.input-busca-usuario:focus { border-color: var(--accent); }
.input-busca-usuario::placeholder { color: var(--text-muted); }
.usuarios-lista { max-height: 140px; overflow-y: auto; display: flex; flex-direction: column; gap: 2px; border: 1px solid var(--border); border-radius: 8px; padding: 6px; }
.usuario-check { display: flex; align-items: center; gap: 8px; padding: 6px 8px; border-radius: 6px; cursor: pointer; font-size: 13px; color: var(--text-secondary); transition: background 0.1s; }
.usuario-check:hover { background: var(--bg-elevated); }
.usuario-check.selected { background: rgba(6,182,212,0.08); color: var(--text-primary); }
.usuario-check input[type="checkbox"] { accent-color: var(--accent); width: 14px; height: 14px; }
.usuario-avatar { width: 22px; height: 22px; border-radius: 50%; background: var(--accent); color: #000; display: flex; align-items: center; justify-content: center; font-size: 10px; font-weight: 700; flex-shrink: 0; }
.sem-usuarios { text-align: center; color: var(--text-muted); font-size: 12px; padding: 12px; }
.selecionados-count { font-size: 11px; color: var(--accent); margin-top: 6px; font-weight: 500; }
.condicoes-header { display: flex; align-items: center; gap: 8px; font-size: 13px; font-weight: 600; color: var(--text-secondary); margin-bottom: 12px; }
.badge-count { background: var(--accent); color: #000; font-size: 10px; font-weight: 700; width: 18px; height: 18px; border-radius: 50%; display: flex; align-items: center; justify-content: center; }
.condicoes-vazio { display: flex; flex-direction: column; align-items: center; justify-content: center; flex: 1; color: var(--text-muted); gap: 8px; text-align: center; padding: 40px 20px; }
.condicoes-vazio .mdi { font-size: 32px; opacity: 0.4; }
.condicoes-vazio p { font-size: 13px; }
.condicoes-lista { display: flex; flex-direction: column; gap: 10px; }
.condicao-card { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 12px; }
.condicao-top { display: flex; align-items: center; justify-content: space-between; margin-bottom: 10px; }
.condicao-label { font-size: 13px; font-weight: 500; color: var(--accent); }
.condicao-del { background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 16px; }
.condicao-del:hover { color: #ef4444; }
.condicao-fields { display: flex; gap: 8px; }
.condicao-fields select, .condicao-fields input { flex: 1; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 6px; padding: 8px 10px; color: var(--text-primary); font-size: 13px; outline: none; }
.condicao-fields select:focus, .condicao-fields input:focus { border-color: var(--accent); }
.enum-multi { display: flex; flex-wrap: wrap; gap: 6px; flex: 1; }
.enum-chip { padding: 5px 12px; border: 1px solid var(--border); border-radius: 16px; background: transparent; color: var(--text-secondary); font-size: 12px; cursor: pointer; transition: all 0.15s; }
.enum-chip:hover { border-color: var(--accent); color: var(--text-primary); }
.enum-chip.selected { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.fk-loading { font-size: 12px; color: var(--text-muted); padding: 4px 0; }
.fk-vazio { font-size: 12px; color: var(--text-muted); padding: 8px 0; font-style: italic; }
.bool-toggle { display: flex; gap: 0; border: 1px solid var(--border); border-radius: 8px; overflow: hidden; flex: 1; }
.bool-opt { flex: 1; padding: 8px 12px; border: none; background: transparent; color: var(--text-muted); font-size: 13px; cursor: pointer; transition: all 0.15s; }
.bool-opt:first-child { border-right: 1px solid var(--border); }
.bool-opt:hover { color: var(--text-primary); background: var(--bg-elevated); }
.bool-opt.active { background: var(--accent); color: #000; font-weight: 600; }
.condicao-fields input[type="date"] { flex: 1; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 6px; padding: 8px 10px; color: var(--text-primary); font-size: 13px; outline: none; color-scheme: dark; }
.condicao-fields input[type="date"]:focus { border-color: var(--accent); }
.modal-footer { display: flex; align-items: center; gap: 10px; padding: 14px 24px; border-top: 1px solid var(--border); }
.footer-spacer { flex: 1; }
.btn-excluir { display: flex; align-items: center; gap: 4px; padding: 8px 14px; border: 1px solid rgba(239,68,68,0.3); border-radius: 8px; background: transparent; color: #ef4444; font-size: 13px; cursor: pointer; }
.btn-excluir:hover { background: rgba(239,68,68,0.1); }
.btn-cancelar { padding: 8px 16px; border: 1px solid var(--border); border-radius: 8px; background: transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.btn-cancelar:hover { background: var(--bg-elevated); color: var(--text-primary); }
.btn-criar { display: flex; align-items: center; gap: 6px; padding: 8px 20px; border: none; border-radius: 8px; background: var(--accent); color: #000; font-size: 13px; font-weight: 600; cursor: pointer; }
.btn-criar:hover { background: var(--accent-hover); }
.btn-criar:disabled { opacity: 0.4; cursor: not-allowed; }

/* Vertical mode */
.tabs-filtro.vertical { margin-bottom: 0; }
.tabs-filtro.vertical .tabs-bar { flex-direction: column; border-bottom: none; overflow-x: visible; gap: 2px; align-items: stretch; }
.tabs-filtro.vertical .tabs-bar-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 8px; padding-bottom: 8px; border-bottom: 1px solid var(--border); }
.tabs-filtro.vertical .tabs-bar-header .tab { padding: 6px 0; font-size: 12px; font-weight: 600; color: var(--text-secondary); border: none; }
.tabs-filtro.vertical .tabs-bar-header .tab.active { color: var(--accent); }
.tabs-filtro.vertical .tabs-bar-header .tab-add { padding: 4px 8px; font-size: 14px; border-radius: 4px; }
.tabs-filtro.vertical .tabs-bar-header .tab-add:hover { background: var(--bg-elevated); }
.tabs-filtro.vertical .tab { border-bottom: none; border-radius: 6px; border-left: 3px solid transparent; justify-content: flex-start; padding: 9px 12px; font-size: 13px; white-space: normal; line-height: 1.3; }
.tabs-filtro.vertical .tab:hover { background: var(--bg-elevated); }
.tabs-filtro.vertical .tab.active { border-left-color: var(--accent); border-bottom-color: transparent; background: var(--bg-elevated); color: var(--text-primary); }
.tabs-filtro.vertical .tabs-actions { display: none; }
.tabs-filtro.vertical .tab-wrapper { width: 100%; position: relative; }
.tabs-filtro.vertical .tab-wrapper .tab { width: 100%; }
.tabs-filtro.vertical .tab-menu-trigger { margin-left: auto; }
.tabs-filtro.vertical .tab-dropdown { position: absolute; left: 100%; top: 0; z-index: 200; min-width: 160px; }
.tabs-filtro.vertical .filtro-rapido { display: none; }
</style>
