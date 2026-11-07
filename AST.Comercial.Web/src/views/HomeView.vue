<script setup lang="ts">
// Resumo comercial: indicadores, funil resumido, atividades e negócios recentes.
// Cada bloco carrega de forma independente (erro em um não derruba os outros).
import { computed, onMounted, ref, watch } from 'vue'
import { useToast } from '@/composables/useToast'
import { useAuthStore } from '@/stores/auth'
import CartaoIndicador from '@/components/dashboard/CartaoIndicador.vue'
import ValoresMoeda from '@/components/dashboard/ValoresMoeda.vue'
import FunilResumo from '@/components/dashboard/FunilResumo.vue'
import MinhasAtividades from '@/components/dashboard/MinhasAtividades.vue'
import NegociosRecentes from '@/components/dashboard/NegociosRecentes.vue'
import {
  PERIODOS, carregarApoio, carregarBloco, concluirAtividade, criarBloco, intervaloDoPeriodo,
  atividadesPendentes, contagemAtividades, negociosAbertos, negociosGanhos, negociosPerdidos, negociosRecentes,
  propostasAguardando, resumoFunil, somaQtd, totalNegocios,
  type Apoio, type AtividadeItem, type Escopo, type EtapaResumo, type NegocioRecente, type PeriodoChave, type TotalMoeda,
} from '@/components/dashboard/dashboardDados'

const toast = useToast()
const auth = useAuthStore()
const usuarioId = computed(() => auth.usuario?.usuarioId ?? null)
const escopoAtiv = ref<Escopo>('minhas')
const escopoNeg = ref<Escopo>('minhas')
const periodo = ref<PeriodoChave>('mes')
const funilId = ref<number | null>(null)
const ativRef = ref<InstanceType<typeof MinhasAtividades> | null>(null)

const apoio = criarBloco<Apoio>({ moedas: [], status: [], funis: [], etapas: [] })
const total = criarBloco<number | null>(null)
const abertos = criarBloco<TotalMoeda[]>([])
const ganhos = criarBloco<TotalMoeda[]>([])
const perdidos = criarBloco<TotalMoeda[]>([])
const atividades = criarBloco<AtividadeItem[]>([])
const contagem = criarBloco<{ hoje: number; atrasadas: number }>({ hoje: 0, atrasadas: 0 })
const propostas = criarBloco<TotalMoeda[]>([])
const funil = criarBloco<EtapaResumo[]>([])
const recentes = criarBloco<NegocioRecente[]>([])

const moedas = computed(() => apoio.dados.value.moedas)
const status = computed(() => apoio.dados.value.status)
const resumoAtiv = computed(() => contagem.dados.value)
const conversao = computed(() => {
  const g = somaQtd(ganhos.dados.value)
  const p = somaQtd(perdidos.dados.value)
  return g + p === 0 ? null : Math.round((g / (g + p)) * 100)
})
const periodoRotulo = computed(() => PERIODOS.find((p) => p.chave === periodo.value)?.rotulo ?? '')
const semDados = computed(() => total.dados.value === 0 && !total.erro.value && !total.carregando.value)

/** Executa um bloco que depende dos cadastros de apoio; se eles falharam, repete o erro. */
function comApoio<T>(b: ReturnType<typeof criarBloco<T>>, fn: () => Promise<T>) {
  if (apoio.erro.value) { b.carregando.value = false; b.erro.value = apoio.erro.value; return Promise.resolve() }
  return carregarBloco(b, fn)
}

