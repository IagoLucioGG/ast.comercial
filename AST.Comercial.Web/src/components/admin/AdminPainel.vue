<script setup lang="ts">
import TabelaUsuarios from './TabelaUsuarios.vue'
import RegistroAtividades from './RegistroAtividades.vue'
import UsuariosIntegracao from './UsuariosIntegracao.vue'
import EquipesAdmin from './EquipesAdmin.vue'
import AutomacoesAdmin from './AutomacoesAdmin.vue'
import FunisAdmin from './FunisAdmin.vue'
import EtiquetasAdmin from './EtiquetasAdmin.vue'
import IntegracoesAdmin from './IntegracoesAdmin.vue'
import DadosEmpresa from './DadosEmpresa.vue'
import CamposFormularios from './CamposFormularios.vue'
import ImportadorPloomes from './ImportadorPloomes.vue'
import ModelosDocumentoAdmin from './ModelosDocumentoAdmin.vue'

const props = defineProps<{ titulo: string; componente: string }>()
defineEmits<{ fechar: [] }>()

const componentes: Record<string, any> = {
  TabelaUsuarios,
  RegistroAtividades,
  UsuariosIntegracao,
  Equipes: EquipesAdmin,
  Automacoes: AutomacoesAdmin,
  Funis: FunisAdmin,
  Etiquetas: EtiquetasAdmin,
  Integracoes: IntegracoesAdmin,
  DadosEmpresa,
  CamposFormularios,
  ImportadorPloomes,
  ModelosDocumentos: ModelosDocumentoAdmin,
}
</script>

<template>
  <div class="admin-painel">
    <div class="painel-header">
      <button class="btn-voltar" @click="$emit('fechar')">
        <i class="mdi mdi-arrow-left"></i>
      </button>
      <h2 class="painel-titulo">{{ titulo }}</h2>
    </div>
    <div class="painel-content">
      <component v-if="componentes[props.componente]" :is="componentes[props.componente]" />
      <p v-else class="placeholder">Conteúdo de "{{ titulo }}" em construção</p>
    </div>
  </div>
</template>

<style scoped>
.admin-painel {
  position: absolute;
  inset: 0;
  background: var(--bg-primary);
  z-index: 10;
  display: flex;
  flex-direction: column;
  animation: slideIn 0.2s ease;
}

@keyframes slideIn {
  from { opacity: 0; }
  to { opacity: 1; }
}

.painel-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 24px;
  border-bottom: 1px solid var(--border);
}

.btn-voltar {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  border: 1px solid var(--border);
  background: var(--bg-surface);
  color: var(--text-secondary);
  cursor: pointer;
  display: flex;
  align-items: center;
  justify-content: center;
  transition: background 0.15s;
}

.btn-voltar:hover {
  background: var(--bg-elevated);
  color: var(--text-primary);
}

.btn-voltar .mdi { font-size: 18px; }

.painel-titulo {
  font-size: 18px;
  font-weight: 600;
  color: var(--text-primary);
}

.painel-content {
  flex: 1;
  padding: 16px;
  overflow-y: auto;
  overflow-y: auto;
}

.placeholder {
  color: var(--text-muted);
  font-size: 14px;
}
</style>
