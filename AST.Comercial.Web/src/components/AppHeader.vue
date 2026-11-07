<script setup lang="ts">
import { onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useTemaStore } from '@/stores/tema'
import { useAtalhos, ATALHOS } from '@/composables/useAtalhos'
import BuscaGlobal from '@/components/busca/BuscaGlobal.vue'
import UiModal from '@/components/ui/UiModal.vue'

defineEmits<{ 'toggle-sidebar': [] }>()

const router = useRouter()
const auth = useAuthStore()
const tema = useTemaStore()
const menuAberto = ref(false)
const criarAberto = ref(false)
const ajudaAberta = ref(false)
const busca = ref<InstanceType<typeof BuscaGlobal> | null>(null)
const criarRaiz = ref<HTMLElement | null>(null)

// Convenção: a lista de cada entidade abre o formulário de criação com ?novo=1.
const CRIACAO = [
  { rotulo: 'Novo cliente', icone: 'mdi-domain', rota: '/clientes' },
  { rotulo: 'Novo negócio', icone: 'mdi-handshake-outline', rota: '/negocios' },
  { rotulo: 'Nova atividade', icone: 'mdi-calendar-check-outline', rota: '/atividades' },
  { rotulo: 'Nova proposta', icone: 'mdi-file-document-outline', rota: '/propostas' },
  { rotulo: 'Novo contato', icone: 'mdi-account-plus-outline', rota: '/contatos' },
]

function criar(rota: string) {
  criarAberto.value = false
  router.push({ path: rota, query: { novo: '1' } })
}

useAtalhos({
  buscar: () => busca.value?.focar(),
  navegar: (caminho) => router.push(caminho),
  ajuda: () => { ajudaAberta.value = true },
})

function fecharAoClicarFora(e: MouseEvent) {
  if (criarAberto.value && !criarRaiz.value?.contains(e.target as Node)) criarAberto.value = false
}
function fecharComEsc(e: KeyboardEvent) {
  if (e.key === 'Escape') { criarAberto.value = false; menuAberto.value = false }
}
onMounted(() => {
  document.addEventListener('mousedown', fecharAoClicarFora)
  document.addEventListener('keydown', fecharComEsc)
})
onBeforeUnmount(() => {
  document.removeEventListener('mousedown', fecharAoClicarFora)
  document.removeEventListener('keydown', fecharComEsc)
})
</script>

<template>
  <header class="app-header">
    <div class="header-left">
      <button class="btn-icon" @click="$emit('toggle-sidebar')" aria-label="Menu">
        <i class="mdi mdi-menu"></i>
      </button>
      <BuscaGlobal ref="busca" />
    </div>

    <div class="header-right">
      <div ref="criarRaiz" class="criar-wrap">
        <button class="btn-criar" aria-label="Criação rápida" aria-haspopup="menu" :aria-expanded="criarAberto"
          @click="criarAberto = !criarAberto">
          <i class="mdi mdi-plus"></i> Criar
        </button>
        <div v-if="criarAberto" class="dropdown criar" role="menu">
          <button v-for="c in CRIACAO" :key="c.rota" class="dropdown-item" role="menuitem" @click="criar(c.rota)">
            <i class="mdi" :class="c.icone"></i> {{ c.rotulo }}
          </button>
        </div>
      </div>

      <button class="btn-icon" @click="ajudaAberta = true" aria-label="Atalhos de teclado" title="Atalhos de teclado (?)">
        <i class="mdi mdi-keyboard-outline"></i>
      </button>

      <button class="btn-icon" @click="tema.alternar" :aria-label="tema.dark ? 'Modo claro' : 'Modo escuro'">
        <i class="mdi" :class="tema.dark ? 'mdi-weather-sunny' : 'mdi-weather-night'"></i>
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

    <UiModal v-model="ajudaAberta" titulo="Atalhos de teclado" largura="420px">
      <ul class="atalhos-lista">
        <li v-for="a in ATALHOS" :key="a.descricao">
          <span>{{ a.descricao }}</span>
          <span class="teclas">
            <template v-for="(t, i) in a.teclas" :key="t">
              <span v-if="i > 0">depois</span>
              <kbd>{{ t }}</kbd>
            </template>
          </span>
        </li>
      </ul>
    </UiModal>
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

.criar-wrap { position: relative; }
.btn-criar {
  display: inline-flex; align-items: center; gap: 4px; height: 34px; padding: 0 12px; border: 0;
  border-radius: 8px; background: var(--accent); color: #000; font-size: 13px; font-weight: 600; cursor: pointer;
}
.btn-criar:hover { background: var(--accent-hover); }
.btn-criar .mdi { font-size: 18px; }

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
.dropdown.criar { left: 0; right: auto; min-width: 190px; }

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

.atalhos-lista { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 10px; }
.atalhos-lista li { display: flex; justify-content: space-between; align-items: center; font-size: 13px; color: var(--text-secondary); }
.teclas { display: inline-flex; gap: 4px; align-items: center; }
.teclas span { color: var(--text-muted); font-size: 11px; }
kbd {
  min-width: 22px; padding: 2px 7px; text-align: center; border-radius: 5px; font-size: 12px;
  background: var(--bg-elevated); border: 1px solid var(--border); color: var(--text-primary); font-family: inherit;
}
</style>
