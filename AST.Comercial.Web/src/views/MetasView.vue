<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import GraficoMeta from '@/components/metas/GraficoMeta.vue'
import FormMeta from '@/components/metas/FormMeta.vue'
import { ApiError, odataRequest, odataTodos } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { useAuthStore } from '@/stores/auth'
import { formatarValor, iniciais, infoMetrica, type ProgressoMeta } from '@/components/metas/tipos'

const toast = useToast()
const auth = useAuthStore()

type Periodo = 'mes' | 'trimestre' | 'ano' | 'custom'
type Escopo = 'minhas' | 'equipe' | 'empresa'

const periodo = ref<Periodo>('mes')
const escopo = ref<Escopo>('minhas')
const hoje = new Date()
const pad = (n: number) => String(n).padStart(2, '0')
const iso = (y: number, m: number, d: number) => `${y}-${pad(m + 1)}-${pad(d)}`
const ultimoDia = (y: number, m: number) => new Date(Date.UTC(y, m + 1, 0)).getUTCDate()
const customIni = ref(iso(hoje.getFullYear(), hoje.getMonth(), 1))
const customFim = ref(iso(hoje.getFullYear(), hoje.getMonth(), ultimoDia(hoje.getFullYear(), hoje.getMonth())))

const intervalo = computed(() => {
  const y = hoje.getFullYear(), m = hoje.getMonth()
  if (periodo.value === 'mes') return { ini: iso(y, m, 1), fim: iso(y, m, ultimoDia(y, m)) }
  if (periodo.value === 'trimestre') {
    const q = Math.floor(m / 3) * 3
    return { ini: iso(y, q, 1), fim: iso(y, q + 2, ultimoDia(y, q + 2)) }
  }
  if (periodo.value === 'ano') return { ini: iso(y, 0, 1), fim: iso(y, 11, 31) }
  return { ini: customIni.value, fim: customFim.value }
})

const metas = ref<ProgressoMeta[]>([])
const carregando = ref(true)
const erro = ref('')
const abertos = ref<Record<number, boolean>>({})

interface Opcao { Id: number; Nome: string }
interface MoedaOpcao { Id: number; Codigo: string; Simbolo?: string | null }
const usuarios = ref<Opcao[]>([])
const equipes = ref<Opcao[]>([])
const funis = ref<Opcao[]>([])
const moedas = ref<MoedaOpcao[]>([])

function avisar(e: unknown, padrao: string) {
  // 403 (sem permissão) e 422 (regra de negócio) chegam com mensagem legível do servidor
  toast.error(e instanceof ApiError ? e.message : padrao)
}

async function carregar() {
  const { ini, fim } = intervalo.value
  if (!ini || !fim || fim < ini) {
    metas.value = []
    erro.value = 'Período inválido: o fim deve ser posterior ao início.'
    carregando.value = false
    return
  }
  carregando.value = true
  erro.value = ''
  const p = new URLSearchParams({ inicio: `${ini}T00:00:00Z`, fim: `${fim}T23:59:59.999Z` })
  if (escopo.value === 'minhas') p.set('usuarioId', String(auth.usuario?.usuarioId ?? 0))
  else p.set('escopo', escopo.value)
  try {
    // odataRequest prefixa /odata/; o ".." leva ao endpoint api/ mantendo o refresh de token do api.ts
    metas.value = await odataRequest<ProgressoMeta[]>('GET', `../api/metas/progresso?${p.toString()}`)
    metas.value.forEach((m, i) => { if (!(m.id in abertos.value)) abertos.value[m.id] = i === 0 })
  } catch (e) {
    metas.value = []
    erro.value = e instanceof ApiError ? e.message : 'Não foi possível carregar as metas.'
    avisar(e, erro.value)
  } finally {
    carregando.value = false
  }
}

async function carregarOpcoes() {
  try {
    const [u, e, f, m] = await Promise.all([
      odataTodos<Opcao>('Usuarios?$select=Id,Nome&$orderby=Nome'),
      odataTodos<Opcao>('Equipes?$select=Id,Nome&$orderby=Nome'),
      odataTodos<Opcao>('Funis?$select=Id,Nome&$orderby=Nome'),
      odataTodos<MoedaOpcao>('Moedas?$orderby=Id'),
    ])
    usuarios.value = u; equipes.value = e; funis.value = f; moedas.value = m
  } catch (e) {
    avisar(e, 'Não foi possível carregar usuários, equipes e funis.')
  }
}

