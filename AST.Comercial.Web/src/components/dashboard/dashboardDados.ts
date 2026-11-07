// Camada de dados do resumo comercial. Cada bloco carrega de forma independente
// (um erro não derruba os demais). Valores monetários são sempre agrupados por moeda.
import { ref, type Ref } from 'vue'
import { odataQuery, odataRequest, odataAction, odataTodos, montarQuery } from '@/services/api'

export type PeriodoChave = 'mes' | 'mesAnterior' | '30dias' | 'ano'

export const PERIODOS: { chave: PeriodoChave; rotulo: string }[] = [
  { chave: 'mes', rotulo: 'Este mês' },
  { chave: 'mesAnterior', rotulo: 'Mês anterior' },
  { chave: '30dias', rotulo: 'Últimos 30 dias' },
  { chave: 'ano', rotulo: 'Ano' },
]

export interface Intervalo { ini: Date; fim: Date }

/** Intervalo [ini, fim) no fuso local para o período escolhido. */
export function intervaloDoPeriodo(chave: PeriodoChave, agora = new Date()): Intervalo {
  const a = agora.getFullYear()
  const m = agora.getMonth()
  switch (chave) {
    case 'mesAnterior': return { ini: new Date(a, m - 1, 1), fim: new Date(a, m, 1) }
    case '30dias': {
      const fim = new Date(a, m, agora.getDate() + 1)
      return { ini: new Date(a, m, agora.getDate() - 29), fim }
    }
    case 'ano': return { ini: new Date(a, 0, 1), fim: new Date(a + 1, 0, 1) }
    default: return { ini: new Date(a, m, 1), fim: new Date(a, m + 1, 1) }
  }
}

// ---- Tipos ---------------------------------------------------------------

export interface TotalMoeda { moedaId: number | null; qtd: number; total: number }
export interface Moeda { id: number; codigo: string; simbolo?: string | null; padrao?: boolean }
export interface StatusNeg { id: number; nome: string; tipoBase: number }
export interface Etapa { id: number; nome: string; ordem: number; funilId: number }
export interface FunilItem { id: number; nome: string; ordem: number }
export interface AtividadeItem {
  id: number; titulo: string; tipo: number | string; dataVencimento: string | null; concluida: boolean
  negocioId?: number | null
}
export interface NegocioRecente {
  id: number; titulo: string; valor: number | null; moedaId: number | null
  etapaId: number; statusId: number | null; criadoEm: string; clienteNome: string
}
/** Escopo das listas do resumo: só do usuário logado ou todas. */
export type Escopo = 'minhas' | 'todas'
export interface EtapaResumo { etapa: Etapa; qtd: number; porMoeda: TotalMoeda[] }

/** Estado de um bloco: carregando / erro / dados. */
export interface Bloco<T> { carregando: Ref<boolean>; erro: Ref<string>; dados: Ref<T> }
export function criarBloco<T>(inicial: T): Bloco<T> {
  return { carregando: ref(true), erro: ref(''), dados: ref(inicial) as Ref<T> }
}
export async function carregarBloco<T>(b: Bloco<T>, fn: () => Promise<T>): Promise<void> {
  b.carregando.value = true
  b.erro.value = ''
  try { b.dados.value = await fn() }
  catch (e) { b.erro.value = e instanceof Error ? e.message : 'Falha ao carregar' }
  finally { b.carregando.value = false }
}

// ---- Utilitários ---------------------------------------------------------

/** Lê propriedade ignorando a caixa (o OData pode devolver "Qtd" ou "qtd"). */
function pega(o: Record<string, unknown>, nome: string): unknown {
  if (nome in o) return o[nome]
  const baixo = nome.toLowerCase()
  for (const k of Object.keys(o)) if (k.toLowerCase() === baixo) return o[k]
  return undefined
}
const num = (v: unknown): number => (typeof v === 'number' ? v : Number(v) || 0)
const idOuNulo = (v: unknown): number | null => (v === null || v === undefined ? null : Number(v))
// O servidor exige data-hora com fuso: usa ISO em UTC (…Z) a partir do limite no fuso local.
const iso = (d: Date) => d.toISOString()
const texto = (v: unknown): string => (typeof v === 'string' ? v : '')

function paraTotais(linhas: Record<string, unknown>[]): TotalMoeda[] {
  return linhas.map((l) => ({
    moedaId: idOuNulo(pega(l, 'MoedaId')),
    qtd: num(pega(l, 'Qtd')),
    total: num(pega(l, 'Total')),
  }))
}

export function somaQtd(t: TotalMoeda[]): number { return t.reduce((s, x) => s + x.qtd, 0) }

const AGREGADO = 'aggregate(Valor with sum as Total,$count as Qtd)'

async function agrupar(entidade: string, filtro: string, chaves: string[]): Promise<Record<string, unknown>[]> {
  const f = filtro ? `filter(${filtro})/` : ''
  const r = await odataQuery<Record<string, unknown>>(
    `${entidade}${montarQuery({ $apply: `${f}groupby((${chaves.join(',')}),${AGREGADO})` })}`)
  return r.value ?? []
}

