<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { odataGet, odataPost, odataPatch, odataDelete, type ODataParams } from '@/services/api'
import TabsFiltro from '@/components/shared/TabsFiltro.vue'
import EditorFormula from '@/components/shared/EditorFormula.vue'

interface Campo {
  Id: number; Nome: string; Chave: string; TipoCampo: string; EntidadeAlvo: string
  Obrigatorio: boolean; Visivel: boolean; SomenteLeitura: boolean; Ordem: number; Ativo: boolean
  Descricao: string | null; TamanhoMinimo: number | null; TamanhoMaximo: number | null
  ValorMinimo: number | null; ValorMaximo: number | null
}

const camposEstaticos = [
  { nome: 'Nome', tipo: 'string' },
  { nome: 'TipoCampo', tipo: 'enum:Texto|TextoLongo|Numero|Decimal|Moeda|Data|DataHora|Booleano|Lista|MultiLista|Email|Telefone|Url|Cpf|Cnpj|Cep|Formula|Referencia' },
  { nome: 'EntidadeAlvo', tipo: 'enum:Cliente|Negocio|Funil|EtapaFunil|Atividade|Produto|Empresa|Usuario|Equipe|Departamento|Cargo|Proposta' },
  { nome: 'Obrigatorio', tipo: 'boolean' },
  { nome: 'Visivel', tipo: 'boolean' },
  { nome: 'Ativo', tipo: 'boolean' },
]

const tiposCampo = [
  { valor: 'Texto', label: 'Texto simples', icone: 'mdi-format-text' },
  { valor: 'TextoLongo', label: 'Texto multilinha', icone: 'mdi-text-long' },
  { valor: 'Numero', label: 'Número inteiro', icone: 'mdi-numeric' },
  { valor: 'Decimal', label: 'Número com decimais', icone: 'mdi-decimal' },
  { valor: 'Moeda', label: 'Moeda', icone: 'mdi-currency-usd' },
  { valor: 'Lista', label: 'Opções pré-cadastradas', icone: 'mdi-format-list-bulleted' },
  { valor: 'MultiLista', label: 'Múltipla escolha', icone: 'mdi-format-list-checks' },
  { valor: 'Data', label: 'Data', icone: 'mdi-calendar' },
  { valor: 'DataHora', label: 'Horário', icone: 'mdi-clock-outline' },
  { valor: 'Booleano', label: 'Checkbox', icone: 'mdi-checkbox-marked-outline' },
  { valor: 'Cpf', label: 'CPF', icone: 'mdi-card-account-details-outline' },
  { valor: 'Cnpj', label: 'CNPJ', icone: 'mdi-domain' },
  { valor: 'Email', label: 'Email', icone: 'mdi-email-outline' },
  { valor: 'Telefone', label: 'Telefone', icone: 'mdi-phone-outline' },
  { valor: 'Url', label: 'URL / Percentagem', icone: 'mdi-link-variant' },
  { valor: 'Cep', label: 'Endereço', icone: 'mdi-map-marker-outline' },
  { valor: 'Formula', label: 'Fórmula', icone: 'mdi-function-variant' },
  { valor: 'Referencia', label: 'Imagem', icone: 'mdi-image-outline' },
]

const entidades = ['Cliente', 'Negocio', 'Funil', 'EtapaFunil', 'Atividade', 'Produto', 'Empresa', 'Usuario', 'Equipe', 'Departamento', 'Cargo', 'Proposta']

const itens = ref<Campo[]>([])
const total = ref(0)
const carregando = ref(false)
const pagina = ref(1)
const porPagina = ref(20)
const filtroAba = ref<string | null>(null)
const busca = ref('')
const entidadeFiltro = ref<string | null>(null)

