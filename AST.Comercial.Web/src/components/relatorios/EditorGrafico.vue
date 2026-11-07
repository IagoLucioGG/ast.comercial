<script setup lang="ts">
// Editor de gráfico em painel lateral, com pré-visualização ao vivo.
import { computed, reactive, ref, watch } from 'vue'
import UiSlideOver from '@/components/ui/UiSlideOver.vue'
import GraficoConteudo from './GraficoConteudo.vue'
import { useToast } from '@/composables/useToast'
import {
  relatoriosApi,
  type Agregacao, type Catalogo, type CampoCatalogo, type DefinicaoGrafico, type FiltroRel, type Grafico,
  type GraficoEntrada, type OpcaoRel, type TipoGrafico,
} from './relatoriosApi'

const props = defineProps<{
  modelValue: boolean
  catalogo: Catalogo | null
  painelId: number
  grafico: Grafico | null
  periodoPainel: string | null
}>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean]; salvo: [g: Grafico] }>()
const toast = useToast()

const tipos: { valor: TipoGrafico; rotulo: string; icone: string }[] = [
  { valor: 'Numero', rotulo: 'Número', icone: 'numeric' },
  { valor: 'Barras', rotulo: 'Barras', icone: 'chart-bar' },
  { valor: 'Linhas', rotulo: 'Linhas', icone: 'chart-line' },
  { valor: 'Pizza', rotulo: 'Pizza', icone: 'chart-donut' },
  { valor: 'Tabela', rotulo: 'Tabela', icone: 'table' },
]
const nomeAgregacao: Record<string, string> = { Contagem: 'Contagem de registros', Soma: 'Soma', Media: 'Média' }

function padrao(): GraficoEntrada {
  const ent = props.catalogo?.entidades[0]
  return {
    titulo: '', tipo: 'Barras', largura: 2, entidade: ent?.chave ?? 'Negocio', agregacao: 'Contagem',
    campoValor: null, campoAgrupamento: null, filtros: [], periodoCampo: ent?.periodoCampoPadrao ?? null, periodoRelativo: null, limite: null,
  }
}
const form = reactive<GraficoEntrada>(padrao())
const salvando = ref(false)

watch(() => props.modelValue, (aberto) => {
  if (!aberto) return
  const g = props.grafico
  Object.assign(form, g
    ? {
        titulo: g.titulo, tipo: g.tipo, largura: g.largura, entidade: g.entidade, agregacao: g.agregacao,
        campoValor: g.campoValor ?? null, campoAgrupamento: g.campoAgrupamento ?? null,
        filtros: (g.filtros ?? []).map((f) => ({ ...f })), periodoCampo: g.periodoCampo ?? null,
        periodoRelativo: g.periodoRelativo ?? null, limite: g.limite ?? null,
      }
    : padrao())
  atualizarPreview()
  for (const f of form.filtros) garantirOpcoes(campoDe(f.campo))
})

const entidade = computed(() => props.catalogo?.entidades.find((e) => e.chave === form.entidade) ?? null)
const camposValor = computed(() => entidade.value?.camposValor ?? [])
const dimAtual = computed(() => entidade.value?.camposAgrupamento.find((c) => c.chave === form.campoAgrupamento) ?? null)
const nomeOperador = (op: string) => props.catalogo?.operadores.find((o) => o.valor === op)?.rotulo ?? op

function aoMudarEntidade() {
  form.campoValor = null
  form.campoAgrupamento = null
  form.filtros = []
  form.agregacao = 'Contagem'
  form.periodoCampo = entidade.value?.periodoCampoPadrao ?? null
  form.limite = null
}
function aoMudarAgregacao() {
  if (form.agregacao === 'Contagem') form.campoValor = null
  else if (!form.campoValor) form.campoValor = camposValor.value[0]?.chave ?? null
}
function aoMudarAgrupamento() {
  if (dimAtual.value?.tipo === 'mes') form.limite = null
}

