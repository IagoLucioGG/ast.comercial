<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { odataGet, odataPost, odataPatch, odataDelete } from '@/services/api'

interface Formulario {
  Id: number; Nome: string; EntidadeAlvo: string; Padrao: boolean; Ordem: number; Ativo: boolean
}
interface SecaoFormulario {
  Id: number; Nome: string; Ordem: number; Colunas: number | null; Colapsavel: boolean; IniciaColapsada: boolean; FormularioId: number
}

function toggleSecaoColapsada(id: number) {
  if (secoesColapsadas.value.has(id)) secoesColapsadas.value.delete(id)
  else secoesColapsadas.value.add(id)
  secoesColapsadas.value = new Set(secoesColapsadas.value)
}
interface CampoFormulario {
  Id: number; NomeCampoFixo: string | null; CampoId: number | null; Ordem: number
  Coluna: number | null; LarguraColunas: number | null; Rotulo: string | null
  Placeholder: string | null; Dica: string | null; Icone: string | null
  Visivel: boolean; Obrigatorio: boolean; SomenteLeitura: boolean
  SecaoFormularioId: number | null; FormularioId: number
}

const entidades = ['Cliente', 'Produto', 'Empresa', 'Usuario', 'Atividade']
const formularios = ref<Formulario[]>([])
const carregando = ref(false)
const gruposAbertos = ref<Record<string, boolean>>({})

// Editor state
const formularioSelecionado = ref<Formulario | null>(null)
const secoes = ref<SecaoFormulario[]>([])
const campos = ref<CampoFormulario[]>([])
const carregandoEditor = ref(false)
const secoesColapsadas = ref<Set<number>>(new Set())

// Modal novo formulário
const modalNovoAberto = ref(false)
const novoForm = ref({ Nome: '', EntidadeAlvo: 'Cliente' })

// Modal seção
const modalSecaoAberto = ref(false)
const secaoForm = ref({ Nome: '', Colunas: 2, Colapsavel: false, IniciaColapsada: false })
const secaoEditandoId = ref<number | null>(null)

// Modal campo
const modalCampoAberto = ref(false)
const campoForm = ref({ NomeCampoFixo: '', Rotulo: '', Placeholder: '', Dica: '', Coluna: null as number | null, LarguraColunas: null as number | null, Obrigatorio: false, Visivel: true, SomenteLeitura: false, SecaoFormularioId: null as number | null })
const campoEditandoId = ref<number | null>(null)

const formulariosPorEntidade = computed(() => {
  const mapa: Record<string, Formulario[]> = {}
  // Grupos visuais que agrupam várias entidades
  const grupoEntidades: Record<string, string[]> = {
    Cliente: ['Cliente', 'PessoaContato'],
    Produto: ['Produto'],
    Empresa: ['Empresa'],
    Usuario: ['Usuario', 'Equipe'],
    Atividade: ['Atividade']
  }
  for (const e of entidades) mapa[e] = []
  for (const f of formularios.value) {
    for (const [grupo, ents] of Object.entries(grupoEntidades)) {
      if (ents.includes(f.EntidadeAlvo)) {
        mapa[grupo]?.push(f)
        break
      }
    }
  }
  return mapa
})

function labelEntidade(e: string): string {
  const map: Record<string, string> = {
    Cliente: 'Cliente', Produto: 'Produto', Empresa: 'Sua Empresa',
    Usuario: 'Usuário', Atividade: 'Registro de Interação'
  }
  return map[e] || e
}

function toggleGrupo(e: string) {
  gruposAbertos.value[e] = !gruposAbertos.value[e]
}

async function carregarFormularios() {
  carregando.value = true
  try {
    const res = await odataGet<Formulario>('Formularios', { orderby: 'EntidadeAlvo asc,Ordem asc,Nome asc', top: 100, count: true })
    formularios.value = res.value
    // Abrir todos os grupos que têm formulários
    for (const e of entidades) {
      gruposAbertos.value[e] = (formulariosPorEntidade.value[e]?.length ?? 0) > 0
    }
    // Selecionar primeiro formulário automaticamente
    if (formularios.value.length && !formularioSelecionado.value) {
      await selecionarFormulario(formularios.value[0])
    }
  } finally { carregando.value = false }
}