onMounted(() => { carregarOpcoes(); carregar() })
watch([periodo, escopo, customIni, customFim], carregar)

// ---- formulário / exclusão
const formAberto = ref(false)
const editando = ref<ProgressoMeta | null>(null)
function novaMeta() { editando.value = null; formAberto.value = true }
function editar(m: ProgressoMeta) { editando.value = m; formAberto.value = true }

const excluindo = ref<ProgressoMeta | null>(null)
const confirmaAberta = computed({
  get: () => excluindo.value !== null,
  set: (v: boolean) => { if (!v) excluindo.value = null },
})
const apagando = ref(false)
async function excluir() {
  const m = excluindo.value
  if (!m) return
  apagando.value = true
  try {
    await odataRequest('DELETE', `Metas(${m.id})`)
    toast.success('Meta excluída.')
    excluindo.value = null
    await carregar()
  } catch (e) {
    avisar(e, 'Não foi possível excluir a meta.')
  } finally {
    apagando.value = false
  }
}

// ---- apresentação
const rotuloPeriodo = computed(() => {
  const f = (s: string) => s.split('-').reverse().join('/')
  return `${f(intervalo.value.ini)} a ${f(intervalo.value.fim)}`
})
function responsavelRotulo(m: ProgressoMeta): string {
  if (m.escopo === 'usuario') return m.usuarioNome ?? 'Usuário'
  if (m.escopo === 'equipe') return `Equipe ${m.equipeNome ?? ''}`.trim()
  return 'Empresa inteira'
}
function fmt(m: ProgressoMeta, v: number) { return formatarValor(v, m) }
function larguraBarra(m: ProgressoMeta) { return `${Math.min(100, Math.max(0, m.percentual))}%` }
function dataBr(isoStr: string) { return new Date(isoStr).toLocaleDateString('pt-BR', { timeZone: 'UTC' }) }
const periodos: { v: Periodo; r: string }[] = [
  { v: 'mes', r: 'Mês atual' }, { v: 'trimestre', r: 'Trimestre' }, { v: 'ano', r: 'Ano' }, { v: 'custom', r: 'Personalizado' },
]
const escopos: { v: Escopo; r: string }[] = [{ v: 'minhas', r: 'Minhas' }, { v: 'equipe', r: 'Equipe' }, { v: 'empresa', r: 'Empresa' }]
</script>

