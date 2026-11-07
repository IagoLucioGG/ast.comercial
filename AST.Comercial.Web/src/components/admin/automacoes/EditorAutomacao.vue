<script setup lang="ts">
import { ref, computed, watch, nextTick, onBeforeUnmount } from 'vue'
import UiSlideOver from '@/components/ui/UiSlideOver.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import { useToast } from '@/composables/useToast'
import { odataTodos } from '@/services/api'
import {
  automacaoApi, mensagensDoErro,
  type Catalogo, type AcaoCatalogo, type AutomacaoDto, type AutomacaoEntrada, type CampoCatalogo,
} from './automacaoApi'

interface Condicao { campo: string; operador: string; valor: string }
interface Bloco { uid: number; condicoes: Condicao[] }
interface Alteracao { campo: string; valor: string }
interface AcaoLocal {
  uid: number; tipo: string
  cfg: Record<string, string>      // valores simples (texto/numero) em string
  alteracoes: Alteracao[]          // EditarDados
  headersTexto: string             // ExecutarWebhook: "Chave: valor" por linha
}
interface Opcao { id: number; nome: string }

const props = defineProps<{ modelValue: boolean; automacaoId: number | null; entidade: string; catalogo: Catalogo }>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean]; salvo: [a: AutomacaoDto] }>()
const toast = useToast()

let seq = 0
const carregando = ref(false)
const salvando = ref(false)
const erros = ref<string[]>([])
const nome = ref('')
const descricao = ref('')
const gatilho = ref('')
const impedirCascata = ref(false)
const ativo = ref(true)
const horario = ref('')
const blocos = ref<Bloco[]>([])
const acoes = ref<AcaoLocal[]>([])
const entidadeAlvo = ref('')
const novaAcaoTipo = ref('')
const status = ref<Opcao[]>([])
const etapas = ref<Opcao[]>([])
const etiquetas = ref<Opcao[]>([])
const nomeEl = ref<HTMLInputElement | null>(null)
const confirmandoSaida = ref(false)
const continuarEl = ref<HTMLButtonElement | null>(null)
let instantaneo = ''
let focoAnterior: HTMLElement | null = null

const entidadeCat = computed(() => props.catalogo.entidades.find((e) => e.valor === entidadeAlvo.value))
const campos = computed<CampoCatalogo[]>(() => entidadeCat.value?.campos ?? [])
const gatilhosDaEntidade = computed(() => props.catalogo.gatilhos.filter((g) => g.entidades.includes(entidadeAlvo.value)))
const gatilhoSel = computed(() => props.catalogo.gatilhos.find((g) => g.valor === gatilho.value))
const periodico = computed(() => gatilho.value === 'Periodicamente')
const operadores = computed(() => props.catalogo.operadores)
const acoesDisponiveis = computed(() => props.catalogo.acoes.filter((a) => !a.entidades || a.entidades.includes(entidadeAlvo.value)))
const titulo = computed(() => props.automacaoId ? 'Regras da automação' : 'Regras da automação (nova)')

function acaoCat(tipo: string): AcaoCatalogo | undefined { return props.catalogo.acoes.find((a) => a.valor === tipo) }
function campoCat(nomeCampo: string): CampoCatalogo | undefined { return campos.value.find((c) => c.nome === nomeCampo) }
function operadorCat(v: string) { return operadores.value.find((o) => o.valor === v) }

function novaCondicao(): Condicao {
  const op = operadores.value.find((o) => o.implementado)
  return { campo: campos.value[0]?.nome ?? '', operador: op?.valor ?? 'Igual', valor: '' }
}
function novaAcao(tipo: string): AcaoLocal {
  const cfg: Record<string, string> = {}
  if (tipo === 'CriarTarefa') { cfg.tipo = 'Tarefa'; cfg.prazo = '0' }
  if (tipo === 'ExecutarWebhook') cfg.metodo = 'POST'
  return { uid: ++seq, tipo, cfg, alteracoes: tipo === 'EditarDados' ? [{ campo: '', valor: '' }] : [], headersTexto: '' }
}

