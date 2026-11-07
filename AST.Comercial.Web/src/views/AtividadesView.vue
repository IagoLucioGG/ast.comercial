<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { odataAction, odataTodos, ApiError } from '@/services/api'
import { useToast } from '@/composables/useToast'
import AtividadeForm from '@/components/atividades/AtividadeForm.vue'
import CalendarioAtividades from '@/components/atividades/CalendarioAtividades.vue'
import {
  TIPOS, TIPO_META, normalizar, inicioDoDia, somarDias, formatarDataHora, intervaloCalendario,
  EXPAND_ATIVIDADE, iniciais, nomeCurto, carregarUsuariosAtivos, usuarioLogadoId,
  type Atividade, type ModoCalendario, type TipoAtividade, type UsuarioOpcao,
} from '@/components/atividades/atividades'

const route = useRoute()
const router = useRouter()
const toast = useToast()

type Visao = 'lista' | 'calendario'
const visao = ref<Visao>('lista')
const modoCal = ref<ModoCalendario>('mes')
const dataRef = ref(new Date())

const itens = ref<Atividade[]>([])
const carregando = ref(false)
const erro = ref(false)
let seq = 0

// Filtros
const busca = ref('')
const filtroTipo = ref<TipoAtividade | ''>('')
const filtroVinculo = ref<'' | 'cliente' | 'contato' | 'negocio' | 'nenhum'>('')
const mostrarConcluidas = ref(false)

// Formulário
const formAberto = ref(false)
const editando = ref<Atividade | null>(null)
const dataInicial = ref<Date | null>(null)

const usuarios = ref<UsuarioOpcao[]>([])
const meuId = usuarioLogadoId()
// 'todos' | 'meus' | id do usuário (como texto)
const filtroResp = ref<string>('todos')

/** Condição de responsável para o $filter (ou null para todos). */
function condicaoResp(): string | null {
  if (filtroResp.value === 'todos') return null
  const id = filtroResp.value === 'meus' ? meuId : Number(filtroResp.value)
  return id ? `ResponsavelId eq ${id}` : null
}

function montarFiltro(...partes: (string | null)[]): string {
  return encodeURIComponent(partes.filter(Boolean).join(' and '))
}

async function carregar() {
  const meu = ++seq
  carregando.value = true
  erro.value = false
  try {
    let linhas: Record<string, any>[]
    const resp = condicaoResp()
    if (visao.value === 'lista') {
      const [pend, conc] = await Promise.all([
        odataTodos<Record<string, any>>(`Atividades?$filter=${montarFiltro('Concluida eq false', resp)}&$orderby=DataVencimento,Id&${EXPAND_ATIVIDADE}`),
        odataTodos<Record<string, any>>(`Atividades?$filter=${montarFiltro('Concluida eq true', resp)}&$orderby=ConcluidaEm desc,Id desc&${EXPAND_ATIVIDADE}`, 500),
      ])
      linhas = [...pend, ...conc]
    } else {
      const { inicio, fim } = intervaloCalendario(modoCal.value, dataRef.value)
      const f = `DataVencimento ge ${inicio.toISOString()} and DataVencimento lt ${fim.toISOString()}`
      linhas = await odataTodos<Record<string, any>>(`Atividades?$filter=${montarFiltro(f, resp)}&$orderby=DataVencimento,Id&${EXPAND_ATIVIDADE}`)
    }
    if (meu !== seq) return // resposta antiga descartada
    itens.value = linhas.map(normalizar)
  } catch (e) {
    if (meu !== seq) return
    erro.value = true
    toast.error(e instanceof ApiError ? e.message : 'Não foi possível carregar as atividades.')
  } finally {
    if (meu === seq) carregando.value = false
  }
}

watch([visao, modoCal, dataRef, filtroResp], carregar)

onMounted(() => {
  carregarUsuariosAtivos().then((u) => { usuarios.value = u }).catch(() => { /* filtro por usuário fica só com Meus/Todos */ })
  carregar()
  tratarNovo()
})

watch(() => route.query.novo, tratarNovo)

