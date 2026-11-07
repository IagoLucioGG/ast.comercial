<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import UiTabs, { type AbaConfig } from '@/components/ui/UiTabs.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import PropostaItensTab from '@/components/propostas/PropostaItensTab.vue'
import PropostaParcelasTab from '@/components/propostas/PropostaParcelasTab.vue'
import PropostaDocumentoTab from '@/components/propostas/PropostaDocumentoTab.vue'
import { ApiError, odataAction, odataRequest, montarQuery } from '@/services/api'
import { useToast } from '@/composables/useToast'
import {
  listarTodos, numeroDaProposta, statusAprovacaoNome, statusAprovacaoVisual, paraInputData, deInputData,
  type Proposta, type ItemProposta, type ParcelaProposta, type Moeda,
} from '@/composables/usePropostaApi'
import { usePropostaTotais, formatarMoeda, arredondar, registrarSimbolos } from '@/composables/usePropostaTotais'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const id = computed(() => Number(route.params.id))
const proposta = ref<Proposta | null>(null)
const itens = ref<ItemProposta[]>([])
const parcelas = ref<ParcelaProposta[]>([])
const clienteNome = ref('')
const moeda = ref('')
const carregando = ref(true)
const erro = ref('')
const aba = ref('itens')
const versaoDoc = ref(0)
const modeloInicial = ref<number | null>(Number(route.query.modelo) || null)

// Campos monetários editáveis das condições (separados para cálculo em tempo real)
const desconto = ref<number>(0)
const frete = ref<number>(0)
const totais = usePropostaTotais(itens, desconto, frete)

const abas = computed<AbaConfig[]>(() => [
  { chave: 'itens', rotulo: 'Itens', icone: 'mdi-format-list-bulleted', contador: itens.value.length },
  { chave: 'parcelas', rotulo: 'Parcelas', icone: 'mdi-calendar-month-outline', contador: parcelas.value.length },
  { chave: 'condicoes', rotulo: 'Resumo / Condições', icone: 'mdi-clipboard-text-outline' },
  { chave: 'documento', rotulo: 'Documento', icone: 'mdi-file-document-outline' },
])

const status = computed(() => statusAprovacaoVisual(proposta.value?.StatusAprovacao))
const linkPublico = computed(() =>
  proposta.value?.Chave ? `${window.location.origin}/p/${proposta.value.Chave}` : '')

// ---------- carregamento ----------
async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const p = await odataRequest<Proposta>('GET', `Propostas(${id.value})`)
    proposta.value = p
    preencherForm(p)
    desconto.value = Number(p.Desconto ?? 0)
    frete.value = Number(p.CustoFrete ?? 0)
    const [its, pcs] = await Promise.all([
      listarTodos<ItemProposta>(`Propostas@Produtos${montarQuery({ $filter: `PropostaId eq ${id.value}`, $orderby: 'Ordem,Id' })}`),
      listarTodos<ParcelaProposta>(`Propostas@Parcelas${montarQuery({ $filter: `PropostaId eq ${id.value}`, $orderby: 'Numero,Id' })}`),
    ])
    itens.value = its
    parcelas.value = pcs
    carregarComplementos(p)
  } catch (e) {
    erro.value = e instanceof ApiError && e.status === 404 ? 'Proposta não encontrada.' : (e instanceof Error ? e.message : 'Falha ao carregar a proposta.')
  } finally {
    carregando.value = false
  }
}

async function carregarComplementos(p: Proposta) {
  clienteNome.value = ''
  if (p.ClienteId) {
    try {
      const c = await odataRequest<{ Nome: string }>('GET', `Clientes(${p.ClienteId})${montarQuery({ $select: 'Nome' })}`)
      clienteNome.value = c.Nome
    } catch { /* o cabeçalho funciona sem o nome */ }
  }
  try {
    // Moeda da proposta (ou a padrão da empresa); os símbolos vêm do cadastro de moedas
    const moedas = await listarTodos<Moeda>(`Moedas${montarQuery({ $orderby: 'Id' })}`)
    registrarSimbolos(moedas)
    const m = moedas.find((x) => x.Id === p.MoedaId) ?? (p.MoedaId ? undefined : moedas.find((x) => x.Padrao))
    moeda.value = m?.Codigo ?? ''
  } catch { moeda.value = '' /* sem moeda conhecida mostra só o número */ }
}