const carregarAbertos = () => comApoio(abertos, () => negociosAbertos(status.value))
const carregarFechados = () => {
  const p = intervaloDoPeriodo(periodo.value)
  return Promise.all([
    comApoio(ganhos, () => negociosGanhos(status.value, p)),
    comApoio(perdidos, () => negociosPerdidos(status.value, p)),
  ])
}
const carregarAtividades = () => Promise.all([
  carregarBloco(atividades, () => atividadesPendentes(escopoAtiv.value, usuarioId.value)),
  carregarBloco(contagem, () => contagemAtividades(escopoAtiv.value, usuarioId.value)),
])
const carregarPropostas = () => carregarBloco(propostas, propostasAguardando)
const carregarRecentes = () => carregarBloco(recentes, () => negociosRecentes(escopoNeg.value, usuarioId.value))
const carregarTotal = () => carregarBloco(total, totalNegocios)
const carregarFunil = () => {
  if (funilId.value === null) { funil.carregando.value = false; funil.dados.value = []; return Promise.resolve() }
  const id = funilId.value
  return comApoio(funil, () => resumoFunil(id, apoio.dados.value.etapas, status.value))
}

async function iniciar() {
  await carregarBloco(apoio, carregarApoio)
  if (!funilId.value) funilId.value = apoio.dados.value.funis[0]?.id ?? null
  await Promise.all([
    carregarTotal(), carregarAbertos(), carregarFechados(), carregarAtividades(),
    carregarPropostas(), carregarFunil(), carregarRecentes(),
  ])
}

async function concluir(id: number) {
  try {
    await concluirAtividade(id)
    toast.success('Atividade concluída')
    await carregarAtividades()
    ativRef.value?.liberar()
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível concluir a atividade')
    ativRef.value?.liberar()
  }
}

function trocarEscopoAtiv(e: Escopo) { escopoAtiv.value = e; carregarAtividades() }
function trocarEscopoNeg(e: Escopo) { escopoNeg.value = e; carregarRecentes() }
function trocarFunil(id: number) { funilId.value = id; carregarFunil() }

watch(periodo, carregarFechados)
onMounted(iniciar)
</script>

<template>
  <div class="resumo">
    <header class="topo">
      <h1>Resumo</h1>
      <label class="periodo">
        <span>Período</span>
        <select v-model="periodo" aria-label="Período dos indicadores">
          <option v-for="p in PERIODOS" :key="p.chave" :value="p.chave">{{ p.rotulo }}</option>
        </select>
      </label>
    </header>

    <div v-if="semDados" class="vazio" role="status">
      <i class="mdi mdi-rocket-launch-outline"></i>
      <div>
        <strong>Sem dados ainda</strong>
        <p>Crie o primeiro negócio para começar a acompanhar seus indicadores.</p>
      </div>
      <router-link to="/negocios" class="cta"><i class="mdi mdi-plus"></i> Criar primeiro negócio</router-link>
    </div>

    <section class="cartoes" aria-label="Indicadores">
      <CartaoIndicador titulo="Negócios em aberto" icone="mdi-briefcase-outline" to="/negocios"
        :carregando="abertos.carregando.value" :erro="abertos.erro.value" @tentar="carregarAbertos">
        <div class="num">{{ somaQtd(abertos.dados.value) }}</div>
        <ValoresMoeda :totais="abertos.dados.value" :moedas="moedas" />
      </CartaoIndicador>

      <CartaoIndicador :titulo="`Ganhos · ${periodoRotulo}`" icone="mdi-trophy-outline" to="/negocios"
        :carregando="ganhos.carregando.value" :erro="ganhos.erro.value" @tentar="carregarFechados">
        <div class="num">{{ somaQtd(ganhos.dados.value) }}</div>
        <ValoresMoeda :totais="ganhos.dados.value" :moedas="moedas" />
      </CartaoIndicador>

      <CartaoIndicador :titulo="`Conversão · ${periodoRotulo}`" icone="mdi-percent-outline" to="/negocios"
        :carregando="ganhos.carregando.value || perdidos.carregando.value"
        :erro="ganhos.erro.value || perdidos.erro.value" @tentar="carregarFechados">
        <div class="num">{{ conversao === null ? '—' : `${conversao}%` }}</div>
        <div class="sub">{{ somaQtd(ganhos.dados.value) }} ganhos · {{ somaQtd(perdidos.dados.value) }} perdidos</div>
      </CartaoIndicador>

      <CartaoIndicador :titulo="`Atividades · ${escopoAtiv === 'minhas' ? 'minhas' : 'todas'}`" icone="mdi-calendar-check-outline" to="/atividades"
        :carregando="contagem.carregando.value" :erro="contagem.erro.value" @tentar="carregarAtividades">
        <div class="num">{{ resumoAtiv.hoje }} <small>hoje</small></div>
        <div :class="['sub', { alerta: resumoAtiv.atrasadas > 0 }]">{{ resumoAtiv.atrasadas }} atrasadas</div>
      </CartaoIndicador>

      <CartaoIndicador titulo="Propostas aguardando" icone="mdi-file-document-outline" to="/propostas"
        :carregando="propostas.carregando.value" :erro="propostas.erro.value" @tentar="carregarPropostas">
        <div class="num">{{ somaQtd(propostas.dados.value) }}</div>
        <ValoresMoeda :totais="propostas.dados.value" :moedas="moedas" />
      </CartaoIndicador>
    </section>

    <div class="grade">
      <FunilResumo :funis="apoio.dados.value.funis" :funil-id="funilId" :etapas="funil.dados.value" :moedas="moedas"
        :carregando="funil.carregando.value || apoio.carregando.value" :erro="funil.erro.value"
        @trocar="trocarFunil" @tentar="carregarFunil" />
      <MinhasAtividades ref="ativRef" :escopo="escopoAtiv" :itens="atividades.dados.value" :carregando="atividades.carregando.value"
        :erro="atividades.erro.value" @concluir="concluir" @tentar="carregarAtividades" @escopo="trocarEscopoAtiv" />
      <NegociosRecentes class="largo" :itens="recentes.dados.value" :etapas="apoio.dados.value.etapas" :status="status"
        :moedas="moedas" :escopo="escopoNeg" :carregando="recentes.carregando.value" :erro="recentes.erro.value"
        @tentar="carregarRecentes" @escopo="trocarEscopoNeg" />
    </div>
  </div>
