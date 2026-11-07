<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { odataRequest, odataTodos, montarQuery } from '@/services/api'
import {
  useEntidadeDetalhe, campo, lista, mensagemErro, formatarValorMoeda, formatarData, carregarMoedas, type Registro, type MoedaInfo,
} from '@/composables/useEntidadeDetalhe'
import { useAcoesNegocio } from '@/composables/useAcoesNegocio'
import { useToast } from '@/composables/useToast'
import UiTabs, { type AbaConfig } from '@/components/ui/UiTabs.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import PerderNegocioModal from '@/components/negocio/PerderNegocioModal.vue'
import DadosEntidade, { type CampoDef } from '@/components/detalhe/DadosEntidade.vue'
import ContatosEntidade from '@/components/detalhe/ContatosEntidade.vue'
import AtividadesEntidade from '@/components/detalhe/AtividadesEntidade.vue'
import PropostasEntidade from '@/components/detalhe/PropostasEntidade.vue'
import TimelineEntidade from '@/components/detalhe/TimelineEntidade.vue'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'

const route = useRoute()
const toast = useToast()
const id = computed(() => Number(route.params.id))

const EXPANDIR = 'Cliente($select=Id,Nome),Etapa($select=Id,Nome,Ordem),Status($select=Id,Nome,TipoBase),Funil($select=Id,Nome),MotivoPerda($select=Id,Nome),PessoaContato($select=Id,Nome),Responsavel($select=Id,Nome)'
const { registro: negocio, carregando, erro, naoEncontrado, carregar } = useEntidadeDetalhe('Negocios', EXPANDIR)
const { ganhar, reabrir, processando } = useAcoesNegocio()

interface EtapaItem { id: number; nome: string; ordem: number }
const etapas = ref<EtapaItem[]>([])
const statusLista = ref<Registro[]>([])
const moedas = ref<MoedaInfo[]>([])
const usuarios = ref<{ id: number; nome: string }[]>([])
const editandoResp = ref(false)
const salvandoResp = ref(false)
const contatos = ref<{ id: number; nome: string }[]>([])
const movendo = ref<number | null>(null)
const perderAberto = ref(false)
const abaAtiva = ref('dados')
const chaveTimeline = ref(0)
const auxPronto = ref(false)

const abas: AbaConfig[] = [
  { chave: 'dados', rotulo: 'Dados', icone: 'mdi-form-select' },
  { chave: 'contatos', rotulo: 'Contatos', icone: 'mdi-account-multiple-outline' },
  { chave: 'atividades', rotulo: 'Atividades', icone: 'mdi-calendar-check-outline' },
  { chave: 'propostas', rotulo: 'Propostas', icone: 'mdi-file-document-outline' },
  { chave: 'historico', rotulo: 'Histórico', icone: 'mdi-history' },
]

const tipoStatus = computed<'aberto' | 'ganho' | 'perdido'>(() => {
  const t = campo(campo(negocio.value, 'Status'), 'TipoBase')
  if (t === 1 || t === 'Ganho') return 'ganho'
  if (t === 2 || t === 'Perdido') return 'perdido'
  return 'aberto'
})
// O servidor marca FechadoEm ao ganhar/perder; usamos também esse sinal para saber se está fechado.
const fechado = computed(() => tipoStatus.value !== 'aberto' || !!campo(negocio.value, 'FechadoEm'))
const rotuloStatus = computed(() => {
  if (tipoStatus.value === 'ganho') return 'Ganho'
  if (tipoStatus.value === 'perdido') return 'Perdido'
  return fechado.value ? 'Fechado' : (campo<string>(campo(negocio.value, 'Status'), 'Nome') ?? 'Aberto')
})
const tomStatus = computed(() => (tipoStatus.value === 'ganho' ? 'sucesso' : tipoStatus.value === 'perdido' ? 'perigo' : fechado.value ? 'neutro' : 'info'))

