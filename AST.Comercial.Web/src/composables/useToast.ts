import { reactive } from 'vue'

export type ToastTipo = 'success' | 'error' | 'info' | 'warning'

export interface ToastItem {
  id: number
  tipo: ToastTipo
  mensagem: string
}

const estado = reactive<{ itens: ToastItem[] }>({ itens: [] })
let seq = 0

function mostrar(tipo: ToastTipo, mensagem: string, ms = 4000) {
  const id = ++seq
  estado.itens.push({ id, tipo, mensagem })
  setTimeout(() => remover(id), ms)
}

function remover(id: number) {
  const i = estado.itens.findIndex((t) => t.id === id)
  if (i >= 0) estado.itens.splice(i, 1)
}

/** Feedback para o usuário. Uso: const toast = useToast(); toast.error('Falhou') */
export function useToast() {
  return {
    itens: estado.itens,
    remover,
    success: (m: string) => mostrar('success', m),
    error: (m: string) => mostrar('error', m, 6000),
    info: (m: string) => mostrar('info', m),
    warning: (m: string) => mostrar('warning', m, 5000),
  }
}