<template>
  <div class="metas">
    <header class="topo">
      <div>
        <h2>Metas</h2>
        <p class="sub">{{ rotuloPeriodo }}</p>
      </div>
      <button class="b pri" @click="novaMeta"><i class="mdi mdi-plus"></i> Nova meta</button>
    </header>

    <div class="filtros">
      <div class="grupo" role="group" aria-label="Período">
        <button v-for="p in periodos" :key="p.v" :class="{ on: periodo === p.v }" :aria-pressed="periodo === p.v" @click="periodo = p.v">{{ p.r }}</button>
      </div>
      <div v-if="periodo === 'custom'" class="datas">
        <input v-model="customIni" type="date" aria-label="Início do período" />
        <span>até</span>
        <input v-model="customFim" type="date" aria-label="Fim do período" />
      </div>
      <div class="grupo" role="group" aria-label="Escopo">
        <button v-for="e in escopos" :key="e.v" :class="{ on: escopo === e.v }" :aria-pressed="escopo === e.v" @click="escopo = e.v">{{ e.r }}</button>
      </div>
    </div>

    <div v-if="carregando" class="estado" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando metas…</div>
    <div v-else-if="erro" class="estado err" role="alert">
      <i class="mdi mdi-alert-circle-outline"></i> {{ erro }}
      <button class="b sec" @click="carregar">Tentar de novo</button>
    </div>
    <div v-else-if="!metas.length" class="estado">
      <i class="mdi mdi-flag-checkered"></i>
      <p>Nenhuma meta neste período e filtro.</p>
      <button class="b pri" @click="novaMeta">Criar meta</button>
    </div>

    <div v-else class="grade">
      <article v-for="m in metas" :key="m.id" class="card">
        <header class="ch">
          <div class="tt">
            <h3>{{ m.nome }}</h3>
            <div class="tags">
              <UiBadge>{{ infoMetrica(m.metrica).rotulo }}</UiBadge>
              <span class="resp"><span class="av" aria-hidden="true">{{ iniciais(responsavelRotulo(m)) }}</span>{{ responsavelRotulo(m) }}</span>
              <span v-if="m.funilNome" class="resp"><i class="mdi mdi-filter-variant"></i>{{ m.funilNome }}</span>
            </div>
          </div>
          <div class="acoes">
            <button class="ic" :aria-label="`Editar ${m.nome}`" @click="editar(m)"><i class="mdi mdi-pencil-outline"></i></button>
            <button class="ic" :aria-label="`Excluir ${m.nome}`" @click="excluindo = m"><i class="mdi mdi-trash-can-outline"></i></button>
          </div>
        </header>

        <div class="num">
          <strong>{{ m.percentual.toLocaleString('pt-BR', { maximumFractionDigits: 1 }) }}%</strong>
          <span>{{ fmt(m, m.realizado) }} de {{ fmt(m, m.alvo) }}</span>
        </div>
        <div class="barra" role="progressbar" :aria-valuenow="Math.round(m.percentual)" aria-valuemin="0" aria-valuemax="100" :aria-label="`Progresso de ${m.nome}`">
          <div class="preench" :class="{ ok: m.percentual >= 100 }" :style="{ width: larguraBarra(m) }"></div>
        </div>

        <p class="proj" :class="m.projecao === null ? 'neutro' : m.noRitmo ? 'bom' : 'ruim'">
          <template v-if="m.projecao === null">Período ainda não começou ({{ dataBr(m.inicio) }}).</template>
          <template v-else>
            <i class="mdi" :class="m.noRitmo ? 'mdi-trending-up' : 'mdi-trending-down'"></i>
            {{ m.noRitmo ? 'No ritmo' : 'Abaixo do ritmo' }}: projeção de {{ fmt(m, m.projecao) }}
            ({{ (m.percentualProjetado ?? 0).toLocaleString('pt-BR', { maximumFractionDigits: 1 }) }}% do alvo)
          </template>
        </p>
        <p class="per">{{ dataBr(m.inicio) }} a {{ dataBr(m.fim) }} · dia {{ m.diasDecorridos }} de {{ m.diasTotais }}</p>

        <button class="lk" :aria-expanded="!!abertos[m.id]" @click="abertos[m.id] = !abertos[m.id]">
          <i class="mdi" :class="abertos[m.id] ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i> Evolução e ranking
        </button>
        <div v-if="abertos[m.id]" class="det">
          <GraficoMeta :serie="m.serie" :alvo="m.alvo" rotulo-alvo="Ritmo necessário (alvo linear)" :formatar="(v) => fmt(m, v)" />
          <div v-if="m.escopo !== 'usuario'" class="rank">
            <h4>Ranking (top 5)</h4>
            <p v-if="!m.ranking.length" class="vz">Sem resultados no período.</p>
            <ol v-else>
              <li v-for="r in m.ranking" :key="r.usuarioId">
                <span class="av" aria-hidden="true">{{ iniciais(r.nome) }}</span>
                <span class="rn">{{ r.nome }}</span>
                <span class="rv">{{ fmt(m, r.realizado) }}</span>
              </li>
            </ol>
          </div>
        </div>
      </article>
    </div>

    <FormMeta v-model="formAberto" :meta="editando" :usuarios="usuarios" :equipes="equipes" :funis="funis" :moedas="moedas"
      :inicio-padrao="`${intervalo.ini}T00:00:00Z`" :fim-padrao="`${intervalo.fim}T00:00:00Z`" @salvo="carregar" />

    <UiModal v-model="confirmaAberta" titulo="Excluir meta" largura="420px">
      <p class="conf">Excluir a meta <strong>{{ excluindo?.nome }}</strong>? Essa ação remove a meta da lista.</p>
      <template #rodape>
        <button class="b sec" @click="excluindo = null">Cancelar</button>
        <button class="b dang" :disabled="apagando" @click="excluir">{{ apagando ? 'Excluindo…' : 'Excluir' }}</button>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.metas { display: flex; flex-direction: column; gap: 16px; }
