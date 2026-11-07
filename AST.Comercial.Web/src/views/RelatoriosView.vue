<script setup lang="ts">
// Relatórios: painéis com gráficos configuráveis, filtro de período do painel e modo apresentação.
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import UiModal from '@/components/ui/UiModal.vue'
import GraficoCard from '@/components/relatorios/GraficoCard.vue'
import EditorGrafico from '@/components/relatorios/EditorGrafico.vue'
import ModoApresentacao from '@/components/relatorios/ModoApresentacao.vue'
import { useToast } from '@/composables/useToast'
import { PERIODOS_FALLBACK, relatoriosApi, type Catalogo, type Grafico, type Painel } from '@/components/relatorios/relatoriosApi'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const paineis = ref<Painel[]>([])
const painel = ref<Painel | null>(null)
const catalogo = ref<Catalogo | null>(null)
const carregandoLista = ref(true)
const carregandoPainel = ref(false)
const erroLista = ref('')
const erroPainel = ref('')
const versao = ref(0)

const periodoPainel = ref<string>('')
const periodos = computed(() => catalogo.value?.periodos?.length ? catalogo.value.periodos : PERIODOS_FALLBACK)
const rotuloPeriodo = computed(() => periodos.value.find((p) => p.valor === periodoPainel.value)?.rotulo ?? '')

const graficos = computed<Grafico[]>(() => painel.value?.graficos ?? [])
const idRota = computed(() => (route.params.id ? Number(route.params.id) : null))

function msg(e: unknown, padrao: string): string { return e instanceof Error && e.message ? e.message : padrao }

async function carregarLista() {
  carregandoLista.value = true
  erroLista.value = ''
  try {
    paineis.value = await relatoriosApi.listarPaineis()
  } catch (e) {
    erroLista.value = msg(e, 'Não foi possível carregar os painéis.')
    toast.error(erroLista.value)
  } finally {
    carregandoLista.value = false
  }
}

async function carregarPainel(id: number) {
  carregandoPainel.value = true
  erroPainel.value = ''
  try {
    painel.value = await relatoriosApi.obterPainel(id)
  } catch (e) {
    painel.value = null
    erroPainel.value = msg(e, 'Não foi possível abrir o painel.')
    toast.error(erroPainel.value)
  } finally {
    carregandoPainel.value = false
  }
}

async function iniciar() {
  try { catalogo.value = await relatoriosApi.catalogo() } catch (e) { toast.error(msg(e, 'Não foi possível carregar as opções de relatório.')) }
  await carregarLista()
  selecionarAtual()
}

function selecionarAtual() {
  if (idRota.value) { carregarPainel(idRota.value); return }
  const primeiro = paineis.value[0]
  if (primeiro) router.replace(`/relatorios/${primeiro.id}`)
  else painel.value = null
}

watch(idRota, (id) => { if (id) carregarPainel(id); else selecionarAtual() })
onMounted(iniciar)

function abrir(id: number) { if (id !== idRota.value) router.push(`/relatorios/${id}`) }
function atualizar() { versao.value++ }

// ----- Painéis: criar, renomear, compartilhar, excluir -----
const modalPainel = ref(false)
const editandoPainel = ref<Painel | null>(null)
const formNome = ref('')
const formDescricao = ref('')
const formCompartilhado = ref(false)
const salvandoPainel = ref(false)