async function carregarOpcoes() {
  const ler = async (caminho: string): Promise<Opcao[]> => {
    try {
      const lista = await odataTodos<Record<string, unknown>>(caminho)
      return lista.map((x) => ({ id: Number(x.id ?? x.Id), nome: String(x.nome ?? x.Nome ?? x.id ?? x.Id) }))
    } catch { return [] }
  }
  const [s, e, t] = await Promise.all([ler('Negocios@Status?$orderby=Id'), ler('Negocios@Etapas?$orderby=Id'), ler('Etiquetas?$orderby=Id')])
  status.value = s; etapas.value = e; etiquetas.value = t
}

function reiniciar() {
  erros.value = []
  nome.value = ''; descricao.value = ''; impedirCascata.value = false; ativo.value = true; horario.value = ''
  entidadeAlvo.value = props.entidade
  gatilho.value = ''
  blocos.value = [{ uid: ++seq, condicoes: [] }]
  acoes.value = []
  novaAcaoTipo.value = ''
}

function preencher(a: AutomacaoDto) {
  nome.value = a.nome; descricao.value = a.descricao ?? ''; entidadeAlvo.value = a.entidadeAlvo
  gatilho.value = a.gatilho; impedirCascata.value = a.impedirCascata; ativo.value = a.ativo
  horario.value = a.horarioExecucao ?? ''
  const grupos = new Map<number, Condicao[]>()
  for (const f of a.filtros) {
    const lista = grupos.get(f.grupo) ?? []
    lista.push({ campo: f.campoReferencia, operador: f.operador, valor: f.valor ?? '' })
    grupos.set(f.grupo, lista)
  }
  blocos.value = [...grupos.keys()].sort((x, y) => x - y).map((g) => ({ uid: ++seq, condicoes: grupos.get(g) ?? [] }))
  if (!blocos.value.length) blocos.value = [{ uid: ++seq, condicoes: [] }]
  acoes.value = a.acoes.map((ac) => {
    const base = novaAcao(ac.tipo)
    const c = ac.configuracao ?? {}
    for (const [k, v] of Object.entries(c)) {
      if (k === 'alteracoes' && Array.isArray(v)) {
        base.alteracoes = (v as { campo?: unknown; valor?: unknown }[]).map((x) => ({ campo: String(x.campo ?? ''), valor: String(x.valor ?? '') }))
      } else if (k === 'headers' && v && typeof v === 'object') {
        base.headersTexto = Object.entries(v as Record<string, unknown>).map(([hk, hv]) => `${hk}: ${hv}`).join('\n')
      } else if (v !== null && v !== undefined) base.cfg[k] = String(v)
    }
    return base
  })
}

// Estado editável serializado: serve para detectar alterações não salvas.
function fotografia(): string {
  return JSON.stringify([nome.value, descricao.value, gatilho.value, impedirCascata.value, ativo.value, horario.value,
    blocos.value.map((b) => b.condicoes), acoes.value.map((a) => [a.tipo, a.cfg, a.alteracoes, a.headersTexto])])
}
const alterado = computed(() => !carregando.value && instantaneo !== '' && fotografia() !== instantaneo)