// ---------- sincronização de totais ----------
const servidorRecalcula = ref<boolean | null>(null)
const divergencia = ref(false)
let temporizador: ReturnType<typeof setTimeout> | null = null

function agendarSincronia() {
  versaoDoc.value++
  if (temporizador) clearTimeout(temporizador)
  temporizador = setTimeout(sincronizar, 600)
}

/** Salva o total calculado no cliente e pede ao servidor o recálculo final (se existir). */
async function sincronizar() {
  if (!proposta.value) return
  try {
    const r = await odataAction<Proposta | null>('Propostas', id.value, 'Recalcular')
    servidorRecalcula.value = true
    if (r && typeof r.Valor === 'number') {
      proposta.value.Valor = r.Valor
      divergencia.value = Math.abs(arredondar(r.Valor) - totais.total.value) > 0.005
    }
  } catch (e) {
    if (e instanceof ApiError && [404, 405, 501].includes(e.status)) {
      // Recalcular ainda não existe no servidor: grava o total do navegador
      servidorRecalcula.value = false
      try {
        await odataRequest('PATCH', `Propostas(${id.value})`, { Valor: totais.total.value })
        proposta.value.Valor = totais.total.value
      } catch (e2) {
        toast.error(e2 instanceof Error ? e2.message : 'Não foi possível gravar o valor total.')
      }
    } else {
      toast.error(e instanceof Error ? e.message : 'Não foi possível recalcular a proposta.')
    }
  }
}

onBeforeUnmount(() => { if (temporizador) clearTimeout(temporizador) })

// ---------- ações ----------
const executando = ref(false)

async function acao(nome: 'Aprovar' | 'Rejeitar') {
  if (!proposta.value) return
  executando.value = true
  try {
    await odataAction('Propostas', id.value, nome)
    proposta.value.StatusAprovacao = nome === 'Aprovar' ? 'Aprovada' : 'Rejeitada'
    toast.success(nome === 'Aprovar' ? 'Proposta aprovada.' : 'Proposta rejeitada.')
  } catch (e) {
    toast.error(e instanceof Error ? e.message : `Não foi possível ${nome.toLowerCase()} a proposta.`)
  } finally {
    executando.value = false
  }
}

const compartilhando = ref(false)
async function compartilhar() {
  if (!proposta.value) return
  compartilhando.value = true
  try {
    const r = await odataAction<{ chave?: string; url?: string }>('Propostas', id.value, 'Compartilhar')
    if (!r?.chave) throw new Error('O servidor não devolveu a chave de compartilhamento.')
    proposta.value.Chave = r.chave
    proposta.value.Compartilhada = true
    toast.success('Link gerado.')
  } catch (e) {
    if (e instanceof ApiError && [404, 405, 501].includes(e.status)) {
      toast.error('O compartilhamento ainda não está disponível no servidor.')
    } else {
      toast.error(e instanceof Error ? e.message : 'Não foi possível gerar o link.')
    }
  } finally {
    compartilhando.value = false
  }
}

async function copiarLink() {
  try {
    await navigator.clipboard.writeText(linkPublico.value)
    toast.success('Link copiado.')
  } catch {
    toast.warning('Não foi possível copiar automaticamente. Selecione o link e copie.')
  }
}

// ---------- condições ----------
const form = ref({
  FormaPagamento: '', PrazoEntrega: '', ModalFrete: '', Observacoes: '', Validade: '', NumeroParcelas: '' as string | number,
})
function preencherForm(p: Proposta) {
  form.value = {
    FormaPagamento: p.FormaPagamento ?? '',
    PrazoEntrega: p.PrazoEntrega ?? '',
    ModalFrete: p.ModalFrete ?? '',
    Observacoes: p.Observacoes ?? '',
    Validade: paraInputData(p.DataExpiracao),
    NumeroParcelas: p.NumeroParcelas ?? '',
  }
}

