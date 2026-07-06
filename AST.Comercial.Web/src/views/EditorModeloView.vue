<script setup lang="ts">
import { ref, onMounted, nextTick } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { odataGet, odataPatch, odataPost, odataDelete } from '@/services/api'

interface Secao { Id: number; Nome: string; Ordem: number; ConteudoHtml: string; ModeloDocumentoId: number }
interface CampoDisp { nome: string; tipo: string; caminho: string; chave: string }

const route = useRoute()
const router = useRouter()
const modeloId = Number(route.params.id)

const modeloNome = ref('')
const secoes = ref<Secao[]>([])
const secaoAtiva = ref<Secao | null>(null)
const salvando = ref(false)
const mensagem = ref('')
const modoFonte = ref(false)
const editorRef = ref<HTMLDivElement | null>(null)

const abaLateral = ref<'campos' | 'blocos'>('campos')
const entidades = ['Proposta', 'Cliente', 'Produto', 'ItemProposta', 'SecaoProposta', 'Usuario', 'Empresa']
const entidadeAberta = ref<string | null>(null)
const camposPorEntidade = ref<Record<string, CampoDisp[]>>({})
const modalSecao = ref(false)
const novaSecaoNome = ref('')

const varPrefixo: Record<string, string> = { Proposta: '$proposta', Cliente: '$cliente', Produto: '$produto', ItemProposta: '$item', SecaoProposta: '$bloco', Usuario: '$usuario', Empresa: '$empresa' }
const labelEnt: Record<string, string> = { Proposta: 'Proposta', Cliente: 'Cliente', Produto: 'Produto', ItemProposta: 'Produto da proposta', SecaoProposta: 'Bloco', Usuario: 'Usuário', Empresa: 'Sua empresa' }

async function carregar() {
  const res = await odataGet<any>('ModelosDocumento', { filter: `Id eq ${modeloId}`, top: 1 })
  if (res.value.length) modeloNome.value = res.value[0].Nome
  const resS = await odataGet<Secao>('SecoesModelo', { filter: `ModeloDocumentoId eq ${modeloId}`, orderby: 'Ordem asc', top: 20 })
  secoes.value = resS.value
  if (secoes.value.length && !secaoAtiva.value) { secaoAtiva.value = secoes.value[0]; await nextTick(); sincronizarEditor() }
}

async function carregarCampos() {
  for (const ent of entidades) {
    try {
      const res = await fetch(`/odata/Campos@Esquema(${ent})`, { headers: { Authorization: `Bearer ${localStorage.getItem('token')}` } })
      if (res.ok) {
        const data = await res.json()
        const props = data?.['$registro']?.propriedades ?? data?.['$registro']?.Propriedades ?? {}
        camposPorEntidade.value[ent] = Object.entries(props).map(([nome, info]: any) => ({
          nome,
          tipo: info?.tipo ?? info?.Tipo ?? 'string',
          caminho: `${varPrefixo[ent]}.${nome}`,
          chave: info?.chave ?? info?.Chave ?? `${ent}_${nome}`
        }))
      } else { camposPorEntidade.value[ent] = [] }
    } catch { camposPorEntidade.value[ent] = [] }
  }
}

function selecionarSecao(s: Secao) { if (editorRef.value && secaoAtiva.value && !modoFonte.value) secaoAtiva.value.ConteudoHtml = editorRef.value.innerHTML; secaoAtiva.value = s; modoFonte.value = false; nextTick(() => sincronizarEditor()) }
function sincronizarEditor() { if (editorRef.value && secaoAtiva.value) editorRef.value.innerHTML = secaoAtiva.value.ConteudoHtml }
function onEditorInput() { if (editorRef.value && secaoAtiva.value) secaoAtiva.value.ConteudoHtml = editorRef.value.innerHTML }

function toggleFonte() {
  if (modoFonte.value) { modoFonte.value = false; nextTick(() => sincronizarEditor()) }
  else { if (editorRef.value && secaoAtiva.value) secaoAtiva.value.ConteudoHtml = editorRef.value.innerHTML; modoFonte.value = true }
}

