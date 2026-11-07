// Tipos, metadados e utilitários de data compartilhados pela tela de Atividades.
import { odataTodos } from '@/services/api'

export const TIPOS = ['Tarefa', 'Ligacao', 'Email', 'Reuniao', 'Nota'] as const
export type TipoAtividade = (typeof TIPOS)[number]

export const TIPO_META: Record<TipoAtividade, { rotulo: string; icone: string; cor: string }> = {
  Tarefa: { rotulo: 'Tarefa', icone: 'mdi-checkbox-marked-circle-outline', cor: '#3b82f6' },
  Ligacao: { rotulo: 'Ligação', icone: 'mdi-phone-outline', cor: '#22c55e' },
  Email: { rotulo: 'E-mail', icone: 'mdi-email-outline', cor: '#f59e0b' },
  Reuniao: { rotulo: 'Reunião', icone: 'mdi-account-group-outline', cor: '#a855f7' },
  Nota: { rotulo: 'Nota', icone: 'mdi-note-text-outline', cor: '#64748b' },
}

export interface Atividade {
  Id: number
  Titulo: string
  Descricao?: string | null
  Tipo: TipoAtividade
  DataVencimento?: Date | null
  ConcluidaEm?: Date | null
  Concluida: boolean
  ClienteId?: number | null
  PessoaContatoId?: number | null
  NegocioId?: number | null
  ClienteNome?: string | null
  ContatoNome?: string | null
  NegocioTitulo?: string | null
  ResponsavelId?: number | null
  ResponsavelNome?: string | null
}

/** Converte a linha crua do OData (Tipo pode vir como texto ou número; datas como ISO) em Atividade. */
export function normalizar(r: Record<string, any>): Atividade {
  const tipoBruto = r.Tipo
  const tipo: TipoAtividade = typeof tipoBruto === 'number'
    ? (TIPOS[tipoBruto] ?? 'Tarefa')
    : ((TIPOS as readonly string[]).includes(tipoBruto) ? tipoBruto : 'Tarefa')
  return {
    Id: r.Id,
    Titulo: r.Titulo ?? '',
    Descricao: r.Descricao ?? null,
    Tipo: tipo,
    DataVencimento: lerData(r.DataVencimento),
    ConcluidaEm: lerData(r.ConcluidaEm),
    Concluida: !!r.Concluida,
    ClienteId: r.ClienteId ?? null,
    PessoaContatoId: r.PessoaContatoId ?? null,
    NegocioId: r.NegocioId ?? null,
    ClienteNome: r.Cliente?.Nome ?? null,
    ContatoNome: r.PessoaContato?.Nome ?? null,
    NegocioTitulo: r.Negocio?.Titulo ?? null,
    ResponsavelId: r.ResponsavelId ?? null,
    ResponsavelNome: r.Responsavel?.Nome ?? null,
  }
}

/** Datas sem fuso vindas do servidor são tratadas como UTC. */
function lerData(v: unknown): Date | null {
  if (typeof v !== 'string' || !v) return null
  const temFuso = /([zZ]|[+-]\d{2}:?\d{2})$/.test(v)
  const d = new Date(temFuso ? v : v + 'Z')
  return isNaN(d.getTime()) ? null : d
}

export function chaveDia(d: Date): string {
  const m = String(d.getMonth() + 1).padStart(2, '0')
  const dia = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${m}-${dia}`
}

export function inicioDoDia(d: Date): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate())
}

export function somarDias(d: Date, n: number): Date {
  return new Date(d.getFullYear(), d.getMonth(), d.getDate() + n, d.getHours(), d.getMinutes())
}

export function formatarHora(d: Date): string {
  return d.toLocaleTimeString('pt-BR', { hour: '2-digit', minute: '2-digit' })
}

export function formatarDataHora(d: Date): string {
  return d.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }) + ' ' + formatarHora(d)
}

/** Intervalo exibido pelo calendário (semana começa no domingo). */
export type ModoCalendario = 'mes' | 'semana'

export function intervaloCalendario(modo: ModoCalendario, ref: Date): { inicio: Date; fim: Date; dias: Date[] } {
  let inicio: Date
  let total: number
  if (modo === 'semana') {
    inicio = somarDias(inicioDoDia(ref), -ref.getDay())
    total = 7
  } else {
    const primeiro = new Date(ref.getFullYear(), ref.getMonth(), 1)
    inicio = somarDias(primeiro, -primeiro.getDay())
    const ultimo = new Date(ref.getFullYear(), ref.getMonth() + 1, 0)
    total = Math.ceil((ultimo.getDate() + primeiro.getDay()) / 7) * 7
  }
  const dias = Array.from({ length: total }, (_, i) => somarDias(inicio, i))
  return { inicio, fim: somarDias(inicio, total), dias }
}

/** Valor para <input type="date"> e <input type="time"> a partir de Date local. */
export function paraInputData(d: Date): string { return chaveDia(d) }
export function paraInputHora(d: Date): string {
  return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}`
}

/** Parâmetro de expansão usado em todas as consultas de atividades (nomes reais de navegação da entidade). */
export const EXPAND_ATIVIDADE =
  '$expand=Cliente($select=Id,Nome),Negocio($select=Id,Titulo),PessoaContato($select=Id,Nome),Responsavel($select=Id,Nome)'

/** Iniciais (até 2 letras) pela primeira letra de cada palavra, ignorando o prefixo "[Demo]". */
export function iniciais(nome?: string | null): string {
  const limpo = (nome ?? '').replace(/\[[^\]]*\]/g, '').trim()
  if (!limpo) return '?'
  const partes = limpo.split(/\s+/).filter(Boolean)
  const letra = (p: string) => (p.match(/\p{L}/u)?.[0] ?? '')
  const a = letra(partes[0] ?? '')
  const b = partes.length > 1 ? letra(partes[partes.length - 1] ?? '') : ''
  return ((a + b) || '?').toUpperCase()
}

/** Nome sem o prefixo "[Demo]" para exibição curta. */
export function nomeCurto(nome?: string | null): string {
  return (nome ?? '').replace(/\[[^\]]*\]\s*/g, '').trim()
}

export interface UsuarioOpcao { Id: number; Nome: string }

/** Usuários ativos da empresa (para o seletor de responsável e o filtro). */
export function carregarUsuariosAtivos(): Promise<UsuarioOpcao[]> {
  return odataTodos<UsuarioOpcao>(`Usuarios?$select=Id,Nome&$filter=${encodeURIComponent('Ativo eq true')}&$orderby=Nome,Id`)
}

/** Id do usuário logado, lido do localStorage (mesmo formato gravado por stores/auth.ts). */
export function usuarioLogadoId(): number | null {
  try {
    const u = JSON.parse(localStorage.getItem('usuario') ?? 'null')
    return typeof u?.usuarioId === 'number' ? u.usuarioId : null
  } catch { return null }
}
