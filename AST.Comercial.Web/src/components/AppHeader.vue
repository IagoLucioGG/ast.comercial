<script setup lang="ts">
import { ref } from 'vue'
import { useAuthStore } from '@/stores/auth'
import { useTemaStore } from '@/stores/tema'

defineEmits<{ 'toggle-sidebar': [] }>()

const auth = useAuthStore()
const tema = useTemaStore()
const menuAberto = ref(false)
</script>

<template>
  <header class="app-header">
    <div class="header-left">
      <button class="btn-icon" @click="$emit('toggle-sidebar')" aria-label="Menu">
        <i class="mdi mdi-menu"></i>
      </button>
      <div class="search-box">
        <i class="mdi mdi-magnify"></i>
        <input type="text" placeholder="Buscar..." />
      </div>
    </div>

    <div class="header-right">
      <button class="btn-icon" @click="tema.alternar" :aria-label="tema.dark ? 'Modo claro' : 'Modo escuro'">
        <i class="mdi" :class="tema.dark ? 'mdi-weather-sunny' : 'mdi-weather-night'"></i>
      </button>

      <button class="btn-icon notification-btn" aria-label="Notificações">
        <i class="mdi mdi-bell-outline"></i>
        <span class="notification-badge"></span>
      </button>

      <div class="user-menu" @click="menuAberto = !menuAberto">
        <div class="user-avatar">
          {{ auth.iniciais }}
        </div>
        <span class="user-name">{{ auth.nome }}</span>
        <i class="mdi mdi-chevron-down"></i>

        <div v-if="menuAberto" class="dropdown">
          <button class="dropdown-item">
            <i class="mdi mdi-account-outline"></i> Meu Perfil
          </button>
          <button class="dropdown-item">
            <i class="mdi mdi-cog-outline"></i> Configurações
          </button>
          <hr class="dropdown-divider" />
          <button class="dropdown-item danger" @click="auth.logout()">
            <i class="mdi mdi-logout"></i> Sair
          </button>
        </div>
      </div>
    </div>
  </header>
</template>

<style scoped>
.app-header {
  height: 56px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  border-bottom: 1px solid var(--border);
  background: var(--bg-primary);
  flex-shrink: 0;
}

.header-left, .header-right {
  display: flex;
  align-items: center;
  gap: 12px;
}

.btn-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  border: none;
  background: transparent;
  color: var(--text-secondary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s, color 0.15s;
}

.btn-icon:hover {
  background: var(--bg-elevated);
  color: var(--text-primary);
}

.btn-icon .mdi {
  font-size: 20px;
}

.search-box {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--bg-surface);
  border: 1px solid var(--border);
  border-radius: 8px;
  padding: 6px 12px;
  width: 280px;
}

.search-box .mdi {
  color: var(--text-muted);
  font-size: 18px;
}

.search-box input {
  border: none;
  background: transparent;
  color: var(--text-primary);
  font-size: 13px;
  outline: none;
  width: 100%;
}

.search-box input::placeholder {
  color: var(--text-muted);
}

.notification-btn {
  position: relative;
}

.notification-badge {
  position: absolute;
  top: 6px;
  right: 6px;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #ef4444;
}

.user-menu {
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: 8px;
  transition: background 0.15s;
  position: relative;
}

.user-menu:hover {
  background: var(--bg-elevated);
}

.user-avatar {
  width: 32px;
  height: 32px;
  border-radius: 50%;
  background: var(--accent);
  color: #000;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
  font-weight: 700;
}

.user-name {
  color: var(--text-primary);
  font-size: 13px;
  font-weight: 500;
}

.user-menu .mdi-chevron-down {
  color: var(--text-muted);
  font-size: 16px;
}

.dropdown {
  position: absolute;
  top: 100%;
  right: 0;
  margin-top: 8px;
  background: var(--bg-surface);
  border: 1px solid var(--border);
  border-radius: 10px;
  padding: 6px;
  min-width: 180px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.3);
  z-index: 100;
}

.dropdown-item {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  padding: 8px 12px;
  border: none;
  background: none;
  color: var(--text-secondary);
  font-size: 13px;
  cursor: pointer;
  border-radius: 6px;
  transition: background 0.15s;
}

.dropdown-item:hover {
  background: var(--bg-elevated);
  color: var(--text-primary);
}

.dropdown-item.danger:hover {
  background: rgba(239, 68, 68, 0.1);
  color: #ef4444;
}

.dropdown-divider {
  border: none;
  border-top: 1px solid var(--border);
  margin: 4px 0;
}
</style>