async function contar(entidade: string, filtro: string): Promise<number> {
  const r = await odataQuery<unknown>(`${entidade}${montarQuery({ $filter: filtro || undefined, $count: true, $top: 0 })}`)
  return r['@odata.count'] ?? 0
}

// ---- Cadastros de apoio --------------------------------------------------

export interface Apoio { moedas: Moeda[]; status: StatusNeg[]; funis: FunilItem[]; etapas: Etapa[] }

let moedasEmCache: Promise<Moeda[]> | null = null

/** Moedas da empresa (carregadas uma vez e memorizadas; em falha, tenta de novo na próxima). */
export function carregarMoedas(): Promise<Moeda[]> {
  if (!moedasEmCache) {
    moedasEmCache = odataTodos<Record<string, unknown>>(`Moedas${montarQuery({ $orderby: 'Id' })}`)
      .then((linhas) => linhas.map((l) => ({
        id: num(pega(l, 'Id')),
        codigo: texto(pega(l, 'Codigo')).toUpperCase(),
        simbolo: texto(pega(l, 'Simbolo')) || null,
        padrao: pega(l, 'Padrao') === true,
      })))
      .catch((e) => { moedasEmCache = null; throw e })
  }
  return moedasEmCache
}

export async function carregarApoio(): Promise<Apoio> {
  const [moedas, status, funis, etapas] = await Promise.all([
    carregarMoedas().catch(() => [] as Moeda[]),
    odataRequest<StatusNeg[]>('GET', 'Negocios@Status'),
    odataRequest<FunilItem[]>('GET', 'Negocios@Funis'),
    odataRequest<Etapa[]>('GET', 'Negocios@Etapas'),
  ])
  funis.sort((a, b) => a.ordem - b.ordem)
  etapas.sort((a, b) => a.ordem - b.ordem)
  return { moedas, status, funis, etapas }
}

const lista = (ids: number[]) => ids.map((i) => `StatusId eq ${i}`).join(' or ')
const idsDoTipo = (s: StatusNeg[], tipo: number) => s.filter((x) => x.tipoBase === tipo).map((x) => x.id)

/** Filtro de "em aberto": status do tipo Aberto ou negócio sem status. */
export function filtroAberto(status: StatusNeg[]): string {
  const ids = idsDoTipo(status, 0)
  return ['StatusId eq null', ...(ids.length ? [lista(ids)] : [])].join(' or ')
}

// ---- Indicadores ---------------------------------------------------------

export function totalNegocios(): Promise<number> { return contar('Negocios', '') }

export function negociosAbertos(status: StatusNeg[]): Promise<TotalMoeda[]> {
  return agrupar('Negocios', `(${filtroAberto(status)})`, ['MoedaId']).then(paraTotais)
}

async function fechadosNoPeriodo(status: StatusNeg[], tipo: number, p: Intervalo): Promise<TotalMoeda[]> {
  const ids = idsDoTipo(status, tipo)
  if (!ids.length) return []
  const filtro = `(${lista(ids)}) and FechadoEm ge ${iso(p.ini)} and FechadoEm lt ${iso(p.fim)}`
  return agrupar('Negocios', filtro, ['MoedaId']).then(paraTotais)
}

export const negociosGanhos = (s: StatusNeg[], p: Intervalo) => fechadosNoPeriodo(s, 1, p)
export const negociosPerdidos = (s: StatusNeg[], p: Intervalo) => fechadosNoPeriodo(s, 2, p)

/** Propostas que aguardam aprovação (Pendente) ou revisão, ignorando modelos. */
export async function propostasAguardando(): Promise<TotalMoeda[]> {
  const linhas = await agrupar('Propostas', 'IsTemplate eq false and UltimaRevisao eq true', ['StatusAprovacao', 'MoedaId'])
  const aguardando = linhas.filter((l) => {
    const s = pega(l, 'StatusAprovacao')
    return s === 0 || s === 3 || s === 'Pendente' || s === 'EmRevisao'
  })
  return paraTotais(aguardando)
}

// ---- Atividades ----------------------------------------------------------

/** Filtro de responsável: "minhas" restringe ao usuário logado; "todas" não filtra. */
function filtroEscopo(escopo: Escopo, usuarioId: number | null): string {
  return escopo === 'minhas' && usuarioId !== null ? `ResponsavelId eq ${usuarioId}` : ''
}
const juntar = (...partes: string[]) => partes.filter(Boolean).join(' and ')

/**
 * Próximas atividades não concluídas (até 8), ordenadas por vencimento; sem data ficam por último.
 * O `odata/Atividades` devolve a entidade em PascalCase.
 */