function tratarNovo() {
  if (route.query.novo !== '1') return
  abrirNovo(null)
  const { novo: _n, ...resto } = route.query
  router.replace({ query: resto })
}

// --- Filtros aplicados no cliente ---
const filtrados = computed(() => {
  const q = busca.value.trim().toLowerCase()
  return itens.value.filter((a) => {
    if (q && !(`${a.Titulo} ${a.ClienteNome ?? ''} ${a.ContatoNome ?? ''} ${a.NegocioTitulo ?? ''}`.toLowerCase().includes(q))) return false
    if (filtroTipo.value && a.Tipo !== filtroTipo.value) return false
    if (filtroVinculo.value === 'cliente' && !a.ClienteId) return false
    if (filtroVinculo.value === 'contato' && !a.PessoaContatoId) return false
    if (filtroVinculo.value === 'negocio' && !a.NegocioId) return false
    if (filtroVinculo.value === 'nenhum' && (a.ClienteId || a.PessoaContatoId || a.NegocioId)) return false
    return true
  })
})

const secoes = computed(() => {
  const hoje0 = inicioDoDia(new Date()).getTime()
  const amanha0 = somarDias(inicioDoDia(new Date()), 1).getTime()
  const atrasadas: Atividade[] = [], hoje: Atividade[] = [], proximas: Atividade[] = [], concluidas: Atividade[] = []
  for (const a of filtrados.value) {
    if (a.Concluida) { concluidas.push(a); continue }
    const t = a.DataVencimento?.getTime()
    if (t === undefined) proximas.push(a)
    else if (t < hoje0) atrasadas.push(a)
    else if (t < amanha0) hoje.push(a)
    else proximas.push(a)
  }
  return [
    { chave: 'atrasadas', titulo: 'Atrasadas', icone: 'mdi-alert-circle-outline', cor: 'var(--danger)', itens: atrasadas },
    { chave: 'hoje', titulo: 'Hoje', icone: 'mdi-calendar-today', cor: 'var(--accent)', itens: hoje },
    { chave: 'proximas', titulo: 'Próximas', icone: 'mdi-calendar-arrow-right', cor: '#3b82f6', itens: proximas },
    { chave: 'concluidas', titulo: 'Concluídas', icone: 'mdi-check-circle-outline', cor: '#22c55e', itens: concluidas },
  ]
})

const nadaNaLista = computed(() => !carregando.value && !erro.value && filtrados.value.length === 0)

// --- Ações ---
function abrirNovo(data: Date | null) {
  editando.value = null
  dataInicial.value = data
  formAberto.value = true
}

function abrirEdicao(a: Atividade) {
  editando.value = a
  dataInicial.value = null
  formAberto.value = true
}

const concluindo = ref<number | null>(null)
async function concluir(a: Atividade) {
  if (a.Concluida || concluindo.value) return
  concluindo.value = a.Id
  try {
    await odataAction('Atividades', a.Id, 'Concluir')
    a.Concluida = true
    a.ConcluidaEm = new Date()
    toast.success('Atividade concluída.')
  } catch (e) {
    toast.error(e instanceof ApiError ? e.message : 'Não foi possível concluir.')
  } finally {
    concluindo.value = null
  }
}

function aoSalvar() { carregar() }
function aoExcluir(id: number) { itens.value = itens.value.filter((a) => a.Id !== id) }

// --- Navegação do calendário ---
function navegar(dir: -1 | 1) {
  const d = new Date(dataRef.value)
  if (modoCal.value === 'mes') { d.setDate(1); d.setMonth(d.getMonth() + dir) }
  else d.setDate(d.getDate() + dir * 7)
  dataRef.value = d
}
function irHoje() { dataRef.value = new Date() }

const tituloPeriodo = computed(() => {
  if (modoCal.value === 'mes') {
    const t = dataRef.value.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
    return t.charAt(0).toUpperCase() + t.slice(1)
  }
  const { dias } = intervaloCalendario('semana', dataRef.value)
  const a = dias[0]!, b = dias[6]!
  const fmt = (d: Date) => d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' })
  return `${fmt(a)} – ${fmt(b)} ${b.getFullYear()}`
})

