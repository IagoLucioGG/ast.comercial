import { ref } from 'vue'
import { ApiError, odataAction, odataRequest } from '@/services/api'
import { extrairLista, lerCampo } from '@/composables/useFunilFormato'
import { useToast } from '@/composables/useToast'

export interface MotivoPerdaItem {
  id: number
  nome: string
}

/**
 * Ações de fechamento de um negócio (Ganhar, Perder, Reabrir) e motivos de perda.
 * Reutilizável pelo kanban e pela tela de detalhe. Cada ação devolve `true` em sucesso;
 * em falha mostra o toast de erro e devolve `false`.
 */
export function useAcoesNegocio() {
  const toast = useToast()
  const processando = ref(false)
  const motivos = ref<MotivoPerdaItem[]>([])
  const carregandoMotivos = ref(false)

  function mensagem(e: unknown, padrao: string): string {
    return e instanceof ApiError || e instanceof Error ? e.message || padrao : padrao
  }

  async function executar(fn: () => Promise<unknown>, sucesso: string, falha: string): Promise<boolean> {
    if (processando.value) return false
    processando.value = true
    try {
      await fn()
      toast.success(sucesso)
      return true
    } catch (e) {
      toast.error(mensagem(e, falha))
      return false
    } finally {
      processando.value = false
    }
  }

  const ganhar = (id: number) =>
    executar(() => odataAction('Negocios', id, 'Ganhar'), 'Negócio marcado como ganho.', 'Não foi possível ganhar o negócio.')

  const reabrir = (id: number) =>
    executar(() => odataAction('Negocios', id, 'Reabrir'), 'Negócio reaberto.', 'Não foi possível reabrir o negócio.')

  /** Executa a ação Perder; o motivo (opcional) vai no corpo da própria ação. */
  const perder = (id: number, motivoPerdaId?: number | null) =>
    executar(
      () => odataAction('Negocios', id, 'Perder', motivoPerdaId ? { MotivoPerdaId: motivoPerdaId } : {}),
      'Negócio marcado como perdido.',
      'Não foi possível perder o negócio.',
    )

  async function carregarMotivos() {
    if (motivos.value.length || carregandoMotivos.value) return
    carregandoMotivos.value = true
    try {
      const r = await odataRequest<unknown>('GET', 'Negocios@MotivosPerda')
      motivos.value = extrairLista(r)
        .filter((m) => lerCampo(m, 'Ativo') !== false)
        .map((m) => ({
          id: Number(lerCampo(m, 'Id')),
          nome: String(lerCampo(m, 'Nome') ?? ''),
          ordem: Number(lerCampo(m, 'Ordem') ?? 0),
        }))
        .sort((a, b) => a.ordem - b.ordem)
        .map(({ id, nome }) => ({ id, nome }))
    } catch (e) {
      toast.error(mensagem(e, 'Não foi possível carregar os motivos de perda.'))
    } finally {
      carregandoMotivos.value = false
    }
  }

  return { processando, motivos, carregandoMotivos, ganhar, perder, reabrir, carregarMotivos }
}
