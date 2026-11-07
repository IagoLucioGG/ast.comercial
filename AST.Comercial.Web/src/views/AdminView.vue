<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AdminPainel from '@/components/admin/AdminPainel.vue'
import UiBadge from '@/components/ui/UiBadge.vue'

const route = useRoute()
const router = useRouter()
const painelAtivo = computed(() => route.params.painel as string | undefined)

interface AdminItem {
  icon: string
  label: string
  /** Chave do mapa de componentes em AdminPainel.vue. */
  componente: string
  slug: string
  emBreve?: boolean
}

interface AdminCategoria {
  titulo: string
  items: AdminItem[]
}

const categorias: AdminCategoria[] = [
  {
    titulo: 'Geral',
    items: [
      { icon: 'mdi-domain', label: 'Dados da empresa', componente: 'DadosEmpresa', slug: 'dados-empresa' },
      { icon: 'mdi-form-select', label: 'Campos e formulários', componente: 'CamposFormularios', slug: 'campos-formularios' },
      { icon: 'mdi-filter-outline', label: 'Funis', componente: 'Funis', slug: 'funis' },
      { icon: 'mdi-auto-fix', label: 'Automações', componente: 'Automacoes', slug: 'automacoes' },
      { icon: 'mdi-timer-sand', label: 'Fila de automações', componente: 'FilaProcessamento', slug: 'fila-automacoes' },
      { icon: 'mdi-tag-multiple-outline', label: 'Etiquetas', componente: 'Etiquetas', slug: 'etiquetas' },
      { icon: 'mdi-currency-usd', label: 'Moedas', componente: 'Moedas', slug: 'moedas' },
    ],
  },
  {
    titulo: 'Usuários',
    items: [
      { icon: 'mdi-account-multiple-outline', label: 'Tabela de usuários', componente: 'TabelaUsuarios', slug: 'usuarios' },
      { icon: 'mdi-account-supervisor', label: 'Equipes e colaboradores', componente: 'Equipes', slug: 'equipes' },
      { icon: 'mdi-briefcase-outline', label: 'Cargos', componente: 'Cargos', slug: 'cargos' },
      { icon: 'mdi-sitemap-outline', label: 'Departamentos', componente: 'Departamentos', slug: 'departamentos' },
      { icon: 'mdi-shield-account-outline', label: 'Perfis e permissões', componente: 'Perfis', slug: 'perfis' },
      { icon: 'mdi-history', label: 'Registro de atividades', componente: 'RegistroAtividades', slug: 'atividades' },
    ],
  },
  {
    titulo: 'Clientes e produtos',
    items: [
      { icon: 'mdi-account-arrow-right-outline', label: 'Origens de contato', componente: 'Origens', slug: 'origens' },
      { icon: 'mdi-factory', label: 'Ramos de atividade', componente: 'Ramos', slug: 'ramos' },
      { icon: 'mdi-office-building-outline', label: 'Portes de empresa', componente: 'Portes', slug: 'portes' },
      { icon: 'mdi-list-status', label: 'Status de cliente', componente: 'StatusCliente', slug: 'status-cliente' },
      { icon: 'mdi-folder-outline', label: 'Famílias de produto', componente: 'FamiliasProduto', slug: 'familias-produto' },
      { icon: 'mdi-folder-multiple-outline', label: 'Grupos de produto', componente: 'GruposProduto', slug: 'grupos-produto' },
    ],
  },
  {
    titulo: 'Propostas e vendas',
    items: [
      { icon: 'mdi-file-document-multiple-outline', label: 'Modelos de documentos', componente: 'ModelosDocumentos', slug: 'modelos-documentos' },
      { icon: 'mdi-flag-variant-outline', label: 'Status de negócio', componente: 'StatusNegocio', slug: 'status-negocio' },
      { icon: 'mdi-thumb-down-outline', label: 'Motivos de perda', componente: 'MotivosPerda', slug: 'motivos-perda' },
      { icon: 'mdi-shield-check-outline', label: 'Fluxos de aprovação', componente: 'FluxosAprovacao', slug: 'fluxos-aprovacao' },
    ],
  },
  {
    titulo: 'Integrações',
    items: [
      { icon: 'mdi-puzzle-outline', label: 'Integrações e aplicativos', componente: 'Integracoes', slug: 'integracoes' },
      { icon: 'mdi-account-convert-outline', label: 'Usuários de integração', componente: 'UsuariosIntegracao', slug: 'usuarios-integracao' },
      { icon: 'mdi-cloud-download-outline', label: 'Importar da Ploomes', componente: 'ImportadorPloomes', slug: 'importar-ploomes' },
      { icon: 'mdi-database-import-outline', label: 'Importação de dados', componente: 'ImportacaoDados', slug: 'importacao-dados' },
    ],
  },
]

