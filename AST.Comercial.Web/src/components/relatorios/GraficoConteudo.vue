<script setup lang="ts">
// Busca os dados de um gráfico e mostra os estados de carregando, vazio e erro.
import { computed, onMounted, ref, watch } from 'vue'
import GraficoRenderer from './GraficoRenderer.vue'
import { relatoriosApi, type Catalogo, type DadosGrafico, type DefinicaoGrafico, type TipoGrafico } from './relatoriosApi'

const props = defineProps<{
  definicao: DefinicaoGrafico
  tipo: TipoGrafico
  periodoPainel?: string | null
  catalogo?: Catalogo | null
  grande?: boolean
  /** Muda para forçar nova consulta. */
  versao?: number
}>()

const dados = ref<DadosGrafico | null>(null)
const carregando = ref(false)
const erro = ref('')
let sequencia = 0

async function carregar() {
  const minha = ++sequencia
  carregando.value = true
  erro.value = ''
  try {
    const d = props.definicao
    const r = await relatoriosApi.dados({
      entidade: d.entidade, agregacao: d.agregacao, campoValor: d.campoValor || null,
      campoAgrupamento: d.campoAgrupamento || null, filtros: d.filtros ?? [],
      periodoCampo: d.periodoCampo || null, periodoRelativo: d.periodoRelativo || null,
      limite: d.limite || null, tipo: props.tipo, periodoPainel: props.periodoPainel || null,
    })
    if (minha !== sequencia) return
    dados.value = r
  } catch (e) {
    if (minha !== sequencia) return
    dados.value = null
    erro.value = e instanceof Error ? e.message : 'Não foi possível carregar o gráfico.'
  } finally {
    if (minha === sequencia) carregando.value = false
  }
}

const chave = computed(() => JSON.stringify([props.definicao, props.tipo, props.periodoPainel, props.versao]))
watch(chave, carregar)
onMounted(carregar)

const vazio = computed(() => !!dados.value && dados.value.series.every((s) => s.quantidade === 0))
const entidade = computed(() => props.catalogo?.entidades.find((e) => e.chave === props.definicao.entidade))
const rotuloCampo = computed(() => entidade.value?.camposValor.find((c) => c.chave === props.definicao.campoValor)?.rotulo)
const rotuloAgrupamento = computed(() => entidade.value?.camposAgrupamento.find((c) => c.chave === props.definicao.campoAgrupamento)?.rotulo)
</script>

<template>
  <div class="gc" :class="{ grande }">
    <div v-if="carregando && !dados" class="gc-estado" role="status" aria-live="polite">
      <i class="mdi mdi-loading mdi-spin"></i> Carregando…
    </div>
    <div v-else-if="erro" class="gc-estado erro" role="alert">
      <i class="mdi mdi-alert-circle-outline"></i>
      <span>{{ erro }}</span>
      <button class="gc-retry" @click="carregar">Tentar de novo</button>
    </div>
    <div v-else-if="vazio" class="gc-estado">
      <i class="mdi mdi-chart-box-outline"></i> Sem dados para este filtro e período.
    </div>
    <div v-else-if="dados" class="gc-corpo" :class="{ atualizando: carregando }">
      <GraficoRenderer :dados="dados" :tipo="tipo" :rotulo-campo="rotuloCampo" :rotulo-agrupamento="rotuloAgrupamento" :grande="grande" />
    </div>
  </div>
</template>

<style scoped>
.gc { min-height: 110px; display: flex; flex-direction: column; justify-content: center; }
.gc.grande { min-height: 300px; }
.gc-estado { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px; padding: 22px 10px; text-align: center; color: var(--text-muted); font-size: 12px; }
.gc-estado i { font-size: 24px; }
.gc-estado.erro { color: var(--danger); }
.gc-estado.erro span { color: var(--text-secondary); max-width: 320px; }
.gc-retry { margin-top: 4px; padding: 5px 12px; border: 1px solid var(--border); border-radius: 6px; background: var(--bg-surface); color: var(--text-primary); font-size: 12px; cursor: pointer; }
.gc-retry:hover { border-color: var(--accent); color: var(--accent); }
.gc-corpo { transition: opacity .15s; }
.gc-corpo.atualizando { opacity: .55; }
</style>
