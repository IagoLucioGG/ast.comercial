<script setup lang="ts">
import { computed, ref } from 'vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import TrilhaAprovacao from './TrilhaAprovacao.vue'
import { dataHora, situacaoVisual, type Solicitacao } from './aprovacaoApi'

const props = defineProps<{
  /** Solicitações da proposta, da mais recente para a mais antiga. */
  solicitacoes: Solicitacao[]
  carregando?: boolean
  erro?: string
  ocupado?: boolean
  /** Motivo do bloqueio do compartilhamento; vazio quando liberado. */
  bloqueio?: string
}>()
defineEmits<{ solicitar: []; aprovar: []; rejeitar: []; recarregar: [] }>()

const atual = computed(() => props.solicitacoes[0] ?? null)
const emAndamento = computed(() => atual.value?.situacao === 'EmAndamento')
const anteriores = computed(() => props.solicitacoes.slice(1))
const verAnteriores = ref(false)
</script>

<template>
  <section class="aprov" aria-label="Aprovação da proposta">
    <header class="cab">
      <h2><i class="mdi mdi-shield-check-outline"></i> Aprovação</h2>
      <UiBadge v-if="atual" :tom="situacaoVisual(atual.situacao).tom">{{ situacaoVisual(atual.situacao).rotulo }}</UiBadge>
      <span class="espaco"></span>
      <button type="button" class="btn" :disabled="ocupado || emAndamento" :title="emAndamento ? 'Já existe uma solicitação em andamento' : ''" @click="$emit('solicitar')">
        <i class="mdi mdi-send-check-outline"></i> Solicitar aprovação
      </button>
    </header>

    <div v-if="bloqueio" class="aviso" role="alert">
      <i class="mdi mdi-lock-outline"></i>
      <div><strong>Compartilhamento bloqueado.</strong> {{ bloqueio }}</div>
    </div>

    <p v-if="carregando" class="mudo" role="status">Carregando aprovação...</p>
    <div v-else-if="erro" class="erro" role="alert">
      {{ erro }} <button type="button" class="link" @click="$emit('recarregar')">Tentar novamente</button>
    </div>
    <p v-else-if="!atual" class="mudo">
      Nenhuma solicitação de aprovação. Se os valores desta proposta se enquadrarem em um fluxo de aprovação, ela será enviada automaticamente ao salvar.
    </p>

    <template v-else>
      <div class="resumo">
        <span><small>Fluxo</small> {{ atual.fluxoNome || '-' }}</span>
        <span><small>Nível</small> {{ atual.nivelAtual }} de {{ atual.totalNiveis }}</span>
        <span><small>Solicitado por</small> {{ atual.solicitadoPorNome || 'Sistema' }} · {{ dataHora(atual.solicitadoEm) }}</span>
      </div>
      <p v-if="atual.observacao" class="obs"><i class="mdi mdi-message-text-outline"></i> {{ atual.observacao }}</p>

      <TrilhaAprovacao :passos="atual.passos" />

      <div v-if="emAndamento" class="decidir">
        <template v-if="atual.podeDecidir">
          <button type="button" class="btn perigo" :disabled="ocupado" @click="$emit('rejeitar')"><i class="mdi mdi-close"></i> Rejeitar</button>
          <button type="button" class="btn sucesso" :disabled="ocupado" @click="$emit('aprovar')"><i class="mdi mdi-check"></i> Aprovar nível {{ atual.nivelAtual }}</button>
        </template>
        <p v-else class="mudo">Aguardando os aprovadores do nível {{ atual.nivelAtual }}. Você não é aprovador deste nível.</p>
      </div>

      <div v-if="anteriores.length" class="anteriores">
        <button type="button" class="link" :aria-expanded="verAnteriores" @click="verAnteriores = !verAnteriores">
          <i class="mdi" :class="verAnteriores ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i> Solicitações anteriores ({{ anteriores.length }})
        </button>
        <ul v-if="verAnteriores">
          <li v-for="s in anteriores" :key="s.id">
            <div class="ant-cab">
              <UiBadge :tom="situacaoVisual(s.situacao).tom">{{ situacaoVisual(s.situacao).rotulo }}</UiBadge>
              <span>{{ s.fluxoNome || 'Fluxo' }} · {{ dataHora(s.solicitadoEm) }}</span>
            </div>
            <TrilhaAprovacao :passos="s.passos" />
          </li>
        </ul>
      </div>
    </template>
  </section>
</template>

<style scoped>
.aprov { padding: 14px 16px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; display: flex; flex-direction: column; gap: 12px; }
.cab { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.cab h2 { margin: 0; font-size: 14px; font-weight: 600; color: var(--text-secondary); display: flex; align-items: center; gap: 6px; }
.espaco { flex: 1; }
.aviso { display: flex; gap: 10px; align-items: flex-start; padding: 10px 12px; border-radius: 8px; background: rgba(245, 158, 11, .1); border: 1px solid rgba(245, 158, 11, .4); color: var(--text-primary); font-size: 13px; }
.aviso .mdi { color: #f59e0b; font-size: 18px; }
.mudo { margin: 0; font-size: 13px; color: var(--text-muted); }
.erro { font-size: 13px; color: var(--danger); }
.resumo { display: flex; flex-wrap: wrap; gap: 6px 22px; font-size: 13px; }
.resumo small { color: var(--text-muted); text-transform: uppercase; font-size: 10px; letter-spacing: .04em; margin-right: 4px; }
.obs { margin: 0; font-size: 12px; color: var(--text-secondary); overflow-wrap: anywhere; }
.decidir { display: flex; gap: 8px; justify-content: flex-end; flex-wrap: wrap; }
.anteriores ul { list-style: none; margin: 10px 0 0; padding: 0; display: flex; flex-direction: column; gap: 14px; }
.ant-cab { display: flex; align-items: center; gap: 8px; font-size: 12px; color: var(--text-secondary); margin-bottom: 8px; }
.link { background: none; border: 0; padding: 0; color: var(--text-secondary); font-size: 12px; cursor: pointer; font-family: inherit; }
.link:hover { color: var(--accent); }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; font-family: inherit; }
.btn:hover:not(:disabled) { border-color: var(--accent); }
.btn:focus-visible, .link:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.btn:disabled { opacity: .5; cursor: default; }
.btn.sucesso { color: #22c55e; border-color: #22c55e; }
.btn.perigo { color: var(--danger); border-color: var(--danger); }
</style>
