import { reactive, ref, watch, type Ref } from 'vue'
import { ApiError, montarQuery, odataRequest } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { compararMoedas, lerCampo, literalOData } from '@/composables/useFunilFormato'
import { useFunisStore, type EtapaItem } from '@/stores/funis'

export const TAMANHO_PAGINA = 15

/** Recorte do quadro por situação do negócio. */
export type FiltroStatus = 'abertos' | 'ganhos' | 'perdidos' | 'todos'

export interface CardNegocio {
  id: number
  titulo: string
  valor: number | null
  moedaId: number | null
  etapaId: number
  statusId: number | null
  responsavelId: number | null
  responsavelNome: string
  clienteId: number | null
  clienteNome: string
  previsaoFechamento: string | null
  fechadoEm: string | null
}

export interface TotalMoeda {
  moedaId: number | null
  total: number
  qtd: number
}

export interface EstadoColuna {
  cards: CardNegocio[]
  /** Quantidade de negócios do recorte atual na etapa (do servidor, ajustada nas movimentações). */
  qtd: number
  carregando: boolean
  carregandoMais: boolean
  fim: boolean
  erro: string | null
  /** Totais por moeda vindos do `$apply`; null quando o servidor não suporta (usa-se a soma parcial). */
  totaisServidor: TotalMoeda[] | null
}

const CAMPOS = 'Id,Titulo,Valor,MoedaId,EtapaId,StatusId,ResponsavelId,ClienteId,PrevisaoFechamento,FechadoEm'

function novaColuna(): EstadoColuna {
  return { cards: [], qtd: 0, carregando: true, carregandoMais: false, fim: false, erro: null, totaisServidor: null }
}

function paraCard(row: unknown): CardNegocio {
  const cliente = lerCampo<unknown>(row, 'Cliente')
  const responsavel = lerCampo<unknown>(row, 'Responsavel')
  return {
    id: Number(lerCampo(row, 'Id')),
    titulo: String(lerCampo(row, 'Titulo') ?? ''),
    valor: lerCampo<number | null>(row, 'Valor') ?? null,
    moedaId: lerCampo<number | null>(row, 'MoedaId') ?? null,
    etapaId: Number(lerCampo(row, 'EtapaId')),
    statusId: lerCampo<number | null>(row, 'StatusId') ?? null,
    responsavelId: lerCampo<number | null>(row, 'ResponsavelId') ?? null,
    responsavelNome: String(lerCampo(responsavel, 'Nome') ?? ''),
    clienteId: lerCampo<number | null>(row, 'ClienteId') ?? null,
    clienteNome: String(lerCampo(cliente, 'Nome') ?? ''),
    previsaoFechamento: lerCampo<string | null>(row, 'PrevisaoFechamento') ?? null,
    fechadoEm: lerCampo<string | null>(row, 'FechadoEm') ?? null,
  }
}

/**
 * Estado do kanban de um funil: uma lista paginada de negócios por etapa (recorte por status e
 * responsável), totais por moeda, movimentação otimista com reversão.
 */
