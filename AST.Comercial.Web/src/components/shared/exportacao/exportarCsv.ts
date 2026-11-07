// Exportação CSV no navegador: busca todos os registros (até 5.000) com o filtro/ordenação atuais.
import { odataTodos } from '@/services/api'
import { carregarMoedas } from './moedas'
import { LIMITE_EXPORTACAO, gerarCsv, montarTabelaCsv, nomeArquivoCsv, type ColunaCsv } from './csv'

export interface PedidoExportacao {
  entidade: string
  endpoint: string
  colunas: ColunaCsv[]
  filtro?: string
  orderby: string
  select?: string
  expand?: string
  campoMoeda?: string
}

export interface ResultadoExportacao { total: number; truncado: boolean; arquivo: string }

export async function exportarCsv(pedido: PedidoExportacao): Promise<ResultadoExportacao> {
  const partes: string[] = []
  if (pedido.filtro) partes.push(`$filter=${encodeURIComponent(pedido.filtro)}`)
  // Id como desempate mantém a paginação estável.
  partes.push(`$orderby=${/\bId\b/.test(pedido.orderby) ? pedido.orderby : `${pedido.orderby},Id asc`}`)
  if (pedido.select) partes.push(`$select=${pedido.select}`)
  if (pedido.expand) partes.push(`$expand=${pedido.expand}`)

  // Um registro além do limite basta para saber que truncou (odataTodos pagina de 100 em 100).
  const todos = await odataTodos<unknown>(`${pedido.endpoint}?${partes.join('&')}`, LIMITE_EXPORTACAO + 100)
  const truncado = todos.length > LIMITE_EXPORTACAO
  const itens = truncado ? todos.slice(0, LIMITE_EXPORTACAO) : todos

  const moedas = pedido.colunas.some(c => c.tipo === 'moeda') ? await carregarMoedas() : []
  const { cabecalho, linhas } = montarTabelaCsv(pedido.colunas, itens, { moedas, campoMoeda: pedido.campoMoeda })
  const arquivo = nomeArquivoCsv(pedido.entidade)

  const blob = new Blob([gerarCsv(cabecalho, linhas)], { type: 'text/csv;charset=utf-8' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = arquivo
  document.body.appendChild(a)
  a.click()
  a.remove()
  setTimeout(() => URL.revokeObjectURL(url), 1000)
  return { total: itens.length, truncado, arquivo }
}