.topo { display: flex; justify-content: space-between; align-items: center; gap: 12px; }
.topo h2 { font-size: 20px; color: var(--text-primary); }
.sub { color: var(--text-muted); font-size: 12px; margin-top: 2px; }
.filtros { display: flex; gap: 12px; flex-wrap: wrap; align-items: center; justify-content: space-between; }
.grupo { display: inline-flex; border: 1px solid var(--border); border-radius: 8px; overflow: hidden; }
.grupo button { background: var(--bg-surface); border: 0; color: var(--text-secondary); padding: 7px 14px; font-size: 13px; cursor: pointer; font-family: inherit; }
.grupo button + button { border-left: 1px solid var(--border); }
.grupo button.on { background: var(--accent); color: #04121a; font-weight: 600; }
.grupo button:focus-visible { outline: 2px solid var(--accent); outline-offset: -2px; }
.datas { display: flex; gap: 8px; align-items: center; color: var(--text-secondary); font-size: 12px; }
.datas input { background: var(--bg-secondary); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); padding: 6px 8px; font-family: inherit; }
.b { border-radius: 6px; padding: 8px 14px; font-size: 13px; cursor: pointer; border: 1px solid var(--border); font-family: inherit; display: inline-flex; gap: 6px; align-items: center; }
.b.sec { background: none; color: var(--text-secondary); }
.b.pri { background: var(--accent); color: #04121a; border-color: var(--accent); font-weight: 600; }
.b.pri:hover { background: var(--accent-hover); }
.b.dang { background: var(--danger); border-color: var(--danger); color: #fff; }
.b:disabled { opacity: .6; cursor: default; }
.estado { display: flex; flex-direction: column; align-items: center; gap: 10px; padding: 48px 12px; color: var(--text-secondary); background: var(--bg-surface); border: 1px dashed var(--border); border-radius: 10px; }
.estado .mdi { font-size: 28px; color: var(--text-muted); }
.estado.err, .estado.err .mdi { color: var(--danger); }
.grade { display: grid; grid-template-columns: repeat(auto-fill, minmax(420px, 1fr)); gap: 16px; }
.card { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 16px; display: flex; flex-direction: column; gap: 8px; min-width: 0; }
.ch { display: flex; justify-content: space-between; gap: 8px; }
.tt h3 { font-size: 15px; color: var(--text-primary); overflow-wrap: anywhere; }
.tags { display: flex; gap: 8px; flex-wrap: wrap; align-items: center; margin-top: 6px; }
.resp { display: inline-flex; align-items: center; gap: 5px; font-size: 12px; color: var(--text-secondary); }
.av { display: inline-grid; place-items: center; width: 20px; height: 20px; border-radius: 50%; background: var(--bg-elevated); color: var(--text-primary); font-size: 9px; font-weight: 600; }
.acoes { display: flex; gap: 2px; align-items: flex-start; }
.ic { background: none; border: 0; color: var(--text-secondary); cursor: pointer; font-size: 17px; padding: 4px 6px; border-radius: 6px; }
.ic:hover { background: var(--bg-elevated); color: var(--text-primary); }
.num { display: flex; align-items: baseline; gap: 10px; margin-top: 4px; }
.num strong { font-size: 26px; color: var(--text-primary); }
.num span { font-size: 13px; color: var(--text-secondary); }
.barra { height: 8px; background: var(--bg-elevated); border-radius: 99px; overflow: hidden; }
.preench { height: 100%; background: var(--accent); border-radius: 99px; transition: width .3s ease; }
.preench.ok { background: #22c55e; }
.proj { font-size: 12.5px; display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.proj.bom { color: #22c55e; }
.proj.ruim { color: #f59e0b; }
.proj.neutro { color: var(--text-muted); }
.per { font-size: 11px; color: var(--text-muted); }
.lk { align-self: flex-start; background: none; border: 0; color: var(--accent); cursor: pointer; font-size: 12px; padding: 2px 0; font-family: inherit; }
.det { display: flex; flex-direction: column; gap: 12px; border-top: 1px solid var(--border); padding-top: 10px; }
.rank h4 { font-size: 12px; color: var(--text-secondary); margin-bottom: 6px; }
.rank ol { list-style: none; display: flex; flex-direction: column; gap: 6px; padding: 0; margin: 0; }
.rank li { display: flex; align-items: center; gap: 8px; font-size: 13px; }
.rn { flex: 1; color: var(--text-primary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.rv { color: var(--text-secondary); font-variant-numeric: tabular-nums; }
.vz { font-size: 12px; color: var(--text-muted); }
.conf { font-size: 13px; color: var(--text-secondary); }
@media (max-width: 520px) { .grade { grid-template-columns: 1fr; } }
</style>
