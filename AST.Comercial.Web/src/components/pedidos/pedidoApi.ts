// Tipos e acesso à API de pedidos (OData em PascalCase; geração a partir de proposta em /api/pedidos, camelCase).
import { odataRequest, odataTodos, montarQuery } from '@/services/api'
import { apiAprov } from '@/components/aprovacoes/aprovacaoApi'
import type { Moeda } from '@/composables/usePropostaApi'

export interface EtapaPedido { Id: number; Nome: string; Ordem: number; Cor?: string | null }

export interface ItemPedido {
  Id: number
  ProdutoNome?: string | null
  ProdutoCodigo?: string | null
  Quantidade?: number | null
  PrecoUnitario?: number | null
  Desconto?: number | null
  Total?: number | null
  Ordem: number
}

export interface Pedido {
  Id: number
  Numero: number
  Titulo?: string | null
  Observacoes?: string | null
  Data: string
  Valor?: number | null
  Desconto?: number | null
  CustoFrete?: number | null
  NumeroParcelas?: number | null
  FormaPagamento?: string | null
  MoedaId?: number | null
  ClienteId?: number | null
  NegocioId?: number | null
  PropostaId?: number | null
  EtapaPedidoId?: number | null
  Cliente?: { Id: number; Nome?: string | null } | null
  Etapa?: EtapaPedido | null
  Itens?: ItemPedido[]
}

/** Número exibido do pedido: PED-000042. */
export function numeroDoPedido(p: { Id: number; Numero?: number | null }): string {
  return `PED-${String(p.Numero ?? p.Id).padStart(6, '0')}`
}

export function carregarPedido(id: number): Promise<Pedido> {
  const q = montarQuery({ $expand: 'Itens($orderby=Ordem),Etapa,Cliente($select=Id,Nome)' })
  return odataRequest<Pedido>('GET', `Pedidos(${id})${q}`)
}

export function listarEtapas(): Promise<EtapaPedido[]> {
  return odataTodos<EtapaPedido>(`EtapasPedido${montarQuery({ $orderby: 'Ordem' })}`)
}

export function buscarMoeda(id: number): Promise<Moeda> {
  return odataRequest<Moeda>('GET', `Moedas(${id})`)
}

export async function moverParaEtapa(pedidoId: number, etapaId: number): Promise<void> {
  await odataRequest('PATCH', `Pedidos(${pedidoId})`, { EtapaPedidoId: etapaId })
}

/** Gera o pedido de uma proposta aprovada/aceita. Erros 403/422 chegam como ApiError com a mensagem do servidor. */
export function gerarPedidoDeProposta(propostaId: number): Promise<{ id: number; numero: number }> {
  return apiAprov<{ id: number; numero: number }>('POST', '/api/pedidos/de-proposta', { propostaId })
}