function inserirCampo(c: CampoDisp) {
  if (!secaoAtiva.value) return
  const entidade = c.caminho.split('.')[0].replace('$', '')
  const fieldHtml = `<field key="${c.chave}" entity="${entidade}" path="${c.caminho}" contenteditable="false">[${entidade}.${c.nome}]</field>&nbsp;`
  if (modoFonte.value) { secaoAtiva.value.ConteudoHtml += fieldHtml; return }
  editorRef.value?.focus()
  document.execCommand('insertHTML', false, fieldHtml)
  onEditorInput()
}

function execCmd(cmd: string, val?: string) { document.execCommand(cmd, false, val); editorRef.value?.focus(); onEditorInput() }

async function salvarTudo() {
  if (editorRef.value && secaoAtiva.value && !modoFonte.value) secaoAtiva.value.ConteudoHtml = editorRef.value.innerHTML
  salvando.value = true; mensagem.value = ''
  try { for (const s of secoes.value) await odataPatch('SecoesModelo', s.Id, { ConteudoHtml: s.ConteudoHtml }); mensagem.value = 'Salvo!' } finally { salvando.value = false; setTimeout(() => mensagem.value = '', 2000) }
}

async function criarSecao() { if (!novaSecaoNome.value.trim()) return; await odataPost('SecoesModelo', { Nome: novaSecaoNome.value, Ordem: secoes.value.length, ConteudoHtml: '<p></p>', ModeloDocumentoId: modeloId }); modalSecao.value = false; novaSecaoNome.value = ''; await carregar() }
async function excluirSecao(s: Secao) { if (!confirm(`Excluir "${s.Nome}"?`)) return; await odataDelete('SecoesModelo', s.Id); secaoAtiva.value = null; await carregar() }
function voltar() { router.push({ name: 'admin-painel', params: { painel: 'modelos-documentos' } }) }
onMounted(async () => { await carregar(); await carregarCampos() })
</script>