const salvandoCondicoes = ref(false)
async function salvarCondicoes() {
  if (!proposta.value) return
  salvandoCondicoes.value = true
  try {
    const f = form.value
    const corpo = {
      Desconto: arredondar(Number(desconto.value) || 0),
      CustoFrete: arredondar(Number(frete.value) || 0),
      FormaPagamento: f.FormaPagamento.trim() || null,
      PrazoEntrega: f.PrazoEntrega.trim() || null,
      ModalFrete: f.ModalFrete.trim() || null,
      Observacoes: f.Observacoes.trim() || null,
      DataExpiracao: deInputData(f.Validade),
      NumeroParcelas: f.NumeroParcelas === '' ? null : Number(f.NumeroParcelas),
    }
    await odataRequest('PATCH', `Propostas(${id.value})`, corpo)
    Object.assign(proposta.value, corpo)
    toast.success('Condições salvas.')
    agendarSincronia()
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível salvar as condições.')
  } finally {
    salvandoCondicoes.value = false
  }
}

function numeroDe(ev: Event): number {
  const v = Number((ev.target as HTMLInputElement).value)
  return Number.isFinite(v) && v >= 0 ? v : 0
}

const numeroExibido = computed(() => {
  const p = proposta.value
  if (!p) return ''
  const n = numeroDaProposta(p)
  return p.Revisao ? `${n} · rev. ${p.Revisao}` : n
})

// ---------- resposta do cliente (aceite/recusa pela página pública) ----------
const respostaNome = computed(() => {
  const v = proposta.value?.StatusResposta
  const nomes = ['Aguardando', 'Aceita', 'Recusada']
  if (typeof v === 'number') return nomes[v] ?? 'Aguardando'
  return v || 'Aguardando'
})
const respostaVisual = computed(() => {
  switch (respostaNome.value) {
    case 'Aceita': return { rotulo: 'Aceita pelo cliente', tom: 'sucesso' as const }
    case 'Recusada': return { rotulo: 'Recusada pelo cliente', tom: 'perigo' as const }
    default: return { rotulo: 'Aguardando resposta', tom: 'neutro' as const }
  }
})
function dataHoraBR(iso?: string | null): string {
  if (!iso) return '-'
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleString('pt-BR')
}

onMounted(carregar)
watch(id, carregar)
</script>

