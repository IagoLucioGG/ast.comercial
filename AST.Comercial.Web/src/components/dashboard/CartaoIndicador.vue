<script setup lang="ts">
// Cartão de indicador: skeleton ao carregar, erro isolado com "tentar de novo" e link para a lista.
defineProps<{
  titulo: string
  icone: string
  to: string
  carregando: boolean
  erro: string
}>()
defineEmits<{ (e: 'tentar'): void }>()
</script>

<template>
  <div v-if="carregando" class="cartao skeleton" aria-busy="true" :aria-label="`Carregando ${titulo}`">
    <span class="sk sk-t"></span>
    <span class="sk sk-v"></span>
    <span class="sk sk-s"></span>
  </div>
  <div v-else-if="erro" class="cartao erro" role="alert">
    <div class="topo"><i class="mdi mdi-alert-circle-outline"></i>{{ titulo }}</div>
    <p class="msg">Não foi possível carregar.</p>
    <button type="button" class="tentar" @click="$emit('tentar')">
      <i class="mdi mdi-refresh"></i> Tentar de novo
    </button>
  </div>
  <router-link v-else :to="to" class="cartao link" :aria-label="`${titulo}: abrir lista`">
    <div class="topo"><i :class="['mdi', icone]"></i>{{ titulo }}</div>
    <slot />
  </router-link>
</template>

<style scoped>
.cartao {
  display: flex; flex-direction: column; gap: 6px; min-height: 112px; padding: 14px 16px;
  background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px;
  color: var(--text-primary); text-decoration: none;
}
.link { transition: border-color .15s, transform .15s; }
.link:hover, .link:focus-visible { border-color: var(--accent); transform: translateY(-1px); outline: none; }
.topo { display: flex; align-items: center; gap: 6px; font-size: 12px; color: var(--text-secondary); text-transform: uppercase; letter-spacing: .04em; }
.topo .mdi { font-size: 16px; color: var(--accent); }
.erro { border-color: var(--danger); }
.erro .topo .mdi { color: var(--danger); }
.msg { margin: 0; font-size: 13px; color: var(--text-muted); }
.tentar { align-self: flex-start; margin-top: auto; background: none; border: 1px solid var(--border); color: var(--text-primary); border-radius: 6px; padding: 4px 10px; font-size: 12px; cursor: pointer; }
.tentar:hover { border-color: var(--accent); }
.sk { display: block; border-radius: 4px; background: linear-gradient(90deg, var(--bg-elevated), var(--border), var(--bg-elevated)); background-size: 200% 100%; animation: brilho 1.3s linear infinite; }
.sk-t { width: 50%; height: 10px; }
.sk-v { width: 70%; height: 24px; margin-top: 6px; }
.sk-s { width: 40%; height: 10px; }
@keyframes brilho { to { background-position: -200% 0; } }
@media (prefers-reduced-motion: reduce) { .sk { animation: none; } }
</style>