function novoPainel() {
  editandoPainel.value = null
  formNome.value = ''; formDescricao.value = ''; formCompartilhado.value = false
  modalPainel.value = true
}
function renomear(p: Painel) {
  editandoPainel.value = p
  formNome.value = p.nome; formDescricao.value = p.descricao ?? ''; formCompartilhado.value = p.compartilhado
  modalPainel.value = true
}
async function salvarPainel() {
  if (!formNome.value.trim()) { toast.warning('Informe o nome do painel.'); return }
  salvandoPainel.value = true
  try {
    if (editandoPainel.value) {
      await relatoriosApi.atualizarPainel(editandoPainel.value.id, { nome: formNome.value, descricao: formDescricao.value, compartilhado: formCompartilhado.value })
      toast.success('Painel atualizado.')
      modalPainel.value = false
      await carregarLista()
      if (painel.value?.id === editandoPainel.value.id) await carregarPainel(editandoPainel.value.id)
    } else {
      const novo = await relatoriosApi.criarPainel({ nome: formNome.value, descricao: formDescricao.value, compartilhado: formCompartilhado.value })
      toast.success('Painel criado.')
      modalPainel.value = false
      await carregarLista()
      router.push(`/relatorios/${novo.id}`)
    }
  } catch (e) {
    toast.error(msg(e, 'Não foi possível salvar o painel.'))
  } finally {
    salvandoPainel.value = false
  }
}
async function alternarCompartilhar(p: Painel) {
  try {
    await relatoriosApi.atualizarPainel(p.id, { compartilhado: !p.compartilhado })
    toast.success(p.compartilhado ? 'Painel agora é só seu.' : 'Painel compartilhado com a empresa.')
    await carregarLista()
    if (painel.value?.id === p.id) painel.value = { ...painel.value, compartilhado: !p.compartilhado }
  } catch (e) {
    toast.error(msg(e, 'Não foi possível alterar o compartilhamento.'))
  }
}

const confirmar = ref<{ tipo: 'painel' | 'grafico'; id: number; nome: string } | null>(null)
const excluindo = ref(false)
async function executarExclusao() {
  const c = confirmar.value
  if (!c) return
  excluindo.value = true
  try {
    if (c.tipo === 'painel') {
      await relatoriosApi.excluirPainel(c.id)
      toast.success('Painel excluído.')
      confirmar.value = null
      await carregarLista()
      if (painel.value?.id === c.id) { painel.value = null; router.replace(paineis.value[0] ? `/relatorios/${paineis.value[0].id}` : '/relatorios') }
    } else {
      await relatoriosApi.excluirGrafico(c.id)
      toast.success('Gráfico excluído.')
      confirmar.value = null
      if (painel.value) await carregarPainel(painel.value.id)
      carregarLista()
    }
  } catch (e) {
    toast.error(msg(e, 'Não foi possível excluir.'))
  } finally {
    excluindo.value = false
  }
}

// ----- Gráficos: editor e ordem -----
const editorAberto = ref(false)
const graficoEditando = ref<Grafico | null>(null)
function novoGrafico() { graficoEditando.value = null; editorAberto.value = true }
function editarGrafico(g: Grafico) { graficoEditando.value = g; editorAberto.value = true }
async function aoSalvarGrafico() {
  if (painel.value) await carregarPainel(painel.value.id)
  carregarLista()
}
async function mover(g: Grafico, direcao: -1 | 1) {
  const p = painel.value
  if (!p?.graficos) return
  const lista = [...p.graficos]
  const i = lista.findIndex((x) => x.id === g.id)
  const j = i + direcao
  if (i < 0 || j < 0 || j >= lista.length) return
  const a = lista[i] as Grafico
  lista[i] = lista[j] as Grafico
  lista[j] = a
  painel.value = { ...p, graficos: lista }
  try {
    await relatoriosApi.reordenar(p.id, lista.map((x) => x.id))
  } catch (e) {
    toast.error(msg(e, 'Não foi possível reordenar.'))
    await carregarPainel(p.id)
  }
}

// ----- Apresentação -----
const apresentando = ref(false)
const inicioApresentacao = ref(0)
function apresentar(indice = 0) {
  if (!graficos.value.length) { toast.info('Adicione gráficos ao painel para apresentar.'); return }
  inicioApresentacao.value = indice
  apresentando.value = true
}
</script>

