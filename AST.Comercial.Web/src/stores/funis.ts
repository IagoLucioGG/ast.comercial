import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { odataRequest, odataTodos } from '@/services/api'
import { extrairLista, lerCampo, type MoedaInfo } from '@/composables/useFunilFormato'

export interface EtapaItem {
  id: number
  nome: string
  ordem: number
  funilId: number
}

export interface FunilItem {
  id: number
  nome: string
  ordem: number
  etapas: EtapaItem[]
}

export interface UsuarioItem {
  id: number
  nome: string
}

/** Status de negócio configurável; 	ipoBase: 0 Aberto, 1 Ganho, 2 Perdido. */
export interface StatusItem {
  id: number
  nome: string
  tipoBase: number
}

export const useFunisStore = defineStore('funis', () => {
  const funis = ref<FunilItem[]>([])
  const moedas = ref<MoedaInfo[]>([])
  const usuarios = ref<UsuarioItem[]>([])
  const status = ref<StatusItem[]>([])
  const carregando = ref(false)
  const carregado = ref(false)
  const erro = ref<string | null>(null)

  const moedaPorId = computed(() => {
    const m = new Map<number, MoedaInfo>()
    for (const x of moedas.value) m.set(x.id, x)
    return m
  })

  /**
   * Moeda do registro. Sem id usa a moeda padrão (ou BRL); id fora da lista devolve um marcador
   * com o identificador, para nunca exibir um símbolo errado.
   */
  function moedaDe(id: number | null | undefined): MoedaInfo {
    if (id === null || id === undefined) {
      return moedas.value.find((m) => m.padrao) ?? moedas.value.find((m) => m.codigo === 'BRL') ?? { id: 0, codigo: 'BRL' }
    }
    return moedaPorId.value.get(id) ?? { id, codigo: `Moeda ${id}` }
  }

  function mapEtapa(e: unknown): EtapaItem {
    return {
      id: Number(lerCampo(e, 'Id')),
      nome: String(lerCampo(e, 'Nome') ?? ''),
      ordem: Number(lerCampo(e, 'Ordem') ?? 0),
      funilId: Number(lerCampo(e, 'FunilId')),
    }
  }

  function montar(funisBrutos: unknown[], etapasBrutas: unknown[]): FunilItem[] {
    const etapas = etapasBrutas
      .filter((e) => lerCampo(e, 'Ativo') !== false)
      .map(mapEtapa)
      .sort((a, b) => a.ordem - b.ordem || a.id - b.id)
    return funisBrutos
      .filter((f) => lerCampo(f, 'Ativo') !== false)
      .map((f) => {
        const id = Number(lerCampo(f, 'Id'))
        return {
          id,
          nome: String(lerCampo(f, 'Nome') ?? ''),
          ordem: Number(lerCampo(f, 'Ordem') ?? 0),
          etapas: etapas.filter((e) => e.funilId === id),
        }
      })
      .sort((a, b) => a.ordem - b.ordem || a.id - b.id)
  }

  async function buscarPrincipal(): Promise<FunilItem[]> {
    const [f, e] = await Promise.all([
      odataRequest<unknown>('GET', 'Negocios@Funis'),
      odataRequest<unknown>('GET', 'Negocios@Etapas'),
    ])
    return montar(extrairLista(f), extrairLista(e))
  }

  /** Alternativa quando as rotas `Negocios@*` deixarem de existir. */
  async function buscarAlternativa(): Promise<FunilItem[]> {
    const r = await odataRequest<unknown>('GET', 'Funis?$expand=Etapas')
    const lista = extrairLista(r)
    const etapas = lista.flatMap((f) => extrairLista(lerCampo(f, 'Etapas')))
    return montar(lista, etapas)
  }

  async function carregar(forcar = false) {
    if (carregando.value || (carregado.value && !forcar)) return
    carregando.value = true
    erro.value = null
    try {
      try {
        funis.value = await buscarPrincipal()
      } catch {
        funis.value = await buscarAlternativa()
      }
      // Dados auxiliares: se algum falhar, o quadro continua funcionando com o que houver.
      const [rm, ru, rs] = await Promise.allSettled([
        odataRequest<unknown>('GET', 'Moedas'),
        odataTodos<unknown>('Usuarios?$select=Id,Nome&$orderby=Nome asc'),
        odataRequest<unknown>('GET', 'Negocios@Status'),
      ])
      moedas.value = rm.status === 'fulfilled'
        ? extrairLista(rm.value).map((m) => ({
            id: Number(lerCampo(m, 'Id')),
            codigo: String(lerCampo(m, 'Codigo') ?? ''),
            simbolo: lerCampo<string | null>(m, 'Simbolo') ?? null,
            padrao: lerCampo<boolean>(m, 'Padrao') === true,
          }))
        : []
      usuarios.value = ru.status === 'fulfilled'
        ? ru.value.map((u) => ({ id: Number(lerCampo(u, 'Id')), nome: String(lerCampo(u, 'Nome') ?? '') }))
        : []
      status.value = rs.status === 'fulfilled'
        ? extrairLista(rs.value).map((s) => ({
            id: Number(lerCampo(s, 'Id')),
            nome: String(lerCampo(s, 'Nome') ?? ''),
            tipoBase: Number(lerCampo(s, 'TipoBase') ?? 0),
          }))
        : []
      carregado.value = true
    } catch (e) {
      erro.value = e instanceof Error ? e.message : 'Não foi possível carregar os funis.'
    } finally {
      carregando.value = false
    }
  }

  function funilPorId(id: number | null | undefined): FunilItem | undefined {
    return funis.value.find((f) => f.id === id)
  }

  return { funis, moedas, moedaPorId, moedaDe, usuarios, status, carregando, carregado, erro, carregar, funilPorId }
})
