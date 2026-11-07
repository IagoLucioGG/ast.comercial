// Moedas carregadas uma única vez e compartilhadas por todas as grades.
import { odataQuery, montarQuery } from '@/services/api'
import type { MoedaCsv } from './csv'

let promessa: Promise<MoedaCsv[]> | null = null

export function carregarMoedas(): Promise<MoedaCsv[]> {
  if (!promessa) {
    const q = montarQuery({ $select: 'Id,Codigo,Simbolo,Padrao', $orderby: 'Id asc', $top: 100 })
    promessa = odataQuery<MoedaCsv>(`Moedas${q}`)
      .then(r => r.value ?? [])
      .catch(() => { promessa = null; return [] as MoedaCsv[] }) // falhou: tenta de novo na próxima grade
  }
  return promessa
}

/** Símbolo da moeda do registro (ou da padrão); sem moedas carregadas, cai em "R$". */
export function simboloDaMoeda(moedas: MoedaCsv[], moedaId: unknown): string {
  const m = (moedaId != null ? moedas.find(x => x.Id === Number(moedaId)) : undefined) ?? moedas.find(x => x.Padrao)
  return m?.Simbolo || m?.Codigo || 'R$'
}
