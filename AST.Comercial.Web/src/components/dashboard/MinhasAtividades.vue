<script setup lang="ts">
// Próximas atividades pendentes com conclusão rápida.
import { computed, ref } from 'vue'
import SeletorEscopo from './SeletorEscopo.vue'
import type { AtividadeItem, Escopo } from './dashboardDados'

const props = defineProps<{ itens: AtividadeItem[]; escopo: Escopo; carregando: boolean; erro: string }>()
const emit = defineEmits<{ (e: 'concluir', id: number): void; (e: 'tentar'): void; (e: 'escopo', v: Escopo): void }>()

const proximas = computed(() => props.itens.slice(0, 8))
const emAndamento = ref<number | null>(null)

const ICONES = ['mdi-checkbox-marked-circle-outline', 'mdi-phone', 'mdi-email-outline', 'mdi-account-group-outline', 'mdi-note-text-outline']
const NOMES = ['Tarefa', 'Ligacao', 'Email', 'Reuniao', 'Nota']
function icone(tipo: number | string): string {
  const i = typeof tipo === 'number' ? tipo : NOMES.indexOf(tipo)
  return ICONES[i] ?? ICONES[0]!
}
function atrasada(a: AtividadeItem): boolean {
  if (!a.dataVencimento) return false
  const hoje = new Date(); hoje.setHours(0, 0, 0, 0)
  return new Date(a.dataVencimento).getTime() < hoje.getTime()
}
function data(a: AtividadeItem): string {
  if (!a.dataVencimento) return 'Sem data'
  return new Date(a.dataVencimento).toLocaleString('pt-BR', { day: '2-digit', month: '2-digit', hour: '2-digit', minute: '2-digit' })
}
function concluir(a: AtividadeItem) {
  emAndamento.value = a.id
  emit('concluir', a.id)
}
// Libera o botão quando a lista é atualizada (item sai da lista).
defineExpose({ liberar: () => { emAndamento.value = null } })
</script>

<template>
  <section class="painel" aria-labelledby="t-ativ">
    <header>
      <h2 id="t-ativ"><i class="mdi mdi-calendar-check-outline"></i> Próximas atividades</h2>
      <SeletorEscopo :model-value="escopo" rotulo="Atividades exibidas" @update:model-value="(v) => emit('escopo', v)" />
      <router-link to="/atividades" class="ver">Abrir</router-link>
    </header>

    <div v-if="carregando" class="skel" aria-busy="true"><span v-for="n in 4" :key="n" class="sk"></span></div>
    <div v-else-if="erro" class="msg erro" role="alert">
      Não foi possível carregar as atividades.
      <button type="button" @click="$emit('tentar')"><i class="mdi mdi-refresh"></i> Tentar de novo</button>
    </div>
    <p v-else-if="!proximas.length && escopo === 'minhas'" class="msg">
      Você não tem atividades pendentes.
      <button type="button" @click="emit('escopo', 'todas')">Ver todas</button>
    </p>
    <p v-else-if="!proximas.length" class="msg">Nenhuma atividade pendente.</p>
    <ul v-else>
      <li v-for="a in proximas" :key="a.id">
        <button
          type="button" class="check" :disabled="emAndamento === a.id"
          :aria-label="`Concluir atividade ${a.titulo}`" @click="concluir(a)"
        ><i class="mdi mdi-check"></i></button>
        <i :class="['mdi', icone(a.tipo), 'tipo']"></i>
        <span class="titulo">{{ a.titulo }}</span>
        <span :class="['data', { atrasada: atrasada(a) }]">{{ data(a) }}</span>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.painel { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 14px 16px; min-width: 0; }
header { display: flex; align-items: center; justify-content: space-between; gap: 8px; margin-bottom: 10px; }
h2 { margin: 0; font-size: 15px; font-weight: 600; color: var(--text-primary); display: flex; align-items: center; gap: 6px; }
h2 .mdi { color: var(--accent); }
.ver { font-size: 13px; color: var(--accent); text-decoration: none; }
ul { list-style: none; margin: 0; padding: 0; }
li { display: flex; align-items: center; gap: 8px; padding: 7px 0; border-top: 1px solid var(--border); font-size: 13px; }
li:first-child { border-top: none; }
.check { flex: none; width: 22px; height: 22px; border-radius: 50%; border: 1.5px solid var(--border); background: none; color: transparent; cursor: pointer; display: grid; place-items: center; padding: 0; }
.check:hover:not(:disabled), .check:focus-visible { border-color: var(--accent); color: var(--accent); outline: none; }
.check:disabled { opacity: .5; cursor: progress; }
.check .mdi { font-size: 14px; }
.tipo { color: var(--text-muted); font-size: 16px; }
.titulo { flex: 1; min-width: 0; color: var(--text-primary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.data { flex: none; font-size: 12px; color: var(--text-muted); }
.data.atrasada { color: var(--danger); font-weight: 600; }
.msg { margin: 0; padding: 18px 0; font-size: 13px; color: var(--text-muted); text-align: center; }
.msg.erro { color: var(--danger); }
.msg button { margin-left: 8px; background: none; border: 1px solid var(--border); color: var(--text-primary); border-radius: 6px; padding: 3px 10px; cursor: pointer; font-size: 12px; }
.skel { display: flex; flex-direction: column; gap: 12px; }
.sk { display: block; height: 14px; border-radius: 4px; background: linear-gradient(90deg, var(--bg-elevated), var(--border), var(--bg-elevated)); background-size: 200% 100%; animation: brilho 1.3s linear infinite; }
@keyframes brilho { to { background-position: -200% 0; } }
@media (prefers-reduced-motion: reduce) { .sk { animation: none; } }
</style>
