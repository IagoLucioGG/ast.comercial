<script setup lang="ts">
// Últimos negócios criados.
import SeletorEscopo from './SeletorEscopo.vue'
import { formatarData, formatarValor, inicialDoNome, type Escopo, type Etapa, type Moeda, type NegocioRecente, type StatusNeg } from './dashboardDados'

const props = defineProps<{
  itens: NegocioRecente[]; etapas: Etapa[]; status: StatusNeg[]; moedas: Moeda[]; escopo: Escopo
  carregando: boolean; erro: string
}>()
const emit = defineEmits<{ (e: 'tentar'): void; (e: 'escopo', v: Escopo): void }>()

function etapa(n: NegocioRecente): string { return props.etapas.find((e) => e.id === n.etapaId)?.nome ?? '' }
function situacao(n: NegocioRecente): { nome: string; tipo: number } {
  const s = props.status.find((x) => x.id === n.statusId)
  return { nome: s?.nome ?? 'Aberto', tipo: s?.tipoBase ?? 0 }
}
function criado(n: NegocioRecente): string { return formatarData(n.criadoEm) }
</script>

<template>
  <section class="painel" aria-labelledby="t-neg">
    <header>
      <h2 id="t-neg"><i class="mdi mdi-handshake-outline"></i> Negócios recentes</h2>
      <SeletorEscopo :model-value="escopo" rotulo="Negócios exibidos" @update:model-value="(v) => emit('escopo', v)" />
      <router-link to="/negocios" class="ver">Abrir funil</router-link>
    </header>

    <div v-if="carregando" class="skel" aria-busy="true"><span v-for="n in 4" :key="n" class="sk"></span></div>
    <div v-else-if="erro" class="msg erro" role="alert">
      Não foi possível carregar os negócios.
      <button type="button" @click="$emit('tentar')"><i class="mdi mdi-refresh"></i> Tentar de novo</button>
    </div>
    <p v-else-if="!itens.length && escopo === 'minhas'" class="msg">
      Você não é responsável por nenhum negócio.
      <button type="button" @click="emit('escopo', 'todas')">Ver todas</button>
    </p>
    <p v-else-if="!itens.length" class="msg">Nenhum negócio ainda.</p>
    <ul v-else>
      <li v-for="n in itens" :key="n.id">
        <router-link :to="`/negocios/${n.id}`" class="linha">
          <span class="avatar" aria-hidden="true">{{ inicialDoNome(n.clienteNome || n.titulo) }}</span>
          <span class="info">
            <span class="titulo">{{ n.titulo }}</span>
            <span class="sub">{{ [n.clienteNome, etapa(n), criado(n)].filter(Boolean).join(' · ') }}</span>
          </span>
          <span class="dir">
            <span v-if="n.valor != null" class="valor">{{ formatarValor(n.valor, n.moedaId, moedas) }}</span>
            <span :class="['sit', `t${situacao(n).tipo}`]">{{ situacao(n).nome }}</span>
          </span>
        </router-link>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.painel { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 14px 16px; min-width: 0; }
header { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-bottom: 10px; }
.avatar { flex: none; width: 28px; height: 28px; border-radius: 50%; background: var(--bg-elevated); color: var(--accent); font-size: 12px; font-weight: 600; display: grid; place-items: center; }
h2 { margin: 0; font-size: 15px; font-weight: 600; color: var(--text-primary); display: flex; align-items: center; gap: 6px; }
h2 .mdi { color: var(--accent); }
.ver { font-size: 13px; color: var(--accent); text-decoration: none; }
ul { list-style: none; margin: 0; padding: 0; }
li { border-top: 1px solid var(--border); }
li:first-child { border-top: none; }
.linha { display: flex; align-items: center; justify-content: space-between; gap: 10px; padding: 8px 4px; text-decoration: none; color: var(--text-primary); border-radius: 6px; }
.linha:hover, .linha:focus-visible { background: var(--bg-elevated); outline: none; }
.info { display: flex; flex-direction: column; min-width: 0; flex: 1; }
.titulo { font-size: 13px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.sub { font-size: 12px; color: var(--text-muted); }
.dir { display: flex; flex-direction: column; align-items: flex-end; flex: none; gap: 2px; }
.valor { font-size: 13px; font-weight: 600; }
.sit { font-size: 11px; padding: 1px 8px; border-radius: 10px; background: var(--bg-elevated); color: var(--text-secondary); }
.sit.t1 { color: #1e9e57; }
.sit.t2 { color: var(--danger); }
.msg { margin: 0; padding: 18px 0; font-size: 13px; color: var(--text-muted); text-align: center; }
.msg.erro { color: var(--danger); }
.msg button { margin-left: 8px; background: none; border: 1px solid var(--border); color: var(--text-primary); border-radius: 6px; padding: 3px 10px; cursor: pointer; font-size: 12px; }
.skel { display: flex; flex-direction: column; gap: 12px; }
.sk { display: block; height: 28px; border-radius: 4px; background: linear-gradient(90deg, var(--bg-elevated), var(--border), var(--bg-elevated)); background-size: 200% 100%; animation: brilho 1.3s linear infinite; }
@keyframes brilho { to { background-position: -200% 0; } }
@media (prefers-reduced-motion: reduce) { .sk { animation: none; } }
</style>
