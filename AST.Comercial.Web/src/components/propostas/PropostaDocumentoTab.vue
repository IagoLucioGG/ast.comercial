<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { odataRequest, montarQuery } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { listar } from '@/composables/usePropostaApi'

const props = defineProps<{ propostaId: number; modeloInicial?: number | null; versao: number }>()

const toast = useToast()
const modelos = ref<{ Id: number; Nome: string }[]>([])
const modeloId = ref<number | null>(props.modeloInicial ?? null)
const html = ref('')
const carregando = ref(false)
const erro = ref('')

async function carregarModelos() {
  try {
    const r = await listar<{ Id: number; Nome: string }>(`ModelosDocumento${montarQuery({ $select: 'Id,Nome', $orderby: 'Nome', $top: 100 })}`)
    modelos.value = r
    if (!modeloId.value && r.length === 1) modeloId.value = r[0]?.Id ?? null
  } catch (e) {
    erro.value = e instanceof Error ? e.message : 'Não foi possível carregar os modelos.'
  }
}

async function renderizar() {
  html.value = ''
  erro.value = ''
  if (!modeloId.value) return
  carregando.value = true
  try {
    // A resposta é text/html; odataRequest devolve o texto quando não é JSON
    const r = await odataRequest<unknown>('GET', `ModelosDocumento(${modeloId.value})/Renderizar(${props.propostaId})`)
    html.value = typeof r === 'string' ? r : ''
    if (!html.value) erro.value = 'O modelo não gerou conteúdo para esta proposta.'
  } catch (e) {
    erro.value = e instanceof Error ? e.message : 'Falha ao gerar o documento.'
    toast.error(erro.value)
  } finally {
    carregando.value = false
  }
}

onMounted(async () => { await carregarModelos(); await renderizar() })
watch(modeloId, renderizar)
// re-renderiza quando itens/condições mudam
watch(() => props.versao, renderizar)
</script>

<template>
  <div class="doc">
    <div class="barra">
      <label>
        <span>Modelo de documento</span>
        <select v-model="modeloId">
          <option :value="null">Selecione um modelo...</option>
          <option v-for="m in modelos" :key="m.Id" :value="m.Id">{{ m.Nome }}</option>
        </select>
      </label>
      <button type="button" class="btn" :disabled="!modeloId || carregando" @click="renderizar"><i class="mdi mdi-refresh"></i> Atualizar</button>
    </div>
    <p v-if="!modelos.length && !erro" class="aviso">Nenhum modelo de documento cadastrado.</p>
    <p v-if="erro" class="aviso erro" role="alert">{{ erro }}</p>
    <p v-else-if="carregando" class="aviso">Gerando pré-visualização...</p>
    <p v-else-if="!modeloId && modelos.length" class="aviso">Escolha um modelo para ver a pré-visualização.</p>
    <!-- sandbox sem allow-scripts: o HTML do servidor nunca executa script -->
    <iframe v-if="html" class="frame" sandbox="" :srcdoc="html" title="Pré-visualização do documento"></iframe>
  </div>
</template>

<style scoped>
.doc { display: flex; flex-direction: column; gap: 12px; }
.barra { display: flex; align-items: flex-end; gap: 10px; flex-wrap: wrap; }
.barra label { display: flex; flex-direction: column; gap: 5px; font-size: 12px; color: var(--text-secondary); min-width: 240px; }
select { padding: 7px 10px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); font-size: 13px; }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; }
.btn:disabled { opacity: .6; cursor: default; }
.aviso { margin: 0; font-size: 13px; color: var(--text-muted); }
.aviso.erro { color: var(--danger); }
.frame { width: 100%; height: 70vh; min-height: 420px; border: 1px solid var(--border); border-radius: 8px; background: #fff; }
</style>