// Teclado/foco: o UiSlideOver não prende o foco nem o devolve; o editor faz isso aqui.
function painel(): HTMLElement | null { return document.querySelector<HTMLElement>('.so-panel[aria-label]') }
function aoTeclar(e: KeyboardEvent) {
  if (e.key !== 'Tab') return
  const p = painel(); if (!p) return
  const foco = [...p.querySelectorAll<HTMLElement>('a[href],button,input,select,textarea,[tabindex]:not([tabindex="-1"])')]
    .filter((el) => !(el as HTMLButtonElement).disabled && el.offsetParent !== null)
  const primeiro = foco[0], ultimo = foco[foco.length - 1]
  if (!primeiro || !ultimo) return
  const ativoEl = document.activeElement
  if (!p.contains(ativoEl)) { e.preventDefault(); primeiro.focus() }
  else if (e.shiftKey && ativoEl === primeiro) { e.preventDefault(); ultimo.focus() }
  else if (!e.shiftKey && ativoEl === ultimo) { e.preventDefault(); primeiro.focus() }
}
watch(() => props.modelValue, (aberto) => {
  if (aberto) { focoAnterior = document.activeElement as HTMLElement | null; document.addEventListener('keydown', aoTeclar) }
  else {
    document.removeEventListener('keydown', aoTeclar)
    confirmandoSaida.value = false
    focoAnterior?.focus?.(); focoAnterior = null
  }
})
watch(confirmandoSaida, async (v) => { if (v) { await nextTick(); continuarEl.value?.focus() } })
onBeforeUnmount(() => document.removeEventListener('keydown', aoTeclar))

watch(() => props.modelValue, async (aberto) => {
  if (!aberto) return
  instantaneo = ''
  reiniciar()
  carregarOpcoes()
  if (props.automacaoId) {
    carregando.value = true
    try { preencher(await automacaoApi.obter(props.automacaoId)) }
    catch (e) { toast.error(mensagensDoErro(e)[0] ?? 'Falha ao carregar a automação.'); emit('update:modelValue', false) }
    finally { carregando.value = false }
  }
  await nextTick()
  instantaneo = fotografia()
  nomeEl.value?.focus()
}, { immediate: true })

// Ao trocar de gatilho, mantém só o que faz sentido
watch(gatilho, (g) => { if (g !== 'Periodicamente') horario.value = '' })

// --- Blocos de condição ---
function addBloco() { blocos.value.push({ uid: ++seq, condicoes: [novaCondicao()] }) }
function removerBloco(i: number) { blocos.value.splice(i, 1) }
function duplicarBloco(i: number) {
  const b = blocos.value[i]; if (!b) return
  blocos.value.splice(i + 1, 0, { uid: ++seq, condicoes: b.condicoes.map((c) => ({ ...c })) })
}
function addCondicao(b: Bloco) { b.condicoes.push(novaCondicao()) }
function removerCondicao(b: Bloco, i: number) { b.condicoes.splice(i, 1) }
function trocouCampo(c: Condicao) { c.valor = '' }
function tipoInput(c: Condicao): 'booleano' | 'enum' | 'data' | 'numero' | 'texto' {
  const t = campoCat(c.campo)?.tipo
  if (t === 'booleano' || t === 'enum' || t === 'data' || t === 'numero') return t
  return 'texto'
}

// --- Ações ---
function adicionarAcao() {
  if (!novaAcaoTipo.value) return
  acoes.value.push(novaAcao(novaAcaoTipo.value)); novaAcaoTipo.value = ''
}
function mover(i: number, d: number) {
  const j = i + d
  if (j < 0 || j >= acoes.value.length) return
  const a = acoes.value[i], b = acoes.value[j]
  if (!a || !b) return
  acoes.value[i] = b; acoes.value[j] = a
}
function removerAcao(i: number) { acoes.value.splice(i, 1) }
function opcoesRef(chave: string): Opcao[] {
  if (chave === 'statusId') return status.value
  if (chave === 'etapaId') return etapas.value
  if (chave === 'etiquetaId') return etiquetas.value
  return []
}
const tiposTarefa = ['Tarefa', 'Ligacao', 'Email', 'Reuniao', 'Nota']
const metodosHttp = ['POST', 'GET', 'PUT', 'PATCH', 'DELETE']
function rotuloParam(p: { chave: string; descricao?: string | null }) { return p.descricao || p.chave }
function paramsSimples(a: AcaoLocal) {
  return (acaoCat(a.tipo)?.configuracao ?? []).filter((p) => !['alteracoes', 'headers'].includes(p.chave))
}

