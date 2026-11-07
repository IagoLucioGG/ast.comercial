import { odataRequest } from '@/services/api'
import { campo, lista, type Registro } from '@/composables/useEntidadeDetalhe'

export interface Opcao { id: number; nome: string; familiaId?: number | null }

/** Carrega famílias e grupos de produto (cadastros auxiliares) ignorando inativos. */
export async function carregarFamiliasEGrupos(): Promise<{ familias: Opcao[]; grupos: Opcao[] }> {
  const [f, g] = await Promise.all([
    odataRequest<unknown>('GET', 'Produtos@Familias'),
    odataRequest<unknown>('GET', 'Produtos@Grupos'),
  ])
  const ativos = (r: unknown) => lista<Registro>(r).filter((x) => campo(x, 'Ativo') !== false)
  return {
    familias: ativos(f).map((x) => ({ id: Number(campo(x, 'Id')), nome: String(campo(x, 'Nome') ?? '') })),
    grupos: ativos(g).map((x) => ({
      id: Number(campo(x, 'Id')),
      nome: String(campo(x, 'Nome') ?? ''),
      familiaId: campo<number | null>(x, 'FamiliaProdutoId') ?? null,
    })),
  }
}

export const CATEGORIAS = [
  { valor: 'Produto', rotulo: 'Produto' },
  { valor: 'Servico', rotulo: 'Serviço' },
  { valor: 'Assinatura', rotulo: 'Assinatura' },
]

/** Converte texto de input numérico em número (ou null quando vazio/inválido). */
export function numeroOuNulo(v: unknown): number | null {
  if (v === '' || v === null || v === undefined) return null
  const n = Number(v)
  return Number.isFinite(n) ? n : null
}

/** Data (ISO) para <input type="date">. */
export function paraInputData(v: string | null | undefined): string {
  return v ? v.slice(0, 10) : ''
}

/** Data de <input type="date"> para ISO UTC (meia-noite), como o servidor exige fuso. */
export function deInputData(v: string): string | null {
  return v ? `${v}T00:00:00Z` : null
}