<template>
  <div class="rel">
    <aside class="lateral" aria-label="Painéis de relatório">
      <div class="lat-topo">
        <h2>Painéis</h2>
        <button class="b-pri pequeno" @click="novoPainel"><i class="mdi mdi-plus"></i> Novo</button>
      </div>

      <div v-if="carregandoLista" class="lat-estado" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando…</div>
      <div v-else-if="erroLista" class="lat-estado erro" role="alert">
        <span>{{ erroLista }}</span>
        <button class="b-sec pequeno" @click="carregarLista">Tentar de novo</button>
      </div>
      <div v-else-if="!paineis.length" class="lat-estado">Nenhum painel ainda. Crie o primeiro.</div>
      <ul v-else class="lista">
        <li v-for="p in paineis" :key="p.id" :class="{ ativo: p.id === idRota }">
          <button class="item" :aria-current="p.id === idRota ? 'page' : undefined" @click="abrir(p.id)">
            <i class="mdi" :class="p.compartilhado ? 'mdi-account-group-outline' : 'mdi-lock-outline'" :title="p.compartilhado ? 'Compartilhado' : 'Só você vê'"></i>
            <span class="nome">{{ p.nome }}</span>
            <span class="qtd">{{ p.quantidadeGraficos }}</span>
          </button>
          <div v-if="p.podeEditar" class="item-acoes">
            <button class="ic" :aria-label="`Renomear ${p.nome}`" title="Renomear" @click="renomear(p)"><i class="mdi mdi-pencil-outline"></i></button>
            <button class="ic" :aria-label="p.compartilhado ? `Deixar ${p.nome} privado` : `Compartilhar ${p.nome}`" :title="p.compartilhado ? 'Tornar privado' : 'Compartilhar'" @click="alternarCompartilhar(p)">
              <i class="mdi" :class="p.compartilhado ? 'mdi-lock-open-variant-outline' : 'mdi-share-variant-outline'"></i>
            </button>
            <button class="ic perigo" :aria-label="`Excluir ${p.nome}`" title="Excluir" @click="confirmar = { tipo: 'painel', id: p.id, nome: p.nome }"><i class="mdi mdi-trash-can-outline"></i></button>
          </div>
        </li>
      </ul>
    </aside>

    <section class="principal">
      <div v-if="carregandoPainel && !painel" class="estado" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando painel…</div>
      <div v-else-if="erroPainel" class="estado erro" role="alert">
        <i class="mdi mdi-alert-circle-outline"></i><span>{{ erroPainel }}</span>
        <button class="b-sec" @click="idRota && carregarPainel(idRota)">Tentar de novo</button>
      </div>
      <div v-else-if="!painel" class="estado">
        <i class="mdi mdi-chart-box-outline"></i>
        <span v-if="!carregandoLista && !paineis.length">Crie um painel para montar seus gráficos.</span>
        <span v-else-if="!carregandoLista">Escolha um painel na lista.</span>
      </div>

      <template v-else>
        <header class="cab">
          <div class="cab-titulo">
            <h1>{{ painel.nome }}</h1>
            <p v-if="painel.descricao">{{ painel.descricao }}</p>
            <p class="meta">
              <i class="mdi" :class="painel.compartilhado ? 'mdi-account-group-outline' : 'mdi-lock-outline'"></i>
              {{ painel.compartilhado ? 'Compartilhado com a empresa' : 'Visível só para você' }}
              <template v-if="painel.criadoPorNome"> · criado por {{ painel.criadoPorNome }}</template>
            </p>
          </div>
          <div class="cab-acoes">
            <label class="per">
              <span class="sr">Período do painel</span>
              <i class="mdi mdi-calendar-range" aria-hidden="true"></i>
              <select v-model="periodoPainel" aria-label="Período do painel">
                <option value="">Período de cada gráfico</option>
                <option v-for="p in periodos" :key="p.valor" :value="p.valor">{{ p.rotulo }}</option>
              </select>
            </label>
            <button class="b-sec icone" aria-label="Atualizar dados" title="Atualizar dados" @click="atualizar"><i class="mdi mdi-refresh"></i></button>
            <button class="b-sec" :disabled="!graficos.length" @click="apresentar(0)"><i class="mdi mdi-presentation-play"></i> Apresentar</button>
            <button v-if="painel.podeEditar" class="b-pri" @click="novoGrafico"><i class="mdi mdi-plus"></i> Novo gráfico</button>
          </div>
        </header>
        <p v-if="periodoPainel" class="aviso-periodo"><i class="mdi mdi-information-outline"></i> O período «{{ rotuloPeriodo }}» está sobrepondo o período definido em cada gráfico.</p>

        <div v-if="!graficos.length" class="estado vazio">
          <i class="mdi mdi-chart-box-plus-outline"></i>
          <span>Este painel ainda não tem gráficos.</span>
          <button v-if="painel.podeEditar" class="b-pri" @click="novoGrafico"><i class="mdi mdi-plus"></i> Adicionar gráfico</button>
        </div>
        <div v-else class="grade">
          <GraficoCard
            v-for="(g, i) in graficos" :key="g.id" :grafico="g" :catalogo="catalogo" :periodo-painel="periodoPainel || null"
            :pode-editar="painel.podeEditar" :primeiro="i === 0" :ultimo="i === graficos.length - 1" :versao="versao"
            @editar="editarGrafico(g)" @excluir="confirmar = { tipo: 'grafico', id: g.id, nome: g.titulo }"
            @mover="mover(g, $event)" @ampliar="apresentar(i)"
          />
        </div>
      </template>
    </section>

    <EditorGrafico
      v-if="painel" v-model="editorAberto" :catalogo="catalogo" :painel-id="painel.id" :grafico="graficoEditando"
      :periodo-painel="periodoPainel || null" @salvo="aoSalvarGrafico"
    />

    <ModoApresentacao
      v-if="apresentando && painel" :graficos="graficos" :inicio="inicioApresentacao" :catalogo="catalogo"
      :periodo-painel="periodoPainel || null" :titulo="painel.nome" :rotulo-periodo="rotuloPeriodo" @sair="apresentando = false"
    />

    <UiModal v-model="modalPainel" :titulo="editandoPainel ? 'Renomear painel' : 'Novo painel'" largura="460px">
      <form class="form" @submit.prevent="salvarPainel">
        <label for="rp-nome">Nome</label>
        <input id="rp-nome" v-model="formNome" maxlength="200" autofocus placeholder="Ex.: Resultados do trimestre" />
        <label for="rp-desc">Descrição (opcional)</label>
        <input id="rp-desc" v-model="formDescricao" maxlength="1000" />
        <label class="chk"><input v-model="formCompartilhado" type="checkbox" /> Compartilhar com todos da empresa</label>
        <p class="dica">Painéis não compartilhados só aparecem para quem os criou (e para administradores).</p>
      </form>
      <template #rodape>
        <button class="b-sec" @click="modalPainel = false">Cancelar</button>
        <button class="b-pri" :disabled="salvandoPainel" @click="salvarPainel">{{ editandoPainel ? 'Salvar' : 'Criar painel' }}</button>
      </template>
    </UiModal>

    <UiModal :model-value="!!confirmar" titulo="Confirmar exclusão" largura="420px" @update:model-value="confirmar = null">
      <p class="conf">
        Excluir {{ confirmar?.tipo === 'painel' ? 'o painel' : 'o gráfico' }} <strong>{{ confirmar?.nome }}</strong>?
        <template v-if="confirmar?.tipo === 'painel'"> Todos os gráficos dele serão removidos.</template>
      </p>
      <template #rodape>
        <button class="b-sec" @click="confirmar = null">Cancelar</button>
        <button class="b-perigo" :disabled="excluindo" @click="executarExclusao">Excluir</button>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.rel { display: flex; gap: 20px; align-items: flex-start; min-height: 100%; }
