<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { ApiError, odataRequest, odataTodos, montarQuery } from '@/services/api'
import {
  campo, lista, mensagemErro, formatarData, formatarDataHora, formatarValorMoeda, carregarMoedas,
  TIPOS_ATIVIDADE, type Registro, type MoedaInfo,
} from '@/composables/useEntidadeDetalhe'
import DetalheEstado from './DetalheEstado.vue'

/**
 * Linha do tempo de uma entidade (mais recente primeiro), montada a partir de
 * odata/RegistrosAlteracao (filtrado por EntidadeAlvo e RegistroId) e das atividades concluídas.
 * `nomes` permite traduzir ids em nomes, ex.: { EtapaId: { 3: 'Proposta' } }.
 *
 * No servidor, `DadosAntes` traz só os campos que mudaram (com o valor anterior) e
 * `DadosDepois` o registro completo já alterado.
 */
const props = defineProps<{
  entidade: 'Cliente' | 'Negocio' | 'PessoaContato'
  registroId: number
  /** Coleção OData do registro, usada para ler as atividades concluídas. */
  endpoint: 'Clientes' | 'Negocios' | 'PessoasContato'
  nomes?: Record<string, Record<number, string>>
}>()

interface ItemLinha {
  chave: string
  data: string
  icone: string
  tom: 'neutro' | 'sucesso' | 'aviso' | 'perigo' | 'info'
  titulo: string
  usuario?: string
  frases: string[]
}

// Máximo de 100 por página no servidor; usamos 50 (+1 para saber se há mais).
const POR_PAGINA = 50
const alteracoes = ref<Registro[]>([])
const atividades = ref<Registro[]>([])
const moedas = ref<MoedaInfo[]>([])
const nomesUsuarios = ref<Record<number, string>>({})
const carregando = ref(false)
const carregandoMais = ref(false)
const erro = ref('')
const historicoIndisponivel = ref(false)
const temMais = ref(false)

/** Campos técnicos que nunca viram frase. */
const IGNORAR = new Set([
  'Id', 'EmpresaId', 'CriadoEm', 'AtualizadoEm', 'ChaveExterna', 'OutrasPropriedades', 'UltimoAcesso',
  'Atividades', 'Negocios', 'Contatos', 'Propostas', 'SenhaHash', 'Ativo',
])
const ROTULOS: Record<string, string> = {
  Titulo: 'Título', Nome: 'Nome', Valor: 'Valor', FechadoEm: 'Data de fechamento', PrevisaoFechamento: 'Previsão de fechamento',
  ClienteId: 'Cliente', PessoaContatoId: 'Contato', FunilId: 'Funil', EtapaId: 'Etapa', StatusId: 'Status',
  MotivoPerdaId: 'Motivo da perda', MoedaId: 'Moeda', ResponsavelId: 'Responsável', RazaoSocial: 'Razão social',
  Email: 'E-mail', Telefone: 'Telefone', Documento: 'Documento', Cidade: 'Cidade', Estado: 'Estado', Endereco: 'Endereço',
  Cep: 'CEP', Observacoes: 'Observações', Site: 'Site', StatusClienteId: 'Status do cliente', OrigemId: 'Origem',
  RamoAtividadeId: 'Ramo de atividade', PorteEmpresaId: 'Porte', Cargo: 'Cargo', Decisor: 'Decisor', Descricao: 'Descrição',
  Tipo: 'Tipo', DataVencimento: 'Vencimento', Concluida: 'Concluída', ConcluidaEm: 'Concluída em',
}
const MONETARIOS = new Set(['Valor'])

function rotulo(k: string): string {
  if (ROTULOS[k]) return ROTULOS[k]
  const limpo = k.replace(/Id$/, '').replace(/([a-z])([A-Z])/g, '$1 $2').toLowerCase()
  return limpo.charAt(0).toUpperCase() + limpo.slice(1)
}

const ISO = /^\d{4}-\d{2}-\d{2}T/
const SEM_FUSO = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}(:\d{2}(\.\d+)?)?$/
/** O servidor grava em UTC; textos sem fuso são tratados como UTC. */
function comFuso(v: string): string { return SEM_FUSO.test(v) ? `${v}Z` : v }