function vinculoTexto(a: Atividade): string {
  if (a.NegocioId) return a.NegocioTitulo ?? `Negócio #${a.NegocioId}`
  if (a.PessoaContatoId) return a.ContatoNome ?? `Contato #${a.PessoaContatoId}`
  if (a.ClienteId) return a.ClienteNome ?? `Cliente #${a.ClienteId}`
  return ''
}
function vinculoIcone(a: Atividade): string {
  if (a.NegocioId) return 'mdi-handshake-outline'
  if (a.PessoaContatoId) return 'mdi-account-outline'
  return 'mdi-domain'
}
function irVinculo(a: Atividade) {
  if (a.NegocioId) router.push(`/negocios/${a.NegocioId}`)
  else if (a.ClienteId) router.push(`/clientes/${a.ClienteId}`)
  else if (a.PessoaContatoId) router.push('/contatos')
}
</script>

<template>
  <div class="ativ">
    <header class="topo">
      <h2>Atividades</h2>
      <div class="alterna" role="tablist" aria-label="Visualização">
        <button role="tab" :aria-selected="visao === 'lista'" :class="{ ativo: visao === 'lista' }" @click="visao = 'lista'">
          <i class="mdi mdi-format-list-bulleted"></i> Lista
        </button>
        <button role="tab" :aria-selected="visao === 'calendario'" :class="{ ativo: visao === 'calendario' }" @click="visao = 'calendario'">
          <i class="mdi mdi-calendar-month-outline"></i> Calendário
        </button>
      </div>
      <span class="espaco"></span>
      <button class="btn-primario" @click="abrirNovo(null)"><i class="mdi mdi-plus"></i> Nova atividade</button>
    </header>

    <div class="filtros">
      <div class="busca">
        <i class="mdi mdi-magnify"></i>
        <input v-model="busca" placeholder="Buscar atividade ou vínculo…" aria-label="Buscar atividades" />
      </div>
      <select v-model="filtroResp" aria-label="Filtrar por responsável">
        <option value="todos">Todos</option>
        <option v-if="meuId" value="meus">Meus</option>
        <option v-for="u in usuarios" :key="u.Id" :value="String(u.Id)">{{ nomeCurto(u.Nome) }}</option>
      </select>
      <select v-model="filtroTipo" aria-label="Filtrar por tipo">
        <option value="">Todos os tipos</option>
        <option v-for="t in TIPOS" :key="t" :value="t">{{ TIPO_META[t].rotulo }}</option>
      </select>
      <select v-model="filtroVinculo" aria-label="Filtrar por vínculo">
        <option value="">Qualquer vínculo</option>
        <option value="cliente">Com cliente</option>
        <option value="contato">Com contato</option>
        <option value="negocio">Com negócio</option>
        <option value="nenhum">Sem vínculo</option>
      </select>

      <template v-if="visao === 'calendario'">
        <span class="espaco"></span>
        <div class="nav-cal">
          <button class="btn-icon" aria-label="Período anterior" @click="navegar(-1)"><i class="mdi mdi-chevron-left"></i></button>
          <button class="btn-borda" @click="irHoje">Hoje</button>
          <button class="btn-icon" aria-label="Próximo período" @click="navegar(1)"><i class="mdi mdi-chevron-right"></i></button>
          <strong class="periodo">{{ tituloPeriodo }}</strong>
        </div>
        <div class="alterna pequeno">
          <button :class="{ ativo: modoCal === 'mes' }" @click="modoCal = 'mes'">Mês</button>
          <button :class="{ ativo: modoCal === 'semana' }" @click="modoCal = 'semana'">Semana</button>
        </div>
      </template>
    </div>

    <div v-if="erro" class="estado">
      <i class="mdi mdi-alert-outline"></i>
      <p>Não foi possível carregar as atividades.</p>
      <button class="btn-borda" @click="carregar">Tentar de novo</button>
    </div>

    <div v-else-if="carregando && !itens.length" class="estado"><i class="mdi mdi-loading mdi-spin"></i><p>Carregando…</p></div>

    <!-- Lista -->
    <div v-else-if="visao === 'lista'" class="lista">
      <div v-if="nadaNaLista" class="estado">
        <i class="mdi mdi-check-all"></i>
        <p>Nenhuma atividade encontrada.</p>
        <button class="btn-borda" @click="abrirNovo(null)">Criar atividade</button>
      </div>
      <template v-else>
        <section v-for="s in secoes" :key="s.chave" class="secao">
          <button class="secao-cab" :disabled="s.chave !== 'concluidas'" @click="s.chave === 'concluidas' && (mostrarConcluidas = !mostrarConcluidas)">
            <i class="mdi" :class="s.icone" :style="{ color: s.cor }"></i>
            <span class="secao-tit">{{ s.titulo }}</span>
            <span class="contador" :style="s.itens.length && s.chave === 'atrasadas' ? { background: 'var(--danger)', color: '#fff' } : {}">{{ s.itens.length }}</span>
            <i v-if="s.chave === 'concluidas'" class="mdi" :class="mostrarConcluidas ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i>
          </button>
          <template v-if="s.chave !== 'concluidas' || mostrarConcluidas">
            <p v-if="!s.itens.length" class="vazio-secao">Nada por aqui.</p>
            <ul v-else class="itens">
              <li v-for="a in s.itens" :key="a.Id" class="item" :class="{ feita: a.Concluida }">
                <button class="check" :disabled="a.Concluida || concluindo === a.Id"
                  :aria-label="a.Concluida ? 'Concluída' : `Concluir ${a.Titulo}`" @click="concluir(a)">
                  <i class="mdi" :class="a.Concluida ? 'mdi-check-circle' : 'mdi-circle-outline'"></i>
                </button>
                <span class="icone-tipo" :style="{ color: TIPO_META[a.Tipo].cor }" :title="TIPO_META[a.Tipo].rotulo">
                  <i class="mdi" :class="TIPO_META[a.Tipo].icone"></i>
                </span>
                <button class="item-corpo" @click="abrirEdicao(a)">
                  <span class="item-tit">{{ a.Titulo }}</span>
                  <span v-if="a.DataVencimento" class="item-data" :class="{ atras: s.chave === 'atrasadas' }">
                    {{ formatarDataHora(a.DataVencimento) }}
                  </span>
                  <span v-else class="item-data">Sem data</span>
                </button>
                <span v-if="a.ResponsavelNome" class="resp" :title="'Responsável: ' + nomeCurto(a.ResponsavelNome)"
                  role="img" :aria-label="'Responsável: ' + nomeCurto(a.ResponsavelNome)">{{ iniciais(a.ResponsavelNome) }}</span>
                <button v-if="vinculoTexto(a)" class="vinculo" :title="'Abrir ' + vinculoTexto(a)" @click="irVinculo(a)">
                  <i class="mdi" :class="vinculoIcone(a)"></i> {{ vinculoTexto(a) }}
                </button>
              </li>
            </ul>
          </template>
        </section>
      </template>
    </div>

    <!-- Calendário -->
    <CalendarioAtividades v-else :modo="modoCal" :data-ref="dataRef" :itens="filtrados"
      @abrir="abrirEdicao" @novo="abrirNovo" />

    <AtividadeForm v-model="formAberto" :atividade="editando" :data-inicial="dataInicial"
      @salvo="aoSalvar" @excluido="aoExcluir" />
  </div>
