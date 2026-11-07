import { ref, type Ref } from 'vue'
import { ApiError, odataRequest, montarQuery } from '@/services/api'

/** Registro OData genérico (propriedades em PascalCase nas entidades, camelCase nas rotas "@"). */
export type Registro = Record<string, any>

/** Lê uma propriedade tolerando PascalCase ou camelCase. */
export function campo<T = any>(obj: unknown, nome: string): T | undefined {
  if (!obj || typeof obj !== 'object') return undefined
  const o = obj as Record<string, any>
  const camel = nome.charAt(0).toLowerCase() + nome.slice(1)
  return (nome in o ? o[nome] : o[camel]) as T | undefined
}

/** Algumas rotas devolvem array puro, outras `{ value: [...] }`. */
export function lista<T = Registro>(resposta: unknown): T[] {
  if (Array.isArray(resposta)) return resposta as T[]
  if (resposta && typeof resposta === 'object') {
    const v = (resposta as Record<string, unknown>).value
    if (Array.isArray(v)) return v as T[]
  }
  return []
}

export function mensagemErro(e: unknown, padrao = 'Não foi possível concluir a operação'): string {
  if (e instanceof ApiError) return e.message || padrao
  if (e instanceof Error) return e.message || padrao
  return padrao
}

/** Primeira letra do nome, ignorando prefixos entre colchetes como "[Demo]". */
export function inicialDe(nome: unknown): string {
  const limpo = String(nome ?? '').replace(/^(\s*\[[^\]]*\]\s*)+/, '').trim()
  const letra = limpo.match(/[\p{L}\p{N}]/u)?.[0]
  return (letra ?? '?').toUpperCase()
}

/**
 * Data dd/mm/aaaa. Datas "sem hora" (meia-noite UTC ou só a data) são lidas pelo texto,
 * para não recuar um dia por causa do fuso; as demais usam o fuso local.
 */
export function formatarData(valor: string | null | undefined): string {
  if (!valor) return '-'
  const m = /^(\d{4})-(\d{2})-(\d{2})(?:T00:00:00(?:\.0+)?(?:Z|[+-]00:00)?)?$/.exec(valor)
  if (m) return `${m[3]}/${m[2]}/${m[1]}`
  const d = new Date(valor)
  return isNaN(d.getTime()) ? '-' : d.toLocaleDateString('pt-BR')
}

export function formatarDataHora(valor: string | null | undefined): string {
  if (!valor) return '-'
  const d = new Date(valor)
  return isNaN(d.getTime()) ? '-' : d.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' })
}

export function formatarMoeda(valor: number | null | undefined, codigo?: string | null): string {
  const n = valor ?? 0
  try {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: (codigo || 'BRL').toUpperCase() }).format(n)
  } catch {
    return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(n)
  }
}

/** Número em pt-BR com 2 casas, precedido do símbolo da moeda do registro (ex.: "R$ 72.700,00"). */
export function formatarValorMoeda(valor: number | null | undefined, simbolo?: string | null, codigo?: string | null): string {
  const n = Number(valor ?? 0)
  const simb = simbolo || codigo
  if (!simb) return formatarMoeda(n, codigo)
  return `${simb} ${n.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
}

export interface MoedaInfo { id: number; codigo: string; simbolo: string; nome: string }

let cacheMoedas: Promise<MoedaInfo[]> | null = null
/** Moedas da empresa (odata/Moedas), carregadas uma vez. Em falha devolve lista vazia e tenta de novo na próxima. */
export function carregarMoedas(): Promise<MoedaInfo[]> {
  if (!cacheMoedas) {
    cacheMoedas = odataRequest<unknown>('GET', 'Moedas').then((r) =>
      lista<Registro>(r).filter((m) => campo(m, 'Ativo') !== false).map((m) => ({
        id: Number(campo(m, 'Id')), codigo: String(campo(m, 'Codigo') ?? ''), simbolo: String(campo(m, 'Simbolo') ?? ''), nome: String(campo(m, 'Nome') ?? ''),
      })),
    ).catch(() => { cacheMoedas = null; return [] as MoedaInfo[] })
  }
  return cacheMoedas
}

/** Escapa aspas simples para literais de $filter. */
export function lit(texto: string): string {
  return texto.replace(/'/g, "''")
}

export const TIPOS_ATIVIDADE: Record<number | string, { nome: string; icone: string }> = {
  0: { nome: 'Tarefa', icone: 'mdi-checkbox-marked-circle-outline' },
  1: { nome: 'Ligação', icone: 'mdi-phone-outline' },
  2: { nome: 'E-mail', icone: 'mdi-email-outline' },
  3: { nome: 'Reunião', icone: 'mdi-account-group-outline' },
  4: { nome: 'Nota', icone: 'mdi-note-text-outline' },
  Tarefa: { nome: 'Tarefa', icone: 'mdi-checkbox-marked-circle-outline' },
  Ligacao: { nome: 'Ligação', icone: 'mdi-phone-outline' },
  Email: { nome: 'E-mail', icone: 'mdi-email-outline' },
  Reuniao: { nome: 'Reunião', icone: 'mdi-account-group-outline' },
  Nota: { nome: 'Nota', icone: 'mdi-note-text-outline' },
}

export const STATUS_APROVACAO: Record<number | string, { nome: string; tom: 'neutro' | 'sucesso' | 'perigo' | 'aviso' | 'info' }> = {
  0: { nome: 'Pendente', tom: 'aviso' },
  1: { nome: 'Aprovada', tom: 'sucesso' },
  2: { nome: 'Rejeitada', tom: 'perigo' },
  3: { nome: 'Em revisão', tom: 'info' },
  Pendente: { nome: 'Pendente', tom: 'aviso' },
  Aprovada: { nome: 'Aprovada', tom: 'sucesso' },
  Rejeitada: { nome: 'Rejeitada', tom: 'perigo' },
  EmRevisao: { nome: 'Em revisão', tom: 'info' },
}

/**
 * Carrega um registro por id (GET Entidade(id)) com estados de carregando,
 * erro e "não encontrado" (404).
 */
export function useEntidadeDetalhe(endpoint: string, expand?: string) {
  const registro: Ref<Registro | null> = ref(null)
  const carregando = ref(false)
  const erro = ref('')
  const naoEncontrado = ref(false)

  async function carregar(id: number, silencioso = false) {
    if (!silencioso) carregando.value = true
    erro.value = ''
    naoEncontrado.value = false
    try {
      // Consulta pela coleção (filtro por Id): aceita $expand de forma uniforme e vazio = não encontrado.
      const q = montarQuery({ $filter: `Id eq ${id}`, $top: 1, $expand: expand })
      const res = await odataRequest<unknown>('GET', `${endpoint}${q}`)
      const item = lista<Registro>(res)[0]
      if (!item) { registro.value = null; naoEncontrado.value = true; return }
      registro.value = item
    } catch (e) {
      registro.value = null
      if (e instanceof ApiError && e.status === 404) naoEncontrado.value = true
      else erro.value = mensagemErro(e, 'Não foi possível carregar o registro')
    } finally {
      carregando.value = false
    }
  }

  return { registro, carregando, erro, naoEncontrado, carregar }
}