function formatarValor(k: string, v: unknown, moedaId: unknown): string {
  if (v === null || v === undefined || v === '') return 'vazio'
  if (typeof v === 'boolean') return v ? 'Sim' : 'Não'
  if (typeof v === 'number') {
    if (MONETARIOS.has(k)) {
      const m = moedas.value.find((x) => x.id === Number(moedaId))
      return m ? formatarValorMoeda(v, m.simbolo, m.codigo) : v.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
    }
    if (k === 'ResponsavelId') return nomesUsuarios.value[v] ?? props.nomes?.[k]?.[v] ?? `#${v}`
    if (k.endsWith('Id')) return props.nomes?.[k]?.[v] ?? `#${v}`
    if (k === 'Tipo') return TIPOS_ATIVIDADE[v]?.nome ?? String(v)
    return v.toLocaleString('pt-BR')
  }
  if (typeof v === 'string') {
    if (ISO.test(v)) {
      const ajustado = comFuso(v)
      return /T00:00:00(\.0+)?(Z|[+-]00:00)?$/.test(ajustado) ? formatarData(ajustado) : formatarDataHora(ajustado)
    }
    return v.length > 80 ? v.slice(0, 77) + '…' : v
  }
  return ''
}

function tentarJson(texto: string | null | undefined): unknown {
  if (!texto) return null
  try { return JSON.parse(texto) } catch { return null }
}
function comoObjeto(x: unknown): Record<string, unknown> | null {
  return x && typeof x === 'object' && !Array.isArray(x) ? (x as Record<string, unknown>) : null
}
const vazio = (v: unknown) => v === null || v === undefined || v === ''

interface Diferenca { frases: string[]; inativado: boolean; reativado: boolean; relacionados: boolean }

function diferencas(r: Registro): Diferenca {
  const vazioRes: Diferenca = { frases: [], inativado: false, reativado: false, relacionados: false }
  const antesBruto = tentarJson(campo(r, 'DadosAntes'))
  const depoisBruto = tentarJson(campo(r, 'DadosDepois'))
  const antes = comoObjeto(antesBruto)
  const depois = comoObjeto(depoisBruto) ?? {}
  // DadosDepois em lista = alteração em itens relacionados (ex.: propostas do negócio), sem diferença de campos deste registro.
  if (!antes) return Array.isArray(depoisBruto) ? { ...vazioRes, relacionados: true } : vazioRes
  const res: Diferenca = { ...vazioRes }
  for (const [k, vAntes] of Object.entries(antes)) {
    if (k === 'Ativo') {
      if (depois.Ativo === false) res.inativado = true
      else if (depois.Ativo === true && vAntes === false) res.reativado = true
      continue
    }
    if (IGNORAR.has(k)) continue
    if (vAntes !== null && typeof vAntes === 'object') continue
    const vDepois = depois[k]
    if (JSON.stringify(vAntes ?? null) === JSON.stringify(vDepois ?? null)) continue
    // Valor numérico "72700.00" x 72700: compara como número.
    if (typeof vAntes === 'number' && typeof vDepois === 'number' && vAntes === vDepois) continue
    const moedaId = depois.MoedaId ?? antes.MoedaId
    const nome = rotulo(k)
    if (vazio(vAntes) && !vazio(vDepois)) res.frases.push(`${nome}: definido como ${formatarValor(k, vDepois, moedaId)}`)
    else if (!vazio(vAntes) && vazio(vDepois)) res.frases.push(`${nome}: removido (era ${formatarValor(k, vAntes, moedaId)})`)
    else res.frases.push(`${nome}: de ${formatarValor(k, vAntes, moedaId)} para ${formatarValor(k, vDepois, moedaId)}`)
  }
  return res
}

