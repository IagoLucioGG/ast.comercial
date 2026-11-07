<script setup lang="ts">
import { ref } from 'vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import TrilhaAprovacao from './TrilhaAprovacao.vue'
import { dataHora, dinheiro, iniciaisDe, numeroProposta, situacaoVisual, type Solicitacao } from './aprovacaoApi'

defineProps<{ solicitacao: Solicitacao; comAcoes?: boolean; ocupado?: boolean }>()
defineEmits<{ aprovar: [s: Solicitacao]; rejeitar: [s: Solicitacao] }>()

const aberto = ref(false)
</script>

<template>
  <article class="cartao" :aria-label="`Aprovação da proposta ${solicitacao.proposta ? numeroProposta(solicitacao.proposta) : solicitacao.registroId}`">
    <header class="topo">
      <div class="ident">
        <span class="numero">{{ solicitacao.proposta ? numeroProposta(solicitacao.proposta) : `Proposta ${solicitacao.registroId}` }}</span>
        <RouterLink class="titulo" :to="`/propostas/${solicitacao.registroId}`">
          {{ solicitacao.proposta?.titulo || 'Sem título' }}
        </RouterLink>
        <span v-if="solicitacao.proposta?.clienteNome" class="cliente"><i class="mdi mdi-domain"></i> {{ solicitacao.proposta.clienteNome }}</span>
      </div>
      <div class="selos">
        <UiBadge :tom="situacaoVisual(solicitacao.situacao).tom">{{ situacaoVisual(solicitacao.situacao).rotulo }}</UiBadge>
        <UiBadge tom="info">Nível {{ solicitacao.nivelAtual }}/{{ solicitacao.totalNiveis }}</UiBadge>
      </div>
    </header>

    <dl class="dados">
      <div><dt>Valor</dt><dd>{{ dinheiro(solicitacao.proposta) }}</dd></div>
      <div><dt>Desconto</dt><dd>{{ solicitacao.proposta ? `${solicitacao.proposta.descontoPercentual.toLocaleString('pt-BR', { maximumFractionDigits: 2 })}%` : '-' }}</dd></div>
      <div><dt>Fluxo</dt><dd>{{ solicitacao.fluxoNome || '-' }}</dd></div>
      <div>
        <dt>Solicitado por</dt>
        <dd class="pessoa">
          <span v-if="solicitacao.solicitadoPorNome" class="avatar" aria-hidden="true">{{ iniciaisDe(solicitacao.solicitadoPorNome) }}</span>
          {{ solicitacao.solicitadoPorNome || 'Sistema' }} <small>{{ dataHora(solicitacao.solicitadoEm) }}</small>
        </dd>
      </div>
    </dl>

    <p v-if="solicitacao.observacao" class="nota"><i class="mdi mdi-message-text-outline"></i> {{ solicitacao.observacao }}</p>
    <p v-if="solicitacao.situacao === 'Aprovada' || solicitacao.situacao === 'Rejeitada'" class="nota decisao">
      <i class="mdi" :class="solicitacao.situacao === 'Aprovada' ? 'mdi-check-circle-outline' : 'mdi-close-circle-outline'"></i>
      {{ solicitacao.situacao === 'Aprovada' ? 'Aprovada' : 'Rejeitada' }} por {{ solicitacao.decididoPorNome || '-' }} em {{ dataHora(solicitacao.decididoEm) }}
      <template v-if="solicitacao.comentarioDecisao"> · “{{ solicitacao.comentarioDecisao }}”</template>
    </p>

    <footer class="rodape">
      <button type="button" class="link-btn" :aria-expanded="aberto" @click="aberto = !aberto">
        <i class="mdi" :class="aberto ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i> {{ aberto ? 'Ocultar níveis' : 'Ver níveis' }}
      </button>
      <RouterLink class="link-btn" :to="`/propostas/${solicitacao.registroId}`"><i class="mdi mdi-open-in-new"></i> Abrir proposta</RouterLink>
      <span class="espaco"></span>
      <template v-if="comAcoes && solicitacao.situacao === 'EmAndamento'">
        <button type="button" class="btn perigo" :disabled="ocupado || !solicitacao.podeDecidir" @click="$emit('rejeitar', solicitacao)"><i class="mdi mdi-close"></i> Rejeitar</button>
        <button type="button" class="btn sucesso" :disabled="ocupado || !solicitacao.podeDecidir" @click="$emit('aprovar', solicitacao)"><i class="mdi mdi-check"></i> Aprovar</button>
      </template>
    </footer>

    <div v-if="aberto" class="trilha-wrap"><TrilhaAprovacao :passos="solicitacao.passos" /></div>
  </article>
</template>

<style scoped>
.cartao { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 14px 16px; display: flex; flex-direction: column; gap: 10px; }
.topo { display: flex; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
.ident { display: flex; flex-direction: column; gap: 2px; min-width: 0; flex: 1 1 260px; }
.numero { font-size: 11px; color: var(--text-muted); }
.titulo { font-size: 15px; font-weight: 600; color: var(--text-primary); text-decoration: none; overflow-wrap: anywhere; }
.titulo:hover { color: var(--accent); text-decoration: underline; }
.cliente { font-size: 12px; color: var(--text-secondary); }
.selos { display: flex; gap: 6px; align-items: flex-start; flex-wrap: wrap; }
.dados { display: grid; grid-template-columns: repeat(auto-fit, minmax(150px, 1fr)); gap: 10px; margin: 0; }
.dados > div { background: var(--bg-elevated); border-radius: 8px; padding: 8px 10px; min-width: 0; }
.dados dt { font-size: 10px; color: var(--text-muted); text-transform: uppercase; letter-spacing: .04em; }
.dados dd { margin: 2px 0 0; font-size: 13px; color: var(--text-primary); font-variant-numeric: tabular-nums; overflow-wrap: anywhere; }
.dados dd small { color: var(--text-muted); font-size: 11px; display: block; }
.pessoa { display: flex; flex-wrap: wrap; align-items: center; gap: 6px; }
.avatar { width: 22px; height: 22px; border-radius: 50%; background: var(--bg-surface); border: 1px solid var(--border); display: inline-flex; align-items: center; justify-content: center; font-size: 9px; font-weight: 700; color: var(--accent); }
.nota { margin: 0; font-size: 12px; color: var(--text-secondary); overflow-wrap: anywhere; }
.nota.decisao { color: var(--text-primary); }
.rodape { display: flex; align-items: center; gap: 12px; flex-wrap: wrap; }
.espaco { flex: 1; }
.link-btn { background: none; border: 0; padding: 0; font-size: 12px; color: var(--text-secondary); cursor: pointer; text-decoration: none; font-family: inherit; }
.link-btn:hover { color: var(--accent); }
.link-btn:focus-visible, .btn:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 6px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; font-family: inherit; }
.btn:hover:not(:disabled) { border-color: var(--accent); }
.btn:disabled { opacity: .5; cursor: default; }
.btn.sucesso { color: #22c55e; border-color: #22c55e; }
.btn.perigo { color: var(--danger); border-color: var(--danger); }
.trilha-wrap { border-top: 1px solid var(--border); padding-top: 12px; }
</style>