<template>
  <div class="detalhe">
    <button type="button" class="voltar" @click="router.push('/propostas')"><i class="mdi mdi-arrow-left"></i> Propostas</button>

    <p v-if="carregando" class="estado">Carregando proposta...</p>
    <div v-else-if="erro" class="estado erro" role="alert">
      <p>{{ erro }}</p>
      <button type="button" class="btn" @click="carregar">Tentar novamente</button>
    </div>

    <template v-else-if="proposta">
      <header class="cabecalho">
        <div class="titulos">
          <span class="numero">{{ numeroExibido }}</span>
          <h1>{{ proposta.Titulo || 'Sem título' }}</h1>
          <div class="meta">
            <span v-if="clienteNome"><i class="mdi mdi-domain"></i> {{ clienteNome }}</span>
            <UiBadge :tom="status.tom">{{ status.rotulo }}</UiBadge>
            <UiBadge v-if="proposta.Compartilhada" tom="info">Compartilhada</UiBadge>
          </div>
        </div>
        <div class="valor">
          <small>Valor total</small>
          <strong>{{ formatarMoeda(totais.total.value, moeda) }}</strong>
        </div>
        <div class="acoes">
          <button type="button" class="btn sucesso" :disabled="executando || statusAprovacaoNome(proposta.StatusAprovacao) === 'Aprovada'" @click="acao('Aprovar')"><i class="mdi mdi-check"></i> Aprovar</button>
          <button type="button" class="btn perigo" :disabled="executando || statusAprovacaoNome(proposta.StatusAprovacao) === 'Rejeitada'" @click="acao('Rejeitar')"><i class="mdi mdi-close"></i> Rejeitar</button>
          <button type="button" class="btn primario" :disabled="compartilhando" @click="compartilhar"><i class="mdi mdi-share-variant"></i> Compartilhar</button>
        </div>
      </header>

      <section class="resposta" aria-label="Resposta do cliente">
        <h2>Resposta do cliente</h2>
        <div class="resposta-corpo">
          <UiBadge :tom="respostaVisual.tom">{{ respostaVisual.rotulo }}</UiBadge>
          <template v-if="respostaNome !== 'Aguardando'">
            <span><small>Respondida em</small> {{ dataHoraBR(proposta.RespondidaEm) }}</span>
            <span v-if="proposta.RespondidaPorNome"><small>Por</small> {{ proposta.RespondidaPorNome }}</span>
            <span v-if="proposta.RespondidaPorEmail"><small>E-mail</small> {{ proposta.RespondidaPorEmail }}</span>
            <span v-if="respostaNome === 'Recusada'" class="motivo"><small>Motivo</small> {{ proposta.MotivoRecusa || 'Não informado' }}</span>
          </template>
          <span v-else class="mudo">O cliente ainda não respondeu{{ proposta.Compartilhada ? '.' : ' (a proposta ainda não foi compartilhada).' }}</span>
        </div>
      </section>

      <div v-if="linkPublico" class="link">
        <i class="mdi mdi-link-variant"></i>
        <input :value="linkPublico" readonly aria-label="Link público da proposta" @focus="($event.target as HTMLInputElement).select()" />
        <button type="button" class="btn" @click="copiarLink"><i class="mdi mdi-content-copy"></i> Copiar</button>
      </div>

      <UiTabs v-model="aba" :abas="abas" />

      <section class="painel">
        <PropostaItensTab v-if="aba === 'itens'" v-model="itens" :proposta-id="id" :moeda="moeda" @mudou="agendarSincronia" />
        <PropostaParcelasTab v-else-if="aba === 'parcelas'" v-model="parcelas" :proposta-id="id" :moeda="moeda" :total="totais.total.value" @mudou="versaoDoc++" />

        <div v-else-if="aba === 'condicoes'" class="condicoes">
          <div class="grade">
            <label class="campo"><span>Desconto geral ({{ moeda }})</span>
              <input type="number" min="0" step="0.01" :value="desconto" @input="desconto = numeroDe($event)" /></label>
            <label class="campo"><span>Frete ({{ moeda }})</span>
              <input type="number" min="0" step="0.01" :value="frete" @input="frete = numeroDe($event)" /></label>
            <label class="campo"><span>Modalidade do frete</span>
              <input v-model="form.ModalFrete" type="text" maxlength="60" /></label>
            <label class="campo"><span>Forma de pagamento</span>
              <input v-model="form.FormaPagamento" type="text" maxlength="120" /></label>
            <label class="campo"><span>Nº de parcelas</span>
              <input v-model="form.NumeroParcelas" type="number" min="1" max="120" step="1" /></label>
            <label class="campo"><span>Prazo de entrega</span>
              <input v-model="form.PrazoEntrega" type="text" maxlength="120" /></label>
            <label class="campo"><span>Validade da proposta</span>
              <input v-model="form.Validade" type="date" /></label>
          </div>
          <label class="campo"><span>Observações</span>
            <textarea v-model="form.Observacoes" rows="4" maxlength="4000"></textarea></label>

          <dl class="resumo">
            <div><dt>Subtotal</dt><dd>{{ formatarMoeda(totais.subtotal.value, moeda) }}</dd></div>
            <div><dt>Desconto</dt><dd>− {{ formatarMoeda(totais.desconto.value, moeda) }}</dd></div>
            <div><dt>Frete</dt><dd>{{ formatarMoeda(totais.frete.value, moeda) }}</dd></div>
            <div class="total"><dt>Total</dt><dd>{{ formatarMoeda(totais.total.value, moeda) }}</dd></div>
          </dl>
          <div><button type="button" class="btn primario" :disabled="salvandoCondicoes" @click="salvarCondicoes">{{ salvandoCondicoes ? 'Salvando...' : 'Salvar condições' }}</button></div>
        </div>

        <PropostaDocumentoTab v-else :proposta-id="id" :modelo-inicial="modeloInicial" :versao="versaoDoc" />
      </section>

      <footer v-if="aba === 'itens'" class="totais" aria-live="polite">
        <div><span>Subtotal</span><b>{{ formatarMoeda(totais.subtotal.value, moeda) }}</b></div>
        <div><span>Desconto</span><b>− {{ formatarMoeda(totais.desconto.value, moeda) }}</b></div>
        <div><span>Frete</span><b>{{ formatarMoeda(totais.frete.value, moeda) }}</b></div>
        <div class="total"><span>Total</span><b>{{ formatarMoeda(totais.total.value, moeda) }}</b></div>
        <p v-if="servidorRecalcula === false" class="nota">Total calculado no navegador; o servidor ainda não oferece recálculo.</p>
        <p v-if="divergencia" class="nota aviso">O servidor calculou um valor diferente: {{ formatarMoeda(proposta.Valor, moeda) }}.</p>
      </footer>
    </template>
  </div>
