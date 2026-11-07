<script setup lang="ts">
import UiBadge from '@/components/ui/UiBadge.vue'
import './comum/admin-comum.css'
</script>

<template>
  <div class="em-breve">
    <div class="topo">
      <i class="mdi mdi-shield-lock-outline"></i>
      <div>
        <h3>Perfis e permissões <UiBadge tom="aviso">Em breve</UiBadge></h3>
        <p>
          Hoje o sistema só <strong>registra</strong> o perfil de cada usuário (Administrador, Gerente, Vendedor ou Visualizador).
          O servidor ainda não usa esse perfil para liberar ou bloquear telas e ações, então uma matriz de permissões aqui
          não teria efeito real. Para não passar uma falsa sensação de segurança, esta área fica desativada até a
          autorização por perfil ser implementada no backend.
        </p>
      </div>
    </div>
    <div class="adm-aviso">
      <strong>O que já funciona:</strong> definir o perfil de cada pessoa em <em>Usuários &gt; Tabela de usuários</em>.
      O sistema também impede remover o último administrador ativo da empresa.
    </div>
  </div>
</template>

<style scoped>
.em-breve { max-width: 640px; display: flex; flex-direction: column; gap: 16px; }
.topo { display: flex; gap: 14px; align-items: flex-start; }
.topo > .mdi { font-size: 32px; color: var(--accent); }
h3 { font-size: 16px; font-weight: 600; display: flex; align-items: center; gap: 8px; margin-bottom: 8px; }
p { font-size: 14px; line-height: 1.6; color: var(--text-secondary); }
</style>