function montarConfig(a: AcaoLocal): Record<string, unknown> {
  const out: Record<string, unknown> = {}
  for (const p of acaoCat(a.tipo)?.configuracao ?? []) {
    if (p.chave === 'alteracoes') {
      out.alteracoes = a.alteracoes.filter((x) => x.campo).map((x) => ({ campo: x.campo, valor: x.valor }))
    } else if (p.chave === 'headers') {
      const h: Record<string, string> = {}
      for (const linha of a.headersTexto.split('\n')) {
        const idx = linha.indexOf(':')
        if (idx > 0) h[linha.slice(0, idx).trim()] = linha.slice(idx + 1).trim()
      }
      if (Object.keys(h).length) out.headers = h
    } else {
      const v = (a.cfg[p.chave] ?? '').trim()
      if (v === '') continue
      const numerico = /id$/i.test(p.chave) || p.chave === 'prazo' || /int|num/i.test(p.tipo)
      out[p.chave] = numerico && !isNaN(Number(v)) ? Number(v) : v
    }
  }
  return out
}

function validar(): string[] {
  const e: string[] = []
  if (!nome.value.trim()) e.push('Informe o nome da automação.')
  if (!gatilho.value) e.push('Escolha o gatilho.')
  if (periodico.value && !/^\d{2}:\d{2}$/.test(horario.value)) e.push('Informe o horário de execução (HH:mm).')
  blocos.value.forEach((b, bi) => b.condicoes.forEach((c, ci) => {
    if (!c.campo) e.push(`Bloco ${bi + 1}, condição ${ci + 1}: escolha o campo.`)
    else if (operadorCat(c.operador)?.precisaValor && c.valor.trim() === '') e.push(`Bloco ${bi + 1}, condição ${ci + 1}: informe o valor.`)
  }))
  if (!acoes.value.length) e.push('Adicione ao menos uma ação.')
  acoes.value.forEach((a, i) => {
    const cat = acaoCat(a.tipo)
    for (const p of cat?.configuracao ?? []) {
      if (!p.obrigatorio) continue
      if (p.chave === 'alteracoes') { if (!a.alteracoes.some((x) => x.campo)) e.push(`Ação ${i + 1} (${cat?.nome}): informe ao menos uma alteração.`) }
      else if (p.chave === 'headers') continue
      else if (!(a.cfg[p.chave] ?? '').trim()) e.push(`Ação ${i + 1} (${cat?.nome}): preencha "${rotuloParam(p)}".`)
    }
  })
  return e
}

async function salvar() {
  erros.value = validar()
  if (erros.value.length) return
  const entrada: AutomacaoEntrada = {
    nome: nome.value.trim(), descricao: descricao.value.trim() || null,
    entidadeAlvo: entidadeAlvo.value, gatilho: gatilho.value, ordem: 0,
    impedirCascata: impedirCascata.value, ativo: ativo.value,
    horarioExecucao: periodico.value ? horario.value : null,
    filtros: blocos.value.flatMap((b, g) => b.condicoes.map((c) => ({
      grupo: g, campoReferencia: c.campo, operador: c.operador,
      valor: operadorCat(c.operador)?.precisaValor ? c.valor : null,
    }))),
    acoes: acoes.value.map((a) => ({ tipo: a.tipo, configuracao: montarConfig(a) })),
  }
  // Remove grupos vazios mantendo numeração contínua
  const usados = [...new Set(entrada.filtros.map((f) => f.grupo))]
  entrada.filtros.forEach((f) => { f.grupo = usados.indexOf(f.grupo) })
  salvando.value = true
  try {
    const r = props.automacaoId ? await automacaoApi.atualizar(props.automacaoId, entrada) : await automacaoApi.criar(entrada)
    toast.success('Automação salva.')
    emit('salvo', r)
    emit('update:modelValue', false)
  } catch (e) {
    erros.value = mensagensDoErro(e)
    toast.error(erros.value[0] ?? 'Não foi possível salvar.')
  } finally { salvando.value = false }
}
// Esc, X e Cancelar passam por aqui: com alterações pendentes pede confirmação antes de descartar.
function fechar() {
  if (confirmandoSaida.value) return
  if (alterado.value && !salvando.value) { confirmandoSaida.value = true; return }
  emit('update:modelValue', false)
}
function descartar() { confirmandoSaida.value = false; emit('update:modelValue', false) }
</script>

