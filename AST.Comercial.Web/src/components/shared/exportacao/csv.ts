// Lógica pura (sem Vue, sem rede) da exportação CSV: formatação das células, escape e proteção
// contra injeção de fórmula. Fica separada para poder ser testada fora do navegador.

export interface MoedaCsv { Id: number; Codigo: string; Simbolo?: string | null; Padrao?: boolean }

export interface ColunaCsv {
  campo: string
  label: string
  tipo?: 'texto' | 'data' | 'badge' | 'status' | 'moeda' | 'avatar' | 'booleano' | 'enum'
  /** Rótulos legíveis por valor, usado pelo tipo `enum`. */
  rotulos?: Record<string, string>
}

export const LIMITE_EXPORTACAO = 5000
const BOM = '\uFEFF'
const SEPARADOR = ';'

/** Lê um campo, aceitando caminho de navegação ("Cliente/Nome"). */
export function valorDoCampo(item: unknown, campo: string): unknown {
  let val: any = item
  for (const parte of campo.split('/')) {
    val = val?.[parte]
    if (val === undefined || val === null) return val
  }
  return val
}

/** "EmRevisao" -> "Em revisao" (usado quando o enum não tem rótulo mapeado). */
export function humanizarEnum(valor: string): string {
  const separado = valor.replace(/([a-z0-9])([A-Z])/g, '$1 $2').replace(/_/g, ' ').trim()
  return separado ? separado.charAt(0).toUpperCase() + separado.slice(1).toLowerCase() : separado
}

/** Prefixa com ' valores de texto que uma planilha interpretaria como fórmula. */
export function protegerFormula(texto: string): string {
  return /^[=+\-@\t\r]/.test(texto) ? `'${texto}` : texto
}

function doisDigitos(n: number) { return String(n).padStart(2, '0') }

/** dd/mm/aaaa (data local, como a grade). Retorna null se não for uma data válida. */
export function formatarDataBr(val: unknown): string | null {
  const d = val instanceof Date ? val : new Date(String(val))
  if (Number.isNaN(d.getTime())) return null
  return `${doisDigitos(d.getDate())}/${doisDigitos(d.getMonth() + 1)}/${d.getFullYear()}`
}

/** Número com vírgula decimal e sem separador de milhar (para a planilha reconhecer como número). */
export function formatarNumeroCsv(n: number): string {
  return String(n).replace('.', ',')
}

/** Converte o valor de uma célula para o texto final do CSV (já protegido contra fórmula). */
export function formatarCelulaCsv(valor: unknown, col: Pick<ColunaCsv, 'tipo' | 'rotulos'>): string {
  if (valor === null || valor === undefined) return ''
  switch (col.tipo) {
    case 'data': {
      const d = formatarDataBr(valor)
      if (d) return d
      break
    }
    case 'moeda': {
      const n = Number(valor)
      if (Number.isFinite(n)) return formatarNumeroCsv(n)
      break
    }
    case 'status': return valor ? 'Ativo' : 'Inativo'
    case 'booleano': return valor ? 'Sim' : 'Não'
    case 'enum': {
      const chave = String(valor)
      return protegerFormula(col.rotulos?.[chave] ?? humanizarEnum(chave))
    }
  }
  if (typeof valor === 'number' && Number.isFinite(valor)) return formatarNumeroCsv(valor)
  if (typeof valor === 'boolean') return valor ? 'Sim' : 'Não'
  return protegerFormula(String(valor))
}

/** Escapa um campo: aspas quando tem separador, aspas ou quebra de linha. */
export function escaparCampo(texto: string): string {
  return /[;"\r\n]/.test(texto) ? `"${texto.replace(/"/g, '""')}"` : texto
}

export interface OpcoesTabelaCsv {
  moedas?: MoedaCsv[]
  /** Campo do registro que guarda o Id da moeda (ex.: 'MoedaId'). */
  campoMoeda?: string
}

/** Moeda do registro; sem moeda no registro, usa a padrão. */
export function moedaDoRegistro(item: unknown, moedas: MoedaCsv[] = [], campoMoeda?: string): MoedaCsv | undefined {
  const id = campoMoeda ? valorDoCampo(item, campoMoeda) : null
  const doRegistro = id != null ? moedas.find(m => m.Id === Number(id)) : undefined
  return doRegistro ?? moedas.find(m => m.Padrao)
}

/** Monta cabeçalho e linhas. Colunas de moeda ganham uma coluna "Moeda" (código) ao lado, pois o valor sai sem símbolo. */
export function montarTabelaCsv(colunas: ColunaCsv[], itens: unknown[], opcoes: OpcoesTabelaCsv = {}) {
  const cabecalho: string[] = []
  const moedasCols = colunas.filter(c => c.tipo === 'moeda')
  for (const c of colunas) {
    cabecalho.push(c.label)
    if (c.tipo === 'moeda') cabecalho.push(moedasCols.length > 1 ? `Moeda (${c.label})` : 'Moeda')
  }
  const linhas = itens.map(item => {
    const linha: string[] = []
    for (const c of colunas) {
      linha.push(formatarCelulaCsv(valorDoCampo(item, c.campo), c))
      if (c.tipo === 'moeda') {
        const vazio = valorDoCampo(item, c.campo) == null
        linha.push(vazio ? '' : (moedaDoRegistro(item, opcoes.moedas, opcoes.campoMoeda)?.Codigo ?? ''))
      }
    }
    return linha
  })
  return { cabecalho, linhas }
}

/** Texto do arquivo: BOM UTF-8, separador ';', quebra de linha CRLF. */
export function gerarCsv(cabecalho: string[], linhas: string[][]): string {
  const todas = [cabecalho, ...linhas]
  return BOM + todas.map(l => l.map(escaparCampo).join(SEPARADOR)).join('\r\n') + '\r\n'
}

/** `<entidade>-AAAA-MM-DD.csv` */
export function nomeArquivoCsv(entidade: string, data: Date = new Date()): string {
  const slug = entidade.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '') || 'dados'
  return `${slug}-${data.getFullYear()}-${doisDigitos(data.getMonth() + 1)}-${doisDigitos(data.getDate())}.csv`
}