// Links antigos continuam funcionando.
const ALIAS: Record<string, string> = { papeis: 'perfis', permissoes: 'perfis' }

const painelInfo = computed(() => {
  const slug = painelAtivo.value ? (ALIAS[painelAtivo.value] ?? painelAtivo.value) : undefined
  if (!slug) return null
  for (const cat of categorias) {
    const item = cat.items.find((i) => i.slug === slug)
    if (item) return item
  }
  return null
})

function abrirPainel(item: AdminItem) {
  router.push({ name: 'admin-painel', params: { painel: item.slug } })
}

function fecharPainel() {
  router.push({ name: 'admin' })
}
</script>

<template>
  <div class="admin-page">
    <div class="admin-grid">
      <section v-for="cat in categorias" :key="cat.titulo" class="admin-categoria" :aria-label="cat.titulo">
        <h3 class="categoria-titulo">{{ cat.titulo }}</h3>
        <div class="categoria-items">
          <button
            v-for="item in cat.items"
            :key="item.slug"
            class="admin-card"
            :class="{ 'em-breve': item.emBreve }"
            @click="abrirPainel(item)"
          >
            <i class="mdi" :class="item.icon"></i>
            <span class="admin-card-rotulo">{{ item.label }}</span>
            <UiBadge v-if="item.emBreve" tom="aviso">Em breve</UiBadge>
          </button>
        </div>
      </section>
    </div>

    <AdminPainel
      v-if="painelInfo"
      :titulo="painelInfo.label"
      :componente="painelInfo.componente"
      @fechar="fecharPainel"
    />
  </div>
</template>

<style scoped>
.admin-page {
  position: relative;
  height: 100%;
}

.admin-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 32px;
}

.admin-categoria {
  display: flex;
  flex-direction: column;
}

.categoria-titulo {
  font-size: 12px;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 1px;
  margin-bottom: 14px;
}

.categoria-items {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.admin-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 14px 16px;
  border: 1px solid var(--border);
  background: var(--bg-surface);
  color: var(--text-secondary);
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  border-radius: 10px;
  transition: all 0.2s ease;
  text-align: left;
  width: 100%;
  position: relative;
  overflow: hidden;
  font-family: inherit;
}

.admin-card-rotulo { flex: 1; min-width: 0; }

.admin-card::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, rgba(6, 182, 212, 0.05), transparent);
  opacity: 0;
  transition: opacity 0.2s ease;
  pointer-events: none;
}

.admin-card:hover {
  border-color: var(--accent);
  color: var(--text-primary);
  transform: translateY(-2px);
  box-shadow: 0 4px 16px rgba(6, 182, 212, 0.1);
}

.admin-card:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }

.admin-card:hover::before { opacity: 1; }
.admin-card:active { transform: translateY(0); }

.admin-card.em-breve { opacity: 0.8; border-style: dashed; }

.admin-card .mdi {
  font-size: 20px;
  color: var(--accent);
  flex-shrink: 0;
  transition: transform 0.2s ease;
}

.admin-card:hover .mdi { transform: scale(1.15); }
</style>
