import { onBeforeUnmount, onMounted } from 'vue'

export interface AtalhoInfo {
  teclas: string[]
  descricao: string
}

/** Lista exibida no modal de ajuda (?). */
export const ATALHOS: AtalhoInfo[] = [
  { teclas: ['/'], descricao: 'Focar a busca global' },
  { teclas: ['g', 'c'], descricao: 'Ir para Clientes' },
  { teclas: ['g', 'n'], descricao: 'Ir para Negócios' },
  { teclas: ['g', 'a'], descricao: 'Ir para Atividades' },
  { teclas: ['g', 'p'], descricao: 'Ir para Propostas' },
  { teclas: ['?'], descricao: 'Mostrar esta lista de atalhos' },
  { teclas: ['Esc'], descricao: 'Fechar janelas e a busca' },
]

export interface AtalhosHandlers {
  buscar: () => void
  navegar: (caminho: string) => void
  ajuda: () => void
}

const DESTINOS_G: Record<string, string> = {
  c: '/clientes',
  n: '/negocios',
  a: '/atividades',
  p: '/propostas',
}

const TEMPO_SEQUENCIA_MS = 1200

function emCampoDeTexto(alvo: EventTarget | null): boolean {
  const el = alvo as HTMLElement | null
  if (!el || !el.tagName) return false
  const tag = el.tagName
  if (tag === 'INPUT') {
    const tipo = (el as HTMLInputElement).type
    // Checkbox, rádio e botões não digitam texto.
    return !['checkbox', 'radio', 'button', 'submit', 'reset'].includes(tipo)
  }
  return tag === 'TEXTAREA' || tag === 'SELECT' || el.isContentEditable
}

/**
 * Atalhos globais de teclado. Registra no mount e remove no unmount.
 * Ignorados quando o foco está em campo de texto ou com Ctrl/Alt/Meta pressionados.
 */
export function useAtalhos(h: AtalhosHandlers) {
  let aguardandoG = false
  let timer: number | undefined

  function limpar() {
    aguardandoG = false
    if (timer) { clearTimeout(timer); timer = undefined }
  }

  function onKey(e: KeyboardEvent) {
    if (e.ctrlKey || e.metaKey || e.altKey) return
    if (emCampoDeTexto(e.target)) return

    if (aguardandoG) {
      const destino = DESTINOS_G[e.key.toLowerCase()]
      limpar()
      if (destino) { e.preventDefault(); h.navegar(destino) }
      return
    }

    if (e.key === '/') { e.preventDefault(); h.buscar() }
    else if (e.key === '?') { e.preventDefault(); h.ajuda() }
    else if (e.key === 'g' || e.key === 'G') {
      aguardandoG = true
      timer = window.setTimeout(limpar, TEMPO_SEQUENCIA_MS)
    }
  }

  onMounted(() => window.addEventListener('keydown', onKey))
  onBeforeUnmount(() => {
    window.removeEventListener('keydown', onKey)
    limpar()
  })
}