async function selecionarFormulario(f: Formulario) {
  formularioSelecionado.value = f
  carregandoEditor.value = true
  try {
    const [resSecoes, resCampos] = await Promise.all([
      odataGet<SecaoFormulario>('SecoesFormulario', { filter: `FormularioId eq ${f.Id}`, orderby: 'Ordem asc', top: 100 }),
      odataGet<CampoFormulario>('CamposFormulario', { filter: `FormularioId eq ${f.Id}`, orderby: 'Ordem asc', top: 100 })
    ])
    secoes.value = resSecoes.value
    campos.value = resCampos.value
    // Inicializar seções colapsáveis conforme configuração
    secoesColapsadas.value = new Set(resSecoes.value.filter(s => s.IniciaColapsada).map(s => s.Id))
  } finally { carregandoEditor.value = false }
}

function voltarLista() { formularioSelecionado.value = null; secoes.value = []; campos.value = [] }

function camposDaSecao(secaoId: number | null) {
  return campos.value.filter(c => c.SecaoFormularioId === secaoId).sort((a, b) => a.Ordem - b.Ordem)
}

function nomeCampo(c: CampoFormulario): string {
  return c.Rotulo || c.NomeCampoFixo || `Campo #${c.CampoId}` || '(sem nome)'
}

// CRUD Formulário
function abrirNovoFormulario() { novoForm.value = { Nome: '', EntidadeAlvo: 'Cliente' }; modalNovoAberto.value = true }
async function salvarFormulario() {
  if (!novoForm.value.Nome.trim()) return
  await odataPost('Formularios', { Nome: novoForm.value.Nome, EntidadeAlvo: novoForm.value.EntidadeAlvo })
  modalNovoAberto.value = false
  await carregarFormularios()
}
async function excluirFormulario(f: Formulario) {
  if (!confirm(`Excluir formulário "${f.Nome}"?`)) return
  await odataDelete('Formularios', f.Id)
  if (formularioSelecionado.value?.Id === f.Id) voltarLista()
  await carregarFormularios()
}

// CRUD Seção
function abrirNovaSecao() { secaoEditandoId.value = null; secaoForm.value = { Nome: '', Colunas: 2, Colapsavel: false, IniciaColapsada: false }; modalSecaoAberto.value = true }
function abrirEditarSecao(s: SecaoFormulario) { secaoEditandoId.value = s.Id; secaoForm.value = { Nome: s.Nome, Colunas: s.Colunas ?? 2, Colapsavel: s.Colapsavel, IniciaColapsada: s.IniciaColapsada }; modalSecaoAberto.value = true }
async function salvarSecao() {
  if (!secaoForm.value.Nome.trim() || !formularioSelecionado.value) return
  const payload = { ...secaoForm.value, FormularioId: formularioSelecionado.value.Id, Ordem: secoes.value.length }
  if (secaoEditandoId.value) await odataPatch('SecoesFormulario', secaoEditandoId.value, payload)
  else await odataPost('SecoesFormulario', payload)
  modalSecaoAberto.value = false
  await selecionarFormulario(formularioSelecionado.value)
}
async function excluirSecao(s: SecaoFormulario) {
  if (!confirm(`Excluir seção "${s.Nome}"?`)) return
  await odataDelete('SecoesFormulario', s.Id)
  if (formularioSelecionado.value) await selecionarFormulario(formularioSelecionado.value)
}

// CRUD Campo
function abrirNovoCampo(secaoId: number | null) { campoEditandoId.value = null; campoForm.value = { NomeCampoFixo: '', Rotulo: '', Placeholder: '', Dica: '', Coluna: null, LarguraColunas: null, Obrigatorio: false, Visivel: true, SomenteLeitura: false, SecaoFormularioId: secaoId }; modalCampoAberto.value = true }
function abrirEditarCampo(c: CampoFormulario) { campoEditandoId.value = c.Id; campoForm.value = { NomeCampoFixo: c.NomeCampoFixo ?? '', Rotulo: c.Rotulo ?? '', Placeholder: c.Placeholder ?? '', Dica: c.Dica ?? '', Coluna: c.Coluna, LarguraColunas: c.LarguraColunas, Obrigatorio: c.Obrigatorio, Visivel: c.Visivel, SomenteLeitura: c.SomenteLeitura, SecaoFormularioId: c.SecaoFormularioId }; modalCampoAberto.value = true }
async function salvarCampo() {
  if (!formularioSelecionado.value) return
  const payload: any = { ...campoForm.value, FormularioId: formularioSelecionado.value.Id, Ordem: campos.value.length }
  if (!payload.NomeCampoFixo) delete payload.NomeCampoFixo
  if (!payload.Rotulo) delete payload.Rotulo
  if (!payload.Placeholder) delete payload.Placeholder
  if (!payload.Dica) delete payload.Dica
  if (campoEditandoId.value) await odataPatch('CamposFormulario', campoEditandoId.value, payload)
  else await odataPost('CamposFormulario', payload)
  modalCampoAberto.value = false
  await selecionarFormulario(formularioSelecionado.value)
}
async function excluirCampo(c: CampoFormulario) {
  if (!confirm('Excluir este campo do formulário?')) return
  await odataDelete('CamposFormulario', c.Id)
  if (formularioSelecionado.value) await selecionarFormulario(formularioSelecionado.value)
}