</template>

<style scoped>
.resumo { padding: 20px 24px 32px; display: flex; flex-direction: column; gap: 18px; color: var(--text-primary); min-height: 100%; background: var(--bg-primary); }
.topo { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
h1 { margin: 0; font-size: 22px; font-weight: 600; }
.periodo { display: flex; align-items: center; gap: 8px; font-size: 13px; color: var(--text-secondary); }
.periodo select { background: var(--bg-surface); color: var(--text-primary); border: 1px solid var(--border); border-radius: 6px; padding: 6px 10px; font-size: 13px; }
.cartoes { display: grid; grid-template-columns: repeat(auto-fit, minmax(190px, 1fr)); gap: 12px; }
.num { font-size: 26px; font-weight: 700; line-height: 1.2; }
.num small { font-size: 13px; font-weight: 400; color: var(--text-muted); }
.sub { font-size: 13px; color: var(--text-secondary); }
.sub.alerta { color: var(--danger); font-weight: 600; }
.grade { display: grid; grid-template-columns: repeat(2, minmax(0, 1fr)); gap: 12px; }
.largo { grid-column: 1 / -1; }
.vazio { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; padding: 14px 16px; background: var(--bg-surface); border: 1px dashed var(--border); border-radius: 10px; }
.vazio > .mdi { font-size: 28px; color: var(--accent); }
.vazio > div { flex: 1; min-width: 200px; }
.vazio p { margin: 2px 0 0; font-size: 13px; color: var(--text-secondary); }
.cta { display: inline-flex; align-items: center; gap: 6px; background: var(--accent); color: #fff; text-decoration: none; padding: 8px 14px; border-radius: 6px; font-size: 13px; font-weight: 600; }
.cta:hover, .cta:focus-visible { background: var(--accent-hover); outline: none; }
@media (max-width: 860px) { .grade { grid-template-columns: minmax(0, 1fr); } .resumo { padding: 14px; } }
</style>
