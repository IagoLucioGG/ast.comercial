import { computed, type Ref } from 'vue'

/** Linha mínima necessária para o cálculo de totais. */
export interface LinhaCalculo {
  Quantidade?: number | null
  PrecoUnitario?: number | null
  /** Desconto em porcentagem (0 a 100) */
  Desconto?: number | null
}

/** Arredonda a 2 casas evitando erro de ponto flutuante (ex.: 1.005). */
export function arredondar(v: number): number {
  if (!Number.isFinite(v)) return 0
  return Math.round((v + Number.EPSILON) * 100) / 100
}

/** Total da linha: quantidade × preço × (1 − desc%). */
export function totalLinha(l: LinhaCalculo): number {
  const q = Number(l.Quantidade ?? 0)
  const p = Number(l.PrecoUnitario ?? 0)
  const d = Math.min(100, Math.max(0, Number(l.Desconto ?? 0)))
  return arredondar(q * p * (1 - d / 100))
}

/** Símbolos por código de moeda, vindos de odata/Moedas (ex.: BRL → "R$", USD → "$"). */
const simbolos = new Map<string, string>()

/** Registra os símbolos reais das moedas (chamar antes de exibir valores). */
export function registrarSimbolos(moedas: { Codigo?: string | null; Simbolo?: string | null }[]) {
  for (const m of moedas) if (m.Codigo && m.Simbolo) simbolos.set(m.Codigo.toUpperCase(), m.Simbolo)
}

/**
 * Formata dinheiro na moeda da proposta (código ISO). Usa o símbolo cadastrado em Moedas;
 * sem cadastro, usa o formato internacional do código; sem moeda conhecida, só o número
 * (nunca assume "R$").
 */
export function formatarMoeda(v: number | null | undefined, moeda?: string | null): string {
  const n = arredondar(Number(v ?? 0))
  const codigo = (moeda ?? '').trim().toUpperCase()
  const numero = n.toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  if (!codigo) return numero
  const simbolo = simbolos.get(codigo)
  if (simbolo) return `${simbolo} ${numero}`
  try {
    return n.toLocaleString('pt-BR', { style: 'currency', currency: codigo })
  } catch {
    return `${codigo} ${numero}`
  }
}

/**
 * Totais da proposta calculados no cliente, em tempo real.
 * `descontoGeral` e `frete` são valores monetários (não porcentagem).
 * O servidor (ação Recalcular) continua sendo a fonte final.
 */
export function usePropostaTotais(
  linhas: Ref<LinhaCalculo[]>,
  descontoGeral: Ref<number | null | undefined>,
  frete: Ref<number | null | undefined>,
) {
  const subtotal = computed(() => arredondar(linhas.value.reduce((s, l) => s + totalLinha(l), 0)))
  const desconto = computed(() => arredondar(Math.max(0, Number(descontoGeral.value ?? 0) || 0)))
  const freteValor = computed(() => arredondar(Number(frete.value ?? 0) || 0))
  // mesma regra do servidor (Recalcular): Σ itens − desconto + frete, mínimo 0
  const total = computed(() => Math.max(0, arredondar(subtotal.value - desconto.value + freteValor.value)))
  return { subtotal, desconto, frete: freteValor, total }
}