onMounted(carregarFormularios)
</script>

<template>
  <div class="fa-root">
    <div class="fa-layout">
      <!-- Sidebar com formulários agrupados por entidade -->
      <aside class="fa-sidebar">
        <div class="fa-sidebar-head">
          <span class="fa-sidebar-title">Formulários</span>
          <button class="fa-btn-add" title="Novo formulário" @click="abrirNovoFormulario"><i class="mdi mdi-plus"></i></button>
        </div>
        <div v-if="carregando" class="fa-msg">Carregando...</div>
        <div v-else class="fa-grupos">
          <div v-for="ent in entidades" :key="ent" class="fa-grupo">
            <button class="fa-grupo-head" @click="toggleGrupo(ent)" :class="{ aberto: gruposAbertos[ent] }">
              <span class="fa-grupo-label">{{ labelEntidade(ent) }}</span>
              <i class="mdi" :class="gruposAbertos[ent] ? 'mdi-chevron-down' : 'mdi-chevron-right'"></i>
            </button>
            <div v-if="gruposAbertos[ent]" class="fa-grupo-itens">
              <button v-for="f in formulariosPorEntidade[ent]" :key="f.Id" class="fa-item" :class="{ ativo: formularioSelecionado?.Id === f.Id }" @click="selecionarFormulario(f)">{{ f.Nome }}</button>
              <span v-if="!formulariosPorEntidade[ent]?.length" class="fa-vazio">Nenhum formulário</span>
            </div>
          </div>
        </div>
      </aside>

      <!-- Editor de formulário -->
      <div class="fa-editor">
        <div v-if="!formularioSelecionado" class="fa-placeholder"><i class="mdi mdi-form-select"></i><p>Selecione um formulário para editar</p></div>
        <div v-else class="fa-editor-content">
          <div class="fa-editor-head">
            <h3>{{ formularioSelecionado.Nome }}</h3>
            <div class="fa-editor-actions">
              <button class="fa-btn-sec" @click="abrirNovaSecao"><i class="mdi mdi-card-plus-outline"></i> Nova seção</button>
              <button class="fa-btn-sec" @click="abrirNovoCampo(null)"><i class="mdi mdi-form-textbox"></i> Adicionar campo</button>
              <button class="fa-btn-del" @click="excluirFormulario(formularioSelecionado)"><i class="mdi mdi-delete-outline"></i></button>
            </div>
          </div>
          <div v-if="carregandoEditor" class="fa-msg">Carregando...</div>
          <div v-else class="fa-secoes">
            <!-- Campos sem seção -->
            <div v-if="camposDaSecao(null).length" class="fa-secao">
              <div class="fa-secao-head"><span class="fa-secao-nome">(Sem seção)</span></div>
              <div class="fa-campos-grid">
                <div v-for="c in camposDaSecao(null)" :key="c.Id" class="fa-campo" @click="abrirEditarCampo(c)">
                  <span class="fa-campo-label">{{ nomeCampo(c) }}</span>
                  <input class="fa-campo-input" disabled :placeholder="c.Placeholder || nomeCampo(c)" />
                </div>
              </div>
            </div>
            <!-- Seções -->
            <div v-for="s in secoes" :key="s.Id" class="fa-secao">
              <div class="fa-secao-head" :class="{ colapsavel: s.Colapsavel }" @click="s.Colapsavel && toggleSecaoColapsada(s.Id)">
                <div class="fa-secao-nome-wrap">
                  <i v-if="s.Colapsavel" class="mdi" :class="secoesColapsadas.has(s.Id) ? 'mdi-chevron-right' : 'mdi-chevron-down'"></i>
                  <span class="fa-secao-nome">{{ s.Nome }}</span>
                </div>
                <div class="fa-secao-actions" @click.stop>
                  <button title="Adicionar campo" @click="abrirNovoCampo(s.Id)"><i class="mdi mdi-plus"></i></button>
                  <button title="Editar seção" @click="abrirEditarSecao(s)"><i class="mdi mdi-pencil-outline"></i></button>
                  <button title="Excluir seção" @click="excluirSecao(s)"><i class="mdi mdi-delete-outline"></i></button>
                </div>
              </div>
              <template v-if="!secoesColapsadas.has(s.Id)">
              <div class="fa-campos-grid" :style="{ 'grid-template-columns': `repeat(${s.Colunas || 2}, 1fr)` }">
                <div v-for="c in camposDaSecao(s.Id)" :key="c.Id" class="fa-campo" @click="abrirEditarCampo(c)">
                  <span class="fa-campo-label">{{ nomeCampo(c) }}</span>
                  <input class="fa-campo-input" disabled :placeholder="c.Placeholder || nomeCampo(c)" />
                </div>
              </div>
              <div v-if="!camposDaSecao(s.Id).length" class="fa-secao-vazia">Seção vazia — clique "+" para adicionar campos</div>
              </template>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- Modal novo formulário -->
    <div v-if="modalNovoAberto" class="fa-overlay" @click.self="modalNovoAberto = false">
      <div class="fa-modal">
        <div class="fa-modal-head"><h3>Novo Formulário</h3><button @click="modalNovoAberto = false"><i class="mdi mdi-close"></i></button></div>
        <div class="fa-modal-body">
          <div class="fa-field"><label>Nome</label><input v-model="novoForm.Nome" placeholder="Ex: Empresas, Empresas (mini), Pessoas..." /></div>
          <div class="fa-field"><label>Entidade</label><select v-model="novoForm.EntidadeAlvo"><option value="Cliente">Cliente</option><option value="PessoaContato">Pessoa de Contato</option><option value="Produto">Produto</option><option value="Empresa">Empresa</option><option value="Usuario">Usuário</option><option value="Equipe">Equipe</option><option value="Atividade">Registro de Interação</option></select></div>
        </div>
        <div class="fa-modal-foot"><button class="fa-btn-save" :disabled="!novoForm.Nome.trim()" @click="salvarFormulario">Criar</button></div>
      </div>
    </div>

    <!-- Modal seção -->
    <div v-if="modalSecaoAberto" class="fa-overlay" @click.self="modalSecaoAberto = false">
      <div class="fa-modal">
        <div class="fa-modal-head"><h3>{{ secaoEditandoId ? 'Editar Seção' : 'Nova Seção' }}</h3><button @click="modalSecaoAberto = false"><i class="mdi mdi-close"></i></button></div>
        <div class="fa-modal-body">
          <div class="fa-field"><label>Nome da seção</label><input v-model="secaoForm.Nome" placeholder="Ex: Dados Básicos, Endereço..." /></div>
          <div class="fa-field"><label>Colunas</label><select v-model.number="secaoForm.Colunas"><option :value="1">1 coluna</option><option :value="2">2 colunas</option><option :value="3">3 colunas</option></select></div>
          <div class="fa-toggle"><span>Colapsável</span><label class="fa-sw"><input type="checkbox" v-model="secaoForm.Colapsavel" /><span></span></label></div>
          <div v-if="secaoForm.Colapsavel" class="fa-toggle"><span>Inicia colapsada</span><label class="fa-sw"><input type="checkbox" v-model="secaoForm.IniciaColapsada" /><span></span></label></div>
        </div>
        <div class="fa-modal-foot"><button class="fa-btn-save" :disabled="!secaoForm.Nome.trim()" @click="salvarSecao">Salvar</button></div>
      </div>
    </div>

    <!-- Modal campo -->
    <div v-if="modalCampoAberto" class="fa-overlay" @click.self="modalCampoAberto = false">
      <div class="fa-modal">
        <div class="fa-modal-head"><h3>{{ campoEditandoId ? 'Editar Campo' : 'Adicionar Campo' }}</h3><button @click="modalCampoAberto = false"><i class="mdi mdi-close"></i></button></div>
        <div class="fa-modal-body">
          <div class="fa-field"><label>Nome do campo fixo</label><input v-model="campoForm.NomeCampoFixo" placeholder="Ex: Nome, Email, Valor..." /></div>
          <div class="fa-field"><label>Rótulo (override)</label><input v-model="campoForm.Rotulo" placeholder="Rótulo personalizado (opcional)" /></div>
          <div class="fa-field"><label>Placeholder</label><input v-model="campoForm.Placeholder" placeholder="Texto de placeholder" /></div>
          <div class="fa-field"><label>Dica</label><input v-model="campoForm.Dica" placeholder="Texto de ajuda" /></div>
          <div class="fa-row2">
            <div class="fa-field"><label>Seção</label><select v-model.number="campoForm.SecaoFormularioId"><option :value="null">(Sem seção)</option><option v-for="s in secoes" :key="s.Id" :value="s.Id">{{ s.Nome }}</option></select></div>
            <div class="fa-field"><label>Largura (colunas)</label><input v-model.number="campoForm.LarguraColunas" type="number" min="1" max="3" placeholder="Auto" /></div>
          </div>
          <div class="fa-toggle"><span>Obrigatório</span><label class="fa-sw"><input type="checkbox" v-model="campoForm.Obrigatorio" /><span></span></label></div>
          <div class="fa-toggle"><span>Visível</span><label class="fa-sw"><input type="checkbox" v-model="campoForm.Visivel" /><span></span></label></div>
          <div class="fa-toggle"><span>Somente leitura</span><label class="fa-sw"><input type="checkbox" v-model="campoForm.SomenteLeitura" /><span></span></label></div>
        </div>
        <div class="fa-modal-foot">
          <button v-if="campoEditandoId" class="fa-btn-del" @click="excluirCampo({ Id: campoEditandoId } as CampoFormulario)"><i class="mdi mdi-delete-outline"></i></button>
          <div style="flex:1"></div>
          <button class="fa-btn-save" @click="salvarCampo">Salvar</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.fa-root{display:flex;flex-direction:column;height:100%}
