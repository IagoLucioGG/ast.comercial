<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { odataGet, odataPost, odataPatch, odataDelete } from '@/services/api'

const router = useRouter()

interface Modelo { Id: number; Nome: string; Descricao: string | null; EntidadeAlvo: string; Padrao: boolean; Ordem: number; Ativo: boolean }
interface Secao { Id: number; Nome: string; Ordem: number; ConteudoHtml: string; ModeloDocumentoId: number }

const modelos = ref<Modelo[]>([])
const modeloSelecionado = ref<Modelo | null>(null)
const secoes = ref<Secao[]>([])
const secaoAtiva = ref<Secao | null>(null)
const carregando = ref(false)
const salvando = ref(false)
const previewAberto = ref(false)

const modalAberto = ref(false)
const modalForm = ref({ Nome: '', Descricao: '', EntidadeAlvo: 'Proposta' })
const modalSecaoAberto = ref(false)
const secaoForm = ref({ Nome: '' })

async function carregarModelos() {
  carregando.value = true
  try {
    const res = await odataGet<Modelo>('ModelosDocumento', { orderby: 'Ordem asc,Nome asc', top: 50 })
    modelos.value = res.value
  } finally { carregando.value = false }
}

async function selecionarModelo(m: Modelo) {
  router.push({ name: 'editor-modelo', params: { id: m.Id } })
}

function abrirNovoModelo() { modalForm.value = { Nome: '', Descricao: '', EntidadeAlvo: 'Proposta' }; modalAberto.value = true }

