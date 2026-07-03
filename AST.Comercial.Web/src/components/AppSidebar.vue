<script setup lang="ts">
import { useRoute } from 'vue-router'
import logoMenor from '@/assets/logo-menor.png'
import emblema from '@/assets/emblema.png'

defineProps<{ collapsed: boolean }>()
defineEmits<{ toggle: [] }>()

const route = useRoute()

const menuItems = [
  { icon: 'mdi-view-dashboard-outline', label: 'Resumo', to: '/' },
  { icon: 'mdi-account-group-outline', label: 'Clientes', to: '/clientes' },
  { icon: 'mdi-handshake-outline', label: 'Negócios', to: '/negocios' },
  { icon: 'mdi-file-document-outline', label: 'Propostas', to: '/propostas' },
  { icon: 'mdi-package-variant-closed', label: 'Produtos', to: '/produtos' },
  { icon: 'mdi-chart-bar', label: 'Relatórios', to: '/relatorios' },
  { icon: 'mdi-cog-outline', label: 'Administração', to: '/admin' },
]

function isActive(to: string) {
  if (to === '/') return route.path === '/'
  return route.path.startsWith(to)
}
</script>

<template>
  <aside class="sidebar" :class="{ collapsed }">
    <div class="sidebar-header">
      <img v-if="!collapsed" :src="logoMenor" alt="AST Comercial" class="sidebar-logo" />
      <img v-else :src="emblema" alt="AST" class="sidebar-icon" />
    </div>

    <nav class="sidebar-nav">
      <router-link
        v-for="item in menuItems"
        :key="item.to"
        :to="item.to"
        class="nav-item"
        :class="{ active: isActive(item.to) }"
      >
        <i class="mdi" :class="item.icon"></i>
        <span v-if="!collapsed" class="nav-label">{{ item.label }}</span>
      </router-link>
    </nav>

    <button class="sidebar-toggle" @click="$emit('toggle')">
      <i class="mdi" :class="collapsed ? 'mdi-chevron-right' : 'mdi-chevron-left'"></i>
    </button>
  </aside>
</template>

<style scoped>
.sidebar {
  width: 220px;
  height: 100vh;
  background: var(--bg-secondary);
  border-right: 1px solid var(--border);
  display: flex;
  flex-direction: column;
  transition: width 0.2s ease;
  position: relative;
  flex-shrink: 0;
}

.sidebar.collapsed {
  width: 64px;
}

.sidebar-header {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 0 16px;
  border-bottom: 1px solid var(--border);
}

.sidebar-logo {
  height: 44px;
  width: auto;
  max-width: 180px;
}

.sidebar-icon {
  width: 28px;
  height: 28px;
}

.sidebar-nav {
  flex: 1;
  padding: 12px 8px;
  display: flex;
  flex-direction: column;
  gap: 2px;
  overflow-y: auto;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border-radius: 8px;
  color: var(--text-secondary);
  text-decoration: none;
  font-size: 14px;
  font-weight: 500;
  transition: background 0.15s, color 0.15s;
  white-space: nowrap;
}

.sidebar.collapsed .nav-item {
  justify-content: center;
  padding: 10px;
}

.nav-item:hover {
  background: var(--bg-elevated);
  color: var(--text-primary);
}

.nav-item.active {
  background: var(--accent);
  color: #000;
}

.nav-item .mdi {
  font-size: 20px;
  flex-shrink: 0;
}

.nav-label {
  overflow: hidden;
}

.sidebar-toggle {
  position: absolute;
  bottom: 16px;
  left: 50%;
  transform: translateX(-50%);
  width: 32px;
  height: 32px;
  border-radius: 50%;
  border: 1px solid var(--border);
  background: var(--bg-surface);
  color: var(--text-secondary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s;
}

.sidebar-toggle:hover {
  background: var(--bg-elevated);
  color: var(--text-primary);
}
</style>