// ----- Filtros -----
const opcoesRef = reactive<Record<string, OpcaoRel[]>>({})
function campoDe(chave: string): CampoCatalogo | null {
  return entidade.value?.camposFiltro.find((c) => c.chave === chave) ?? null
}
async function garantirOpcoes(c: CampoCatalogo | null) {
  if (!c?.referencia || opcoesRef[c.referencia]) return
  try { opcoesRef[c.referencia] = await relatoriosApi.opcoes(c.referencia) } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível carregar as opções.')
  }
}
function adicionarFiltro() {
  const c = entidade.value?.camposFiltro[0]
  if (!c) return
  form.filtros.push({ campo: c.chave, operador: c.operadores[0] ?? 'eq', valor: valorInicial(c) })
  garantirOpcoes(c)
}
function valorInicial(c: CampoCatalogo): FiltroRel['valor'] {
  if (c.tipo === 'bool') return true
  if (c.tipo === 'enum') return c.opcoes?.[0]?.valor ?? ''
  return ''
}
function aoMudarCampoFiltro(f: FiltroRel) {
  const c = campoDe(f.campo)
  if (!c) return
  f.operador = c.operadores[0] ?? 'eq'
  f.valor = valorInicial(c)
  garantirOpcoes(c)
}
function aoMudarOperador(f: FiltroRel) {
  if (f.operador === 'nulo' || f.operador === 'naonulo') f.valor = null
  else if (f.valor == null) f.valor = valorInicial(campoDe(f.campo) as CampoCatalogo)
}
const semValor = (f: FiltroRel) => f.operador === 'nulo' || f.operador === 'naonulo'

// ----- Validação, pré-visualização e salvamento -----
const definicao = computed<DefinicaoGrafico>(() => ({
  entidade: form.entidade, agregacao: form.agregacao, campoValor: form.campoValor || null,
  campoAgrupamento: form.campoAgrupamento || null,
  filtros: form.filtros.map((f) => ({
    campo: f.campo, operador: f.operador,
    valor: semValor(f) ? null : (campoDe(f.campo)?.tipo === 'numero' && f.valor !== '' ? Number(f.valor) : f.valor),
  })),
  periodoCampo: form.periodoRelativo ? (form.periodoCampo || entidade.value?.periodoCampoPadrao || null) : (form.periodoCampo || null),
  periodoRelativo: form.periodoRelativo || null,
  limite: form.limite ? Number(form.limite) : null,
}))

const problema = computed(() => {
  if (!form.titulo.trim()) return 'Informe um título.'
  if (form.agregacao !== 'Contagem' && !form.campoValor) return 'Escolha o campo de valor.'
  if (form.tipo !== 'Numero' && !form.campoAgrupamento) return 'Escolha como agrupar os dados.'
  for (const f of form.filtros) {
    if (!semValor(f) && (f.valor === '' || f.valor == null)) return 'Preencha o valor de todos os filtros.'
    if (campoDe(f.campo)?.tipo === 'numero' && Number.isNaN(Number(f.valor))) return 'Use um número válido nos filtros numéricos.'
  }
  if (form.limite != null && form.limite !== ('' as unknown) && (Number(form.limite) < 1 || Number(form.limite) > 100)) return 'O limite deve ficar entre 1 e 100.'
  return ''
})
const previewValido = computed(() => {
  if (form.agregacao !== 'Contagem' && !form.campoValor) return false
  if (form.tipo !== 'Numero' && !form.campoAgrupamento) return false
  return form.filtros.every((f) => semValor(f) || (f.valor !== '' && f.valor != null))
})

// Pré-visualização com atraso para não consultar a cada tecla.
const previewDef = ref<DefinicaoGrafico>(definicao.value)
let temporizador: ReturnType<typeof setTimeout> | undefined
function atualizarPreview() { previewDef.value = JSON.parse(JSON.stringify(definicao.value)) }
watch([definicao, () => form.tipo], () => {
  clearTimeout(temporizador)
  temporizador = setTimeout(atualizarPreview, 400)
}, { deep: true })