<template>
  <UiSlideOver :model-value="modelValue" :titulo="titulo" largura="760px" @update:model-value="fechar">
    <div v-if="carregando" class="estado" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando...</div>
    <form v-else class="ed" @submit.prevent="salvar" novalidate>
      <div v-if="erros.length" class="erros" role="alert">
        <strong>Corrija antes de salvar:</strong>
        <ul><li v-for="(m, i) in erros" :key="i">{{ m }}</li></ul>
      </div>

      <!-- 1 Descrição -->
      <section class="passo">
        <h4><span class="num">1</span> Descrição</h4>
        <label class="campo"><span>Nome *</span>
          <input ref="nomeEl" v-model="nome" maxlength="200" placeholder="Ex.: Follow-up após proposta enviada" /></label>
        <label class="campo"><span>Descrição</span>
          <textarea v-model="descricao" rows="2" maxlength="1000" placeholder="Opcional"></textarea></label>
        <p class="dica">Entidade: <UiBadge tom="destaque">{{ entidadeCat?.nome ?? entidadeAlvo }}</UiBadge></p>
      </section>

      <!-- 2 Gatilho -->
      <section class="passo">
        <h4><span class="num">2</span> Gatilho</h4>
        <label class="campo"><span>Quando executar *</span>
          <select v-model="gatilho">
            <option value="" disabled>Selecione...</option>
            <option v-for="g in gatilhosDaEntidade" :key="g.valor" :value="g.valor" :disabled="!g.implementado">
              {{ g.nome }}{{ g.implementado ? '' : ' (em breve)' }}
            </option>
          </select></label>
        <p v-if="gatilhosDaEntidade.some((g) => !g.implementado)" class="aviso">
          <i class="mdi mdi-information-outline"></i>
          Gatilhos marcados como "em breve" ainda não são disparados pelo sistema e não podem ser usados.
        </p>
        <p v-if="gatilhoSel?.observacao" class="dica">{{ gatilhoSel.observacao }}</p>
        <div v-if="periodico" class="linha">
          <label class="campo"><span>Horário (UTC) *</span><input v-model="horario" type="time" /></label>
          <p class="dica">Executa uma vez por dia, no horário informado (UTC). Intervalos personalizados e data de início ainda não são suportados pelo motor.
            Sem registro associado, só as ações Criar tarefa, Registrar interação e Executar webhook são aceitas.</p>
        </div>
        <label class="check"><input v-model="impedirCascata" type="checkbox" />
          Impedir disparo por outras automações</label>
        <label class="check"><input v-model="ativo" type="checkbox" /> Automação ativa</label>
      </section>

      <!-- 3 Filtro -->
      <section class="passo">
        <h4><span class="num">3</span> Filtro <small>(opcional)</small></h4>
        <p class="dica">Condições dentro de um bloco valem em conjunto (E). Basta um bloco ser verdadeiro (OU entre blocos). Sem condições, a automação sempre executa.</p>
        <template v-for="(b, bi) in blocos" :key="b.uid">
          <div v-if="bi > 0" class="ou" aria-hidden="true">OU</div>
          <div class="bloco" role="group" :aria-label="`Bloco de condições ${bi + 1}`">
            <div class="bloco-head">
              <strong>Bloco {{ bi + 1 }}</strong>
              <span class="grow"></span>
              <button type="button" class="ico" :aria-label="`Duplicar bloco ${bi + 1}`" title="Duplicar bloco" @click="duplicarBloco(bi)"><i class="mdi mdi-content-copy"></i></button>
              <button type="button" class="ico perigo" :aria-label="`Remover bloco ${bi + 1}`" title="Remover bloco" @click="removerBloco(bi)"><i class="mdi mdi-delete-outline"></i></button>
            </div>
            <p v-if="!b.condicoes.length" class="dica">Bloco sem condições.</p>
            <template v-for="(c, ci) in b.condicoes" :key="ci">
              <div v-if="ci > 0" class="e" aria-hidden="true">E</div>
              <div class="cond">
                <select v-model="c.campo" :aria-label="`Campo da condição ${ci + 1}`" @change="trocouCampo(c)">
                  <option v-for="f in campos" :key="f.nome" :value="f.nome">{{ f.nome }}</option>
                </select>
                <select v-model="c.operador" :aria-label="`Operador da condição ${ci + 1}`">
                  <option v-for="o in operadores" :key="o.valor" :value="o.valor" :disabled="!o.implementado">
                    {{ o.nome }}{{ o.implementado ? '' : ' (em breve)' }}</option>
                </select>
                <template v-if="operadorCat(c.operador)?.precisaValor !== false">
                  <select v-if="tipoInput(c) === 'booleano'" v-model="c.valor" :aria-label="`Valor da condição ${ci + 1}`">
                    <option value="">Selecione...</option><option value="true">Sim</option><option value="false">Não</option>
                  </select>
                  <select v-else-if="tipoInput(c) === 'enum'" v-model="c.valor" :aria-label="`Valor da condição ${ci + 1}`">
                    <option value="">Selecione...</option>
                    <option v-for="op in campoCat(c.campo)?.opcoes ?? []" :key="op" :value="op">{{ op }}</option>
                  </select>
                  <input v-else-if="tipoInput(c) === 'data'" v-model="c.valor" type="date" :aria-label="`Valor da condição ${ci + 1}`" />
                  <input v-else-if="tipoInput(c) === 'numero'" v-model="c.valor" type="number" step="any" :aria-label="`Valor da condição ${ci + 1}`" />
                  <input v-else v-model="c.valor" :aria-label="`Valor da condição ${ci + 1}`" placeholder="Valor" />
                </template>
                <span v-else class="dica sem-valor">sem valor</span>
                <button type="button" class="ico perigo" :aria-label="`Remover condição ${ci + 1}`" @click="removerCondicao(b, ci)"><i class="mdi mdi-close"></i></button>
              </div>
            </template>
            <button type="button" class="link" @click="addCondicao(b)"><i class="mdi mdi-plus"></i> Adicionar condição</button>
          </div>
        </template>
        <button type="button" class="btn sec" @click="addBloco"><i class="mdi mdi-plus"></i> Adicionar bloco (OU)</button>
      </section>

      <!-- 4 Ações -->
      <section class="passo">
        <h4><span class="num">4</span> Ações</h4>
        <p class="dica">Executadas em sequência, de cima para baixo.</p>
        <p v-if="!acoes.length" class="vazio">Nenhuma ação adicionada.</p>
        <ol class="acoes">
          <li v-for="(a, i) in acoes" :key="a.uid" class="acao">
            <div class="acao-head">
              <span class="num sm">{{ i + 1 }}</span>
              <strong>{{ acaoCat(a.tipo)?.nome ?? a.tipo }}</strong>
              <span class="grow"></span>
              <button type="button" class="ico" :disabled="i === 0" :aria-label="`Mover ação ${i + 1} para cima`" @click="mover(i, -1)"><i class="mdi mdi-arrow-up"></i></button>
              <button type="button" class="ico" :disabled="i === acoes.length - 1" :aria-label="`Mover ação ${i + 1} para baixo`" @click="mover(i, 1)"><i class="mdi mdi-arrow-down"></i></button>
              <button type="button" class="ico perigo" :aria-label="`Remover ação ${i + 1}`" @click="removerAcao(i)"><i class="mdi mdi-delete-outline"></i></button>
            </div>

            <!-- EditarDados -->
            <div v-if="a.tipo === 'EditarDados'" class="cfg">
              <div v-for="(alt, k) in a.alteracoes" :key="k" class="cond">
                <select v-model="alt.campo" :aria-label="`Campo da alteração ${k + 1}`">
                  <option value="">Campo...</option>
                  <option v-for="f in campos" :key="f.nome" :value="f.nome">{{ f.nome }}</option>
                </select>
                <input v-model="alt.valor" :aria-label="`Novo valor da alteração ${k + 1}`" placeholder="Novo valor" />
                <button type="button" class="ico perigo" :aria-label="`Remover alteração ${k + 1}`" @click="a.alteracoes.splice(k, 1)"><i class="mdi mdi-close"></i></button>
              </div>
              <button type="button" class="link" @click="a.alteracoes.push({ campo: '', valor: '' })"><i class="mdi mdi-plus"></i> Adicionar campo</button>
            </div>

            <!-- Demais ações: formulário por parâmetro do catálogo -->
            <div v-else class="cfg">
              <label v-for="p in paramsSimples(a)" :key="p.chave" class="campo">
                <span>{{ rotuloParam(p) }}{{ p.obrigatorio ? ' *' : '' }}</span>
                <select v-if="a.tipo === 'CriarTarefa' && p.chave === 'tipo'" v-model="a.cfg[p.chave]">
                  <option v-for="t in tiposTarefa" :key="t" :value="t">{{ t }}</option>
                </select>
                <select v-else-if="a.tipo === 'ExecutarWebhook' && p.chave === 'metodo'" v-model="a.cfg[p.chave]">
                  <option v-for="m in metodosHttp" :key="m" :value="m">{{ m }}</option>
                </select>
                <select v-else-if="opcoesRef(p.chave).length" v-model="a.cfg[p.chave]">
                  <option value="">Selecione...</option>
                  <option v-for="o in opcoesRef(p.chave)" :key="o.id" :value="String(o.id)">{{ o.nome }}</option>
                </select>
                <input v-else-if="p.chave === 'prazo' || /id$/i.test(p.chave) || /int|num/i.test(p.tipo)" v-model="a.cfg[p.chave]" type="number" min="0" />
                <textarea v-else-if="p.chave === 'corpo' || p.chave === 'descricao'" v-model="a.cfg[p.chave]" rows="3"></textarea>
                <input v-else v-model="a.cfg[p.chave]" :type="p.chave === 'url' ? 'url' : 'text'" />
              </label>
              <label v-if="a.tipo === 'ExecutarWebhook'" class="campo"><span>Cabeçalhos (um por linha, "Nome: valor")</span>
                <textarea v-model="a.headersTexto" rows="2"></textarea></label>
            </div>
          </li>
        </ol>
        <div class="linha add-acao">
          <select v-model="novaAcaoTipo" aria-label="Tipo de ação a adicionar">
            <option value="">Escolha uma ação...</option>
            <option v-for="a in acoesDisponiveis" :key="a.valor" :value="a.valor" :disabled="!a.implementado">
              {{ a.nome }}{{ a.implementado ? '' : ' (em breve)' }}</option>
          </select>
          <button type="button" class="btn sec" :disabled="!novaAcaoTipo" @click="adicionarAcao"><i class="mdi mdi-plus"></i> Adicionar ação</button>
        </div>
        <p v-if="acoesDisponiveis.some((a) => !a.implementado)" class="aviso">
          <i class="mdi mdi-information-outline"></i> Ações "em breve" ainda não são executadas pelo motor.
        </p>
      </section>
    </form>
    <template #rodape>
      <div v-if="confirmandoSaida" class="sair" role="alertdialog" aria-label="Descartar alterações">
        <span>Descartar as alterações não salvas?</span>
        <button type="button" ref="continuarEl" class="btn sec" @click="confirmandoSaida = false">Continuar editando</button>
        <button type="button" class="btn danger" @click="descartar">Descartar</button>
      </div>
      <button type="button" class="btn sec" @click="fechar">Cancelar</button>
      <button type="button" class="btn pri" :disabled="salvando || carregando" @click="salvar">
        <i v-if="salvando" class="mdi mdi-loading mdi-spin"></i> Salvar
      </button>
    </template>
  </UiSlideOver>
