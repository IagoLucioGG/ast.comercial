<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { odataPost, odataPatch, odataDelete } from '@/services/api'

interface CampoForm { nome: string; tipo: string }

const props = defineProps<{ entidade: string; endpoint: string; id?: number | null; titulo: string }>()
const emit = defineEmits<{ fechar: []; salvo: [] }>()

const campos = ref<CampoForm[]>([])
const valores = ref<Record<string, any>>({})
const carregando = ref(false)
const salvando = ref(false)
const erro = ref('')
const modoEdicao = computed(() => !!props.id)

async function carregarSchema() {
  try {
    const res = await fetch(`/odata/Campos@Esquema(${props.entidade})`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` } })
    if (!res.ok) return
    const schema = await res.json()
    const p = schema['$registro']?.propriedades ?? {}
    campos.value = Object.entries(p)
      .filter(([nome]) => !['SenhaHash','ChaveExterna','Id','EmpresaId','CriadoEm','AtualizadoEm','Ativo'].includes(nome))
      .map(([nome, info]: any) => ({ nome, tipo: info.tipo }))
      .filter(c => !c.tipo.startsWith('Cargo') && !c.tipo.startsWith('Departamento') && !c.tipo.startsWith('Empresa') && !c.tipo.startsWith('Equipe'))
  } catch {}
}

async function carregarDados() {
  if (!props.id) return
  carregando.value = true
  try {
    const res = await fetch(`/odata/${props.endpoint}(${props.id})`, { headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` } })
    if (res.ok) { const data = await res.json(); for (const c of campos.value) { if (data[c.nome] !== undefined) valores.value[c.nome] = data[c.nome] } }
  } finally { carregando.value = false }
}

async function salvar() {
  salvando.value = true; erro.value = ''
  try {
    const payload: Record<string, any> = {}
    for (const c of campos.value) { const v = valores.value[c.nome]; if (v !== undefined && v !== '' && v !== null) payload[c.nome] = v }
    if (modoEdicao.value) await odataPatch(props.endpoint, props.id!, payload)
    else await odataPost(props.endpoint, payload)
    emit('salvo'); emit('fechar')
  } catch (e: any) { erro.value = e.message ?? 'Erro ao salvar' } finally { salvando.value = false }
}

async function excluir() {
  if (!props.id || !confirm('Deseja realmente excluir?')) return
  try { await odataDelete(props.endpoint, props.id); emit('salvo'); emit('fechar') } catch (e: any) { erro.value = e.message ?? 'Erro' }
}

function opcoesEnum(tipo: string): string[] { return tipo.startsWith('enum:') ? tipo.split(':')[1].split('|') : [] }
onMounted(async () => { await carregarSchema(); await carregarDados() })
</script>

<template>
<div class="ov" @click.self="emit('fechar')"><div class="md">
  <div class="mh"><h3>{{ modoEdicao ? 'Editar' : 'Novo' }} {{ titulo }}</h3><button class="bx" @click="emit('fechar')"><i class="mdi mdi-close"></i></button></div>
  <div class="mb"><div v-if="carregando" class="ld">Carregando...</div><div v-else class="fg">
    <div v-for="c in campos" :key="c.nome" class="fc">
      <label>{{ c.nome }}</label>
      <div v-if="c.tipo==='boolean'" class="bt"><button :class="{a:valores[c.nome]===true}" @click="valores[c.nome]=true">Sim</button><button :class="{a:valores[c.nome]===false}" @click="valores[c.nome]=false">Não</button></div>
      <select v-else-if="c.tipo.startsWith('enum')" v-model="valores[c.nome]"><option value="">Selecione...</option><option v-for="o in opcoesEnum(c.tipo)" :key="o" :value="o">{{o}}</option></select>
      <input v-else-if="c.tipo==='datetime'" type="datetime-local" v-model="valores[c.nome]"/>
      <input v-else-if="c.tipo==='number'" type="number" v-model="valores[c.nome]"/>
      <input v-else type="text" v-model="valores[c.nome]" :placeholder="c.nome"/>
    </div>
  </div><p v-if="erro" class="er">{{erro}}</p></div>
  <div class="mf"><button v-if="modoEdicao" class="bd" @click="excluir"><i class="mdi mdi-delete-outline"></i> Excluir</button><div class="sp"></div><button class="bc" @click="emit('fechar')">Cancelar</button><button class="bs" :disabled="salvando" @click="salvar"><i class="mdi mdi-check"></i> {{salvando?'Salvando...':'Salvar'}}</button></div>
</div></div>
</template>

<style scoped>
.ov{position:fixed;inset:0;background:rgba(0,0,0,.7);z-index:1000;display:flex;align-items:center;justify-content:center}
.md{background:var(--bg-primary);border:1px solid var(--border);border-radius:14px;width:560px;max-width:95vw;max-height:85vh;display:flex;flex-direction:column;box-shadow:0 24px 64px rgba(0,0,0,.5)}
.mh{display:flex;align-items:center;justify-content:space-between;padding:18px 24px;border-bottom:1px solid var(--border)}
.mh h3{font-size:16px;font-weight:600;color:var(--text-primary)}
.bx{background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:20px}.bx:hover{color:var(--text-primary)}
.mb{flex:1;overflow-y:auto;padding:24px}
.ld{text-align:center;padding:40px;color:var(--text-muted)}
.fg{display:grid;grid-template-columns:1fr 1fr;gap:16px}
.fc{display:flex;flex-direction:column;gap:6px}
.fc label{font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.5px}
.fc input,.fc select{background:var(--bg-surface);border:1px solid var(--border);border-radius:8px;padding:10px 12px;color:var(--text-primary);font-size:14px;outline:none}
.fc input:focus,.fc select:focus{border-color:var(--accent)}
.bt{display:flex;border:1px solid var(--border);border-radius:8px;overflow:hidden}
.bt button{flex:1;padding:8px;border:none;background:transparent;color:var(--text-muted);font-size:13px;cursor:pointer}
.bt button:first-child{border-right:1px solid var(--border)}
.bt button.a{background:var(--accent);color:#000;font-weight:600}
.er{color:#ef4444;font-size:13px;margin-top:12px;padding:8px 12px;background:rgba(239,68,68,.08);border-radius:6px}
.mf{display:flex;align-items:center;gap:10px;padding:14px 24px;border-top:1px solid var(--border)}
.sp{flex:1}
.bd{display:flex;align-items:center;gap:4px;padding:8px 14px;border:1px solid rgba(239,68,68,.3);border-radius:8px;background:transparent;color:#ef4444;font-size:13px;cursor:pointer}.bd:hover{background:rgba(239,68,68,.1)}
.bc{padding:8px 16px;border:1px solid var(--border);border-radius:8px;background:transparent;color:var(--text-secondary);font-size:13px;cursor:pointer}.bc:hover{background:var(--bg-elevated)}
.bs{display:flex;align-items:center;gap:6px;padding:8px 20px;border:none;border-radius:8px;background:var(--accent);color:#000;font-size:13px;font-weight:600;cursor:pointer}.bs:hover{background:var(--accent-hover)}.bs:disabled{opacity:.4;cursor:not-allowed}
</style>