<template>
<div class="em-root">
  <div class="em-header">
    <button class="em-back" @click="voltar"><i class="mdi mdi-arrow-left"></i></button>
    <span class="em-titulo">{{ modeloNome }}</span>
    <span v-if="mensagem" class="em-msg">{{ mensagem }}</span>
    <div style="flex:1"></div>
    <button class="em-btn-save" :disabled="salvando" @click="salvarTudo"><i class="mdi mdi-content-save"></i> Salvar</button>
  </div>
  <div class="em-toolbar">
    <button @click="execCmd('bold')"><i class="mdi mdi-format-bold"></i></button>
    <button @click="execCmd('italic')"><i class="mdi mdi-format-italic"></i></button>
    <button @click="execCmd('underline')"><i class="mdi mdi-format-underline"></i></button>
    <button @click="execCmd('strikethrough')"><i class="mdi mdi-format-strikethrough"></i></button>
    <span class="em-sep"></span>
    <button @click="execCmd('justifyLeft')"><i class="mdi mdi-format-align-left"></i></button>
    <button @click="execCmd('justifyCenter')"><i class="mdi mdi-format-align-center"></i></button>
    <button @click="execCmd('justifyRight')"><i class="mdi mdi-format-align-right"></i></button>
    <span class="em-sep"></span>
    <button @click="execCmd('insertUnorderedList')"><i class="mdi mdi-format-list-bulleted"></i></button>
    <button @click="execCmd('insertOrderedList')"><i class="mdi mdi-format-list-numbered"></i></button>
    <span class="em-sep"></span>
    <button @click="execCmd('insertHorizontalRule')"><i class="mdi mdi-minus"></i></button>
    <button @click="execCmd('insertHTML','&lt;table border=1 cellpadding=5 style=width:100%&gt;&lt;tr&gt;&lt;td&gt;&lt;/td&gt;&lt;td&gt;&lt;/td&gt;&lt;/tr&gt;&lt;/table&gt;')"><i class="mdi mdi-table"></i></button>
    <span class="em-sep"></span>
    <button :class="{ativo:modoFonte}" @click="toggleFonte"><i class="mdi mdi-code-tags"></i> Código fonte</button>
  </div>
  <div class="em-tabs">
    <button class="em-tab em-tab-new" @click="modalSecao=true"><i class="mdi mdi-plus"></i> Nova seção</button>
    <button v-for="s in secoes" :key="s.Id" class="em-tab" :class="{ativo:secaoAtiva?.Id===s.Id}" @click="selecionarSecao(s)">{{s.Nome}}<i class="mdi mdi-close em-tab-x" @click.stop="excluirSecao(s)"></i></button>
  </div>
  <div class="em-body">
    <aside class="em-sidebar">
      <div class="em-sidebar-tabs"><button :class="{ativo:abaLateral==='campos'}" @click="abaLateral='campos'">Campos</button><button :class="{ativo:abaLateral==='blocos'}" @click="abaLateral='blocos'">Blocos</button></div>
      <div v-if="abaLateral==='campos'" class="em-entities">
        <div v-for="ent in entidades" :key="ent" class="em-ent">
          <button class="em-ent-head" @click="entidadeAberta=entidadeAberta===ent?null:ent"><span>{{labelEnt[ent]||ent}}</span><i class="mdi" :class="entidadeAberta===ent?'mdi-chevron-down':'mdi-chevron-right'"></i></button>
          <div v-if="entidadeAberta===ent" class="em-campos"><button v-for="c in (camposPorEntidade[ent]||[])" :key="c.caminho" class="em-campo" @click="inserirCampo(c)">{{c.nome}}</button></div>
        </div>
      </div>
      <div v-else class="em-blocos-info"><p>Blocos (em breve)</p></div>
    </aside>
    <div class="em-main">
      <div v-if="!secaoAtiva" class="em-placeholder"><p>Selecione ou crie uma seção</p></div>
      <template v-else>
        <div v-show="!modoFonte" ref="editorRef" class="em-wysiwyg" contenteditable="true" @input="onEditorInput"></div>
        <textarea v-show="modoFonte" v-model="secaoAtiva.ConteudoHtml" class="em-code"></textarea>
      </template>
    </div>
  </div>
  <div v-if="modalSecao" class="em-overlay" @click.self="modalSecao=false"><div class="em-modal"><h3>Nova Seção</h3><input v-model="novaSecaoNome" placeholder="Nome" @keyup.enter="criarSecao"/><button :disabled="!novaSecaoNome.trim()" @click="criarSecao">Criar</button></div></div>
</div>
</template>

