import { odataRequest } from '@/services/api'

// As rotas compostas de preço ficam em /api (fora do OData). `odataRequest` monta
// "/odata/<path>"; o navegador normaliza "../" e a chamada vai para "/api/...",
// reaproveitando token, refresh de sessão e o tratamento de erro (ApiError).

export interface PrecoLista {
  listaPrecoId: number
  listaNome: string
  padrao: boolean
  vigente: boolean
  vigenciaInicio: string | null
  vigenciaFim: string | null
  moedaId: number | null
  moedaCodigo: string | null
  moedaSimbolo: string | null
  itemId: number | null
  preco: number | null
}

export interface PrecosProduto {
  produtoId: number
  nome: string
  preco: number
  moedaId: number | null
  moedaCodigo: string | null
  moedaSimbolo: string | null
  listas: PrecoLista[]
}

export function obterPrecosProduto(produtoId: number, todas: boolean): Promise<PrecosProduto> {
  return odataRequest<PrecosProduto>('GET', `../api/produtos/${produtoId}/precos${todas ? '?todas=true' : ''}`)
}

export interface ResultadoCopia { criados: number; atualizados: number; ignorados: number }

export function copiarPrecosBase(listaId: number, sobrescrever: boolean): Promise<ResultadoCopia> {
  return odataRequest<ResultadoCopia>('POST', `../api/listas-preco/${listaId}/copiar-precos-base?sobrescrever=${sobrescrever}`, {})
}