const moeda = computed(() => moedas.value.find((m) => m.id === campo(negocio.value, 'MoedaId')))
const valorFormatado = computed(() => formatarValorMoeda(campo(negocio.value, 'Valor'), moeda.value?.simbolo, moeda.value?.codigo))
const etapaAtualId = computed(() => campo<number>(negocio.value, 'EtapaId'))
const indiceEtapa = computed(() => etapas.value.findIndex((e) => e.id === etapaAtualId.value))
const clienteId = computed(() => campo<number>(negocio.value, 'ClienteId'))
const responsavel = computed(() => campo<string>(campo(negocio.value, 'Responsavel'), 'Nome'))
const responsavelId = computed(() => campo<number>(negocio.value, 'ResponsavelId') ?? null)

const nomesTimeline = computed(() => ({
  EtapaId: Object.fromEntries(etapas.value.map((e) => [e.id, e.nome])),
  StatusId: Object.fromEntries(statusLista.value.map((s) => [Number(campo(s, 'Id')), String(campo(s, 'Nome'))])),
  MoedaId: Object.fromEntries(moedas.value.map((m) => [m.id, m.codigo])),
  PessoaContatoId: Object.fromEntries(contatos.value.map((c) => [c.id, c.nome])),
  ClienteId: clienteId.value ? { [clienteId.value]: String(campo(campo(negocio.value, 'Cliente'), 'Nome') ?? `#${clienteId.value}`) } : {},
}))

const camposPadrao = computed<CampoDef[]>(() => [
  { nome: 'Titulo', rotulo: 'Título', tipo: 'texto', obrigatorio: true },
  { nome: 'Valor', rotulo: 'Valor', tipo: 'moeda' },
  { nome: 'MoedaId', rotulo: 'Moeda', tipo: 'select', opcoes: moedas.value.map((m) => ({ id: m.id, nome: `${m.codigo} - ${m.nome}` })) },
  { nome: 'PessoaContatoId', rotulo: 'Contato principal', tipo: 'select', opcoes: contatos.value.map((c) => ({ id: c.id, nome: c.nome })) },
  { nome: 'PrevisaoFechamento', rotulo: 'Previsão de fechamento', tipo: 'data' },
  { nome: 'FechadoEm', rotulo: 'Fechado em', tipo: 'data', somenteLeitura: true },
])

async function carregarAuxiliares() {
  const neg = negocio.value
  if (!neg) return
  const funilId = campo<number>(neg, 'FunilId')
  const [e, s, m, c] = await Promise.allSettled([
    odataRequest<unknown>('GET', `Negocios@Etapas${montarQuery({ $filter: `FunilId eq ${funilId}`, $orderby: 'Ordem asc' })}`),
    odataRequest<unknown>('GET', 'Negocios@Status'),
    carregarMoedas(),
    odataRequest<unknown>('GET', `PessoasContato${montarQuery({ $filter: `ClienteId eq ${campo(neg, 'ClienteId')}`, $select: 'Id,Nome', $orderby: 'Nome asc', $top: 100 })}`),
  ])
  if (e.status === 'fulfilled') {
    etapas.value = lista<Registro>(e.value)
      .map((x) => ({ id: Number(campo(x, 'Id')), nome: String(campo(x, 'Nome')), ordem: Number(campo(x, 'Ordem') ?? 0) }))
      .sort((a, b) => a.ordem - b.ordem)
  }
  if (s.status === 'fulfilled') statusLista.value = lista<Registro>(s.value)
  if (m.status === 'fulfilled') moedas.value = m.value
  if (c.status === 'fulfilled') contatos.value = lista<Registro>(c.value).map((x) => ({ id: Number(campo(x, 'Id')), nome: String(campo(x, 'Nome')) }))
}

async function iniciar() {
  etapas.value = []
  auxPronto.value = false
  await carregar(id.value)
  if (negocio.value) await carregarAuxiliares()
  auxPronto.value = true
}

async function recarregar() {
  await carregar(id.value, true)
  chaveTimeline.value++
}

