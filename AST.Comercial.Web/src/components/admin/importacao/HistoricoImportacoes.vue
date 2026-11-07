<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import { useToast } from '@/composables/useToast'
import {
  ROTULO_STATUS, TOM_STATUS, baixarErros, formatarDataHora, listar,
  type Importacao,
} from './importacaoApi'

const props = defineProps<{ /** Muda quando uma importação termina, para recarregar a lista. */ atualizar?: number }>()
const emit = defineEmits<{ total: [n: number] }>()

const toast = useToast()
const TAMANHO = 20
const itens = ref<Importacao[]>([])
const total = ref(0)
const pagina = ref(1)
const carregando = ref(true)
const erro = ref('')
let temporizador: ReturnType<typeof setTimeout> | null = null

const totalPaginas = computed(() => Math.max(1, Math.ceil(total.value / TAMANHO)))

async function carregar(silencioso = false) {
  if (!silencioso) carregando.value = true
  erro.value = ''
  try {
    const r = await listar(pagina.value, TAMANHO)
    itens.value = r.itens
    total.value = r.total
    emit('total', r.total)
  } catch (e) {
    erro.value = e instanceof Error ? e.message : 'Não foi possível carregar o histórico.'
    if (!silencioso) toast.error(erro.value)
  } finally {
    carregando.value = false
    agendarAtualizacao()
  }
}

/** Enquanto houver importação em andamento, a lista se atualiza sozinha. */
function agendarAtualizacao() {
  if (temporizador) clearTimeout(temporizador)
  temporizador = null
  if (itens.value.some((i) => i.status === 'Processando' || i.status === 'Pendente'))
    temporizador = setTimeout(() => carregar(true), 2000)
}

async function baixar(i: Importacao) {
  try {
    await baixarErros(i.id)
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível baixar o relatório.')
  }
}

function mudarPagina(delta: number) {
  const nova = pagina.value + delta
  if (nova < 1 || nova > totalPaginas.value) return
  pagina.value = nova
  carregar()
}

function resumo(i: Importacao): string {
  const partes: string[] = []
  if (i.criadas) partes.push(`${i.criadas.toLocaleString('pt-BR')} criadas`)
  if (i.atualizadas) partes.push(`${i.atualizadas.toLocaleString('pt-BR')} atualizadas`)
  if (i.ignoradas) partes.push(`${i.ignoradas.toLocaleString('pt-BR')} ignoradas`)
  return partes.join(' · ')
}

onMounted(() => carregar())
watch(() => props.atualizar, () => carregar(true))
onBeforeUnmount(() => { if (temporizador) clearTimeout(temporizador) })
</script>

<template>
  <div class="hist">
    <div class="hist-topo">
      <span class="hist-total">{{ total.toLocaleString('pt-BR') }} {{ total === 1 ? 'importação' : 'importações' }}</span>
      <button type="button" class="hist-btn" :disabled="carregando" @click="carregar()">
        <i class="mdi mdi-refresh" :class="{ 'mdi-spin': carregando }"></i> Atualizar
      </button>
    </div>

    <div v-if="carregando && !itens.length" class="hist-estado" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando histórico…</div>
    <div v-else-if="erro && !itens.length" class="hist-estado erro" role="alert">
      <i class="mdi mdi-alert-circle-outline"></i> {{ erro }}
      <button type="button" class="hist-btn" @click="carregar()">Tentar de novo</button>
    </div>
    <div v-else-if="!itens.length" class="hist-estado">
      <i class="mdi mdi-database-import-outline"></i>
      <p>Nenhuma importação feita ainda.</p>
    </div>

    <div v-else class="hist-wrap">
      <table class="hist-tabela">
        <thead>
          <tr>
            <th scope="col">Data</th>
            <th scope="col">Dados</th>
            <th scope="col">Arquivo</th>
            <th scope="col">Responsável</th>
            <th scope="col">Situação</th>
            <th scope="col" class="num">Linhas</th>
            <th scope="col" class="num">Importadas</th>
            <th scope="col" class="num">Erros</th>
            <th scope="col"><span class="visualmente-oculto">Ações</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="i in itens" :key="i.id">
            <td class="nowrap">{{ formatarDataHora(i.criadoEm) }}</td>
            <td>{{ i.entidadeRotulo }}</td>
            <td class="arquivo" :title="i.nomeArquivo">{{ i.nomeArquivo }}</td>
            <td>{{ i.usuarioNome ?? '—' }}</td>
            <td>
              <UiBadge :tom="TOM_STATUS[i.status]">{{ ROTULO_STATUS[i.status] }}</UiBadge>
              <div v-if="i.status === 'Processando'" class="hist-mini" aria-hidden="true">
                <div class="hist-mini-fill" :style="{ width: Math.min(100, Math.round((i.processadas / Math.max(1, i.totalLinhas)) * 100)) + '%' }"></div>
              </div>
            </td>
            <td class="num">{{ i.totalLinhas.toLocaleString('pt-BR') }}</td>
            <td class="num" :title="resumo(i)">{{ i.sucesso.toLocaleString('pt-BR') }}</td>
            <td class="num" :class="{ ruim: i.falhas > 0 }">{{ i.falhas.toLocaleString('pt-BR') }}</td>
            <td class="acoes">
              <button
                v-if="i.falhas > 0"
                type="button"
                class="hist-icone"
                :aria-label="`Baixar relatório de erros da importação de ${i.nomeArquivo}`"
                title="Baixar relatório de erros"
                @click="baixar(i)"
              ><i class="mdi mdi-file-alert-outline"></i></button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div v-if="totalPaginas > 1" class="hist-paginas">
      <button type="button" class="hist-btn" :disabled="pagina <= 1 || carregando" @click="mudarPagina(-1)"><i class="mdi mdi-chevron-left"></i> Anterior</button>
      <span>Página {{ pagina }} de {{ totalPaginas }}</span>
      <button type="button" class="hist-btn" :disabled="pagina >= totalPaginas || carregando" @click="mudarPagina(1)">Próxima <i class="mdi mdi-chevron-right"></i></button>
    </div>
  </div>
