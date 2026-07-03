<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AdminPainel from '@/components/admin/AdminPainel.vue'

const route = useRoute()
const router = useRouter()

const painelAtivo = computed(() => route.params.painel as string | undefined)

interface AdminItem {
  icon: string
  label: string
  componente: string
  slug: string
}

interface AdminCategoria {
  titulo: string
  items: AdminItem[]
}

const categorias: AdminCategoria[] = [
  {
    titulo: 'Geral',
    items: [
      { icon: 'mdi-earth', label: 'Modelo do sistema', componente: 'ModeloSistema', slug: 'modelo-sistema' },
      { icon: 'mdi-form-select', label: 'Campos e formulários', componente: 'CamposFormularios', slug: 'campos-formularios' },
      { icon: 'mdi-account-supervisor', label: 'Equipes e colaboradores', componente: 'Equipes', slug: 'equipes' },
      { icon: 'mdi-domain', label: 'Dados da empresa', componente: 'DadosEmpresa', slug: 'dados-empresa' },
      { icon: 'mdi-cog-outline', label: 'Configurações de campos', componente: 'ConfigCampos', slug: 'config-campos' },
      { icon: 'mdi-auto-fix', label: 'Automações', componente: 'Automacoes', slug: 'automacoes' },
      { icon: 'mdi-filter-outline', label: 'Funis', componente: 'Funis', slug: 'funis' },
      { icon: 'mdi-label-outline', label: 'Etiquetas', componente: 'Etiquetas', slug: 'etiquetas' },
    ]
  },
  {
    titulo: 'Usuários',
    items: [
      { icon: 'mdi-account-multiple-outline', label: 'Tabela de usuários', componente: 'TabelaUsuarios', slug: 'usuarios' },
      { icon: 'mdi-shield-account-outline', label: 'Papéis de usuários', componente: 'Papeis', slug: 'papeis' },
      { icon: 'mdi-account-key-outline', label: 'Perfis de usuários', componente: 'Perfis', slug: 'perfis' },
      { icon: 'mdi-star-outline', label: 'Permissões do sistema', componente: 'Permissoes', slug: 'permissoes' },
      { icon: 'mdi-history', label: 'Registro de atividades', componente: 'RegistroAtividades', slug: 'atividades' },
    ]
  },
  {
    titulo: 'Propostas e Vendas',
    items: [
      { icon: 'mdi-file-document-multiple-outline', label: 'Modelos de documentos', componente: 'ModelosDocumentos', slug: 'modelos-documentos' },
      { icon: 'mdi-table-large', label: 'Tabela de comissão', componente: 'TabelaComissao', slug: 'comissao' },
    ]
  },
  {
    titulo: 'Integrações',
    items: [
      { icon: 'mdi-puzzle-outline', label: 'Integrações e aplicativos', componente: 'Integracoes', slug: 'integracoes' },
      { icon: 'mdi-account-convert-outline', label: 'Usuários de integração', componente: 'UsuariosIntegracao', slug: 'usuarios-integracao' },
      { icon: 'mdi-key-outline', label: 'Tokens de autenticação', componente: 'Tokens', slug: 'tokens' },
    ]
  },
]

const painelInfo = computed(() => {
  if (!painelAtivo.value) return null
  for (const cat of categorias) {
    const item = cat.items.find(i => i.slug === painelAtivo.value)
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
      <div v-for="cat in categorias" :key="cat.titulo" class="admin-categoria">
        <h3 class="categoria-titulo">{{ cat.titulo }}</h3>
        <div class="categoria-items">
          <button
            v-for="item in cat.items"
            :key="item.componente"
            class="admin-card"
            @click="abrirPainel(item)"
          >
            <i class="mdi" :class="item.icon"></i>
            <span>{{ item.label }}</span>
          </button>
        </div>
      </div>
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
  max-width: 1200px;
  margin: 0 auto;
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
}

.admin-card::before {
  content: '';
  position: absolute;
  inset: 0;
  background: linear-gradient(135deg, rgba(6, 182, 212, 0.05), transparent);
  opacity: 0;
  transition: opacity 0.2s ease;
}

.admin-card:hover {
  border-color: var(--accent);
  color: var(--text-primary);
  transform: translateY(-2px);
  box-shadow: 0 4px 16px rgba(6, 182, 212, 0.1);
}

.admin-card:hover::before {
  opacity: 1;
}

.admin-card:active {
  transform: translateY(0);
}

.admin-card .mdi {
  font-size: 20px;
  color: var(--accent);
  flex-shrink: 0;
  transition: transform 0.2s ease;
}

.admin-card:hover .mdi {
  transform: scale(1.15);
}
</style>