async function irParaEtapa(e: EtapaItem) {
  if (fechado.value || movendo.value !== null || e.id === etapaAtualId.value) return
  movendo.value = e.id
  try {
    await odataRequest('PATCH', `Negocios(${id.value})`, { EtapaId: e.id })
    toast.success(`Negócio movido para "${e.nome}".`)
    await recarregar()
  } catch (err) {
    // 422 = regra do funil (ex.: não pode voltar etapa). A tela permanece como estava.
    toast.error(mensagemErro(err, 'Não foi possível mudar a etapa'))
  } finally {
    movendo.value = null
  }
}

async function iniciarTrocaResp() {
  editandoResp.value = true
  if (usuarios.value.length) return
  try {
    const todos = await odataTodos<Registro>(`Usuarios${montarQuery({ $select: 'Id,Nome', $filter: 'Ativo eq true', $orderby: 'Nome asc' })}`)
    usuarios.value = todos.map((u) => ({ id: Number(campo(u, 'Id')), nome: String(campo(u, 'Nome')) }))
  } catch (e) {
    editandoResp.value = false
    toast.error(mensagemErro(e, 'Não foi possível carregar os usuários'))
  }
}

async function trocarResponsavel(valor: string) {
  const novo = valor === '' ? null : Number(valor)
  if (novo === responsavelId.value) { editandoResp.value = false; return }
  salvandoResp.value = true
  try {
    await odataRequest('PATCH', `Negocios(${id.value})`, { ResponsavelId: novo })
    toast.success('Responsável atualizado.')
    editandoResp.value = false
    await recarregar()
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível trocar o responsável'))
  } finally {
    salvandoResp.value = false
  }
}

async function aoGanhar() { if (await ganhar(id.value)) await recarregar() }
async function aoReabrir() { if (await reabrir(id.value)) await recarregar() }
async function aoPerder() { await recarregar() }

function estadoEtapa(i: number): 'feita' | 'atual' | 'futura' {
  if (i === indiceEtapa.value) return 'atual'
  return i < indiceEtapa.value ? 'feita' : 'futura'
}

watch(id, () => { abaAtiva.value = 'dados'; iniciar() })
onMounted(iniciar)
</script>

