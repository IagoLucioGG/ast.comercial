<script setup lang="ts">
// Barras horizontais por etapa (CSS puro): largura proporcional à quantidade.
import { computed } from 'vue'
import { formatarValor, type EtapaResumo, type FunilItem, type Moeda } from './dashboardDados'

const props = defineProps<{
  funis: FunilItem[]
  funilId: number | null
  etapas: EtapaResumo[]
  moedas: Moeda[]
  carregando: boolean
  erro: string
}>()
const emit = defineEmits<{ (e: 'trocar', id: number): void; (e: 'tentar'): void }>()

const maximo = computed(() => Math.max(1, ...props.etapas.map((e) => e.qtd)))
const total = computed(() => props.etapas.reduce((s, e) => s + e.qtd, 0))

function aoTrocar(ev: Event) { emit('trocar', Number((ev.target as HTMLSelectElement).value)) }
</script>

<template>
  <section class="painel" aria-labelledby="t-funil">
    <header>
      <h2 id="t-funil"><i class="mdi mdi-filter-variant"></i> Funil</h2>
      <select v-if="funis.length > 1" :value="funilId ?? ''" aria-label="Selecionar funil" @change="aoTrocar">
        <option v-for="f in funis" :key="f.id" :value="f.id">{{ f.nome }}</option>
      </select>
      <router-link v-else-if="funis.length" to="/negocios" class="ver">Abrir funil</router-link>
    </header>

    <div v-if="carregando" class="skel" aria-busy="true">
      <span v-for="n in 5" :key="n" class="sk" :style="{ width: `${100 - n * 14}%` }"></span>
    </div>
    <div v-else-if="erro" class="msg erro" role="alert">
      Não foi possível carregar o funil.
      <button type="button" @click="$emit('tentar')"><i class="mdi mdi-refresh"></i> Tentar de novo</button>
    </div>
    <p v-else-if="!etapas.length" class="msg">Nenhuma etapa configurada neste funil.</p>
    <p v-else-if="total === 0" class="msg">Sem negócios em aberto neste funil.</p>
    <ul v-else class="barras">
      <li v-for="e in etapas" :key="e.etapa.id">
        <div class="rotulo">
          <span class="nome">{{ e.etapa.nome }}</span>
          <span class="qtd">{{ e.qtd }}</span>
        </div>
        <div class="trilho" role="img" :aria-label="`${e.etapa.nome}: ${e.qtd} negócios`">
          <div class="barra" :style="{ width: `${(e.qtd / maximo) * 100}%` }"></div>
        </div>
        <div class="valor">
          <span v-for="t in e.porMoeda.filter((x) => x.total !== 0)" :key="t.moedaId ?? 'sem'">
            {{ formatarValor(t.total, t.moedaId, moedas) }}
          </span>
        </div>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.painel { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 14px 16px; min-width: 0; }
header { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-bottom: 12px; }
h2 { margin: 0; font-size: 15px; font-weight: 600; color: var(--text-primary); display: flex; align-items: center; gap: 6px; }
h2 .mdi { color: var(--accent); }
select { background: var(--bg-elevated); color: var(--text-primary); border: 1px solid var(--border); border-radius: 6px; padding: 4px 8px; font-size: 13px; }
.ver { font-size: 13px; color: var(--accent); text-decoration: none; }
.barras { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 12px; }
.rotulo { display: flex; justify-content: space-between; font-size: 13px; color: var(--text-primary); margin-bottom: 4px; }
.qtd { font-weight: 600; }
.trilho { height: 10px; background: var(--bg-elevated); border-radius: 5px; overflow: hidden; }
.barra { height: 100%; min-width: 3px; background: var(--accent); border-radius: 5px; transition: width .3s; }
.valor { display: flex; flex-wrap: wrap; gap: 4px 12px; font-size: 12px; color: var(--text-muted); margin-top: 3px; min-height: 14px; }
.msg { margin: 0; padding: 18px 0; font-size: 13px; color: var(--text-muted); text-align: center; }
.msg.erro { color: var(--danger); }
.msg button { margin-left: 8px; background: none; border: 1px solid var(--border); color: var(--text-primary); border-radius: 6px; padding: 3px 10px; cursor: pointer; font-size: 12px; }
.skel { display: flex; flex-direction: column; gap: 14px; padding: 4px 0; }
.sk { display: block; height: 12px; border-radius: 4px; background: linear-gradient(90deg, var(--bg-elevated), var(--border), var(--bg-elevated)); background-size: 200% 100%; animation: brilho 1.3s linear infinite; }
@keyframes brilho { to { background-position: -200% 0; } }
@media (prefers-reduced-motion: reduce) { .sk { animation: none; } .barra { transition: none; } }
</style>
