<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { odataGet, odataPost, odataPatch, odataDelete } from '@/services/api'

interface SecaoForm { Id: number; Nome: string; Ordem: number; Colunas: number; Colapsavel: boolean; IniciaColapsada: boolean }
interface CampoForm { Id: number; NomeCampoFixo: string | null; Rotulo: string | null; Placeholder: string | null; Dica: string | null; Ordem: number; Obrigatorio: boolean; Visivel: boolean; SomenteLeitura: boolean; SecaoFormularioId: number | null }

const props = defineProps<{ entidade: string; endpoint: string; id?: number | null; titulo: string }>()
const emit = defineEmits<{ fechar: []; salvo: [] }>()

const secoes = ref<SecaoForm[]>([])
const campos = ref<CampoForm[]>([])
const valores = ref<Record<string, any>>({})
const carregando = ref(false)
const salvando = ref(false)
const erro = ref('')
const secoesColapsadas = ref<Set<number>>(new Set())
const modoEdicao = computed(() => !!props.id)

function camposDaSecao(secaoId: number | null) {
  return campos.value.filter(c => c.SecaoFormularioId === secaoId && c.Visivel).sort((a, b) => a.Ordem - b.Ordem)
}

function labelCampo(c: CampoForm): string { return c.Rotulo || c.NomeCampoFixo || '' }

function toggleSecao(id: number) {
  if (secoesColapsadas.value.has(id)) secoesColapsadas.value.delete(id)
  else secoesColapsadas.value.add(id)
  secoesColapsadas.value = new Set(secoesColapsadas.value)
}

async function carregarFormulario() {
  carregando.value = true
  try {
    const resForm = await odataGet<any>('Formularios', { filter: `EntidadeAlvo eq '${props.entidade}' and Padrao eq true`, top: 1 })
    if (!resForm.value.length) { carregando.value = false; return }
    const formId = resForm.value[0].Id
    const [resSecoes, resCampos] = await Promise.all([
      odataGet<SecaoForm>('SecoesFormulario', { filter: `FormularioId eq ${formId}`, orderby: 'Ordem asc', top: 100 }),
      odataGet<CampoForm>('CamposFormulario', { filter: `FormularioId eq ${formId}`, orderby: 'Ordem asc', top: 100 })
    ])
    secoes.value = resSecoes.value
    campos.value = resCampos.value
    secoesColapsadas.value = new Set(resSecoes.value.filter(s => s.IniciaColapsada).map(s => s.Id))
  } finally { carregando.value = false }
}

