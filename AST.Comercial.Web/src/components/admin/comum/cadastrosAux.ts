import { defineComponent, h } from 'vue'
import CadastroAuxAdmin from '../CadastroAuxAdmin.vue'

type Tipo = 'origens' | 'ramos' | 'portes' | 'status-cliente' | 'motivos-perda' | 'status-negocio' | 'familias-produto' | 'grupos-produto'

function para(tipo: Tipo) {
  return defineComponent({ name: `Cadastro_${tipo}`, render: () => h(CadastroAuxAdmin, { tipo }) })
}

/** Um componente por lista de apoio, para o mapa slug -> componente do AdminPainel (que não passa props). */
export const OrigensAdmin = para('origens')
export const RamosAdmin = para('ramos')
export const PortesAdmin = para('portes')
export const StatusClienteAdmin = para('status-cliente')
export const MotivosPerdaAdmin = para('motivos-perda')
export const StatusNegocioAdmin = para('status-negocio')
export const FamiliasProdutoAdmin = para('familias-produto')
export const GruposProdutoAdmin = para('grupos-produto')