export async function atividadesPendentes(escopo: Escopo, usuarioId: number | null): Promise<AtividadeItem[]> {
  const filtro = juntar('Concluida eq false', filtroEscopo(escopo, usuarioId))
  const r = await odataQuery<Record<string, unknown>>(`Atividades${montarQuery({
    $filter: filtro, $orderby: 'DataVencimento asc,Id asc', $top: 8,
    $select: 'Id,Titulo,Tipo,DataVencimento,Concluida,NegocioId',
  })}`)
  return (r.value ?? []).map((l) => ({
    id: num(pega(l, 'Id')),
    titulo: texto(pega(l, 'Titulo')) || '(sem título)',
    tipo: (pega(l, 'Tipo') as number | string | undefined) ?? 0,
    dataVencimento: texto(pega(l, 'DataVencimento')) || null,
    concluida: pega(l, 'Concluida') === true,
    negocioId: idOuNulo(pega(l, 'NegocioId')),
  }))
}

/** Pendentes de hoje e atrasadas, contadas no servidor (limites do dia local convertidos para UTC). */
export async function contagemAtividades(escopo: Escopo, usuarioId: number | null, agora = new Date()) {
  const ini = new Date(agora.getFullYear(), agora.getMonth(), agora.getDate())
  const fim = new Date(agora.getFullYear(), agora.getMonth(), agora.getDate() + 1)
  const base = juntar('Concluida eq false', filtroEscopo(escopo, usuarioId))
  const [hoje, atrasadas] = await Promise.all([
    contar('Atividades', `${base} and DataVencimento ge ${iso(ini)} and DataVencimento lt ${iso(fim)}`),
    contar('Atividades', `${base} and DataVencimento lt ${iso(ini)}`),
  ])
  return { hoje, atrasadas }
}

export function concluirAtividade(id: number): Promise<unknown> {
  return odataAction('Atividades', id, 'Concluir')
}

// ---- Funil e recentes ----------------------------------------------------

export async function resumoFunil(funilId: number, etapas: Etapa[], status: StatusNeg[]): Promise<EtapaResumo[]> {
  const linhas = await agrupar('Negocios', `FunilId eq ${funilId} and (${filtroAberto(status)})`, ['EtapaId', 'MoedaId'])
  return etapas.filter((e) => e.funilId === funilId).map((etapa) => {
    const porMoeda = paraTotais(linhas.filter((l) => num(pega(l, 'EtapaId')) === etapa.id))
    return { etapa, qtd: somaQtd(porMoeda), porMoeda }
  })
}

/** Últimos 8 negócios criados, com o nome do cliente. */
export async function negociosRecentes(escopo: Escopo, usuarioId: number | null): Promise<NegocioRecente[]> {
  const r = await odataQuery<Record<string, unknown>>(`Negocios${montarQuery({
    $filter: filtroEscopo(escopo, usuarioId), $orderby: 'CriadoEm desc,Id desc', $top: 8,
    $select: 'Id,Titulo,Valor,MoedaId,EtapaId,StatusId,CriadoEm',
    $expand: 'Cliente($select=Nome)',
  })}`)
  return (r.value ?? []).map((l) => {
    const cliente = pega(l, 'Cliente') as Record<string, unknown> | null | undefined
    const valor = pega(l, 'Valor')
    return {
      id: num(pega(l, 'Id')),
      titulo: texto(pega(l, 'Titulo')) || '(sem título)',
      valor: valor === null || valor === undefined ? null : num(valor),
      moedaId: idOuNulo(pega(l, 'MoedaId')),
      etapaId: num(pega(l, 'EtapaId')),
      statusId: idOuNulo(pega(l, 'StatusId')),
      criadoEm: texto(pega(l, 'CriadoEm')),
      clienteNome: cliente ? texto(pega(cliente, 'Nome')) : '',
    }
  })
}

// ---- Formatação ----------------------------------------------------------

/** Primeira letra do nome, ignorando o prefixo "[Demo]" (e demais colchetes iniciais). */
export function inicialDoNome(nome: string | null | undefined): string {
  const limpo = (nome ?? '').replace(/^\s*(\[[^\]]*\]\s*)+/, '').trim()
  const m = limpo.match(/\p{L}|\p{N}/u)
  return m ? m[0].toUpperCase() : '?'
}

/** Data de criação no formato dd/mm/aaaa; vazio se inválida. */
export function formatarData(iso8601: string | null | undefined): string {
  if (!iso8601) return ''
  const d = new Date(iso8601)
  return Number.isNaN(d.getTime()) ? '' : d.toLocaleDateString('pt-BR')
}

/**
 * Valor com o símbolo da moeda do registro (sem moeda no registro usa a padrão).
 * Moeda desconhecida: mostra o código se houver, senão "moeda #id". Nunca assume R$.
 */
export function formatarValor(valor: number, moedaId: number | null, moedas: Moeda[]): string {
  const m = moedaId === null ? moedas.find((x) => x.padrao) : moedas.find((x) => x.id === moedaId)
  const numero = valor.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  if (!m) return moedaId === null ? numero : `${numero} (moeda #${moedaId})`
  return `${m.simbolo || m.codigo}\u00a0${numero}`
}