async function carregarDados() {
  if (!props.id) return
  try {
    const res = await fetch(`/odata/${props.endpoint}(${props.id})`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` } })
    if (res.ok) {
      const data = await res.json()
      for (const c of campos.value) {
        const key = c.NomeCampoFixo
        if (key && data[key] !== undefined) valores.value[key] = data[key]
      }
    }
  } catch {}
}

async function salvar() {
  salvando.value = true; erro.value = ''
  try {
    const payload: Record<string, any> = {}
    for (const c of campos.value) {
      const key = c.NomeCampoFixo
      if (!key) continue
      const v = valores.value[key]
      if (v !== undefined && v !== '' && v !== null) payload[key] = v
    }
    if (modoEdicao.value) await odataPatch(props.endpoint, props.id!, payload)
    else await odataPost(props.endpoint, payload)
    emit('salvo'); emit('fechar')
  } catch (e: any) { erro.value = e.message ?? 'Erro ao salvar' } finally { salvando.value = false }
}

async function excluir() {
  if (!props.id || !confirm('Deseja realmente excluir?')) return
  try { await odataDelete(props.endpoint, props.id); emit('salvo'); emit('fechar') } catch (e: any) { erro.value = e.message ?? 'Erro' }
}

onMounted(async () => { await carregarFormulario(); await carregarDados() })
</script>

<template>
<div class="fd-overlay" @click.self="emit('fechar')">
  <div class="fd-modal">
    <div class="fd-head">
      <h3>{{ modoEdicao ? 'Editar' : 'Novo' }} {{ titulo }}</h3>
      <button class="fd-close" @click="emit('fechar')"><i class="mdi mdi-close"></i></button>
    </div>
    <div class="fd-body">
      <div v-if="carregando" class="fd-loading">Carregando formulário...</div>
      <template v-else>
        <!-- Campos sem seção -->
        <div v-if="camposDaSecao(null).length" class="fd-grid">
          <div v-for="c in camposDaSecao(null)" :key="c.Id" class="fd-campo">
            <label :class="{ obrig: c.Obrigatorio }">{{ labelCampo(c) }}</label>
            <input v-model="valores[c.NomeCampoFixo!]" :placeholder="c.Placeholder || labelCampo(c)" :disabled="c.SomenteLeitura" :required="c.Obrigatorio" />
            <small v-if="c.Dica" class="fd-dica">{{ c.Dica }}</small>
          </div>
        </div>
        <!-- Seções -->
        <div v-for="s in secoes" :key="s.Id" class="fd-secao">
          <button class="fd-secao-head" @click="s.Colapsavel && toggleSecao(s.Id)">
            <i v-if="s.Colapsavel" class="mdi" :class="secoesColapsadas.has(s.Id) ? 'mdi-chevron-right' : 'mdi-chevron-down'"></i>
            <span>{{ s.Nome }}</span>
          </button>
          <div v-if="!secoesColapsadas.has(s.Id)" class="fd-grid" :style="{ 'grid-template-columns': `repeat(${s.Colunas || 2}, 1fr)` }">
            <div v-for="c in camposDaSecao(s.Id)" :key="c.Id" class="fd-campo">
              <label :class="{ obrig: c.Obrigatorio }">{{ labelCampo(c) }}</label>
              <input v-model="valores[c.NomeCampoFixo!]" :placeholder="c.Placeholder || labelCampo(c)" :disabled="c.SomenteLeitura" :required="c.Obrigatorio" />
              <small v-if="c.Dica" class="fd-dica">{{ c.Dica }}</small>
            </div>
          </div>
        </div>
      </template>
      <p v-if="erro" class="fd-erro">{{ erro }}</p>
    </div>
    <div class="fd-foot">
      <button v-if="modoEdicao" class="fd-btn-del" @click="excluir"><i class="mdi mdi-delete-outline"></i> Excluir</button>
      <div style="flex:1"></div>
      <button class="fd-btn-cancel" @click="emit('fechar')">Cancelar</button>
      <button class="fd-btn-save" :disabled="salvando" @click="salvar"><i class="mdi mdi-check"></i> {{ salvando ? 'Salvando...' : 'Salvar' }}</button>
    </div>
  </div>
</div>
</template>

<style scoped>
.fd-overlay{position:fixed;inset:0;background:rgba(0,0,0,.7);z-index:1000;display:flex;align-items:flex-start;justify-content:center;padding:40px 20px;overflow-y:auto}
.fd-modal{background:var(--bg-primary);border:1px solid var(--border);border-radius:14px;width:680px;max-width:95vw;display:flex;flex-direction:column;box-shadow:0 24px 64px rgba(0,0,0,.5)}
.fd-head{display:flex;align-items:center;justify-content:space-between;padding:18px 24px;border-bottom:1px solid var(--border)}
.fd-head h3{font-size:16px;font-weight:600;color:var(--text-primary);margin:0}
.fd-close{background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:20px}.fd-close:hover{color:var(--text-primary)}
.fd-body{padding:24px;max-height:65vh;overflow-y:auto}
.fd-loading{text-align:center;padding:40px;color:var(--text-muted)}
.fd-grid{display:grid;grid-template-columns:repeat(2,1fr);gap:16px;margin-bottom:8px}
.fd-campo{display:flex;flex-direction:column;gap:5px}
.fd-campo label{font-size:12px;font-weight:500;color:var(--text-secondary)}
.fd-campo label.obrig::after{content:' *';color:#ef4444}
.fd-campo input{background:var(--bg-surface);border:1px solid var(--border);border-radius:8px;padding:10px 12px;color:var(--text-primary);font-size:14px;outline:none;width:100%;box-sizing:border-box}
.fd-campo input:focus{border-color:var(--accent)}
.fd-campo input:disabled{opacity:.5;cursor:not-allowed}
.fd-dica{font-size:11px;color:var(--text-muted)}
.fd-secao{margin-top:16px;border:1px solid var(--border);border-radius:10px;overflow:hidden}
.fd-secao-head{display:flex;align-items:center;gap:8px;width:100%;padding:12px 16px;border:none;background:var(--bg-secondary);color:var(--text-primary);font-size:13px;font-weight:600;cursor:pointer;text-align:left}
.fd-secao-head:hover{background:var(--bg-elevated)}
.fd-secao-head .mdi{font-size:16px;color:var(--text-muted)}
.fd-secao .fd-grid{padding:16px}
.fd-erro{color:#ef4444;font-size:13px;margin-top:12px;padding:8px 12px;background:rgba(239,68,68,.08);border-radius:6px}
.fd-foot{display:flex;align-items:center;gap:10px;padding:14px 24px;border-top:1px solid var(--border)}
.fd-btn-del{display:flex;align-items:center;gap:4px;padding:8px 14px;border:1px solid rgba(239,68,68,.3);border-radius:8px;background:transparent;color:#ef4444;font-size:13px;cursor:pointer}.fd-btn-del:hover{background:rgba(239,68,68,.1)}
.fd-btn-cancel{padding:8px 16px;border:1px solid var(--border);border-radius:8px;background:transparent;color:var(--text-secondary);font-size:13px;cursor:pointer}.fd-btn-cancel:hover{background:var(--bg-elevated)}
.fd-btn-save{display:flex;align-items:center;gap:6px;padding:8px 20px;border:none;border-radius:8px;background:var(--accent);color:#000;font-size:13px;font-weight:600;cursor:pointer}.fd-btn-save:hover{background:var(--accent-hover)}.fd-btn-save:disabled{opacity:.4;cursor:not-allowed}
</style>