</template>

<style scoped>
.detalhe { display: flex; flex-direction: column; gap: 14px; padding: 4px; color: var(--text-primary); }
.voltar { align-self: flex-start; background: none; border: 0; color: var(--text-secondary); font-size: 13px; cursor: pointer; padding: 0; }
.voltar:hover { color: var(--accent); }
.estado { color: var(--text-secondary); font-size: 14px; }
.estado.erro { color: var(--danger); }
.cabecalho { display: flex; align-items: flex-start; gap: 18px; flex-wrap: wrap; padding: 16px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; }
.titulos { flex: 1 1 280px; min-width: 0; }
.numero { font-size: 12px; color: var(--text-muted); }
h1 { margin: 2px 0 8px; font-size: 20px; font-weight: 600; overflow-wrap: anywhere; }
.meta { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; font-size: 13px; color: var(--text-secondary); }
.valor { text-align: right; }
.valor small { display: block; font-size: 11px; color: var(--text-muted); text-transform: uppercase; letter-spacing: .04em; }
.valor strong { font-size: 22px; color: var(--accent); font-variant-numeric: tabular-nums; }
.acoes { display: flex; gap: 8px; flex-wrap: wrap; width: 100%; }
.resposta { padding: 12px 16px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; }
.resposta h2 { margin: 0 0 8px; font-size: 13px; font-weight: 600; color: var(--text-secondary); }
.resposta-corpo { display: flex; flex-wrap: wrap; align-items: center; gap: 8px 18px; font-size: 13px; }
.resposta-corpo small { color: var(--text-muted); margin-right: 4px; text-transform: uppercase; font-size: 10px; letter-spacing: .04em; }
.resposta-corpo .motivo { flex-basis: 100%; overflow-wrap: anywhere; }
.resposta-corpo .mudo { color: var(--text-muted); }
.link { display: flex; align-items: center; gap: 8px; padding: 8px 12px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 8px; }
.link input { flex: 1; min-width: 0; background: transparent; border: 0; color: var(--text-primary); font-size: 13px; }
.link input:focus { outline: none; }
.painel { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 16px; }
.condicoes { display: flex; flex-direction: column; gap: 14px; }
.grade { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 14px; }
.campo { display: flex; flex-direction: column; gap: 5px; }
.campo > span { font-size: 12px; color: var(--text-secondary); }
.campo input, .campo textarea { padding: 7px 10px; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); font-size: 13px; font-family: inherit; }
.campo input:focus, .campo textarea:focus { outline: none; border-color: var(--accent); }
.resumo { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 10px; margin: 0; }
.resumo div { padding: 10px 12px; background: var(--bg-elevated); border-radius: 8px; }
.resumo dt { font-size: 11px; color: var(--text-muted); text-transform: uppercase; }
.resumo dd { margin: 2px 0 0; font-size: 15px; font-variant-numeric: tabular-nums; }
.resumo .total dd { color: var(--accent); font-weight: 700; }
.totais { display: flex; flex-wrap: wrap; gap: 18px; justify-content: flex-end; align-items: baseline; padding: 10px 16px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; font-size: 13px; }
.totais span { color: var(--text-secondary); margin-right: 6px; }
.totais b { font-variant-numeric: tabular-nums; }
.totais .total b { color: var(--accent); font-size: 16px; }
.nota { flex-basis: 100%; margin: 0; text-align: right; font-size: 11px; color: var(--text-muted); }
.nota.aviso { color: #f59e0b; }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; }
.btn:hover:not(:disabled) { border-color: var(--accent); }
.btn:disabled { opacity: .5; cursor: default; }
.btn.primario { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.btn.sucesso { color: #22c55e; }
.btn.perigo { color: var(--danger); }
@media (max-width: 640px) { .valor { text-align: left; width: 100%; } }
</style>