.lateral { width: 250px; flex: none; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 12px; padding: 14px 10px; position: sticky; top: 0; }
.lat-topo { display: flex; align-items: center; justify-content: space-between; padding: 0 6px 10px; }
.lat-topo h2 { font-size: 13px; font-weight: 600; color: var(--text-primary); }
.lat-estado { display: flex; flex-direction: column; gap: 8px; align-items: center; text-align: center; padding: 18px 8px; font-size: 12px; color: var(--text-muted); }
.lat-estado.erro { color: var(--danger); }
.lista { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 2px; }
.lista li { position: relative; border-radius: 8px; }
.lista li.ativo { background: var(--bg-elevated); box-shadow: inset 2px 0 0 var(--accent); }
.item { width: 100%; display: flex; align-items: center; gap: 8px; padding: 9px 10px; border: 0; background: none; color: var(--text-secondary); font-size: 13px; text-align: left; cursor: pointer; border-radius: 8px; }
.item:hover { color: var(--text-primary); background: var(--bg-elevated); }
.lista li.ativo .item { color: var(--text-primary); font-weight: 600; }
.item i { font-size: 15px; color: var(--text-muted); }
.item .nome { flex: 1; min-width: 0; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.item .qtd { font-size: 10px; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 999px; padding: 1px 7px; color: var(--text-muted); }
.item-acoes { display: none; gap: 2px; padding: 0 6px 6px 30px; }
.lista li:hover .item-acoes, .lista li:focus-within .item-acoes, .lista li.ativo .item-acoes { display: flex; }
.ic { width: 26px; height: 26px; display: inline-flex; align-items: center; justify-content: center; border: 0; background: none; color: var(--text-muted); border-radius: 6px; cursor: pointer; font-size: 15px; }
.ic:hover { background: var(--bg-primary); color: var(--accent); }
.ic.perigo:hover { color: var(--danger); }

.principal { flex: 1; min-width: 0; }
.estado { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 10px; padding: 70px 20px; text-align: center; color: var(--text-muted); font-size: 13px; background: var(--bg-surface); border: 1px dashed var(--border); border-radius: 12px; }
.estado i { font-size: 34px; }
.estado.erro { color: var(--danger); }
.estado.erro span { color: var(--text-secondary); }
.cab { display: flex; justify-content: space-between; align-items: flex-start; gap: 16px; flex-wrap: wrap; margin-bottom: 16px; }
.cab-titulo h1 { font-size: 20px; font-weight: 700; color: var(--text-primary); }
.cab-titulo p { font-size: 12px; color: var(--text-secondary); margin-top: 3px; }
.cab-titulo .meta { color: var(--text-muted); display: flex; align-items: center; gap: 5px; }
.cab-acoes { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
.per { position: relative; display: inline-flex; align-items: center; }
.per i { position: absolute; left: 10px; color: var(--text-muted); pointer-events: none; }
.per select { padding: 8px 10px 8px 32px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-primary); font-size: 12px; }
.per select:focus { outline: none; border-color: var(--accent); }
.sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
.aviso-periodo { display: flex; align-items: center; gap: 6px; font-size: 12px; color: var(--accent); background: var(--bg-surface); border: 1px solid var(--border); border-radius: 8px; padding: 7px 12px; margin-bottom: 14px; }
.grade { display: grid; grid-template-columns: repeat(4, minmax(0, 1fr)); gap: 16px; }

.b-pri, .b-sec, .b-perigo { display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px; border-radius: 8px; font-size: 12px; cursor: pointer; transition: all .15s; }
.b-pri { border: 0; background: var(--accent); color: #000; font-weight: 600; }
.b-pri:hover:not(:disabled) { background: var(--accent-hover); }
.b-sec { border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); }
.b-sec:hover:not(:disabled) { color: var(--text-primary); border-color: var(--text-muted); }
.b-perigo { border: 0; background: var(--danger); color: #fff; font-weight: 600; }
.b-pri:disabled, .b-sec:disabled, .b-perigo:disabled { opacity: .5; cursor: default; }
.pequeno { padding: 5px 11px; }
.icone { padding: 8px 10px; font-size: 15px; }

.form { display: flex; flex-direction: column; gap: 6px; }
.form label { font-size: 11px; font-weight: 600; color: var(--text-secondary); margin-top: 6px; }
.form input:not([type='checkbox']) { padding: 8px 10px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-primary); font-size: 13px; }
.form input:focus { outline: none; border-color: var(--accent); }
.form .chk { display: flex; align-items: center; gap: 8px; font-weight: 400; font-size: 13px; color: var(--text-primary); margin-top: 12px; }
.dica { font-size: 11px; color: var(--text-muted); margin-top: 4px; }
.conf { font-size: 13px; color: var(--text-secondary); line-height: 1.5; }
.conf strong { color: var(--text-primary); }

@media (max-width: 1180px) { .grade { grid-template-columns: repeat(2, minmax(0, 1fr)); } }
@media (max-width: 900px) {
  .rel { flex-direction: column; }
  .lateral { width: 100%; position: static; }
}
@media (max-width: 720px) { .grade { grid-template-columns: minmax(0, 1fr); } }
</style>