.fa-layout{display:flex;flex:1;overflow:hidden}
.fa-sidebar{width:240px;border-right:1px solid var(--border);display:flex;flex-direction:column;overflow:hidden;background:var(--bg-secondary)}
.fa-sidebar-head{display:flex;align-items:center;justify-content:space-between;padding:14px 16px;border-bottom:1px solid var(--border)}
.fa-sidebar-title{font-size:14px;font-weight:600;color:var(--text-primary)}
.fa-btn-add{width:28px;height:28px;border-radius:6px;border:1px solid var(--border);background:var(--bg-surface);color:var(--accent);cursor:pointer;display:flex;align-items:center;justify-content:center;font-size:16px}
.fa-btn-add:hover{background:var(--accent);color:#000}
.fa-grupos{flex:1;overflow-y:auto;padding:8px 0}
.fa-grupo{border-bottom:1px solid var(--border)}
.fa-grupo-head{display:flex;align-items:center;justify-content:space-between;width:100%;padding:10px 16px;border:none;background:transparent;cursor:pointer;font-size:13px;font-weight:600;color:var(--text-primary)}
.fa-grupo-head:hover{background:var(--bg-elevated)}
.fa-grupo-head .mdi{font-size:16px;color:var(--text-muted)}
.fa-grupo-itens{padding:0 8px 8px}
.fa-item{display:block;width:100%;text-align:left;padding:7px 16px;border:none;background:transparent;color:var(--text-secondary);font-size:13px;cursor:pointer;border-radius:6px;transition:all .1s}
.fa-item:hover{background:var(--bg-elevated);color:var(--text-primary)}
.fa-item.ativo{background:var(--accent);color:#000;font-weight:600}
.fa-vazio{display:block;padding:4px 16px;font-size:11px;color:var(--text-muted);font-style:italic}
.fa-editor{flex:1;display:flex;flex-direction:column;overflow:hidden}
.fa-placeholder{flex:1;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:12px;color:var(--text-muted)}
.fa-placeholder .mdi{font-size:48px;opacity:.3}
.fa-placeholder p{font-size:14px}
.fa-editor-content{flex:1;display:flex;flex-direction:column;overflow:hidden}
.fa-editor-head{display:flex;align-items:center;justify-content:space-between;padding:14px 20px;border-bottom:1px solid var(--border);flex-shrink:0}
.fa-editor-head h3{font-size:16px;font-weight:600;color:var(--text-primary);margin:0}
.fa-editor-actions{display:flex;gap:8px}
.fa-btn-sec{display:flex;align-items:center;gap:5px;padding:6px 12px;border:1px solid var(--border);border-radius:6px;background:var(--bg-surface);color:var(--text-secondary);font-size:12px;cursor:pointer}
.fa-btn-sec:hover{background:var(--bg-elevated);color:var(--accent);border-color:var(--accent)}
.fa-btn-del{width:32px;height:32px;border-radius:6px;border:1px solid rgba(239,68,68,.3);background:transparent;color:#ef4444;cursor:pointer;display:flex;align-items:center;justify-content:center;font-size:16px}
.fa-btn-del:hover{background:rgba(239,68,68,.1)}
.fa-secoes{flex:1;overflow-y:auto;padding:20px}
.fa-secao{margin-bottom:24px;border:1px solid var(--border);border-radius:10px;overflow:hidden}
.fa-secao-head{display:flex;align-items:center;justify-content:space-between;padding:12px 16px;background:var(--bg-secondary);border-bottom:1px solid var(--border)}
.fa-secao-nome{font-size:13px;font-weight:600;color:var(--text-primary)}
.fa-secao-nome-wrap{display:flex;align-items:center;gap:6px}
.fa-secao-head.colapsavel{cursor:pointer}
.fa-secao-head.colapsavel:hover{background:var(--bg-elevated)}
.fa-secao-actions{display:flex;gap:4px}
.fa-secao-actions button{width:26px;height:26px;border:none;background:transparent;color:var(--text-muted);cursor:pointer;border-radius:4px;display:flex;align-items:center;justify-content:center;font-size:14px}
.fa-secao-actions button:hover{background:var(--bg-elevated);color:var(--text-primary)}
.fa-campos-grid{display:grid;grid-template-columns:repeat(2,1fr);gap:12px;padding:16px}
.fa-campo{display:flex;flex-direction:column;gap:4px;cursor:pointer;padding:8px 12px;border-radius:6px;border:1px solid transparent;transition:all .1s}
.fa-campo:hover{border-color:var(--accent);background:var(--bg-elevated)}
.fa-campo-label{font-size:12px;font-weight:500;color:var(--text-secondary)}
.fa-campo-input{background:var(--bg-surface);border:1px solid var(--border);border-radius:6px;padding:8px 10px;color:var(--text-muted);font-size:13px;pointer-events:none}
.fa-secao-vazia{padding:20px;text-align:center;color:var(--text-muted);font-size:12px;font-style:italic}
.fa-msg{padding:40px;text-align:center;color:var(--text-muted);font-size:14px}
.fa-overlay{position:fixed;inset:0;background:rgba(0,0,0,.7);z-index:1000;display:flex;align-items:center;justify-content:center}
.fa-modal{background:var(--bg-primary);border:1px solid var(--border);border-radius:12px;width:480px;max-width:90vw;box-shadow:0 24px 64px rgba(0,0,0,.5)}
.fa-modal-head{display:flex;align-items:center;justify-content:space-between;padding:16px 20px;border-bottom:1px solid var(--border)}
.fa-modal-head h3{font-size:15px;font-weight:600;color:var(--text-primary);margin:0}
.fa-modal-head button{background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:18px}
.fa-modal-body{padding:20px;display:flex;flex-direction:column;gap:14px}
.fa-modal-foot{display:flex;align-items:center;gap:10px;padding:14px 20px;border-top:1px solid var(--border)}
.fa-field{display:flex;flex-direction:column;gap:5px}
.fa-field label{font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.3px}
.fa-field input,.fa-field select{background:var(--bg-surface);border:1px solid var(--border);border-radius:8px;padding:10px 12px;color:var(--text-primary);font-size:14px;outline:none;width:100%;box-sizing:border-box}
.fa-field input:focus,.fa-field select:focus{border-color:var(--accent)}
.fa-row2{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.fa-toggle{display:flex;align-items:center;justify-content:space-between;gap:12px}
.fa-toggle span{font-size:13px;color:var(--text-primary)}
.fa-sw{position:relative;display:inline-block;width:42px;height:24px;flex-shrink:0;cursor:pointer}
.fa-sw input{opacity:0;width:0;height:0;position:absolute}
.fa-sw span{position:absolute;inset:0;background:var(--bg-elevated);border:1px solid var(--border);border-radius:24px;transition:.2s}
.fa-sw span::before{content:'';position:absolute;height:18px;width:18px;left:2px;bottom:2px;background:var(--text-muted);border-radius:50%;transition:.2s}
.fa-sw input:checked+span{background:var(--accent);border-color:var(--accent)}
.fa-sw input:checked+span::before{transform:translateX(18px);background:#000}
.fa-btn-save{padding:10px 24px;border:none;border-radius:8px;background:var(--accent);color:#000;font-size:14px;font-weight:600;cursor:pointer;margin-left:auto}
.fa-btn-save:hover{background:var(--accent-hover)}
.fa-btn-save:disabled{opacity:.4;cursor:not-allowed}
</style>