async function salvar() {
  if (problema.value) { toast.warning(problema.value); return }
  salvando.value = true
  try {
    const corpo: GraficoEntrada = { ...definicao.value, titulo: form.titulo.trim(), tipo: form.tipo, largura: form.largura }
    const g = props.grafico
      ? await relatoriosApi.atualizarGrafico(props.grafico.id, corpo)
      : await relatoriosApi.criarGrafico(props.painelId, corpo)
    toast.success(props.grafico ? 'Gráfico atualizado.' : 'Gráfico criado.')
    emit('salvo', g)
    emit('update:modelValue', false)
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível salvar o gráfico.')
  } finally {
    salvando.value = false
  }
}
const aoFechar = (v: boolean) => emit('update:modelValue', v)
</script>

<template>
  <UiSlideOver :model-value="modelValue" :titulo="grafico ? 'Editar gráfico' : 'Novo gráfico'" largura="640px" @update:model-value="aoFechar">
    <div class="ed">
      <section class="prev" aria-label="Pré-visualização">
        <div class="prev-t">Pré-visualização</div>
        <GraficoConteudo
          v-if="previewValido && entidade" :definicao="previewDef" :tipo="form.tipo" :periodo-painel="periodoPainel" :catalogo="catalogo"
        />
        <div v-else class="prev-vazio"><i class="mdi mdi-chart-box-outline"></i> Complete as opções para ver o gráfico.</div>
      </section>

      <div class="campo">
        <label for="eg-titulo">Título</label>
        <input id="eg-titulo" v-model="form.titulo" maxlength="200" placeholder="Ex.: Negócios por etapa" />
      </div>

      <div class="campo">
        <span class="rot">Tipo de gráfico</span>
        <div class="seg" role="radiogroup" aria-label="Tipo de gráfico">
          <button
            v-for="t in tipos" :key="t.valor" type="button" role="radio" :aria-checked="form.tipo === t.valor"
            :class="{ ativo: form.tipo === t.valor }" @click="form.tipo = t.valor"
          ><i class="mdi" :class="`mdi-${t.icone}`"></i> {{ t.rotulo }}</button>
        </div>
      </div>

      <div class="campo">
        <span class="rot">Largura na grade</span>
        <div class="seg" role="radiogroup" aria-label="Largura em colunas">
          <button
            v-for="n in 4" :key="n" type="button" role="radio" :aria-checked="form.largura === n"
            :class="{ ativo: form.largura === n }" @click="form.largura = n"
          >{{ n }} {{ n === 1 ? 'coluna' : 'colunas' }}</button>
        </div>
      </div>

      <div class="linha2">
        <div class="campo">
          <label for="eg-ent">O que medir</label>
          <select id="eg-ent" v-model="form.entidade" @change="aoMudarEntidade">
            <option v-for="e in catalogo?.entidades" :key="e.chave" :value="e.chave">{{ e.rotulo }}</option>
          </select>
        </div>
        <div class="campo">
          <label for="eg-agr">Cálculo</label>
          <select id="eg-agr" v-model="form.agregacao" @change="aoMudarAgregacao">
            <option v-for="a in (entidade?.agregacoes ?? ['Contagem']) as Agregacao[]" :key="a" :value="a">{{ nomeAgregacao[a] }}</option>
          </select>
        </div>
      </div>

      <div v-if="form.agregacao !== 'Contagem'" class="campo">
        <label for="eg-val">Campo de valor</label>
        <select id="eg-val" v-model="form.campoValor">
          <option v-for="c in camposValor" :key="c.chave" :value="c.chave">{{ c.rotulo }}{{ c.monetario ? ' (por moeda)' : '' }}</option>
        </select>
        <p class="dica">Valores em dinheiro nunca misturam moedas: cada moeda vira uma série separada.</p>
      </div>

      <div class="linha2">
        <div class="campo">
          <label for="eg-grp">Agrupar por</label>
          <select id="eg-grp" v-model="form.campoAgrupamento" @change="aoMudarAgrupamento">
            <option :value="null">{{ form.tipo === 'Numero' ? '(total geral)' : 'Escolha…' }}</option>
            <option v-for="c in entidade?.camposAgrupamento" :key="c.chave" :value="c.chave">{{ c.rotulo }}</option>
          </select>
        </div>
        <div v-if="form.campoAgrupamento && dimAtual?.tipo !== 'mes'" class="campo">
          <label for="eg-lim">Mostrar os primeiros</label>
          <input id="eg-lim" v-model.number="form.limite" type="number" min="1" max="100" placeholder="30" />
        </div>
      </div>

      <fieldset class="campo bloco">
        <legend>Período</legend>
        <div class="linha2">
          <div class="campo">
            <label for="eg-pc">Campo de data</label>
            <select id="eg-pc" v-model="form.periodoCampo">
              <option v-for="c in entidade?.camposPeriodo" :key="c.chave" :value="c.chave">{{ c.rotulo }}</option>
            </select>
          </div>
          <div class="campo">
            <label for="eg-pr">Intervalo</label>
            <select id="eg-pr" v-model="form.periodoRelativo">
              <option :value="null">Sem filtro de período</option>
              <option v-for="p in catalogo?.periodos" :key="p.valor" :value="p.valor">{{ p.rotulo }}</option>
            </select>
          </div>
        </div>
        <p v-if="periodoPainel" class="dica">O filtro de período do painel está ativo e sobrepõe este intervalo.</p>
      </fieldset>

      <fieldset class="campo bloco">
        <legend>Filtros</legend>
        <p v-if="!form.filtros.length" class="dica">Nenhum filtro. Todos os registros entram na conta.</p>
        <div v-for="(f, i) in form.filtros" :key="i" class="filtro">
          <select v-model="f.campo" :aria-label="`Campo do filtro ${i + 1}`" @change="aoMudarCampoFiltro(f)">
            <option v-for="c in entidade?.camposFiltro" :key="c.chave" :value="c.chave">{{ c.rotulo }}</option>
          </select>
          <select v-model="f.operador" :aria-label="`Operador do filtro ${i + 1}`" @change="aoMudarOperador(f)">
            <option v-for="o in campoDe(f.campo)?.operadores" :key="o" :value="o">{{ nomeOperador(o) }}</option>
          </select>
          <template v-if="!semValor(f)">
            <select v-if="campoDe(f.campo)?.tipo === 'enum'" v-model="f.valor" :aria-label="`Valor do filtro ${i + 1}`">
              <option v-for="o in campoDe(f.campo)?.opcoes" :key="o.valor" :value="o.valor">{{ o.rotulo }}</option>
            </select>
            <select v-else-if="campoDe(f.campo)?.tipo === 'id'" v-model="f.valor" :aria-label="`Valor do filtro ${i + 1}`">
              <option value="" disabled>Escolha…</option>
              <option v-for="o in opcoesRef[campoDe(f.campo)?.referencia ?? ''] ?? []" :key="o.valor" :value="o.valor">{{ o.rotulo }}</option>
            </select>
            <select v-else-if="campoDe(f.campo)?.tipo === 'bool'" v-model="f.valor" :aria-label="`Valor do filtro ${i + 1}`">
              <option :value="true">Sim</option>
              <option :value="false">Não</option>
            </select>
            <input v-else-if="campoDe(f.campo)?.tipo === 'numero'" v-model="f.valor" type="number" step="any" :aria-label="`Valor do filtro ${i + 1}`" />
            <input v-else-if="campoDe(f.campo)?.tipo === 'data'" v-model="f.valor" type="date" :aria-label="`Valor do filtro ${i + 1}`" />
            <input v-else v-model="f.valor" maxlength="200" :aria-label="`Valor do filtro ${i + 1}`" />
          </template>
          <span v-else class="sem-valor"></span>
          <button type="button" class="rm" :aria-label="`Remover filtro ${i + 1}`" @click="form.filtros.splice(i, 1)"><i class="mdi mdi-close"></i></button>
        </div>
        <button type="button" class="add" :disabled="form.filtros.length >= 10" @click="adicionarFiltro"><i class="mdi mdi-plus"></i> Adicionar filtro</button>
      </fieldset>
    </div>

    <template #rodape>
      <span v-if="problema" class="problema">{{ problema }}</span>
      <button class="b-sec" @click="aoFechar(false)">Cancelar</button>
      <button class="b-pri" :disabled="salvando || !!problema" @click="salvar">
        <i v-if="salvando" class="mdi mdi-loading mdi-spin"></i> {{ grafico ? 'Salvar' : 'Criar gráfico' }}
      </button>
    </template>
  </UiSlideOver>