</template>

<style scoped>
.hist { display: flex; flex-direction: column; gap: 12px; max-width: 1180px; }
.hist-topo { display: flex; justify-content: space-between; align-items: center; }
.hist-total { font-size: 12px; color: var(--text-muted); }
.hist-estado { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 48px 20px; color: var(--text-muted); font-size: 13px; text-align: center; border: 1px dashed var(--border); border-radius: 10px; }
.hist-estado .mdi { font-size: 30px; color: var(--accent); }
.hist-estado p { margin: 0; }
.hist-estado.erro { color: var(--danger); }
.hist-estado.erro .mdi { color: var(--danger); }
.hist-wrap { overflow: auto; border: 1px solid var(--border); border-radius: 8px; }
.hist-tabela { width: 100%; border-collapse: collapse; font-size: 12px; }
.hist-tabela th { background: var(--bg-secondary); color: var(--text-muted); font-weight: 600; font-size: 11px; text-transform: uppercase; padding: 10px 12px; text-align: left; border-bottom: 1px solid var(--border); white-space: nowrap; }
.hist-tabela td { padding: 9px 12px; border-bottom: 1px solid var(--border); color: var(--text-secondary); vertical-align: middle; }
.hist-tabela tr:last-child td { border-bottom: none; }
.hist-tabela tr:hover td { background: var(--bg-elevated); }
.num { text-align: right; font-variant-numeric: tabular-nums; }
.ruim { color: var(--danger); font-weight: 600; }
.nowrap { white-space: nowrap; }
.arquivo { max-width: 220px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; color: var(--text-primary); }
.acoes { width: 40px; text-align: right; }
.hist-mini { margin-top: 4px; height: 4px; width: 90px; border-radius: 999px; background: var(--bg-elevated); overflow: hidden; }
.hist-mini-fill { height: 100%; background: var(--accent); transition: width .3s; }
.hist-btn { display: inline-flex; align-items: center; gap: 4px; padding: 6px 12px; border: 1px solid var(--border); border-radius: 6px; background: transparent; color: var(--text-secondary); font-size: 12px; cursor: pointer; }
.hist-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.hist-btn:disabled { opacity: .5; cursor: not-allowed; }
.hist-icone { width: 30px; height: 30px; border: 1px solid var(--border); border-radius: 6px; background: transparent; color: var(--text-secondary); cursor: pointer; display: inline-flex; align-items: center; justify-content: center; }
.hist-icone:hover { background: var(--bg-elevated); color: var(--accent); }
.hist-btn:focus-visible, .hist-icone:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.hist-paginas { display: flex; justify-content: center; align-items: center; gap: 12px; font-size: 12px; color: var(--text-muted); }
.visualmente-oculto { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
</style>