function itemDeAlteracao(r: Registro): ItemLinha | null {
  const acao = String(campo(r, 'Acao') ?? '')
  const usuario = String(campo(r, 'UsuarioNome') ?? campo(r, 'Usuario') ?? '') || undefined
  const base = { chave: `a-${campo(r, 'Id')}`, data: String(campo(r, 'DataHora') ?? ''), usuario }
  if (acao === 'Criacao') return { ...base, icone: 'mdi-plus-circle-outline', tom: 'sucesso', titulo: 'Criou o registro', frases: [] }
  if (acao === 'Exclusao') return { ...base, icone: 'mdi-delete-outline', tom: 'perigo', titulo: 'Excluiu o registro', frases: [] }
  if (acao === 'AutomacaoExecutada') {
    const t = campo<string>(r, 'Titulo')
    return { ...base, icone: 'mdi-robot-outline', tom: 'info', titulo: t ? `Automação executada: ${t}` : 'Automação executada', frases: [] }
  }
  const d = diferencas(r)
  if (d.inativado) return { ...base, icone: 'mdi-delete-outline', tom: 'perigo', titulo: 'Excluiu o registro', frases: d.frases }
  if (d.reativado) return { ...base, icone: 'mdi-restore', tom: 'sucesso', titulo: 'Reativou o registro', frases: d.frases }
  // Alterações só em campos técnicos (ex.: último acesso) não interessam ao usuário.
  if (!d.frases.length && !d.relacionados) return null
  const frases = d.frases.length > 8 ? [...d.frases.slice(0, 8), `e mais ${d.frases.length - 8} alterações`] : d.frases
  return {
    ...base, icone: 'mdi-pencil-outline', tom: 'info',
    titulo: d.relacionados ? 'Atualizou dados relacionados' : 'Alterou o registro',
    frases,
  }
}

function itemDeAtividade(a: Registro): ItemLinha {
  const tipo = TIPOS_ATIVIDADE[campo(a, 'Tipo') as number | string]
  return {
    chave: `t-${campo(a, 'Id')}`,
    data: String(campo(a, 'ConcluidaEm') ?? campo(a, 'CriadoEm') ?? ''),
    icone: tipo?.icone ?? 'mdi-check-circle-outline',
    tom: 'sucesso',
    titulo: `${tipo?.nome ?? 'Atividade'} concluída: ${campo(a, 'Titulo') ?? ''}`.trim(),
    frases: campo(a, 'Descricao') ? [String(campo(a, 'Descricao'))] : [],
  }
}

const linha = computed<ItemLinha[]>(() =>
  [
    ...alteracoes.value.map(itemDeAlteracao).filter((i): i is ItemLinha => i !== null),
    ...atividades.value.map(itemDeAtividade),
  ]
    .filter((i) => i.data)
    .sort((a, b) => new Date(b.data).getTime() - new Date(a.data).getTime()),
)

async function buscarAlteracoes(skip: number): Promise<Registro[]> {
  const q = montarQuery({
    $filter: `EntidadeAlvo eq '${props.entidade}' and RegistroId eq ${props.registroId}`,
    $orderby: 'DataHora desc,Id desc',
    $top: POR_PAGINA + 1,
    $skip: skip || undefined,
  })
  return lista<Registro>(await odataRequest<unknown>('GET', `RegistrosAlteracao${q}`))
}

async function buscarAtividades(): Promise<Registro[]> {
  const q = montarQuery({
    $filter: `Id eq ${props.registroId}`,
    $top: 1,
    $expand: 'Atividades($filter=Concluida eq true;$orderby=ConcluidaEm desc;$top=100)',
  })
  const res = lista<Registro>(await odataRequest<unknown>('GET', `${props.endpoint}${q}`))
  return lista<Registro>(res[0]?.Atividades ?? [])
}

/** Se houve troca de responsável, busca os nomes dos usuários envolvidos (Usuarios nunca devolve dados sensíveis). */
async function carregarNomesUsuarios(regs: Registro[]) {
  const ids = new Set<number>()
  for (const r of regs) {
    for (const bruto of [campo<string>(r, 'DadosAntes'), campo<string>(r, 'DadosDepois')]) {
      const o = comoObjeto(tentarJson(bruto))
      const v = o?.ResponsavelId
      if (typeof v === 'number' && !(v in nomesUsuarios.value)) ids.add(v)
    }
  }
  if (!ids.size) return
  try {
    const todos = await odataTodos<Registro>(`Usuarios${montarQuery({ $select: 'Id,Nome', $orderby: 'Nome asc' })}`)
    const mapa = { ...nomesUsuarios.value }
    for (const u of todos) mapa[Number(campo(u, 'Id'))] = String(campo(u, 'Nome'))
    nomesUsuarios.value = mapa
  } catch { /* sem o nome, a frase mostra o número */ }
}