const modalAberto = ref(false)
const modoEdicao = ref(false)
const campoEditando = ref<Campo | null>(null)
const form = ref({ Nome: '', Descricao: '', TipoCampo: 'Texto', EntidadeAlvo: 'Cliente', Obrigatorio: false, Visivel: true, SomenteLeitura: false, Ordem: 0, CampoChave: false, DuasColunas: false, TamanhoMinimo: null as number | null, TamanhoMaximo: null as number | null, ValorMinimo: null as number | null, ValorMaximo: null as number | null, Mascara: '', Formula: '', FormulaVisibilidade: '', FormulaObrigatoriedade: '', FormulaCalculo: '', FormulaValorPadrao: '', FormulaSomenteLeitura: '' })
const secoes = ref({ basicas: true, avancadas: false, obrigatoriedade: false, edicao: false, visualizacao: false, calculo: false })

const formulaEditando = ref<string | null>(null)

function abrirEditorFormula(campo: string) { formulaEditando.value = campo }
function fecharEditorFormula() { formulaEditando.value = null }
function salvarFormula(campo: string, valor: string) { (form.value as any)[campo] = valor; formulaEditando.value = null }

const tituloContador = computed(() => `${form.value.Nome.length}/80`)
const descContador = computed(() => `${form.value.Descricao.length}/250`)

async function carregar() {
  carregando.value = true
  try {
    const params: ODataParams = { top: porPagina.value, skip: (pagina.value - 1) * porPagina.value, orderby: 'EntidadeAlvo asc,Ordem asc,Nome asc', count: true, select: 'Id,Nome,Chave,TipoCampo,EntidadeAlvo,Obrigatorio,Visivel,SomenteLeitura,Ordem,Ativo,Descricao' }
    const filtros: string[] = []
    if (filtroAba.value) filtros.push(filtroAba.value)
    if (busca.value) filtros.push(`contains(Nome,'${busca.value}')`)
    if (entidadeFiltro.value) filtros.push(`EntidadeAlvo eq '${entidadeFiltro.value}'`)
    if (filtros.length) params.filter = filtros.join(' and ')
    const res = await odataGet<Campo>('Campos', params)
    itens.value = res.value; total.value = res['@odata.count'] ?? 0
  } finally { carregando.value = false }
}

function onFiltro(f: string | null, _o: string | null, pp: number) { filtroAba.value = f; porPagina.value = pp; pagina.value = 1; carregar() }
function pg(p: number) { pagina.value = p; carregar() }
const tp = () => Math.ceil(total.value / porPagina.value)
function pesquisar() { pagina.value = 1; carregar() }
function filtrarEntidade(e: string | null) { entidadeFiltro.value = e; pagina.value = 1; carregar() }

function abrirNovo() {
  modoEdicao.value = false; campoEditando.value = null
  form.value = { Nome: '', Descricao: '', TipoCampo: 'Texto', EntidadeAlvo: 'Cliente', Obrigatorio: false, Visivel: true, SomenteLeitura: false, Ordem: 0, CampoChave: false, DuasColunas: false, TamanhoMinimo: null, TamanhoMaximo: null, ValorMinimo: null, ValorMaximo: null, Mascara: '', Formula: '', FormulaVisibilidade: '', FormulaObrigatoriedade: '', FormulaCalculo: '', FormulaValorPadrao: '', FormulaSomenteLeitura: '' }
  secoes.value = { basicas: true, avancadas: false, obrigatoriedade: false, edicao: false, visualizacao: false, calculo: false }
  modalAberto.value = true
}

function abrirEditar(campo: Campo) {
  modoEdicao.value = true; campoEditando.value = campo
  form.value = { Nome: campo.Nome, Descricao: campo.Descricao ?? '', TipoCampo: campo.TipoCampo, EntidadeAlvo: campo.EntidadeAlvo, Obrigatorio: campo.Obrigatorio, Visivel: campo.Visivel, SomenteLeitura: campo.SomenteLeitura, Ordem: campo.Ordem, CampoChave: false, DuasColunas: false, TamanhoMinimo: campo.TamanhoMinimo, TamanhoMaximo: campo.TamanhoMaximo, ValorMinimo: campo.ValorMinimo, ValorMaximo: campo.ValorMaximo, Mascara: (campo as any).Mascara ?? '', Formula: (campo as any).Formula ?? '', FormulaVisibilidade: (campo as any).FormulaVisibilidade ?? '', FormulaObrigatoriedade: (campo as any).FormulaObrigatoriedade ?? '', FormulaCalculo: (campo as any).FormulaCalculo ?? '', FormulaValorPadrao: (campo as any).FormulaValorPadrao ?? '', FormulaSomenteLeitura: (campo as any).FormulaSomenteLeitura ?? '' }
  secoes.value = { basicas: true, avancadas: false, obrigatoriedade: false, edicao: false, visualizacao: false, calculo: false }
  modalAberto.value = true
}

