<script setup lang="ts">
import { dataHora, iniciaisDe, type PassoAprovacao } from './aprovacaoApi'

defineProps<{ passos: PassoAprovacao[] }>()

const ROTULOS: Record<string, string> = {
  Aprovado: 'Aprovado', Rejeitado: 'Rejeitado', Atual: 'Aguardando decisão', Aguardando: 'Na fila', Cancelado: 'Cancelado',
}
const ICONES: Record<string, string> = {
  Aprovado: 'mdi-check', Rejeitado: 'mdi-close', Atual: 'mdi-clock-outline', Aguardando: 'mdi-circle-small', Cancelado: 'mdi-cancel',
}
</script>

<template>
  <ol class="trilha" aria-label="Níveis de aprovação">
    <li v-for="p in passos" :key="p.nivel" class="passo" :class="`estado-${p.estado.toLowerCase()}`">
      <span class="marco" aria-hidden="true"><i class="mdi" :class="ICONES[p.estado]"></i></span>
      <div class="conteudo">
        <div class="linha">
          <strong>Nível {{ p.nivel }}<template v-if="p.descricao"> · {{ p.descricao }}</template></strong>
          <span class="estado">{{ ROTULOS[p.estado] ?? p.estado }}</span>
        </div>
        <div class="aprovadores" :title="'Aprovadores do nível'">
          <i class="mdi mdi-account-check-outline"></i> {{ p.aprovadores.join(', ') || 'Sem aprovadores' }}
        </div>
        <div v-if="p.decididoPorNome" class="decisao">
          <span class="avatar" aria-hidden="true">{{ iniciaisDe(p.decididoPorNome) }}</span>
          <span>
            {{ p.decididoPorNome }} · {{ dataHora(p.decididoEm) }}
            <em v-if="p.foraDaAlcada" class="fora" title="Administrador decidiu sem ser aprovador do nível">fora da alçada</em>
          </span>
        </div>
        <p v-if="p.comentario" class="comentario">“{{ p.comentario }}”</p>
      </div>
    </li>
  </ol>
</template>

<style scoped>
.trilha { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 0; }
.passo { display: flex; gap: 12px; position: relative; padding-bottom: 14px; }
.passo:last-child { padding-bottom: 0; }
.passo:not(:last-child)::before { content: ''; position: absolute; left: 13px; top: 28px; bottom: 0; width: 2px; background: var(--border); }
.marco { width: 28px; height: 28px; flex: 0 0 28px; border-radius: 50%; display: inline-flex; align-items: center; justify-content: center; background: var(--bg-elevated); border: 2px solid var(--border); color: var(--text-muted); font-size: 15px; z-index: 1; }
.conteudo { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 3px; }
.linha { display: flex; align-items: baseline; gap: 10px; flex-wrap: wrap; font-size: 13px; }
.estado { font-size: 11px; color: var(--text-muted); text-transform: uppercase; letter-spacing: .04em; }
.aprovadores { font-size: 12px; color: var(--text-secondary); overflow-wrap: anywhere; }
.decisao { display: flex; align-items: center; gap: 8px; font-size: 12px; color: var(--text-secondary); margin-top: 2px; }
.avatar { width: 22px; height: 22px; border-radius: 50%; background: var(--bg-elevated); border: 1px solid var(--border); display: inline-flex; align-items: center; justify-content: center; font-size: 9px; font-weight: 700; color: var(--accent); }
.fora { font-style: normal; font-size: 10px; margin-left: 6px; padding: 1px 6px; border-radius: 999px; background: rgba(245, 158, 11, .14); color: #f59e0b; }
.comentario { margin: 2px 0 0; font-size: 12px; color: var(--text-secondary); font-style: italic; overflow-wrap: anywhere; white-space: pre-wrap; }
.estado-aprovado .marco { background: rgba(34, 197, 94, .15); border-color: #22c55e; color: #22c55e; }
.estado-rejeitado .marco { background: rgba(239, 68, 68, .15); border-color: var(--danger); color: var(--danger); }
.estado-atual .marco { background: rgba(245, 158, 11, .15); border-color: #f59e0b; color: #f59e0b; }
.estado-atual .estado { color: #f59e0b; }
.estado-cancelado { opacity: .6; }
</style>
