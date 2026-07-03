<script setup lang="ts">
import { ref } from 'vue'
import { RouterView } from 'vue-router'
import AppSidebar from '@/components/AppSidebar.vue'
import AppHeader from '@/components/AppHeader.vue'

const sidebarRecolhida = ref(false)

function toggleSidebar() {
  sidebarRecolhida.value = !sidebarRecolhida.value
}
</script>

<template>
  <div class="app-layout" :class="{ 'sidebar-collapsed': sidebarRecolhida }">
    <AppSidebar :collapsed="sidebarRecolhida" @toggle="toggleSidebar" />
    <div class="app-main">
      <AppHeader @toggle-sidebar="toggleSidebar" />
      <main class="app-content">
        <RouterView />
      </main>
    </div>
  </div>
</template>

<style scoped>
.app-layout {
  display: flex;
  height: 100vh;
  background: var(--bg-primary);
}

.app-main {
  flex: 1;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.app-content {
  flex: 1;
  overflow-y: auto;
  padding: 24px;
}
</style>