async function salvar() {
  if (!form.value.Nome.trim()) return
  const payload: any = { Nome: form.value.Nome, Descricao: form.value.Descricao || null, TipoCampo: form.value.TipoCampo, EntidadeAlvo: form.value.EntidadeAlvo, Obrigatorio: form.value.Obrigatorio, Visivel: form.value.Visivel, SomenteLeitura: form.value.SomenteLeitura, Ordem: form.value.Ordem }
  if (form.value.TamanhoMinimo != null) payload.TamanhoMinimo = form.value.TamanhoMinimo
  if (form.value.TamanhoMaximo != null) payload.TamanhoMaximo = form.value.TamanhoMaximo
  if (form.value.ValorMinimo != null) payload.ValorMinimo = form.value.ValorMinimo
  if (form.value.ValorMaximo != null) payload.ValorMaximo = form.value.ValorMaximo
  if (form.value.Mascara) payload.Mascara = form.value.Mascara
  if (form.value.Formula) payload.Formula = form.value.Formula
  if (form.value.FormulaVisibilidade) payload.FormulaVisibilidade = form.value.FormulaVisibilidade
  if (form.value.FormulaObrigatoriedade) payload.FormulaObrigatoriedade = form.value.FormulaObrigatoriedade
  if (form.value.FormulaCalculo) payload.FormulaCalculo = form.value.FormulaCalculo
  if (form.value.FormulaValorPadrao) payload.FormulaValorPadrao = form.value.FormulaValorPadrao
  if (form.value.FormulaSomenteLeitura) payload.FormulaSomenteLeitura = form.value.FormulaSomenteLeitura
  if (modoEdicao.value && campoEditando.value) await odataPatch('Campos', campoEditando.value.Id, payload)
  else await odataPost('Campos', payload)
  modalAberto.value = false; await carregar()
}

async function excluirCampo() {
  if (!campoEditando.value || !confirm('Deseja realmente excluir este campo?')) return
  await odataDelete('Campos', campoEditando.value.Id)
  modalAberto.value = false; await carregar()
}

onMounted(carregar)
</script>