</template>

<style scoped>
.ed { display: flex; flex-direction: column; gap: 14px; }
.prev { border: 1px solid var(--border); border-radius: 10px; padding: 12px 14px; background: var(--bg-secondary); position: sticky; top: -18px; z-index: 2; max-height: 340px; overflow: auto; }
.prev-t { font-size: 10px; text-transform: uppercase; letter-spacing: .6px; color: var(--text-muted); margin-bottom: 8px; font-weight: 600; }
.prev-vazio { display: flex; align-items: center; gap: 8px; color: var(--text-muted); font-size: 12px; padding: 18px 0; justify-content: center; }
.campo { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.campo label, .rot, legend { font-size: 11px; font-weight: 600; color: var(--text-secondary); }
input, select { width: 100%; padding: 8px 10px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-primary); font-size: 13px; }
input:focus, select:focus { outline: none; border-color: var(--accent); }
.linha2 { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.seg { display: flex; flex-wrap: wrap; gap: 6px; }
.seg button { display: inline-flex; align-items: center; gap: 5px; padding: 6px 11px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-secondary); font-size: 12px; cursor: pointer; }
.seg button:hover { color: var(--text-primary); border-color: var(--text-muted); }
.seg button.ativo { border-color: var(--accent); color: var(--accent); background: var(--bg-elevated); font-weight: 600; }
.bloco { border: 1px solid var(--border); border-radius: 10px; padding: 12px; margin: 0; }
legend { padding: 0 6px; }
.dica { font-size: 11px; color: var(--text-muted); }
.filtro { display: grid; grid-template-columns: 1.2fr 1fr 1.3fr 28px; gap: 6px; align-items: center; margin-bottom: 8px; }
.sem-valor { display: block; }
.rm { width: 28px; height: 28px; border: 0; background: none; color: var(--text-muted); border-radius: 6px; cursor: pointer; font-size: 15px; }
.rm:hover { color: var(--danger); background: var(--bg-elevated); }
.add { align-self: flex-start; display: inline-flex; align-items: center; gap: 5px; padding: 6px 12px; border: 1px dashed var(--border); border-radius: 8px; background: none; color: var(--text-secondary); font-size: 12px; cursor: pointer; }
.add:hover:not(:disabled) { border-color: var(--accent); color: var(--accent); }
.add:disabled { opacity: .4; cursor: default; }
.problema { margin-right: auto; font-size: 12px; color: var(--text-muted); }
.b-sec { padding: 8px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-secondary); font-size: 12px; cursor: pointer; }
.b-sec:hover { color: var(--text-primary); border-color: var(--text-muted); }
.b-pri { display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px; border: 0; border-radius: 8px; background: var(--accent); color: #000; font-size: 12px; font-weight: 600; cursor: pointer; }
.b-pri:hover:not(:disabled) { background: var(--accent-hover); }
.b-pri:disabled { opacity: .5; cursor: default; }
@media (max-width: 560px) { .linha2 { grid-template-columns: 1fr; } .filtro { grid-template-columns: 1fr 1fr; } }
</style>
