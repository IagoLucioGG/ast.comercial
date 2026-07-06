<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { odataGet, odataPost, odataPatch, odataDelete, odataGetOne } from '@/services/api'
import TabelaEntidade, { type ColunaConfig, type CampoEstaticoConfig } from '@/components/shared/TabelaEntidade.vue'
import EditorFormula from '@/components/shared/EditorFormula.vue'
import FormulariosAdmin from '@/components/admin/FormulariosAdmin.vue'

const abaAtiva = ref<'campos' | 'formularios'>('campos')

interface Campo {
  Id: number; Nome: string; Chave: string; Tipo: string; EntidadeAlvo: string
  Obrigatorio: boolean; Visivel: boolean; SomenteLeitura: boolean; Ordem: number; Ativo: boolean
  Descricao: string | null; TamanhoMinimo: number | null; TamanhoMaximo: number | null
  ValorMinimo: number | null; ValorMaximo: number | null; Nativo: boolean; PermiteMultiplosValores: boolean
}

const tiposCampo = [
  { valor: 'Texto', label: 'Texto simples', icone: 'mdi-format-text' },
  { valor: 'TextoLongo', label: 'Texto longo', icone: 'mdi-text-long' },
  { valor: 'Numero', label: 'Número inteiro', icone: 'mdi-numeric' },
  { valor: 'Decimal', label: 'Número com decimais', icone: 'mdi-decimal' },
  { valor: 'Moeda', label: 'Moeda', icone: 'mdi-currency-usd' },
  { valor: 'Percentagem', label: 'Percentagem', icone: 'mdi-percent' },
  { valor: 'Lista', label: 'Lista de opções', icone: 'mdi-format-list-bulleted' },
  { valor: 'Data', label: 'Data', icone: 'mdi-calendar' },
  { valor: 'DataHora', label: 'Data e hora', icone: 'mdi-clock-outline' },
  { valor: 'Booleano', label: 'Checkbox', icone: 'mdi-checkbox-marked-outline' },
  { valor: 'Email', label: 'Email', icone: 'mdi-email-outline' },
  { valor: 'Telefone', label: 'Telefone', icone: 'mdi-phone-outline' },
  { valor: 'Cpf', label: 'CPF', icone: 'mdi-card-account-details-outline' },
  { valor: 'Cnpj', label: 'CNPJ', icone: 'mdi-domain' },
  { valor: 'Cep', label: 'CEP', icone: 'mdi-map-marker-outline' },
  { valor: 'Endereco', label: 'Endereço', icone: 'mdi-home-outline' },
  { valor: 'Imagem', label: 'Imagem', icone: 'mdi-image-outline' },
  { valor: 'Html', label: 'HTML / Editor rico', icone: 'mdi-code-tags' },
]

const entidades = ['Cliente', 'Negocio', 'Atividade', 'Produto', 'Proposta', 'PessoaContato', 'Usuario', 'Equipe', 'Empresa', 'Departamento', 'Cargo']

const entidadeSelecionada = ref<string | null>(null)
const contagens = ref<Record<string, number>>({})