</template>

<style scoped>
.estado { text-align: center; padding: 40px; color: var(--text-muted); }
.ed { display: flex; flex-direction: column; gap: 22px; }
.passo { display: flex; flex-direction: column; gap: 10px; }
.passo h4 { display: flex; align-items: center; gap: 8px; font-size: 14px; color: var(--text-primary); }
.passo h4 small { color: var(--text-muted); font-weight: 400; }
.num { width: 22px; height: 22px; border-radius: 50%; background: var(--accent); color: #fff; font-size: 12px; display: inline-flex; align-items: center; justify-content: center; flex-shrink: 0; }
.num.sm { width: 20px; height: 20px; font-size: 11px; }
.campo { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--text-muted); flex: 1; }
input, select, textarea { background: var(--bg-secondary); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); padding: 7px 9px; font-size: 13px; font-family: inherit; min-width: 0; }
input:focus, select:focus, textarea:focus, button:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
textarea { resize: vertical; }
.dica { font-size: 12px; color: var(--text-muted); }
.aviso { font-size: 12px; color: #f59e0b; display: flex; gap: 6px; align-items: flex-start; }
.vazio { font-size: 13px; color: var(--text-muted); padding: 10px; border: 1px dashed var(--border); border-radius: 8px; text-align: center; }
.erros { background: rgba(239, 68, 68, .1); border: 1px solid var(--danger); color: var(--danger); border-radius: 8px; padding: 10px 14px; font-size: 13px; }
.erros ul { margin: 6px 0 0 18px; }
.check { display: flex; gap: 8px; align-items: center; font-size: 13px; color: var(--text-secondary); cursor: pointer; }
.linha { display: flex; gap: 10px; align-items: flex-end; flex-wrap: wrap; }
.grow { flex: 1; }
.bloco { border: 1px solid var(--border); background: var(--bg-secondary); border-radius: 10px; padding: 10px; display: flex; flex-direction: column; gap: 8px; }
.bloco-head { display: flex; align-items: center; gap: 4px; font-size: 13px; }
.ou, .e { font-size: 11px; font-weight: 700; color: var(--accent); text-align: center; }
.e { text-align: left; padding-left: 4px; }
.cond { display: flex; gap: 6px; align-items: center; }
.cond > select:first-child { flex: 1.2; }
.cond > select, .cond > input { flex: 1; }
.sem-valor { flex: 1; }
.acoes { list-style: none; display: flex; flex-direction: column; gap: 10px; padding: 0; margin: 0; }
.acao { border: 1px solid var(--border); border-radius: 10px; background: var(--bg-secondary); padding: 10px; display: flex; flex-direction: column; gap: 8px; }
.acao-head { display: flex; align-items: center; gap: 8px; font-size: 13px; }
.cfg { display: flex; flex-direction: column; gap: 8px; }
.add-acao select { flex: 1; }
.ico { width: 28px; height: 28px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); cursor: pointer; display: inline-flex; align-items: center; justify-content: center; flex-shrink: 0; }
.ico:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.ico.perigo:hover:not(:disabled) { color: var(--danger); border-color: var(--danger); }
.ico:disabled { opacity: .3; cursor: default; }
.link { align-self: flex-start; background: none; border: 0; color: var(--accent); cursor: pointer; font-size: 12px; padding: 2px 0; }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px; border-radius: 8px; font-size: 13px; cursor: pointer; border: 1px solid var(--border); }
.btn.sec { background: var(--bg-surface); color: var(--text-secondary); align-self: flex-start; }
.btn.sec:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.btn.pri { background: var(--accent); border-color: var(--accent); color: #fff; }
.btn.pri:hover:not(:disabled) { background: var(--accent-hover); }
.btn.danger { background: var(--danger); border-color: var(--danger); color: #fff; }
.sair { display: flex; align-items: center; gap: 8px; margin-right: auto; font-size: 13px; color: var(--text-secondary); }
.btn:disabled { opacity: .5; cursor: default; }
</style>