<style scoped>
.em-root{display:flex;flex-direction:column;height:100vh;overflow:hidden;background:var(--bg-primary)}
.em-header{display:flex;align-items:center;gap:10px;padding:8px 16px;border-bottom:1px solid var(--border);flex-shrink:0;background:var(--bg-secondary)}
.em-titulo{font-size:14px;font-weight:600;color:var(--text-primary)}
.em-back{width:30px;height:30px;border-radius:6px;border:1px solid var(--border);background:var(--bg-surface);color:var(--text-secondary);cursor:pointer;display:flex;align-items:center;justify-content:center}.em-back:hover{background:var(--bg-elevated)}
.em-btn-save{display:flex;align-items:center;gap:4px;padding:6px 14px;border:none;border-radius:6px;background:var(--accent);color:#000;font-size:12px;font-weight:600;cursor:pointer}.em-btn-save:disabled{opacity:.4}
.em-msg{font-size:11px;color:var(--accent)}
.em-toolbar{display:flex;align-items:center;gap:1px;padding:4px 12px;border-bottom:1px solid var(--border);flex-shrink:0}
.em-toolbar button{width:28px;height:26px;border:none;background:transparent;color:var(--text-secondary);cursor:pointer;border-radius:4px;display:flex;align-items:center;justify-content:center;font-size:14px}.em-toolbar button:hover{background:var(--bg-elevated);color:var(--text-primary)}.em-toolbar button.ativo{background:var(--accent);color:#000}
.em-sep{width:1px;height:18px;background:var(--border);margin:0 4px}
.em-tabs{display:flex;gap:0;border-bottom:1px solid var(--border);padding:0 12px;flex-shrink:0;overflow-x:auto}
.em-tab{display:flex;align-items:center;gap:4px;padding:7px 12px;border:none;background:transparent;color:var(--text-muted);font-size:11px;cursor:pointer;border-bottom:2px solid transparent;white-space:nowrap}.em-tab:hover{color:var(--text-primary)}.em-tab.ativo{color:var(--accent);border-bottom-color:var(--accent);font-weight:600}
.em-tab-new{color:var(--accent)}
.em-tab-x{font-size:10px;opacity:0}.em-tab:hover .em-tab-x{opacity:1}
.em-body{display:flex;flex:1;overflow:hidden}
.em-sidebar{width:180px;border-right:1px solid var(--border);display:flex;flex-direction:column;overflow:hidden;background:var(--bg-secondary)}
.em-sidebar-tabs{display:flex;border-bottom:1px solid var(--border)}
.em-sidebar-tabs button{flex:1;padding:7px;border:none;background:transparent;color:var(--text-muted);font-size:10px;cursor:pointer;border-bottom:2px solid transparent}.em-sidebar-tabs button.ativo{color:var(--accent);border-bottom-color:var(--accent);font-weight:600}
.em-entities{flex:1;overflow-y:auto}
.em-ent{border-bottom:1px solid var(--border)}
.em-ent-head{display:flex;align-items:center;justify-content:space-between;width:100%;padding:7px 10px;border:none;background:transparent;cursor:pointer;font-size:11px;font-weight:500;color:var(--text-primary)}.em-ent-head:hover{background:var(--bg-elevated)}.em-ent-head .mdi{font-size:13px;color:var(--text-muted)}
.em-campos{padding:2px 6px 6px}
.em-campo{display:block;width:100%;text-align:left;padding:3px 8px;border:none;background:transparent;color:var(--text-secondary);font-size:10px;cursor:pointer;border-radius:3px}.em-campo:hover{background:var(--bg-elevated);color:var(--accent)}
.em-blocos-info{padding:20px;text-align:center;color:var(--text-muted);font-size:10px}
.em-main{flex:1;display:flex;flex-direction:column;overflow:hidden}
.em-placeholder{flex:1;display:flex;align-items:center;justify-content:center;color:var(--text-muted);font-size:13px}
.em-wysiwyg{flex:1;overflow:auto;padding:30px 40px;background:#fff;color:#222;font-size:14px;line-height:1.6;outline:none}
.em-wysiwyg:focus{outline:none}
.em-wysiwyg :deep(field){display:inline-block;background:#e8f5e9;color:#2e7d32;padding:2px 8px;border-radius:4px;font-size:12px;font-weight:500;border:1px solid #a5d6a7;cursor:default;user-select:none;margin:0 2px;vertical-align:baseline}
.em-wysiwyg :deep(table){border-collapse:collapse;width:100%}
.em-wysiwyg :deep(td),.em-wysiwyg :deep(th){border:1px solid #ccc;padding:5px 8px}
.em-wysiwyg :deep(img){max-width:100%;height:auto}
.em-code{flex:1;resize:none;background:#1e1e1e;border:none;padding:16px;color:#d4d4d4;font-family:'JetBrains Mono',monospace;font-size:11px;line-height:1.6;outline:none;overflow:auto}
.em-overlay{position:fixed;inset:0;background:rgba(0,0,0,.7);z-index:1000;display:flex;align-items:center;justify-content:center}
.em-modal{background:var(--bg-primary);border:1px solid var(--border);border-radius:10px;padding:20px;width:320px;display:flex;flex-direction:column;gap:12px}
.em-modal h3{font-size:14px;font-weight:600;color:var(--text-primary);margin:0}
.em-modal input{background:var(--bg-surface);border:1px solid var(--border);border-radius:6px;padding:8px 10px;color:var(--text-primary);font-size:13px;outline:none}.em-modal input:focus{border-color:var(--accent)}
.em-modal button{padding:8px;border:none;border-radius:6px;background:var(--accent);color:#000;font-weight:600;font-size:12px;cursor:pointer}.em-modal button:disabled{opacity:.4}
</style>
