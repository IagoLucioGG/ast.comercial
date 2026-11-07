<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import UiSlideOver from '@/components/ui/UiSlideOver.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import { useToast } from '@/composables/useToast'
import { automacaoApi, mensagensDoErro, formatarData, type ExecucaoDto } from './automacaoApi'

const props = defineProps<{ modelValue: boolean; automacaoId: number | null; nome: string }>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean] }>()
const toast = useToast()

const porPagina = 20
const pagina = ref(1)
const total = ref(0)
const itens = ref<ExecucaoDto[]>([])
const carregando = ref(false)
const falhou = ref(false)
const status = ref('')
const aberto = ref<number | null>(null)
const totalPaginas = computed(() => Math.max(1, Math.ceil(total.value / porPagina)))

const statusInfo: Record<string, { rotulo: string; tom: 'sucesso' | 'perigo' | 'aviso' | 'info' | 'neutro' }> = {
  Sucesso: { rotulo: 'Sucesso', tom: 'sucesso' },
  Falha: { rotulo: 'Falha', tom: 'perigo' },
  FalhaFiltro: { rotulo: 'Filtro não atendido', tom: 'neutro' },
  EmExecucao: { rotulo: 'Em execução', tom: 'info' },
  Cancelada: { rotulo: 'Cancelada', tom: 'aviso' },
}

async function carregar() {
  if (props.automacaoId == null) return
  carregando.value = true; falhou.value = false
  try {
    const r = await automacaoApi.execucoes(props.automacaoId, { status: status.value, skip: (pagina.value - 1) * porPagina, top: porPagina })
    itens.value = r.itens; total.value = r.total
  } catch (e) {
    falhou.value = true; itens.value = []
    toast.error(mensagensDoErro(e)[0] ?? 'Falha ao carregar o histórico.')
  } finally { carregando.value = false }
}
watch(() => props.modelValue, (a) => { if (a) { pagina.value = 1; status.value = ''; aberto.value = null; carregar() } })
function mudarStatus() { pagina.value = 1; carregar() }
function ir(p: number) { pagina.value = p; carregar() }
function duracao(ms: number | null) {
  if (ms == null) return '—'
  return ms < 1000 ? `${ms} ms` : `${(ms / 1000).toFixed(1)} s`
}
</script>

<template>
  <UiSlideOver :model-value="modelValue" :titulo="`Histórico: ${nome}`" largura="680px" @update:model-value="emit('update:modelValue', $event)">
    <div class="topo">
      <label class="f">Status
        <select v-model="status" @change="mudarStatus">
          <option value="">Todos</option>
          <option v-for="(s, k) in statusInfo" :key="k" :value="k">{{ s.rotulo }}</option>
        </select>
      </label>
      <button class="ico" aria-label="Atualizar histórico" title="Atualizar" @click="carregar"><i class="mdi mdi-refresh"></i></button>
    </div>
    <p v-if="carregando" class="estado" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando...</p>
    <p v-else-if="falhou" class="estado erro" role="alert">Não foi possível carregar. <button class="link" @click="carregar">Tentar de novo</button></p>
    <p v-else-if="!itens.length" class="estado">Nenhuma execução registrada.</p>
    <table v-else>
      <thead><tr><th>Status</th><th>Início</th><th>Duração</th><th>Registro</th><th><span class="sr">Detalhes</span></th></tr></thead>
      <tbody>
        <template v-for="x in itens" :key="x.id">
          <tr>
            <td><UiBadge :tom="statusInfo[x.status]?.tom ?? 'neutro'">{{ statusInfo[x.status]?.rotulo ?? x.status }}</UiBadge></td>
            <td>{{ formatarData(x.iniciadaEm) }}</td>
            <td>{{ duracao(x.duracaoMs) }}</td>
            <td>{{ x.registroId ?? '—' }}</td>
            <td><button v-if="x.erro || x.detalhes" class="ico" :aria-expanded="aberto === x.id" aria-label="Ver detalhes" @click="aberto = aberto === x.id ? null : x.id">
              <i class="mdi" :class="aberto === x.id ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button></td>
          </tr>
          <tr v-if="aberto === x.id" class="det"><td colspan="5">
            <p v-if="x.erro" class="msg-erro"><strong>Erro:</strong> {{ x.erro }}</p>
            <pre v-if="x.detalhes">{{ x.detalhes }}</pre>
            <p v-if="x.disparadoPor" class="mut">Disparada por: {{ x.disparadoPor }}</p>
          </td></tr>
        </template>
      </tbody>
    </table>
    <div v-if="totalPaginas > 1" class="pg">
      <button class="ico" :disabled="pagina <= 1" aria-label="Página anterior" @click="ir(pagina - 1)"><i class="mdi mdi-chevron-left"></i></button>
      <span>{{ pagina }} / {{ totalPaginas }} ({{ total }})</span>
      <button class="ico" :disabled="pagina >= totalPaginas" aria-label="Próxima página" @click="ir(pagina + 1)"><i class="mdi mdi-chevron-right"></i></button>
    </div>
  </UiSlideOver>
</template>

<style scoped>
.topo { display: flex; align-items: flex-end; justify-content: space-between; margin-bottom: 12px; }
.f { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--text-muted); }
select { background: var(--bg-secondary); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); padding: 7px 9px; font-size: 13px; }
.estado { text-align: center; padding: 36px; color: var(--text-muted); font-size: 13px; }
.estado.erro { color: var(--danger); }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th { text-align: left; font-size: 11px; text-transform: uppercase; color: var(--text-muted); padding: 8px 10px; border-bottom: 1px solid var(--border); }
td { padding: 8px 10px; border-bottom: 1px solid var(--border); color: var(--text-secondary); }
.det td { background: var(--bg-secondary); }
pre { white-space: pre-wrap; word-break: break-word; font-size: 12px; margin: 6px 0 0; color: var(--text-secondary); }
.msg-erro { color: var(--danger); font-size: 12px; }
.mut { font-size: 12px; color: var(--text-muted); margin-top: 6px; }
.sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
.pg { display: flex; justify-content: center; align-items: center; gap: 12px; padding: 14px 0 0; font-size: 13px; color: var(--text-muted); }
.ico { width: 30px; height: 30px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); cursor: pointer; display: inline-flex; align-items: center; justify-content: center; }
.ico:disabled { opacity: .3; cursor: default; }
.ico:focus-visible, select:focus-visible, .link:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
.link { background: none; border: 0; color: var(--accent); cursor: pointer; }
</style>