</template>

<style scoped>
.ativ { display: flex; flex-direction: column; gap: 14px; }
.topo { display: flex; align-items: center; gap: 16px; }
.topo h2 { font-size: 20px; color: var(--text-primary); }
.espaco { flex: 1; }
.alterna { display: inline-flex; border: 1px solid var(--border); border-radius: 8px; overflow: hidden; background: var(--bg-surface); }
.alterna button { border: 0; background: none; color: var(--text-secondary); padding: 7px 14px; font-size: 13px; cursor: pointer; display: inline-flex; gap: 6px; align-items: center; }
.alterna button.ativo { background: var(--accent); color: #000; font-weight: 600; }
.alterna.pequeno button { padding: 6px 12px; }
.btn-primario { background: var(--accent); color: #000; border: 0; border-radius: 8px; padding: 8px 16px; font-size: 13px; font-weight: 600; cursor: pointer; display: inline-flex; align-items: center; gap: 5px; }
.btn-primario:hover { background: var(--accent-hover); }
.btn-borda { background: var(--bg-surface); border: 1px solid var(--border); color: var(--text-primary); border-radius: 8px; padding: 7px 12px; font-size: 13px; cursor: pointer; }
.btn-borda:hover { background: var(--bg-elevated); }
.btn-icon { width: 32px; height: 32px; border-radius: 8px; border: 0; background: transparent; color: var(--text-secondary); cursor: pointer; display: inline-flex; align-items: center; justify-content: center; font-size: 20px; }
.btn-icon:hover { background: var(--bg-elevated); color: var(--text-primary); }

.filtros { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.busca { display: flex; align-items: center; gap: 8px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 8px; padding: 6px 12px; width: 280px; }
.busca .mdi { color: var(--text-muted); }
.busca input { border: 0; background: transparent; color: var(--text-primary); font-size: 13px; outline: none; width: 100%; }
.filtros select { background: var(--bg-surface); border: 1px solid var(--border); color: var(--text-primary); border-radius: 8px; padding: 7px 10px; font-size: 13px; }
.nav-cal { display: flex; align-items: center; gap: 4px; }
.periodo { margin-left: 8px; font-size: 14px; color: var(--text-primary); min-width: 150px; }

.estado { display: flex; flex-direction: column; align-items: center; gap: 10px; padding: 48px 0; color: var(--text-secondary); }
.estado .mdi { font-size: 36px; color: var(--text-muted); }

.lista { display: flex; flex-direction: column; gap: 14px; }
.secao { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; overflow: hidden; }
.secao-cab { display: flex; align-items: center; gap: 8px; width: 100%; padding: 10px 14px; border: 0; background: var(--bg-secondary); color: var(--text-primary); font-size: 13px; font-weight: 600; text-align: left; cursor: default; }
.secao-cab:not(:disabled) { cursor: pointer; }
.secao-tit { flex: 0 1 auto; }
.contador { background: var(--bg-elevated); color: var(--text-secondary); border-radius: 10px; padding: 1px 8px; font-size: 11px; }
.secao-cab > .mdi:last-child:not(:first-child) { margin-left: auto; }
.vazio-secao { padding: 12px 14px; font-size: 13px; color: var(--text-muted); }
.itens { list-style: none; margin: 0; padding: 0; }
.item { display: flex; align-items: center; gap: 10px; padding: 8px 14px; border-top: 1px solid var(--border); }
.item:hover { background: var(--bg-elevated); }
.check { border: 0; background: none; cursor: pointer; font-size: 22px; color: var(--text-muted); display: flex; padding: 0; }
.check:hover:not(:disabled) { color: #22c55e; }
.item.feita .check { color: #22c55e; cursor: default; }
.icone-tipo { font-size: 18px; display: flex; }
.item-corpo { flex: 1; min-width: 0; border: 0; background: none; text-align: left; cursor: pointer; display: flex; flex-direction: column; gap: 2px; color: var(--text-primary); }
.item-tit { font-size: 13px; font-weight: 500; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.item.feita .item-tit { text-decoration: line-through; color: var(--text-muted); }
.item-data { font-size: 11px; color: var(--text-muted); }
.item-data.atras { color: var(--danger); font-weight: 600; }
.resp { width: 26px; height: 26px; border-radius: 50%; background: var(--bg-elevated); border: 1px solid var(--border); color: var(--text-secondary); font-size: 11px; font-weight: 700; display: inline-flex; align-items: center; justify-content: center; flex-shrink: 0; }
.vinculo { border: 1px solid var(--border); background: var(--bg-primary); color: var(--text-secondary); border-radius: 14px; padding: 3px 10px; font-size: 12px; cursor: pointer; display: inline-flex; gap: 4px; align-items: center; max-width: 220px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.vinculo:hover { color: var(--accent); border-color: var(--accent); }
</style>