<template>
  <div class="nd">
    <DetalheEstado :carregando="carregando" :erro="erro" @tentar="iniciar">
      <div v-if="naoEncontrado" class="nd-nf" role="alert">
        <i class="mdi mdi-file-question-outline"></i>
        <h2>Negócio não encontrado</h2>
        <p>O negócio solicitado não existe ou você não tem acesso a ele.</p>
        <RouterLink class="nd-voltar" to="/negocios"><i class="mdi mdi-arrow-left"></i> Voltar para negócios</RouterLink>
      </div>

      <template v-else-if="negocio">
        <nav class="nd-trilha" aria-label="Navegação">
          <RouterLink to="/negocios"><i class="mdi mdi-arrow-left"></i> Negócios</RouterLink>
        </nav>

        <header class="nd-cab">
          <div class="nd-cab-topo">
            <div class="nd-titulo">
              <h1>{{ campo(negocio, 'Titulo') }}</h1>
              <UiBadge :tom="tomStatus">{{ rotuloStatus }}</UiBadge>
            </div>
            <div class="nd-acoes">
              <template v-if="!fechado">
                <button class="nd-btn nd-ganhar" :disabled="processando" @click="aoGanhar"><i class="mdi mdi-trophy-outline"></i> Ganhar</button>
                <button class="nd-btn nd-perder" :disabled="processando" @click="perderAberto = true"><i class="mdi mdi-close-circle-outline"></i> Perder</button>
              </template>
              <button v-else class="nd-btn" :disabled="processando" @click="aoReabrir"><i class="mdi mdi-restore"></i> Reabrir</button>
            </div>
          </div>

          <dl class="nd-info">
            <div>
              <dt>Cliente</dt>
              <dd>
                <RouterLink v-if="clienteId" class="nd-link" :to="`/clientes/${clienteId}`">{{ campo(campo(negocio, 'Cliente'), 'Nome') ?? `Cliente #${clienteId}` }}</RouterLink>
                <span v-else>-</span>
              </dd>
            </div>
            <div><dt>Valor</dt><dd class="nd-valor">{{ valorFormatado }}</dd></div>
            <div>
              <dt><label for="nd-resp">Responsável</label></dt>
              <dd v-if="!editandoResp" class="nd-resp">
                <span>{{ responsavel ?? 'Não definido' }}</span>
                <button type="button" class="nd-resp-btn" aria-label="Trocar responsável" title="Trocar responsável" @click="iniciarTrocaResp"><i class="mdi mdi-pencil-outline"></i></button>
              </dd>
              <dd v-else class="nd-resp">
                <select id="nd-resp" class="nd-select" :value="responsavelId ?? ''" :disabled="salvandoResp" @change="trocarResponsavel(($event.target as HTMLSelectElement).value)" @keydown.esc="editandoResp = false">
                  <option value="">Sem responsável</option>
                  <option v-for="u in usuarios" :key="u.id" :value="u.id">{{ u.nome }}</option>
                </select>
                <button type="button" class="nd-resp-btn" aria-label="Cancelar troca de responsável" :disabled="salvandoResp" @click="editandoResp = false"><i class="mdi mdi-close"></i></button>
              </dd>
            </div>
            <div><dt>Funil</dt><dd>{{ campo(campo(negocio, 'Funil'), 'Nome') ?? '-' }}</dd></div>
            <div><dt>Previsão</dt><dd>{{ formatarData(campo(negocio, 'PrevisaoFechamento')) }}</dd></div>
            <div v-if="tipoStatus === 'perdido' && campo(negocio, 'MotivoPerda')"><dt>Motivo da perda</dt><dd>{{ campo(campo(negocio, 'MotivoPerda'), 'Nome') }}</dd></div>
          </dl>

          <ol v-if="etapas.length" class="nd-stepper" aria-label="Etapas do funil">
            <li v-for="(e, i) in etapas" :key="e.id" :class="estadoEtapa(i)">
              <button
                type="button"
                class="nd-etapa"
                :disabled="fechado || movendo !== null"
                :aria-current="estadoEtapa(i) === 'atual' ? 'step' : undefined"
                :title="fechado ? 'Reabra o negócio para mudar de etapa' : `Mover para ${e.nome}`"
                @click="irParaEtapa(e)"
              >
                <i v-if="movendo === e.id" class="mdi mdi-loading mdi-spin"></i>
                <i v-else-if="estadoEtapa(i) === 'feita'" class="mdi mdi-check"></i>
                <span v-else class="nd-num">{{ i + 1 }}</span>
                <span class="nd-etapa-nome">{{ e.nome }}</span>
              </button>
            </li>
          </ol>
        </header>

        <UiTabs v-model="abaAtiva" :abas="abas" />

        <section class="nd-aba" :aria-label="abas.find((a) => a.chave === abaAtiva)?.rotulo">
          <DetalheEstado v-if="abaAtiva === 'dados' && !auxPronto" carregando />
          <DadosEntidade v-else-if="abaAtiva === 'dados'" entidade="Negocio" endpoint="Negocios" :registro="negocio" :padrao="camposPadrao" :moedas="moedas" @salvo="recarregar" />
          <template v-else-if="abaAtiva === 'contatos'">
            <ContatosEntidade v-if="clienteId" :cliente-id="clienteId" :principal-id="campo(negocio, 'PessoaContatoId')" />
          </template>
          <AtividadesEntidade v-else-if="abaAtiva === 'atividades'" endpoint="Negocios" :pai-id="id" :vinculo="{ NegocioId: id, ...(clienteId ? { ClienteId: clienteId } : {}) }" @alterado="chaveTimeline++" />
          <PropostasEntidade v-else-if="abaAtiva === 'propostas'" filtro-campo="NegocioId" :pai-id="id" :nova-query="{ negocioId: id, ...(clienteId ? { clienteId } : {}) }" />
          <TimelineEntidade v-else :key="chaveTimeline" entidade="Negocio" endpoint="Negocios" :registro-id="id" :nomes="nomesTimeline" />
        </section>

        <PerderNegocioModal v-model="perderAberto" :negocio-id="id" :titulo="campo(negocio, 'Titulo')" @perdido="aoPerder" />
      </template>
    </DetalheEstado>
  </div>