<template>
  <div class="cf-root">
    <TabsFiltro entidade="Campo" :campos-estaticos="camposEstaticos" @filtro-alterado="onFiltro" />
    <div class="cf-toolbar">
      <div class="cf-search"><i class="mdi mdi-magnify"></i><input v-model="busca" placeholder="Buscar campo..." @keyup.enter="pesquisar" /></div>
      <select class="cf-select" @change="filtrarEntidade(($event.target as HTMLSelectElement).value || null)">
        <option value="">Todas entidades</option>
        <option v-for="e in entidades" :key="e" :value="e">{{ e }}</option>
      </select>
      <button class="cf-btn-novo" @click="abrirNovo"><i class="mdi mdi-plus"></i> Novo campo</button>
      <span class="cf-total">{{ total }} campos</span>
    </div>
    <div class="cf-table"><table><thead><tr><th>Nome</th><th>Chave</th><th>Tipo</th><th>Entidade</th><th>Obrig.</th><th>Status</th></tr></thead>
      <tbody>
        <tr v-if="carregando"><td colspan="6" class="cf-msg">Carregando...</td></tr>
        <tr v-else-if="!itens.length"><td colspan="6" class="cf-msg">Nenhum campo personalizado</td></tr>
        <tr v-for="c in itens" :key="c.Id" class="cf-rowclick" @click="abrirEditar(c)">
          <td class="cf-nome">{{ c.Nome }}</td><td class="cf-mono">{{ c.Chave }}</td>
          <td><span class="cf-badge">{{ c.TipoCampo }}</span></td><td><span class="cf-badge-ent">{{ c.EntidadeAlvo }}</span></td>
          <td>{{ c.Obrigatorio ? 'Sim' : 'Não' }}</td><td><span class="cf-dot" :class="{ on: c.Ativo }"></span></td>
        </tr>
      </tbody></table>
    </div>
    <div v-if="tp() > 1" class="cf-pag"><button :disabled="pagina <= 1" @click="pg(pagina - 1)"><i class="mdi mdi-chevron-left"></i></button><span>{{ pagina }} / {{ tp() }}</span><button :disabled="pagina >= tp()" @click="pg(pagina + 1)"><i class="mdi mdi-chevron-right"></i></button></div>

    <div v-if="modalAberto" class="cf-overlay" @click.self="modalAberto = false">
      <div class="cf-modal">
        <div class="cf-modal-head"><h3>{{ modoEdicao ? 'Editar campo' : 'Novo campo' }}</h3><button @click="modalAberto = false"><i class="mdi mdi-close"></i></button></div>
        <div class="cf-modal-body">
          <div class="cf-tipos">
            <p class="cf-tipos-title">Tipo do campo</p>
            <button v-for="t in tiposCampo" :key="t.valor" class="cf-tipo" :class="{ active: form.TipoCampo === t.valor }" @click="form.TipoCampo = t.valor"><i class="mdi" :class="t.icone"></i><span>{{ t.label }}</span></button>
          </div>
          <div class="cf-config">
            <button class="cf-sec-head cf-sec-blue" @click="secoes.basicas = !secoes.basicas"><span>Configurações básicas</span><i class="mdi" :class="secoes.basicas ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.basicas" class="cf-sec-body">
              <div class="cf-field"><div class="cf-field-top"><label>Título do campo</label><span>{{ tituloContador }}</span></div><input v-model="form.Nome" maxlength="80" placeholder="Nome do campo" /></div>
              <div class="cf-field"><div class="cf-field-top"><label>Texto informativo do campo</label><span>{{ descContador }}</span></div><textarea v-model="form.Descricao" maxlength="250" rows="3" placeholder="Escreva um texto informativo para os usuários..."></textarea></div>
              <div class="cf-field"><label>Entidade alvo</label><select v-model="form.EntidadeAlvo"><option v-for="e in entidades" :key="e" :value="e">{{ e }}</option></select></div>
              <div class="cf-toggle"><div><span class="cf-tl">Campo chave</span><span class="cf-td">Campo chave</span></div><label class="cf-sw"><input type="checkbox" v-model="form.CampoChave" /><span></span></label></div>
              <div class="cf-toggle"><div><span class="cf-tl">Campo de duas colunas</span><span class="cf-td">Ative para que o campo ocupe duas colunas no formulário.</span></div><label class="cf-sw"><input type="checkbox" v-model="form.DuasColunas" /><span></span></label></div>
            </div>
            <button class="cf-sec-head cf-sec-cyan" @click="secoes.avancadas = !secoes.avancadas"><span>Configurações avançadas</span><i class="mdi" :class="secoes.avancadas ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.avancadas" class="cf-sec-body"><div class="cf-row2"><div class="cf-field"><label>Tam. mínimo</label><input v-model.number="form.TamanhoMinimo" type="number" min="0" /></div><div class="cf-field"><label>Tam. máximo</label><input v-model.number="form.TamanhoMaximo" type="number" min="0" /></div></div><div class="cf-row2"><div class="cf-field"><label>Valor mínimo</label><input v-model.number="form.ValorMinimo" type="number" /></div><div class="cf-field"><label>Valor máximo</label><input v-model.number="form.ValorMaximo" type="number" /></div></div><div class="cf-field"><label>Ordem</label><input v-model.number="form.Ordem" type="number" min="0" /></div><div class="cf-field"><label>Máscara de entrada</label><input v-model="form.Mascara" placeholder="Ex: ###.###.###-## ou (##) #####-####" /></div><div v-if="form.TipoCampo === 'Formula'" class="cf-field"><label>Fórmula JS do campo</label><button class="cf-btn-formula" @click="abrirEditorFormula('Formula')"><i class="mdi mdi-function-variant"></i> {{ form.Formula ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div></div>
            <button class="cf-sec-head cf-sec-orange" @click="secoes.obrigatoriedade = !secoes.obrigatoriedade"><span>Configurações de obrigatoriedade</span><i class="mdi" :class="secoes.obrigatoriedade ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.obrigatoriedade" class="cf-sec-body"><div class="cf-toggle"><div><span class="cf-tl">Obrigatório</span><span class="cf-td">O campo deve ser preenchido para salvar</span></div><label class="cf-sw"><input type="checkbox" v-model="form.Obrigatorio" /><span></span></label></div><div class="cf-field"><label>Fórmula JS de obrigatoriedade</label><button class="cf-btn-formula" @click="abrirEditorFormula('FormulaObrigatoriedade')"><i class="mdi mdi-function-variant"></i> {{ form.FormulaObrigatoriedade ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div></div>
            <button class="cf-sec-head cf-sec-orange" @click="secoes.edicao = !secoes.edicao"><span>Configurações de edição e bloqueio</span><i class="mdi" :class="secoes.edicao ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.edicao" class="cf-sec-body"><div class="cf-toggle"><div><span class="cf-tl">Somente leitura</span><span class="cf-td">O campo não pode ser editado</span></div><label class="cf-sw"><input type="checkbox" v-model="form.SomenteLeitura" /><span></span></label></div><div class="cf-field"><label>Fórmula JS de bloqueio</label><button class="cf-btn-formula" @click="abrirEditorFormula('FormulaSomenteLeitura')"><i class="mdi mdi-function-variant"></i> {{ form.FormulaSomenteLeitura ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div></div>
            <button class="cf-sec-head cf-sec-orange" @click="secoes.visualizacao = !secoes.visualizacao"><span>Configurações de visualização</span><i class="mdi" :class="secoes.visualizacao ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.visualizacao" class="cf-sec-body"><div class="cf-toggle"><div><span class="cf-tl">Visível</span><span class="cf-td">O campo aparece nos formulários</span></div><label class="cf-sw"><input type="checkbox" v-model="form.Visivel" /><span></span></label></div><div class="cf-field"><label>Fórmula JS de visibilidade</label><button class="cf-btn-formula" @click="abrirEditorFormula('FormulaVisibilidade')"><i class="mdi mdi-function-variant"></i> {{ form.FormulaVisibilidade ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div></div>
            <button class="cf-sec-head cf-sec-cyan" @click="secoes.calculo = !secoes.calculo"><span>Configurações de cálculo</span><i class="mdi" :class="secoes.calculo ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.calculo" class="cf-sec-body"><div class="cf-field"><label>Fórmula JS de cálculo</label><button class="cf-btn-formula" @click="abrirEditorFormula('FormulaCalculo')"><i class="mdi mdi-function-variant"></i> {{ form.FormulaCalculo ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div><div class="cf-field"><label>Fórmula JS de valor padrão</label><button class="cf-btn-formula" @click="abrirEditorFormula('FormulaValorPadrao')"><i class="mdi mdi-function-variant"></i> {{ form.FormulaValorPadrao ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div></div>
          </div>
        </div>
        <div class="cf-modal-foot">
          <button v-if="modoEdicao" class="cf-btn-del" @click="excluirCampo"><i class="mdi mdi-delete-outline"></i></button>
          <div style="flex:1"></div>
          <button class="cf-btn-save" :disabled="!form.Nome.trim()" @click="salvar">Salvar</button>
        </div>
      </div>
    </div>

    <EditorFormula
      v-if="formulaEditando"
      :model-value="(form as any)[formulaEditando] || ''"
      :entidade="form.EntidadeAlvo"
      :titulo="`Fórmula: ${formulaEditando}`"
      @salvar="(v: string) => salvarFormula(formulaEditando!, v)"
      @fechar="fecharEditorFormula"
    />
  </div>
</template>

<style scoped>
.cf-root{display:flex;flex-direction:column;height:100%}
.cf-toolbar{display:flex;align-items:center;gap:12px;margin-bottom:12px}
.cf-search{display:flex;align-items:center;gap:8px;background:var(--bg-surface);border:1px solid var(--border);border-radius:8px;padding:8px 12px;width:240px}
.cf-search .mdi{color:var(--text-muted);font-size:16px}
.cf-search input{border:none;background:transparent;color:var(--text-primary);font-size:13px;outline:none;width:100%}
.cf-search input::placeholder{color:var(--text-muted)}
.cf-select{background:var(--bg-surface);border:1px solid var(--border);border-radius:6px;padding:7px 10px;color:var(--text-primary);font-size:12px;outline:none}
.cf-btn-novo{display:flex;align-items:center;gap:4px;padding:6px 14px;border:none;border-radius:6px;background:var(--accent);color:#000;font-size:12px;font-weight:600;cursor:pointer;margin-left:auto}
.cf-btn-novo:hover{background:var(--accent-hover)}
.cf-total{color:var(--text-muted);font-size:12px}
.cf-table{flex:1;overflow:auto;border:1px solid var(--border);border-radius:10px}
.cf-table table{width:100%;border-collapse:collapse;font-size:13px}
.cf-table thead{position:sticky;top:0;z-index:1}
.cf-table th{background:var(--bg-secondary);color:var(--text-muted);font-weight:600;text-transform:uppercase;font-size:11px;padding:12px 14px;text-align:left;border-bottom:1px solid var(--border)}
.cf-table td{padding:10px 14px;border-bottom:1px solid var(--border);color:var(--text-secondary)}
.cf-table tr:hover td{background:var(--bg-elevated)}
.cf-msg{text-align:center;padding:40px;color:var(--text-muted)}
.cf-nome{color:var(--text-primary);font-weight:500}
.cf-mono{font-family:monospace;font-size:12px;color:var(--text-muted)}
.cf-badge{background:var(--bg-elevated);border:1px solid var(--border);padding:2px 8px;border-radius:4px;font-size:11px}
.cf-badge-ent{background:rgba(6,182,212,.08);border:1px solid rgba(6,182,212,.2);padding:2px 8px;border-radius:4px;font-size:11px;color:var(--accent)}
.cf-dot{display:inline-block;width:8px;height:8px;border-radius:50%;background:#ef4444}.cf-dot.on{background:#22c55e}
.cf-rowclick{cursor:pointer}
.cf-pag{display:flex;align-items:center;justify-content:center;gap:12px;padding:12px 0}
.cf-pag button{width:32px;height:32px;border-radius:6px;border:1px solid var(--border);background:var(--bg-surface);color:var(--text-secondary);cursor:pointer;display:flex;align-items:center;justify-content:center}
.cf-pag button:disabled{opacity:.3}
.cf-pag span{color:var(--text-muted);font-size:13px}
.cf-overlay{position:fixed;inset:0;background:rgba(0,0,0,.7);z-index:1000;display:flex;align-items:center;justify-content:center}
.cf-modal{background:var(--bg-primary);border:1px solid var(--border);border-radius:14px;width:860px;max-width:95vw;max-height:88vh;display:flex;flex-direction:column;box-shadow:0 24px 64px rgba(0,0,0,.5)}
.cf-modal-head{display:flex;align-items:center;justify-content:space-between;padding:16px 24px;border-bottom:1px solid var(--border)}
.cf-modal-head h3{font-size:16px;font-weight:600;color:var(--text-primary)}
.cf-modal-head button{background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:20px}
.cf-modal-head button:hover{color:var(--text-primary)}
.cf-modal-body{display:flex;flex:1;overflow:hidden}
.cf-tipos{width:240px;border-right:1px solid var(--border);overflow-y:auto;padding:12px 8px;display:flex;flex-direction:column;gap:2px;flex-shrink:0}
.cf-tipos-title{font-size:12px;font-weight:600;color:var(--text-muted);padding:4px 12px 8px;text-transform:uppercase;letter-spacing:.5px}
.cf-tipo{display:flex;align-items:center;gap:10px;padding:10px 12px;border:none;background:transparent;color:var(--text-secondary);font-size:13px;cursor:pointer;border-radius:8px;text-align:left;transition:all .12s}
.cf-tipo:hover{background:var(--bg-elevated);color:var(--text-primary)}
.cf-tipo.active{background:var(--accent);color:#000;font-weight:600}
.cf-tipo .mdi{font-size:18px;width:22px;text-align:center}
.cf-config{flex:1;overflow-y:auto;display:flex;flex-direction:column}
.cf-sec-head{display:flex;align-items:center;justify-content:space-between;width:100%;padding:12px 20px;border:none;background:transparent;cursor:pointer;font-size:13px;font-weight:600;border-bottom:1px solid var(--border)}
.cf-sec-head:hover{background:var(--bg-elevated)}
.cf-sec-blue{color:#06b6d4}
.cf-sec-cyan{color:#06b6d4}
.cf-sec-orange{color:#f59e0b}
.cf-sec-head .mdi{font-size:18px}
.cf-sec-body{padding:16px 20px;border-bottom:1px solid var(--border);display:flex;flex-direction:column;gap:14px}
.cf-field{display:flex;flex-direction:column;gap:5px}
.cf-field label{font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.3px}
.cf-field-top{display:flex;align-items:center;justify-content:space-between}
.cf-field-top span{font-size:11px;color:var(--text-muted)}
.cf-field input,.cf-field textarea,.cf-field select{background:var(--bg-surface);border:1px solid var(--border);border-radius:8px;padding:10px 12px;color:var(--text-primary);font-size:14px;outline:none;resize:none;font-family:inherit;width:100%;box-sizing:border-box}
.cf-field input:focus,.cf-field textarea:focus,.cf-field select:focus{border-color:var(--accent)}
.cf-row2{display:grid;grid-template-columns:1fr 1fr;gap:12px}
.cf-toggle{display:flex;align-items:center;justify-content:space-between;gap:12px}
.cf-tl{font-size:13px;font-weight:500;color:var(--text-primary);display:block}
.cf-td{font-size:11px;color:var(--text-muted);display:block}
.cf-sw{position:relative;display:inline-block;width:42px;height:24px;flex-shrink:0;cursor:pointer}
.cf-sw input{opacity:0;width:0;height:0;position:absolute}
.cf-sw span{position:absolute;inset:0;background:var(--bg-elevated);border:1px solid var(--border);border-radius:24px;transition:.2s}
.cf-sw span::before{content:'';position:absolute;height:18px;width:18px;left:2px;bottom:2px;background:var(--text-muted);border-radius:50%;transition:.2s}
.cf-sw input:checked+span{background:var(--accent);border-color:var(--accent)}
.cf-sw input:checked+span::before{transform:translateX(18px);background:#000}
.cf-modal-foot{display:flex;align-items:center;gap:10px;padding:14px 24px;border-top:1px solid var(--border)}
.cf-btn-del{width:36px;height:36px;border-radius:8px;border:1px solid rgba(239,68,68,.3);background:transparent;color:#ef4444;cursor:pointer;display:flex;align-items:center;justify-content:center;font-size:18px}
.cf-btn-del:hover{background:rgba(239,68,68,.1)}
.cf-btn-save{padding:10px 28px;border:none;border-radius:8px;background:var(--accent);color:#000;font-size:14px;font-weight:600;cursor:pointer}
.cf-btn-save:hover{background:var(--accent-hover)}
.cf-btn-save:disabled{opacity:.4;cursor:not-allowed}
.cf-btn-formula{display:flex;align-items:center;gap:8px;padding:10px 16px;border:1px solid var(--border);border-radius:8px;background:var(--bg-surface);color:var(--text-secondary);font-size:13px;cursor:pointer;width:100%}
.cf-btn-formula:hover{background:var(--bg-elevated);color:var(--accent);border-color:var(--accent)}
.cf-btn-formula .mdi{color:var(--accent)}
</style>