const importando = ref(false)
const importMsg = ref('')
async function importarModeloPloomes() {
  importando.value = true; importMsg.value = ''
  try {
    const res = await fetch('/api/importar-modelo-ploomes', { method: 'POST', headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` } })
    const data = await res.json()
    importMsg.value = data.mensagem || data.erro || 'Concluído'
    await carregarModelos()
  } catch (e: any) { importMsg.value = e.message }
  finally { importando.value = false; setTimeout(() => importMsg.value = '', 5000) }
}
async function salvarModelo() {
  if (!modalForm.value.Nome.trim()) return
  await odataPost('ModelosDocumento', modalForm.value)
  modalAberto.value = false; await carregarModelos()
}

async function excluirModelo() {
  if (!modeloSelecionado.value || !confirm('Excluir modelo?')) return
  await odataDelete('ModelosDocumento', modeloSelecionado.value.Id)
  modeloSelecionado.value = null; secoes.value = []; await carregarModelos()
}

function abrirNovaSecao() { secaoForm.value = { Nome: '' }; modalSecaoAberto.value = true }
async function salvarSecao() {
  if (!secaoForm.value.Nome.trim() || !modeloSelecionado.value) return
  const nova = await odataPost<Secao>('SecoesModelo', { Nome: secaoForm.value.Nome, Ordem: secoes.value.length, ConteudoHtml: '', ModeloDocumentoId: modeloSelecionado.value.Id })
  modalSecaoAberto.value = false
  await selecionarModelo(modeloSelecionado.value)
  secaoAtiva.value = secoes.value.find(s => s.Id === nova.Id) || secaoAtiva.value
}

async function salvarConteudo() {
  if (!secaoAtiva.value) return
  salvando.value = true
  try { await odataPatch('SecoesModelo', secaoAtiva.value.Id, { ConteudoHtml: secaoAtiva.value.ConteudoHtml }) }
  finally { salvando.value = false }
}

async function excluirSecao(s: Secao) {
  if (!confirm(`Excluir seção "${s.Nome}"?`)) return
  await odataDelete('SecoesModelo', s.Id)
  if (modeloSelecionado.value) await selecionarModelo(modeloSelecionado.value)
}

onMounted(carregarModelos)
</script>

<template>
<div class="md-root">
  <div class="md-layout">
    <aside class="md-sidebar">
      <div class="md-sidebar-head"><span class="md-title">Modelos</span><button class="md-btn-add" @click="abrirNovoModelo"><i class="mdi mdi-plus"></i></button></div>
      <div v-if="carregando" class="md-loading">Carregando...</div>
      <div v-else class="md-lista">
        <button v-for="m in modelos" :key="m.Id" class="md-item" :class="{ ativo: modeloSelecionado?.Id === m.Id }" @click="selecionarModelo(m)"><i class="mdi mdi-file-document-outline"></i><span>{{ m.Nome }}</span></button>
        <p v-if="!modelos.length" class="md-vazio">Nenhum modelo criado</p>
      </div>
      <div class="md-sidebar-foot">
        <button class="md-btn-import" :disabled="importando" @click="importarModeloPloomes"><i class="mdi mdi-cloud-download-outline"></i> {{ importando ? 'Importando...' : 'Importar Ploomes' }}</button>
        <p v-if="importMsg" class="md-import-msg">{{ importMsg }}</p>
      </div>
    </aside>
    <div class="md-editor">
      <div v-if="!modeloSelecionado" class="md-placeholder"><i class="mdi mdi-file-document-edit-outline"></i><p>Selecione um modelo para editar</p></div>
      <template v-else>
        <div class="md-editor-head">
          <h3>{{ modeloSelecionado.Nome }}</h3>
          <div class="md-editor-actions">
            <button class="md-btn-sec" @click="abrirNovaSecao"><i class="mdi mdi-tab-plus"></i> Nova seção</button>
            <button class="md-btn-sec" @click="previewAberto = !previewAberto"><i class="mdi" :class="previewAberto ? 'mdi-code-tags' : 'mdi-eye-outline'"></i> {{ previewAberto ? 'Código' : 'Preview' }}</button>
            <button class="md-btn-del" @click="excluirModelo"><i class="mdi mdi-delete-outline"></i></button>
          </div>
        </div>
        <div class="md-tabs">
          <button v-for="s in secoes" :key="s.Id" class="md-tab" :class="{ ativo: secaoAtiva?.Id === s.Id }" @click="secaoAtiva = s">{{ s.Nome }}<i class="mdi mdi-close md-tab-del" @click.stop="excluirSecao(s)"></i></button>
        </div>
        <div v-if="secaoAtiva" class="md-content">
          <div v-if="!previewAberto" class="md-code-area">
            <textarea v-model="secaoAtiva.ConteudoHtml" class="md-textarea" placeholder="HTML do template com $proposta.Campo, $cliente.Campo, $item.Campo..."></textarea>
            <div class="md-code-foot"><span class="md-chars">{{ secaoAtiva.ConteudoHtml.length }} chars</span><button class="md-btn-save" :disabled="salvando" @click="salvarConteudo"><i class="mdi mdi-content-save"></i> {{ salvando ? 'Salvando...' : 'Salvar' }}</button></div>
          </div>
          <div v-else class="md-preview"><div class="md-preview-frame" v-html="secaoAtiva.ConteudoHtml"></div></div>
        </div>
        <div v-else class="md-placeholder"><p>Crie uma seção para começar</p></div>
      </template>
    </div>
  </div>
  <div v-if="modalAberto" class="md-overlay" @click.self="modalAberto = false"><div class="md-modal"><div class="md-modal-head"><h3>Novo Modelo</h3><button @click="modalAberto = false"><i class="mdi mdi-close"></i></button></div><div class="md-modal-body"><div class="md-field"><label>Nome</label><input v-model="modalForm.Nome" placeholder="Ex: Proposta Comercial 3.0" /></div><div class="md-field"><label>Descrição</label><input v-model="modalForm.Descricao" placeholder="Opcional" /></div></div><div class="md-modal-foot"><button class="md-btn-save" :disabled="!modalForm.Nome.trim()" @click="salvarModelo">Criar</button></div></div></div>
  <div v-if="modalSecaoAberto" class="md-overlay" @click.self="modalSecaoAberto = false"><div class="md-modal"><div class="md-modal-head"><h3>Nova Seção</h3><button @click="modalSecaoAberto = false"><i class="mdi mdi-close"></i></button></div><div class="md-modal-body"><div class="md-field"><label>Nome</label><input v-model="secaoForm.Nome" placeholder="Ex: Capa, Produtos..." /></div></div><div class="md-modal-foot"><button class="md-btn-save" :disabled="!secaoForm.Nome.trim()" @click="salvarSecao">Criar</button></div></div></div>
</div>
</template>

<style scoped>
.md-root{display:flex;flex-direction:column;height:100%}
.md-layout{display:flex;flex:1;overflow:hidden}
.md-sidebar{width:220px;border-right:1px solid var(--border);display:flex;flex-direction:column;background:var(--bg-secondary)}
.md-sidebar-head{display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border-bottom:1px solid var(--border)}
.md-title{font-size:14px;font-weight:600;color:var(--text-primary)}
.md-btn-add{width:28px;height:28px;border-radius:6px;border:1px solid var(--border);background:var(--bg-surface);color:var(--accent);cursor:pointer;display:flex;align-items:center;justify-content:center;font-size:16px}.md-btn-add:hover{background:var(--accent);color:#000}
.md-lista{flex:1;overflow-y:auto;padding:8px}
.md-item{display:flex;align-items:center;gap:8px;width:100%;text-align:left;padding:8px 12px;border:none;background:transparent;color:var(--text-secondary);font-size:13px;cursor:pointer;border-radius:6px}.md-item:hover{background:var(--bg-elevated)}.md-item.ativo{background:var(--accent);color:#000;font-weight:600}
.md-vazio{text-align:center;color:var(--text-muted);font-size:12px;padding:20px}
.md-sidebar-foot{padding:12px;border-top:1px solid var(--border)}
.md-btn-import{display:flex;align-items:center;gap:6px;width:100%;padding:8px 12px;border:1px dashed var(--border);border-radius:6px;background:transparent;color:var(--text-secondary);font-size:11px;cursor:pointer}.md-btn-import:hover{background:var(--bg-elevated);color:var(--accent);border-color:var(--accent)}.md-btn-import:disabled{opacity:.4}
.md-import-msg{font-size:10px;color:var(--accent);margin-top:6px;text-align:center}
.md-editor{flex:1;display:flex;flex-direction:column;overflow:hidden}
.md-placeholder{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:12px;color:var(--text-muted)}.md-placeholder .mdi{font-size:48px;opacity:.3}
.md-editor-head{display:flex;align-items:center;justify-content:space-between;padding:12px 20px;border-bottom:1px solid var(--border);flex-shrink:0}
.md-editor-head h3{font-size:15px;font-weight:600;color:var(--text-primary);margin:0}
.md-editor-actions{display:flex;gap:8px}
.md-btn-sec{display:flex;align-items:center;gap:5px;padding:6px 12px;border:1px solid var(--border);border-radius:6px;background:var(--bg-surface);color:var(--text-secondary);font-size:12px;cursor:pointer}.md-btn-sec:hover{background:var(--bg-elevated);color:var(--accent);border-color:var(--accent)}
.md-btn-del{width:30px;height:30px;border-radius:6px;border:1px solid rgba(239,68,68,.3);background:transparent;color:#ef4444;cursor:pointer;display:flex;align-items:center;justify-content:center;font-size:15px}.md-btn-del:hover{background:rgba(239,68,68,.1)}
.md-tabs{display:flex;gap:0;border-bottom:1px solid var(--border);padding:0 16px;flex-shrink:0}
.md-tab{display:flex;align-items:center;gap:6px;padding:10px 16px;border:none;background:transparent;color:var(--text-muted);font-size:12px;cursor:pointer;border-bottom:2px solid transparent}.md-tab:hover{color:var(--text-primary)}.md-tab.ativo{color:var(--accent);border-bottom-color:var(--accent);font-weight:600}
.md-tab-del{font-size:12px;opacity:0;transition:opacity .1s}.md-tab:hover .md-tab-del{opacity:1}.md-tab-del:hover{color:#ef4444}
.md-content{flex:1;display:flex;flex-direction:column;overflow:hidden}
.md-code-area{flex:1;display:flex;flex-direction:column;overflow:hidden}
.md-textarea{flex:1;resize:none;background:var(--bg-surface);border:none;padding:16px 20px;color:var(--text-primary);font-family:'JetBrains Mono',monospace;font-size:12px;line-height:1.6;outline:none;overflow:auto}
.md-code-foot{display:flex;align-items:center;justify-content:space-between;padding:8px 16px;border-top:1px solid var(--border);flex-shrink:0}
.md-chars{font-size:11px;color:var(--text-muted)}
.md-btn-save{display:flex;align-items:center;gap:5px;padding:6px 14px;border:none;border-radius:6px;background:var(--accent);color:#000;font-size:12px;font-weight:600;cursor:pointer}.md-btn-save:hover{background:var(--accent-hover)}.md-btn-save:disabled{opacity:.4;cursor:not-allowed}
.md-preview{flex:1;overflow:auto;background:#fff;padding:20px}
.md-preview-frame{max-width:850px;margin:0 auto}
.md-loading{padding:40px;text-align:center;color:var(--text-muted)}
.md-overlay{position:fixed;inset:0;background:rgba(0,0,0,.7);z-index:1000;display:flex;align-items:center;justify-content:center}
.md-modal{background:var(--bg-primary);border:1px solid var(--border);border-radius:12px;width:420px;max-width:90vw}
.md-modal-head{display:flex;align-items:center;justify-content:space-between;padding:16px 20px;border-bottom:1px solid var(--border)}
.md-modal-head h3{font-size:15px;font-weight:600;color:var(--text-primary);margin:0}
.md-modal-head button{background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:18px}
.md-modal-body{padding:20px;display:flex;flex-direction:column;gap:14px}
.md-modal-foot{display:flex;justify-content:flex-end;padding:14px 20px;border-top:1px solid var(--border)}
.md-field{display:flex;flex-direction:column;gap:5px}
.md-field label{font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase}
.md-field input,.md-field select{background:var(--bg-surface);border:1px solid var(--border);border-radius:8px;padding:10px 12px;color:var(--text-primary);font-size:14px;outline:none;width:100%;box-sizing:border-box}.md-field input:focus,.md-field select:focus{border-color:var(--accent)}
</style>