</template>

<style scoped>
.nd { max-width: 1100px; margin: 0 auto; }
.nd-trilha { margin-bottom: 10px; font-size: 13px; }
.nd-trilha a, .nd-voltar { color: var(--text-secondary); text-decoration: none; display: inline-flex; align-items: center; gap: 4px; }
.nd-trilha a:hover, .nd-voltar:hover { color: var(--accent); }
.nd-nf { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 60px 16px; color: var(--text-secondary); text-align: center; }
.nd-nf .mdi { font-size: 44px; color: var(--text-muted); }
.nd-nf h2 { color: var(--text-primary); font-size: 18px; }
.nd-cab { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 14px; padding: 18px 20px; margin-bottom: 16px; }
.nd-cab-topo { display: flex; justify-content: space-between; align-items: flex-start; gap: 12px; flex-wrap: wrap; }
.nd-titulo { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; min-width: 0; }
.nd-titulo h1 { font-size: 20px; font-weight: 600; color: var(--text-primary); margin: 0; overflow-wrap: anywhere; }
.nd-acoes { display: flex; gap: 8px; }
.nd-btn { display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-secondary); font-size: 13px; font-weight: 600; cursor: pointer; }
.nd-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.nd-btn:disabled { opacity: .5; cursor: not-allowed; }
.nd-ganhar { border-color: rgba(34, 197, 94, .5); color: #22c55e; }
.nd-perder { border-color: rgba(239, 68, 68, .5); color: var(--danger); }
.nd-info { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 12px 20px; margin: 16px 0 0; }
.nd-info dt { font-size: 11px; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); margin-bottom: 3px; }
.nd-info dd { margin: 0; font-size: 14px; color: var(--text-primary); overflow-wrap: anywhere; }
.nd-info dt label { cursor: pointer; }
.nd-resp { display: flex; align-items: center; gap: 6px; }
.nd-resp-btn { width: 24px; height: 24px; border: 1px solid transparent; border-radius: 6px; background: transparent; color: var(--text-muted); cursor: pointer; font-size: 14px; }
.nd-resp-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.nd-select { min-width: 0; flex: 1; padding: 5px 8px; border: 1px solid var(--border); border-radius: 6px; background: var(--bg-primary); color: var(--text-primary); font-size: 13px; }
.nd-valor { font-weight: 600; color: var(--accent) !important; }
.nd-link { color: var(--accent); text-decoration: none; }
.nd-link:hover { text-decoration: underline; }
.nd-stepper { list-style: none; display: flex; gap: 4px; margin: 18px 0 0; padding: 0; overflow-x: auto; }
.nd-stepper li { flex: 1 1 0; min-width: 110px; }
.nd-etapa { width: 100%; display: flex; align-items: center; justify-content: center; gap: 8px; padding: 9px 12px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-muted); font-size: 12px; font-weight: 600; cursor: pointer; }
.nd-etapa:disabled { cursor: not-allowed; }
.nd-etapa:hover:not(:disabled) { border-color: var(--accent); color: var(--text-primary); }
.nd-num { display: inline-flex; align-items: center; justify-content: center; width: 18px; height: 18px; border-radius: 50%; background: var(--bg-elevated); font-size: 10px; }
.nd-etapa-nome { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.feita .nd-etapa { background: rgba(6, 182, 212, .12); border-color: rgba(6, 182, 212, .35); color: var(--accent); }
.atual .nd-etapa { background: var(--accent); border-color: var(--accent); color: #000; }
.atual .nd-num { background: rgba(0, 0, 0, .18); }
.nd-aba { padding: 18px 2px; }
</style>
