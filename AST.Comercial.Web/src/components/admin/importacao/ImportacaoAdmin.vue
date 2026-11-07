<script setup lang="ts">
import { ref } from 'vue'
import UiTabs, { type AbaConfig } from '@/components/ui/UiTabs.vue'
import AssistenteImportacao from './AssistenteImportacao.vue'
import HistoricoImportacoes from './HistoricoImportacoes.vue'

const aba = ref('nova')
const totalHistorico = ref<number | undefined>(undefined)
/** Incrementa quando uma importação termina; o histórico recarrega sozinho. */
const versao = ref(0)

function abas(): AbaConfig[] {
  return [
    { chave: 'nova', rotulo: 'Nova importação', icone: 'mdi-database-import-outline' },
    { chave: 'historico', rotulo: 'Histórico', icone: 'mdi-history', contador: totalHistorico.value },
  ]
}
</script>

<template>
  <div class="ia">
    <p class="ia-intro">
      Traga clientes, contatos, produtos e negócios de uma planilha CSV. Você relaciona as colunas, confere os erros
      antes de gravar e baixa o relatório do que ficou de fora.
    </p>
    <UiTabs v-model="aba" :abas="abas()" />
    <div class="ia-corpo">
      <AssistenteImportacao v-show="aba === 'nova'" @concluida="versao++" />
      <HistoricoImportacoes
        v-show="aba === 'historico'"
        :atualizar="versao"
        @total="totalHistorico = $event"
      />
    </div>
  </div>
</template>

<style scoped>
.ia { display: flex; flex-direction: column; gap: 14px; }
.ia-intro { margin: 0; max-width: 720px; font-size: 13px; line-height: 1.5; color: var(--text-muted); }
.ia-corpo { padding-top: 4px; }
</style>