async function carregar() {
  carregando.value = true
  erro.value = ''
  historicoIndisponivel.value = false
  try {
    const [alt, ativ, mo] = await Promise.all([
      buscarAlteracoes(0).catch((e) => {
        if (e instanceof ApiError && (e.status === 404 || e.status === 501)) { historicoIndisponivel.value = true; return [] as Registro[] }
        throw e
      }),
      buscarAtividades().catch(() => [] as Registro[]),
      carregarMoedas(),
    ])
    moedas.value = mo
    temMais.value = alt.length > POR_PAGINA
    const pagina = alt.slice(0, POR_PAGINA)
    await carregarNomesUsuarios(pagina)
    alteracoes.value = pagina
    atividades.value = ativ
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar o histórico')
  } finally {
    carregando.value = false
  }
}

async function carregarMais() {
  carregandoMais.value = true
  try {
    const alt = await buscarAlteracoes(alteracoes.value.length)
    temMais.value = alt.length > POR_PAGINA
    const pagina = alt.slice(0, POR_PAGINA)
    await carregarNomesUsuarios(pagina)
    alteracoes.value = [...alteracoes.value, ...pagina]
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar mais itens')
  } finally {
    carregandoMais.value = false
  }
}

watch(() => [props.entidade, props.registroId], carregar)
onMounted(carregar)
defineExpose({ carregar })
</script>

<template>
  <DetalheEstado
    :carregando="carregando"
    :erro="erro"
    :vazio="!linha.length"
    texto-vazio="Nenhum evento registrado ainda."
    icone="mdi-history"
    @tentar="carregar"
  >
    <p v-if="historicoIndisponivel" class="tl-aviso" role="status">
      <i class="mdi mdi-information-outline"></i>
      O registro de alterações ainda não está disponível neste servidor. Exibindo apenas atividades concluídas.
    </p>
    <ol class="tl">
      <li v-for="item in linha" :key="item.chave" class="tl-item">
        <span class="tl-ponto" :class="`tom-${item.tom}`"><i class="mdi" :class="item.icone"></i></span>
        <div class="tl-corpo">
          <div class="tl-topo">
            <strong>{{ item.titulo }}</strong>
            <time :datetime="item.data">{{ formatarDataHora(item.data) }}</time>
          </div>
          <div v-if="item.usuario" class="tl-usuario">por {{ item.usuario }}</div>
          <ul v-if="item.frases.length" class="tl-frases">
            <li v-for="(f, i) in item.frases" :key="i">{{ f }}</li>
          </ul>
        </div>
      </li>
    </ol>
    <div v-if="temMais" class="tl-mais">
      <button class="tl-btn" :disabled="carregandoMais" @click="carregarMais">
        {{ carregandoMais ? 'Carregando...' : 'Carregar mais' }}
      </button>
    </div>
  </DetalheEstado>
</template>

<style scoped>
.tl { list-style: none; margin: 0; padding: 0; position: relative; }
.tl::before { content: ''; position: absolute; left: 15px; top: 8px; bottom: 8px; width: 2px; background: var(--border); }
.tl-item { position: relative; display: flex; gap: 14px; padding: 0 0 18px; }
.tl-ponto { z-index: 1; flex: 0 0 32px; height: 32px; border-radius: 50%; display: flex; align-items: center; justify-content: center; background: var(--bg-surface); border: 2px solid var(--border); font-size: 16px; color: var(--text-secondary); }
.tl-ponto.tom-sucesso { color: #22c55e; border-color: rgba(34, 197, 94, .5); }
.tl-ponto.tom-info { color: var(--accent); border-color: rgba(6, 182, 212, .5); }
.tl-ponto.tom-perigo { color: var(--danger); border-color: rgba(239, 68, 68, .5); }
.tl-corpo { flex: 1; min-width: 0; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 10px 14px; }
.tl-topo { display: flex; justify-content: space-between; gap: 12px; align-items: baseline; font-size: 13px; color: var(--text-primary); }
.tl-topo time { font-size: 11px; color: var(--text-muted); white-space: nowrap; }
.tl-usuario { font-size: 11px; color: var(--text-muted); margin-top: 2px; }
.tl-frases { margin: 8px 0 0; padding-left: 18px; font-size: 12px; color: var(--text-secondary); display: flex; flex-direction: column; gap: 3px; }
.tl-mais { display: flex; justify-content: center; padding-top: 4px; }
.tl-btn { padding: 8px 18px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.tl-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.tl-btn:disabled { opacity: .5; cursor: not-allowed; }
.tl-aviso { display: flex; gap: 8px; align-items: center; margin: 0 0 14px; padding: 8px 12px; border-radius: 8px; background: rgba(245, 158, 11, .1); color: #f59e0b; font-size: 12px; }
</style>