export function useFunilKanban(
  funilId: Ref<number | null>,
  etapas: Ref<EtapaItem[]>,
  busca: Ref<string>,
  status: Ref<FiltroStatus>,
  responsavelId: Ref<number | null>,
) {
  const toast = useToast()
  const store = useFunisStore()
  const colunas = reactive<Record<number, EstadoColuna>>({})
  const totaisParciais = ref(false)
  const erroGeral = ref<string | null>(null)
  let geracao = 0

  function filtroBase(): string {
    let f = `Ativo eq true and FunilId eq ${funilId.value}`
    // Aberto = sem data de fechamento; Ganho/Perdido seguem o TipoBase do status (1 e 2).
    if (status.value === 'abertos') f += ' and FechadoEm eq null'
    else if (status.value === 'ganhos') f += ' and Status/TipoBase eq 1'
    else if (status.value === 'perdidos') f += ' and Status/TipoBase eq 2'
    if (responsavelId.value !== null) f += ` and ResponsavelId eq ${responsavelId.value}`
    const termo = busca.value.trim().toLowerCase()
    if (termo) {
      const t = literalOData(termo)
      f += ` and (contains(tolower(Titulo),'${t}') or contains(tolower(Cliente/Nome),'${t}'))`
    }
    return f
  }

  async function carregarColuna(etapaId: number, reset: boolean) {
    const col = colunas[etapaId]
    if (!col) return
    if (reset) {
      col.cards = []
      col.fim = false
      col.erro = null
      col.carregando = true
    } else {
      if (col.carregando || col.carregandoMais || col.fim) return
      col.carregandoMais = true
    }
    const minha = geracao
    const skip = reset ? 0 : col.cards.length
    const q = montarQuery({
      $filter: `${filtroBase()} and EtapaId eq ${etapaId}`,
      $select: CAMPOS,
      $expand: 'Cliente($select=Nome),Responsavel($select=Id,Nome)',
      $orderby: status.value === 'abertos' ? 'CriadoEm desc,Id desc' : 'FechadoEm desc,Id desc',
      $top: TAMANHO_PAGINA,
      $skip: skip || undefined,
      $count: true,
    })
    try {
      const r = await odataRequest<{ value?: unknown[]; '@odata.count'?: number }>('GET', `Negocios${q}`)
      if (minha !== geracao) return
      const novos = (r.value ?? []).map(paraCard)
      const existentes = new Set(col.cards.map((c) => c.id))
      col.cards.push(...novos.filter((n) => !existentes.has(n.id)))
      if (typeof r['@odata.count'] === 'number') col.qtd = r['@odata.count']
      col.fim = novos.length < TAMANHO_PAGINA || col.cards.length >= col.qtd
    } catch (e) {
      if (minha !== geracao) return
      col.erro = e instanceof Error ? e.message : 'Falha ao carregar os negócios.'
    } finally {
      if (minha === geracao) {
        col.carregando = false
        col.carregandoMais = false
      }
    }
  }

  /** Totais de todas as colunas em uma só consulta agregada (respeita status e responsável). */
  async function carregarTotais() {
    const minha = geracao
    const q = montarQuery({
      $apply: `filter(${filtroBase()})/groupby((MoedaId,EtapaId),aggregate(Valor with sum as Total,$count as Qtd))`,
    })
    try {
      const r = await odataRequest<{ value?: unknown[] }>('GET', `Negocios${q}`)
      if (minha !== geracao) return
      const porEtapa = new Map<number, TotalMoeda[]>()
      for (const linha of r.value ?? []) {
        const etapaId = Number(lerCampo(linha, 'EtapaId'))
        const lista = porEtapa.get(etapaId) ?? []
        lista.push({
          moedaId: lerCampo<number | null>(linha, 'MoedaId') ?? null,
          total: Number(lerCampo(linha, 'Total') ?? 0),
          qtd: Number(lerCampo(linha, 'Qtd') ?? 0),
        })
        porEtapa.set(etapaId, lista)
      }
      for (const e of etapas.value) {
        const col = colunas[e.id]
        if (!col) continue
        const lista = porEtapa.get(e.id) ?? []
        col.totaisServidor = lista
        col.qtd = lista.reduce((s, t) => s + t.qtd, 0)
      }
      totaisParciais.value = false
    } catch {
      if (minha !== geracao) return
      // Sem suporte a $apply: mantém a contagem do $count e soma só os cartões carregados.
      for (const e of etapas.value) {
        const col = colunas[e.id]
        if (col) col.totaisServidor = null
      }
      totaisParciais.value = true
    }
  }

  /** Ordem estável dos totais: BRL primeiro, depois as demais moedas por código. */
  function ordenar(lista: TotalMoeda[]): TotalMoeda[] {
    return [...lista].sort((a, b) => compararMoedas(store.moedaDe(a.moedaId), store.moedaDe(b.moedaId)))
  }

  /** Totais por moeda de uma coluna: do servidor ou, no fallback, somados dos cartões carregados. */
  function totaisDaColuna(etapaId: number): TotalMoeda[] {
    const col = colunas[etapaId]
    if (!col) return []
    if (col.totaisServidor) return ordenar(col.totaisServidor.filter((t) => t.qtd > 0))
    const mapa = new Map<number | null, TotalMoeda>()
    for (const c of col.cards) {
      const t = mapa.get(c.moedaId) ?? { moedaId: c.moedaId, total: 0, qtd: 0 }
      t.total += c.valor ?? 0
      t.qtd += 1
      mapa.set(c.moedaId, t)
    }
    return ordenar([...mapa.values()])
  }

  async function carregarTudo() {
    geracao++
    erroGeral.value = null
    const ids = new Set(etapas.value.map((e) => e.id))
    for (const k of Object.keys(colunas)) if (!ids.has(Number(k))) delete colunas[Number(k)]
    if (funilId.value === null || !etapas.value.length) return
    for (const e of etapas.value) colunas[e.id] = novaColuna()
    await Promise.all([...etapas.value.map((e) => carregarColuna(e.id, true)), carregarTotais()])
  }

  const carregarMais = (etapaId: number) => carregarColuna(etapaId, false)

  async function recarregarColunas(...ids: number[]) {
    await Promise.all([...ids.map((id) => carregarColuna(id, true)), carregarTotais()])
  }

  // --------------------------------------------------------------- movimentação
  const arrastando = ref<{ id: number; deEtapaId: number } | null>(null)

  function localizar(id: number): { etapaId: number; indice: number } | null {
    for (const [k, col] of Object.entries(colunas)) {
      const indice = col.cards.findIndex((c) => c.id === id)
      if (indice >= 0) return { etapaId: Number(k), indice }
    }
    return null
  }

  /** Move o cartão de etapa com atualização otimista; reverte se o servidor recusar. Cartões fechados são somente leitura. */
  async function mover(id: number, paraEtapaId: number): Promise<boolean> {
    const origem = localizar(id)
    const colOrigem = origem ? colunas[origem.etapaId] : undefined
    const colDestino = colunas[paraEtapaId]
    if (!origem || !colOrigem || !colDestino || origem.etapaId === paraEtapaId) return false
    const card = colOrigem.cards[origem.indice]
    if (!card || card.fechadoEm) return false
    colOrigem.cards.splice(origem.indice, 1)
    colOrigem.qtd = Math.max(0, colOrigem.qtd - 1)
    card.etapaId = paraEtapaId
    colDestino.cards.unshift(card)
    colDestino.qtd += 1
    try {
      await odataRequest('PATCH', `Negocios(${id})`, { EtapaId: paraEtapaId })
      await carregarTotais()
      return true
    } catch (e) {
      // reversão
      const i = colDestino.cards.findIndex((c) => c.id === id)
      if (i >= 0) colDestino.cards.splice(i, 1)
      colDestino.qtd = Math.max(0, colDestino.qtd - 1)
      card.etapaId = origem.etapaId
      colOrigem.cards.splice(Math.min(origem.indice, colOrigem.cards.length), 0, card)
      colOrigem.qtd += 1
      const msg = e instanceof ApiError && e.status === 422
        ? e.message
        : (e instanceof Error ? e.message : 'Não foi possível mover o negócio.')
      toast.error(msg)
      return false
    }
  }

  /** Remove o cartão da lista (ao ganhar/perder no recorte "Abertos") e atualiza os totais. */
  async function retirar(id: number) {
    const loc = localizar(id)
    if (loc) {
      const col = colunas[loc.etapaId]
      col?.cards.splice(loc.indice, 1)
      if (col) col.qtd = Math.max(0, col.qtd - 1)
    }
    await carregarTotais()
  }

  /** Depois de ganhar/perder/reabrir: some do recorte "Abertos"; nos demais, recarrega tudo. */
  async function aposFechamento(id: number) {
    if (status.value === 'abertos') await retirar(id)
    else await carregarTudo()
  }

  watch(
    [funilId, () => etapas.value.map((e) => e.id).join(','), busca, status, responsavelId],
    () => { carregarTudo() },
    { immediate: true },
  )

  return {
    colunas, totaisParciais, erroGeral, arrastando,
    carregarTudo, carregarMais, recarregarColunas, totaisDaColuna, mover, retirar, aposFechamento,
  }
}