const colunasTabela: ColunaConfig[] = [
  { campo: 'Nome', label: 'Nome' },
  { campo: 'Chave', label: 'Chave' },
  { campo: 'Tipo', label: 'Tipo', tipo: 'badge' },
  { campo: 'Obrigatorio', label: 'Obrig.' },
  { campo: 'Ativo', label: 'Status', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]

const camposEstaticosTabela: CampoEstaticoConfig[] = [
  { nome: 'Nome', tipo: 'string' },
  { nome: 'Tipo', tipo: 'enum:Texto|TextoLongo|Numero|Decimal|Moeda|Data|DataHora|Booleano|Lista|Email|Telefone|Cpf|Cnpj|Cep|Percentagem|Endereco|Imagem|Html' },
  { nome: 'Obrigatorio', tipo: 'boolean' },
  { nome: 'Visivel', tipo: 'boolean' },
  { nome: 'Ativo', tipo: 'boolean' },
]

const modalAberto = ref(false)
const modoEdicao = ref(false)
const campoEditando = ref<Campo | null>(null)
const ehNativo = computed(() => campoEditando.value?.Nativo ?? false)
const form = ref({ Nome: '', Descricao: '', TipoCampo: 'Texto', Obrigatorio: false, Visivel: true, SomenteLeitura: false, PermiteMultiplosValores: false, TamanhoMinimo: null as number | null, TamanhoMaximo: null as number | null, ValorMinimo: null as number | null, ValorMaximo: null as number | null, Mascara: '', Formula: '', FormulaVisibilidade: '', FormulaObrigatoriedade: '', FormulaCalculo: '', FormulaValorPadrao: '', FormulaSomenteLeitura: '' })
const secoes = ref<Record<string, boolean>>({ basicas: true, avancadas: false, obrigatoriedade: false, edicao: false, visualizacao: false })

const formulaEditando = ref<string | null>(null)

function abrirEditorFormula(campo: string) { formulaEditando.value = campo }
function fecharEditorFormula() { formulaEditando.value = null }
function salvarFormula(campo: string, valor: string) { (form.value as any)[campo] = valor; formulaEditando.value = null }

const tituloContador = computed(() => `${form.value.Nome.length}/80`)
const descContador = computed(() => `${form.value.Descricao.length}/250`)

function iconeEntidade(e: string): string {
  const map: Record<string, string> = {
    Cliente: 'mdi-account-group-outline',
    Negocio: 'mdi-handshake-outline',
    Proposta: 'mdi-file-document-outline',
    Produto: 'mdi-package-variant-closed',
    Atividade: 'mdi-calendar-check-outline',
    PessoaContato: 'mdi-card-account-phone-outline',
    Usuario: 'mdi-account-outline',
    Equipe: 'mdi-account-supervisor-outline',
    Empresa: 'mdi-domain',
    Funil: 'mdi-filter-outline',
    EtapaFunil: 'mdi-format-list-numbered',
    Departamento: 'mdi-office-building-outline',
    Cargo: 'mdi-briefcase-outline',
  }
  return map[e] || 'mdi-form-textbox'
}

function contarCampos(e: string): number {
  return contagens.value[e] ?? 0
}

async function carregarContagens() {
  for (const e of entidades) {
    try {
      const res = await odataGet<any>('Campos', { filter: `EntidadeAlvo eq '${e}'`, top: 0, count: true })
      contagens.value[e] = res['@odata.count'] ?? 0
    } catch { contagens.value[e] = 0 }
  }
}

function selecionarEntidade(e: string) {
  entidadeSelecionada.value = e
}

function abrirNovo() {
  modoEdicao.value = false; campoEditando.value = null
  form.value = { Nome: '', Descricao: '', TipoCampo: 'Texto', Obrigatorio: false, Visivel: true, SomenteLeitura: false, PermiteMultiplosValores: false, TamanhoMinimo: null, TamanhoMaximo: null, ValorMinimo: null, ValorMaximo: null, Mascara: '', Formula: '', FormulaVisibilidade: '', FormulaObrigatoriedade: '', FormulaCalculo: '', FormulaValorPadrao: '', FormulaSomenteLeitura: '' }
  secoes.value = { basicas: true, avancadas: false, obrigatoriedade: false, edicao: false, visualizacao: false }
  modalAberto.value = true
}

function abrirEditar(campo: Campo) {
  modoEdicao.value = true; campoEditando.value = campo
  form.value = { Nome: campo.Nome, Descricao: campo.Descricao ?? '', TipoCampo: campo.Tipo, Obrigatorio: campo.Obrigatorio, Visivel: campo.Visivel, SomenteLeitura: campo.SomenteLeitura, PermiteMultiplosValores: campo.PermiteMultiplosValores ?? false, TamanhoMinimo: campo.TamanhoMinimo, TamanhoMaximo: campo.TamanhoMaximo, ValorMinimo: campo.ValorMinimo, ValorMaximo: campo.ValorMaximo, Mascara: (campo as any).Mascara ?? '', Formula: (campo as any).Formula ?? '', FormulaVisibilidade: (campo as any).FormulaVisibilidade ?? '', FormulaObrigatoriedade: (campo as any).FormulaObrigatoriedade ?? '', FormulaCalculo: (campo as any).FormulaCalculo ?? '', FormulaValorPadrao: (campo as any).FormulaValorPadrao ?? '', FormulaSomenteLeitura: (campo as any).FormulaSomenteLeitura ?? '' }
  secoes.value = { basicas: true, avancadas: false, obrigatoriedade: false, edicao: false, visualizacao: false }
  modalAberto.value = true
}

async function abrirEditarPorId(id: number) {
  try {
    const campo = await odataGetOne<Campo>('Campos', id)
    abrirEditar(campo)
  } catch {}
}

const tabelaCamposRef = ref<InstanceType<typeof TabelaEntidade> | null>(null)

async function salvar() {
  if (!form.value.Nome.trim()) return
  const payload: any = { Nome: form.value.Nome, Descricao: form.value.Descricao || null, Tipo: form.value.TipoCampo, EntidadeAlvo: entidadeSelecionada.value, Obrigatorio: form.value.Obrigatorio, Visivel: form.value.Visivel, SomenteLeitura: form.value.SomenteLeitura, PermiteMultiplosValores: form.value.PermiteMultiplosValores }
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
  modalAberto.value = false; tabelaCamposRef.value?.carregar()
}

async function excluirCampo() {
  if (!campoEditando.value || !confirm('Deseja realmente excluir este campo?')) return
  await odataDelete('Campos', campoEditando.value.Id)
  modalAberto.value = false; tabelaCamposRef.value?.carregar()
}

onMounted(carregarContagens)
</script>

<template>
  <div class="cf-root">
    <!-- Abas: Campos | Formulários -->
    <div class="cf-abas">
      <button class="cf-aba" :class="{ ativa: abaAtiva === 'campos' }" @click="abaAtiva = 'campos'"><i class="mdi mdi-form-textbox"></i> Campos</button>
      <button class="cf-aba" :class="{ ativa: abaAtiva === 'formularios' }" @click="abaAtiva = 'formularios'"><i class="mdi mdi-form-select"></i> Formulários</button>
    </div>

    <!-- Tab: Formulários -->
    <FormulariosAdmin v-if="abaAtiva === 'formularios'" />

    <!-- Tab: Campos -->
    <template v-if="abaAtiva === 'campos'">
    <!-- View: Entity Cards -->
    <div v-if="!entidadeSelecionada" class="cf-cards">
      <div class="cf-cards-grid">
        <button v-for="e in entidades" :key="e" class="cf-card" @click="selecionarEntidade(e)">
          <i class="mdi" :class="iconeEntidade(e)"></i>
          <span class="cf-card-nome">{{ e }}</span>
          <span class="cf-card-count">{{ contarCampos(e) }} campos</span>
        </button>
      </div>
    </div>

    <!-- View: Fields of entity -->
    <div v-else class="cf-campos">
      <div class="cf-campos-header">
        <button class="cf-btn-voltar" @click="entidadeSelecionada = null">
          <i class="mdi mdi-arrow-left"></i>
        </button>
        <h3>{{ entidadeSelecionada }}</h3>
      </div>
      <TabelaEntidade
        ref="tabelaCamposRef"
        :key="entidadeSelecionada"
        entidade="Campo"
        endpoint="Campos"
        :titulo="entidadeSelecionada || 'Campo'"
        :colunas="colunasTabela"
        :campos-busca="['Nome', 'Chave']"
        placeholder-busca="Buscar campo..."
        ordenacao-padrao="Ordem asc,Nome asc"
        :campos-estaticos="camposEstaticosTabela"
        :filtro-base="`EntidadeAlvo eq '${entidadeSelecionada}'`"
        sem-form-interno
        @novo="abrirNovo"
        @editar="abrirEditarPorId"
      />
    </div>
    </template>

    <!-- Modal criar/editar -->
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
              <div v-if="form.TipoCampo === 'Lista'" class="cf-toggle"><div><span class="cf-tl">Permite múltipla seleção</span><span class="cf-td">Permite selecionar mais de uma opção ao mesmo tempo</span></div><label class="cf-sw"><input type="checkbox" v-model="form.PermiteMultiplosValores" /><span></span></label></div>
            </div>
            <button class="cf-sec-head cf-sec-cyan" @click="secoes.avancadas = !secoes.avancadas"><span>Configurações avançadas</span><i class="mdi" :class="secoes.avancadas ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.avancadas" class="cf-sec-body">
              <div v-if="['Texto', 'TextoLongo'].includes(form.TipoCampo)" class="cf-row2"><div class="cf-field"><label>Tam. mínimo</label><input v-model.number="form.TamanhoMinimo" type="number" min="0" placeholder="Sem limite" /></div><div class="cf-field"><label>Tam. máximo</label><input v-model.number="form.TamanhoMaximo" type="number" min="0" placeholder="Sem limite" /></div></div>
              <div v-if="['Numero', 'Decimal', 'Moeda', 'Percentagem', 'Data', 'DataHora'].includes(form.TipoCampo)" class="cf-row2"><div class="cf-field"><label>Valor mínimo</label><input v-model.number="form.ValorMinimo" type="number" placeholder="Sem limite" /></div><div class="cf-field"><label>Valor máximo</label><input v-model.number="form.ValorMaximo" type="number" placeholder="Sem limite" /></div></div>
              <div v-if="form.TipoCampo === 'Texto'" class="cf-field"><label>Máscara de entrada</label><input v-model="form.Mascara" placeholder="Ex: AAA-#### (placa), XX-###-YY" /></div>
              <div v-if="form.TipoCampo !== 'Lista'" class="cf-field"><label>Fórmula de cálculo</label><small class="cf-field-desc">Deve retornar o valor do campo (texto, número, data, etc). Ex: <code>$registro.Quantidade * $registro.PrecoUnitario</code></small><button class="cf-btn-formula" @click="abrirEditorFormula('Formula')"><i class="mdi mdi-function-variant"></i> {{ form.Formula ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div>
            </div>
            <button v-if="form.TipoCampo !== 'Booleano'" class="cf-sec-head cf-sec-orange" @click="secoes.obrigatoriedade = !secoes.obrigatoriedade"><span>Configurações de obrigatoriedade</span><i class="mdi" :class="secoes.obrigatoriedade ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.obrigatoriedade && form.TipoCampo !== 'Booleano'" class="cf-sec-body"><div class="cf-toggle"><div><span class="cf-tl">Obrigatório</span><span class="cf-td">O campo deve ser preenchido para salvar</span></div><label class="cf-sw"><input type="checkbox" v-model="form.Obrigatorio" /><span></span></label></div><div class="cf-field"><label>Fórmula de obrigatoriedade condicional</label><small class="cf-field-desc">Deve retornar <code>true</code> (obrigatório) ou <code>false</code> (opcional). Ex: <code>$registro.Valor &gt; 1000</code></small><button class="cf-btn-formula" @click="abrirEditorFormula('FormulaObrigatoriedade')"><i class="mdi mdi-function-variant"></i> {{ form.FormulaObrigatoriedade ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div></div>
            <button class="cf-sec-head cf-sec-orange" @click="secoes.edicao = !secoes.edicao"><span>Configurações de edição e bloqueio</span><i class="mdi" :class="secoes.edicao ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.edicao" class="cf-sec-body"><div class="cf-toggle"><div><span class="cf-tl">Somente leitura</span><span class="cf-td">O campo não pode ser editado</span></div><label class="cf-sw"><input type="checkbox" v-model="form.SomenteLeitura" /><span></span></label></div><div class="cf-field"><label>Fórmula de bloqueio condicional</label><small class="cf-field-desc">Deve retornar <code>true</code> (bloqueado) ou <code>false</code> (editável). Ex: <code>$registro.Status === 'Fechado'</code></small><button class="cf-btn-formula" @click="abrirEditorFormula('FormulaSomenteLeitura')"><i class="mdi mdi-function-variant"></i> {{ form.FormulaSomenteLeitura ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div></div>
            <button class="cf-sec-head cf-sec-orange" @click="secoes.visualizacao = !secoes.visualizacao"><span>Configurações de visualização</span><i class="mdi" :class="secoes.visualizacao ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i></button>
            <div v-if="secoes.visualizacao" class="cf-sec-body"><div class="cf-toggle"><div><span class="cf-tl">Visível</span><span class="cf-td">O campo aparece nos formulários</span></div><label class="cf-sw"><input type="checkbox" v-model="form.Visivel" /><span></span></label></div><div class="cf-field"><label>Fórmula de visibilidade condicional</label><small class="cf-field-desc">Deve retornar <code>true</code> (ocultar campo) ou <code>false</code> (manter visível). Ex: <code>$registro.Tipo === 'Empresa'</code></small><button class="cf-btn-formula" @click="abrirEditorFormula('FormulaVisibilidade')"><i class="mdi mdi-function-variant"></i> {{ form.FormulaVisibilidade ? 'Editar fórmula' : 'Configurar fórmula' }}</button></div></div>
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
      :entidade="entidadeSelecionada || 'Cliente'"
      :titulo="{ Formula: 'Cálculo automático', FormulaObrigatoriedade: 'Quando este campo é obrigatório?', FormulaSomenteLeitura: 'Quando bloquear edição?', FormulaVisibilidade: 'Quando ocultar este campo?' }[formulaEditando!] || 'Configurar fórmula'"
      :tipo-retorno="formulaEditando === 'Formula' ? 'valor' : 'booleano'"
      @salvar="(v: string) => salvarFormula(formulaEditando!, v)"
      @fechar="fecharEditorFormula"
    />
  </div>
</template>

<style scoped>
.cf-root{display:flex;flex-direction:column;height:100%}
.cf-abas{display:flex;gap:0;border-bottom:1px solid var(--border);flex-shrink:0;padding:0 16px}
.cf-aba{padding:12px 20px;border:none;background:transparent;color:var(--text-muted);font-size:13px;font-weight:500;cursor:pointer;border-bottom:2px solid transparent;display:flex;align-items:center;gap:6px;transition:all .15s}
.cf-aba:hover{color:var(--text-primary)}
.cf-aba.ativa{color:var(--accent);border-bottom-color:var(--accent);font-weight:600}
.cf-aba .mdi{font-size:16px}
.cf-campos{display:flex;flex-direction:column;flex:1;overflow:hidden}
.cf-cards-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(200px,1fr));gap:12px;padding:16px}
.cf-card{display:flex;flex-direction:column;align-items:center;gap:8px;padding:24px 16px;border:1px solid var(--border);background:var(--bg-surface);border-radius:10px;cursor:pointer;transition:all .15s}
.cf-card:hover{border-color:var(--accent);transform:translateY(-2px);box-shadow:0 4px 12px rgba(6,182,212,.1)}
.cf-card .mdi{font-size:28px;color:var(--accent)}
.cf-card-nome{font-size:14px;font-weight:500;color:var(--text-primary)}
.cf-card-count{font-size:12px;color:var(--text-muted)}
.cf-campos-header{display:flex;align-items:center;gap:12px;flex-shrink:0;padding:12px 16px;border-bottom:1px solid var(--border)}
.cf-campos-header h3{font-size:16px;font-weight:600;color:var(--text-primary);margin:0}
.cf-btn-voltar{width:36px;height:36px;border-radius:8px;border:1px solid var(--border);background:var(--bg-surface);color:var(--text-secondary);cursor:pointer;display:flex;align-items:center;justify-content:center}
.cf-btn-voltar:hover{background:var(--bg-elevated);color:var(--text-primary)}
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
.cf-field-desc{font-size:11px;color:var(--text-muted);margin-top:-2px;margin-bottom:4px;display:block}
</style>
