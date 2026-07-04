<script setup lang="ts">
import { ref, computed, onMounted, onBeforeUnmount, watch, nextTick } from 'vue'
import * as monaco from 'monaco-editor'
import editorWorker from 'monaco-editor/esm/vs/editor/editor.worker?worker'
import tsWorker from 'monaco-editor/esm/vs/language/typescript/ts.worker?worker'
import { odataGet } from '@/services/api'

// Monaco workers setup
self.MonacoEnvironment = {
  getWorker(_: string, label: string) {
    if (label === 'typescript' || label === 'javascript') return new tsWorker()
    return new editorWorker()
  }
}

// --- Types ---

interface EsquemaNavegacao {
  Tipo: string
  Descricao?: string
  Propriedades?: Record<string, EsquemaNavegacao>
}

interface CampoEsquema {
  nome: string
  tipo: string
  variavel: string
  grupo: 'fixo' | 'personalizado' | 'navegacao'
}

interface Condicao {
  campo: string
  operador: string
  valor: string
}

// --- Props & Emits ---

const props = defineProps<{
  modelValue: string
  entidade: string
  titulo?: string
  tipoRetorno?: 'valor' | 'booleano'
}>()

const emit = defineEmits<{
  'update:modelValue': [valor: string]
  'salvar': [valor: string]
  'fechar': []
}>()

// --- State ---

const editorContainer = ref<HTMLDivElement | null>(null)
let editorInstance: monaco.editor.IStandaloneCodeEditor | null = null
let completionDisposable: monaco.IDisposable | null = null

const codigo = ref(props.modelValue || '')
const abaAtiva = ref<'simples' | 'condicional'>('simples')
const abaInferior = ref<'variaveis' | 'testar'>('variaveis')

const esquema = ref<Record<string, any>>({})
const camposFixos = ref<CampoEsquema[]>([])
const camposPersonalizados = ref<CampoEsquema[]>([])
const camposNavegacao = ref<CampoEsquema[]>([])
const carregando = ref(false)

const mostrarCampos = ref(true)
const mostrarSnippets = ref(false)
const buscaCampo = ref('')

const condicoes = ref<Condicao[]>([{ campo: '', operador: 'igual', valor: '' }])
const resultadoVerdadeiro = ref('')
const resultadoFalso = ref('')
const resultadoTeste = ref('')

// Problem 3: Tree navigation state
const navegacaoAtual = ref<string[]>([])

// Problem 4: Test variables state
const valoresTeste = ref<Record<string, string>>({})

// --- Operators ---

const operadores = [
  { valor: 'igual', label: 'Igual a', js: '===' },
  { valor: 'diferente', label: 'Diferente de', js: '!==' },
  { valor: 'maior', label: 'Maior que', js: '>' },
  { valor: 'menor', label: 'Menor que', js: '<' },
  { valor: 'maiorIgual', label: 'Maior ou igual', js: '>=' },
  { valor: 'menorIgual', label: 'Menor ou igual', js: '<=' },
  { valor: 'contem', label: 'Contém', js: '.includes' },
  { valor: 'vazio', label: 'Está vazio', js: '' },
  { valor: 'naoVazio', label: 'Não está vazio', js: '' },
]

// --- Snippets ---

const snippetsJS = [
  { label: 'Se campo igual a valor', code: `$registro.Campo === 'valor'` },
  { label: 'Se campo maior que', code: `$registro.Campo > 0` },
  { label: 'Campo preenchido', code: `$registro.Campo != null && $registro.Campo !== ''` },
  { label: 'Campo vazio', code: `$registro.Campo == null || $registro.Campo === ''` },
  { label: 'Percentual', code: `$registro.Valor * 0.10` },
  { label: 'Soma', code: `$registro.Campo1 + $registro.Campo2` },
  { label: 'Concatenar', code: `$registro.Campo1 + ' - ' + $registro.Campo2` },
  { label: 'Data atual', code: `new Date().toISOString().split('T')[0]` },
  { label: 'Arredondar', code: `Math.round($registro.Valor * 100) / 100` },
  { label: 'Ternário', code: `$registro.Tipo === 'Empresa' ? $registro.Valor * 0.9 : $registro.Valor` },
]

// --- Computed ---

const todosCampos = computed(() => [...camposFixos.value, ...camposPersonalizados.value, ...camposNavegacao.value])

const variaveisUsadas = computed(() => {
  const texto = codigo.value || ''
  const regex = /\$(?:registro|campos|usuario|empresa|negocio|contato|funil|etapa|proposta)\.[\w.]+/g
  const matches = texto.match(regex)
  return [...new Set(matches || [])]
})

// Problem 3: Tree navigation computed for Campos sidebar
const camposNivelAtual = computed(() => {
  const buscando = buscaCampo.value.toLowerCase()

  // If searching, search all flat
  if (buscando) {
    return todosCampos.value.filter(c => c.nome.toLowerCase().includes(buscando) || c.variavel.toLowerCase().includes(buscando))
      .map(c => ({ nome: c.nome, tipo: c.tipo, variavel: c.variavel, expandivel: false }))
  }

  // Root level: show $registro fields + navigations as expandable + custom fields
  if (navegacaoAtual.value.length === 0) {
    const items: { nome: string; tipo: string; variavel: string; expandivel: boolean }[] = []

    // Fixed fields from $registro
    for (const c of camposFixos.value) {
      items.push({ nome: c.nome, tipo: c.tipo, variavel: c.variavel, expandivel: false })
    }

    // Navigations as expandable (entries from schema that are not $registro/$campos and have propriedades)
    const data = esquema.value as any
    for (const [chave, valor] of Object.entries(data) as [string, any][]) {
      if (chave === '$registro' || chave === '$campos') continue
      const entryProps = valor?.propriedades ?? valor?.Propriedades
      if (entryProps && Object.keys(entryProps).length > 0) {
        items.push({ nome: chave.replace('$', ''), tipo: valor.tipo ?? 'objeto', variavel: chave, expandivel: true })
      }
    }

    // Also add navigation properties from $registro that have sub-properties
    const registro = data['$registro']
    const registroProps = registro?.propriedades ?? registro?.Propriedades ?? {}
    for (const [nome, info] of Object.entries(registroProps) as [string, any][]) {
      const subProps = info?.propriedades ?? info?.Propriedades
      if (subProps && Object.keys(subProps).length > 0) {
        items.push({ nome, tipo: info.tipo ?? 'objeto', variavel: `$registro.${nome}`, expandivel: true })
      }
    }

    // Custom fields
    for (const c of camposPersonalizados.value) {
      items.push({ nome: c.nome, tipo: c.tipo, variavel: c.variavel, expandivel: false })
    }

    return items
  }

  // Navigated level: show properties of the current path
  const data = esquema.value as any
  const path = navegacaoAtual.value
  let current: any = data[path[0] as string]
  for (let i = 1; i < path.length; i++) {
    const navProps: any = current?.propriedades ?? current?.Propriedades ?? {}
    current = navProps[path[i] as string]
  }

  const currentProps: Record<string, any> = current?.propriedades ?? current?.Propriedades ?? {}
  const items: { nome: string; tipo: string; variavel: string; expandivel: boolean }[] = []
  const basePath = navegacaoAtual.value.join('.')

  for (const [nome, info] of Object.entries(currentProps) as [string, any][]) {
    const subProps = info?.propriedades ?? info?.Propriedades
    items.push({
      nome,
      tipo: info.tipo ?? info.Tipo ?? 'string',
      variavel: `${basePath}.${nome}`,
      expandivel: !!(subProps && Object.keys(subProps).length > 0)
    })
  }

  return items
})

// Problem 1: Fixed codigoCondicional computed
const codigoCondicional = computed(() => {
  const condsValidas = condicoes.value.filter(c => c.campo)
  if (!condsValidas.length) return ''

  const partes = condsValidas.map(c => {
    const campo = c.campo
    const op = operadores.find(o => o.valor === c.operador)
    const jsOp = op?.js || '==='

    if (c.operador === 'vazio') return `(${campo} == null || ${campo} === '')`
    if (c.operador === 'naoVazio') return `(${campo} != null && ${campo} !== '')`
    if (c.operador === 'contem') {
      if (!c.valor) return ''
      return `${campo}.includes('${c.valor}')`
    }

    // Only include condition if it has a valid value
    if (!c.valor && c.operador !== 'vazio' && c.operador !== 'naoVazio') return ''
    const val = isNaN(Number(c.valor)) ? `'${c.valor}'` : c.valor
    return `${campo} ${jsOp} ${val}`
  }).filter(p => p)

  if (!partes.length) return ''
  const condicao = partes.join(' && ')
  return `(${condicao}) ? '${resultadoVerdadeiro.value}' : '${resultadoFalso.value}'`
})

// --- Functions ---

async function carregarEsquema() {
  carregando.value = true
  try {
    const res = await odataGet(`Campos@Esquema(${props.entidade})`)
    esquema.value = res ?? {}
    parsearEsquema()
  } catch {
    esquema.value = {}
  } finally {
    carregando.value = false
  }
}

function parsearEsquema() {
  const data = esquema.value as any
  camposFixos.value = []
  camposPersonalizados.value = []
  camposNavegacao.value = []

  // Parse $registro
  const registro = data['$registro']
  if (registro) {
    const registroProps = registro.propriedades ?? registro.Propriedades ?? {}
    for (const [nome, info] of Object.entries(registroProps) as [string, any][]) {
      const subProps = info?.propriedades ?? info?.Propriedades
      if (!subProps || Object.keys(subProps).length === 0) {
        camposFixos.value.push({ nome, tipo: info.tipo ?? 'string', variavel: `$registro.${nome}`, grupo: 'fixo' })
      }
    }
  }

  // Parse $campos
  const campos = data['$campos']
  if (campos) {
    const camposProps = campos.propriedades ?? campos.Propriedades ?? {}
    for (const [nome, info] of Object.entries(camposProps) as [string, any][]) {
      camposPersonalizados.value.push({ nome, tipo: info.tipo ?? 'string', variavel: `$campos.${nome}`, grupo: 'personalizado' })
    }
  }

  // Parse navigations
  for (const [chave, valor] of Object.entries(data) as [string, any][]) {
    if (chave === '$registro' || chave === '$campos') continue
    const navProps = valor?.propriedades ?? valor?.Propriedades
    if (navProps) {
      for (const [nome, info] of Object.entries(navProps) as [string, any][]) {
        camposNavegacao.value.push({ nome: `${chave}.${nome}`, tipo: (info as any).tipo ?? 'string', variavel: `${chave}.${nome}`, grupo: 'navegacao' })
      }
    }
  }
}

function inserirNoCodigo(variavel: string) {
  if (editorInstance) {
    const position = editorInstance.getPosition()
    if (position) {
      editorInstance.executeEdits('inserir-campo', [{
        range: new monaco.Range(position.lineNumber, position.column, position.lineNumber, position.column),
        text: variavel
      }])
      editorInstance.focus()
    }
  } else {
    codigo.value += variavel
  }
  mostrarCampos.value = false
}

function inserirSnippet(code: string) {
  if (editorInstance) {
    const position = editorInstance.getPosition()
    if (position) {
      editorInstance.executeEdits('inserir-snippet', [{
        range: new monaco.Range(position.lineNumber, position.column, position.lineNumber, position.column),
        text: code
      }])
      editorInstance.focus()
    }
  } else {
    codigo.value += code
  }
  mostrarSnippets.value = false
}

function aplicarCondicional() {
  const formula = codigoCondicional.value
  if (formula) {
    codigo.value = formula
    if (editorInstance) {
      editorInstance.setValue(formula)
    }
  }
}

function adicionarCondicao() {
  condicoes.value.push({ campo: '', operador: 'igual', valor: '' })
}

function removerCondicao(index: number) {
  condicoes.value.splice(index, 1)
}

// Problem 3: Navigation function for tree campos
function navegarPara(item: { nome: string; variavel: string; expandivel: boolean }) {
  if (navegacaoAtual.value.length === 0) {
    if (item.variavel.startsWith('$') && !item.variavel.includes('.')) {
      navegacaoAtual.value.push(item.variavel)
    } else {
      navegacaoAtual.value.push('$registro', item.nome)
    }
  } else {
    navegacaoAtual.value.push(item.nome)
  }
}

// Problem 4: Test formula with user-provided variable values
function testarFormula() {
  try {
    const vars: Record<string, any> = {}
    for (const v of variaveisUsadas.value) {
      const raw = valoresTeste.value[v] ?? ''
      if (raw === 'true') vars[v] = true
      else if (raw === 'false') vars[v] = false
      else if (!isNaN(Number(raw)) && raw !== '') vars[v] = Number(raw)
      else vars[v] = raw
    }

    const $registro: Record<string, any> = {}
    const $campos: Record<string, any> = {}
    const $usuario: Record<string, any> = {}
    const $empresa: Record<string, any> = {}
    const $negocio: Record<string, any> = {}
    const $contato: Record<string, any> = {}

    for (const [key, val] of Object.entries(vars)) {
      if (key.startsWith('$registro.')) $registro[key.replace('$registro.', '')] = val
      else if (key.startsWith('$campos.')) $campos[key.replace('$campos.', '')] = val
      else if (key.startsWith('$usuario.')) $usuario[key.replace('$usuario.', '')] = val
      else if (key.startsWith('$empresa.')) $empresa[key.replace('$empresa.', '')] = val
      else if (key.startsWith('$negocio.')) $negocio[key.replace('$negocio.', '')] = val
      else if (key.startsWith('$contato.')) $contato[key.replace('$contato.', '')] = val
    }

    const fn = new Function(
      '$registro', '$campos', '$usuario', '$empresa', '$negocio', '$contato',
      `"use strict"; return (${codigo.value})`
    )
    const resultado = fn($registro, $campos, $usuario, $empresa, $negocio, $contato)
    resultadoTeste.value = `✅ Resultado: ${JSON.stringify(resultado)}`
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    resultadoTeste.value = `❌ Erro: ${msg}`
  }
}

const erroValidacao = ref('')

function validarFormula(): boolean {
  erroValidacao.value = ''
  const src = codigo.value.trim()
  if (!src) return true // vazio é ok (remove a fórmula)

  // Verifica erro de sintaxe
  try {
    new Function('$registro', '$campos', '$usuario', '$empresa', '$negocio', '$contato', `"use strict"; return (${src})`)
  } catch (e: unknown) {
    const msg = e instanceof Error ? e.message : String(e)
    erroValidacao.value = `Erro de sintaxe: ${msg}`
    return false
  }

  // Se tipo retorno é booleano, alerta mas não bloqueia (validação suave)
  if (props.tipoRetorno === 'booleano') {
    try {
      const fn = new Function('$registro', '$campos', '$usuario', '$empresa', `"use strict"; return (${src})`)
      const resultado = fn({}, {}, {}, {})
      if (typeof resultado !== 'boolean' && resultado !== undefined) {
        erroValidacao.value = `Esta fórmula deve retornar true ou false. Resultado obtido: ${JSON.stringify(resultado)}`
        return false
      }
    } catch { /* Pode falhar por falta de dados - ok */ }
  }

  return true
}

function salvar() {
  if (!validarFormula()) return
  emit('update:modelValue', codigo.value)
  emit('salvar', codigo.value)
}

function fechar() {
  emit('fechar')
}

// --- Monaco Editor ---

function inicializarEditor() {
  if (!editorContainer.value) return

  completionDisposable = monaco.languages.registerCompletionItemProvider('javascript', {
    triggerCharacters: ['.', '$'],
    provideCompletionItems(model, position) {
      const word = model.getWordUntilPosition(position)
      const range = {
        startLineNumber: position.lineNumber,
        endLineNumber: position.lineNumber,
        startColumn: word.startColumn,
        endColumn: word.endColumn
      }

      const suggestions: monaco.languages.CompletionItem[] = todosCampos.value.map(c => ({
        label: c.variavel,
        kind: monaco.languages.CompletionItemKind.Variable,
        insertText: c.variavel,
        detail: c.tipo,
        documentation: `${c.grupo}: ${c.nome}`,
        range
      }))

      return { suggestions }
    }
  })

  editorInstance = monaco.editor.create(editorContainer.value, {
    value: codigo.value,
    language: 'javascript',
    theme: 'vs-dark',
    minimap: { enabled: false },
    fontSize: 13,
    lineNumbers: 'on',
    scrollBeyondLastLine: false,
    wordWrap: 'on',
    automaticLayout: true,
    tabSize: 2,
    padding: { top: 8 },
    suggest: { showWords: false }
  })

  editorInstance.onDidChangeModelContent(() => {
    codigo.value = editorInstance!.getModel()?.getValue() || ''
  })
}

// --- Lifecycle ---

onMounted(() => {
  carregarEsquema()
  nextTick(() => inicializarEditor())
})

onBeforeUnmount(() => {
  completionDisposable?.dispose()
  editorInstance?.dispose()
})

watch(() => props.modelValue, (val) => {
  if (val !== codigo.value) {
    codigo.value = val
    if (editorInstance && editorInstance.getValue() !== val) {
      editorInstance.setValue(val)
    }
  }
})
</script>

<template>
  <div class="ef-overlay">
    <div class="ef-modal">
      <!-- Header -->
      <div class="ef-header">
        <div class="ef-header-left">
          <h3 class="ef-titulo">{{ titulo || 'Configurar fórmula' }}</h3>
          <span v-if="tipoRetorno === 'booleano'" class="ef-retorno-badge ef-retorno-bool">Retorna: true ou false</span>
          <span v-else class="ef-retorno-badge ef-retorno-valor">Retorna: valor calculado</span>
        </div>
        <div class="ef-header-actions">
          <button class="ef-btn ef-btn--salvar" @click="salvar">
            <span class="mdi mdi-content-save"></span> Salvar
          </button>
          <button class="ef-btn ef-btn--fechar" @click="fechar">
            <span class="mdi mdi-close"></span>
          </button>
        </div>
      </div>
      <div v-if="erroValidacao" class="ef-erro-validacao"><span class="mdi mdi-alert-circle"></span> {{ erroValidacao }}</div>

      <!-- Body -->
      <div class="ef-body">
        <!-- Sidebar -->
        <div class="ef-sidebar">
          <!-- Abas superiores -->
          <div class="ef-sidebar-abas">
            <button :class="['ef-sidebar-aba', { ativa: mostrarCampos }]" @click="mostrarCampos = true; mostrarSnippets = false">Campos</button>
            <button :class="['ef-sidebar-aba', { ativa: mostrarSnippets }]" @click="mostrarSnippets = true; mostrarCampos = false">Scripts</button>
          </div>

          <!-- Problem 3: Campos dropdown with tree navigation -->
          <div v-if="mostrarCampos" class="ef-dropdown">
            <input v-model="buscaCampo" type="text" placeholder="Buscar campo..." class="ef-busca" />
            <div v-if="navegacaoAtual.length > 0" class="ef-nav-header">
              <button class="ef-nav-back" @click="navegacaoAtual.pop()"><span class="mdi mdi-arrow-left"></span></button>
              <span class="ef-nav-path">{{ navegacaoAtual.join(' → ') }}</span>
            </div>
            <div class="ef-dropdown-scroll">
              <div v-if="!camposNivelAtual.length" class="ef-vazio">{{ carregando ? 'Carregando...' : 'Nenhum campo' }}</div>
              <div v-for="item in camposNivelAtual" :key="item.variavel" class="ef-campo-row">
                <button class="ef-campo-item" @click="!item.expandivel ? inserirNoCodigo(item.variavel) : null" :class="{ 'ef-campo-nav': item.expandivel }">
                  <span class="ef-campo-nome">{{ item.nome }}</span>
                  <span class="ef-campo-tipo">{{ item.tipo }}</span>
                </button>
                <button v-if="item.expandivel" class="ef-campo-seta" @click="navegarPara(item)" title="Expandir">
                  <span class="mdi mdi-chevron-right"></span>
                </button>
              </div>
            </div>
          </div>

          <!-- Snippets dropdown -->
          <div v-if="mostrarSnippets" class="ef-dropdown">
            <div class="ef-dropdown-scroll">
              <button v-for="s in snippetsJS" :key="s.label" class="ef-campo-item" @click="inserirSnippet(s.code)">
                <span class="ef-campo-nome">{{ s.label }}</span>
              </button>
            </div>
          </div>
        </div>

        <!-- Main content -->
        <div class="ef-main">
          <!-- Abas modo -->
          <div class="ef-modo-abas">
            <button :class="['ef-modo-aba', { ativa: abaAtiva === 'simples' }]" @click="abaAtiva = 'simples'">Editor</button>
            <button :class="['ef-modo-aba', { ativa: abaAtiva === 'condicional' }]" @click="abaAtiva = 'condicional'">Condicional</button>
          </div>

          <!-- Editor Monaco -->
          <div v-show="abaAtiva === 'simples'" class="ef-editor-wrap">
            <div ref="editorContainer" class="ef-editor"></div>
          </div>

          <!-- Condicional builder -->
          <div v-show="abaAtiva === 'condicional'" class="ef-condicional">
            <div class="ef-condicional-lista">
              <div v-for="(cond, idx) in condicoes" :key="idx" class="ef-cond-row">
                <select v-model="cond.campo" class="ef-cond-select">
                  <option value="">Campo...</option>
                  <option v-for="c in todosCampos" :key="c.variavel" :value="c.variavel">{{ c.nome }}</option>
                </select>
                <select v-model="cond.operador" class="ef-cond-select ef-cond-select--op">
                  <option v-for="op in operadores" :key="op.valor" :value="op.valor">{{ op.label }}</option>
                </select>
                <input v-if="cond.operador !== 'vazio' && cond.operador !== 'naoVazio'" v-model="cond.valor" class="ef-cond-input" placeholder="Valor" />
                <button class="ef-cond-remove" @click="removerCondicao(idx)" title="Remover">
                  <span class="mdi mdi-close"></span>
                </button>
              </div>
            </div>
            <button class="ef-btn ef-btn--add" @click="adicionarCondicao">+ Condição</button>
            <div class="ef-cond-resultados">
              <div class="ef-cond-resultado">
                <label>Se verdadeiro:</label>
                <input v-model="resultadoVerdadeiro" class="ef-cond-input" placeholder="Resultado quando verdadeiro" />
              </div>
              <div class="ef-cond-resultado">
                <label>Se falso:</label>
                <input v-model="resultadoFalso" class="ef-cond-input" placeholder="Resultado quando falso" />
              </div>
            </div>
            <div class="ef-cond-preview">
              <label>Código gerado:</label>
              <code class="ef-cond-code">{{ codigoCondicional || '(configure as condições acima)' }}</code>
            </div>
            <button class="ef-btn ef-btn--aplicar" @click="aplicarCondicional" :disabled="!codigoCondicional">Aplicar no editor</button>
          </div>

          <!-- Painel inferior -->
          <div class="ef-inferior">
            <div class="ef-inferior-abas">
              <button :class="['ef-inferior-aba', { ativa: abaInferior === 'variaveis' }]" @click="abaInferior = 'variaveis'">Variáveis</button>
              <button :class="['ef-inferior-aba', { ativa: abaInferior === 'testar' }]" @click="abaInferior = 'testar'">Testar</button>
            </div>

            <div v-if="abaInferior === 'variaveis'" class="ef-variaveis">
              <div v-if="variaveisUsadas.length" class="ef-var-lista">
                <span v-for="v in variaveisUsadas" :key="v" class="ef-var-tag">{{ v }}</span>
              </div>
              <p v-else class="ef-vazio">Nenhuma variável detectada na fórmula.</p>
            </div>

            <!-- Problem 4: Test panel with editable variable inputs -->
            <div v-if="abaInferior === 'testar'" class="ef-testar">
              <div v-if="variaveisUsadas.length" class="ef-teste-vars">
                <div v-for="v in variaveisUsadas" :key="v" class="ef-teste-var-row">
                  <label class="ef-teste-var-label">{{ v }}</label>
                  <input v-model="valoresTeste[v]" class="ef-teste-var-input" :placeholder="'Valor para ' + v" />
                </div>
              </div>
              <p v-else class="ef-vazio">Escreva uma fórmula para ver as variáveis de teste.</p>
              <button class="ef-btn ef-btn--testar" @click="testarFormula" :disabled="!variaveisUsadas.length">
                <span class="mdi mdi-play"></span> Executar teste
              </button>
              <div v-if="resultadoTeste" class="ef-resultado-teste">{{ resultadoTeste }}</div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.ef-overlay { position: fixed; inset: 0; background: rgba(0,0,0,0.7); display: flex; align-items: center; justify-content: center; z-index: 2000; }
.ef-modal { background: #1a1a2e; border-radius: 12px; width: 95vw; max-width: 1200px; height: 85vh; display: flex; flex-direction: column; box-shadow: 0 20px 60px rgba(0,0,0,0.5); overflow: hidden; resize: both; min-width: 600px; min-height: 400px; }
.ef-header { display: flex; align-items: center; justify-content: space-between; padding: 12px 20px; border-bottom: 1px solid #2a2a4a; }
.ef-titulo { margin: 0; font-size: 16px; color: #e0e0e0; font-weight: 600; }
.ef-header-left { display: flex; align-items: center; gap: 12px; }
.ef-retorno-badge { font-size: 11px; padding: 3px 10px; border-radius: 4px; font-weight: 500; }
.ef-retorno-bool { background: rgba(245,158,11,.15); color: #f59e0b; border: 1px solid rgba(245,158,11,.3); }
.ef-retorno-valor { background: rgba(6,182,212,.1); color: #4fc3f7; border: 1px solid rgba(6,182,212,.2); }
.ef-erro-validacao { padding: 8px 20px; background: rgba(239,68,68,.1); color: #ef4444; font-size: 12px; display: flex; align-items: center; gap: 6px; border-bottom: 1px solid rgba(239,68,68,.2); }
.ef-header-actions { display: flex; gap: 8px; }
.ef-body { display: flex; flex: 1; overflow: hidden; }
.ef-sidebar { width: 260px; border-right: 1px solid #2a2a4a; display: flex; flex-direction: column; overflow: hidden; }
.ef-sidebar-abas { display: flex; border-bottom: 1px solid #2a2a4a; }
.ef-sidebar-aba { flex: 1; padding: 8px; background: none; border: none; color: #888; cursor: pointer; font-size: 12px; transition: all 0.2s; }
.ef-sidebar-aba.ativa { color: #4fc3f7; border-bottom: 2px solid #4fc3f7; }
.ef-dropdown { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
.ef-busca { padding: 8px 12px; background: #252545; border: none; border-bottom: 1px solid #2a2a4a; color: #e0e0e0; font-size: 12px; outline: none; }
.ef-busca:focus { background: #2a2a5a; }
.ef-dropdown-scroll { flex: 1; overflow-y: auto; }
.ef-campo-item { display: flex; align-items: center; justify-content: space-between; width: 100%; padding: 6px 12px; background: none; border: none; border-bottom: 1px solid #1a1a3a; color: #e0e0e0; cursor: pointer; text-align: left; font-size: 12px; transition: background 0.15s; }
.ef-campo-item:hover { background: #252545; }
.ef-campo-item.ef-campo-nav { color: #4fc3f7; }
.ef-campo-nome { flex: 1; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.ef-campo-tipo { font-size: 10px; color: #666; margin-left: 8px; flex-shrink: 0; }
.ef-vazio { padding: 12px; color: #666; font-size: 12px; text-align: center; }
.ef-main { flex: 1; display: flex; flex-direction: column; overflow: hidden; }
.ef-modo-abas { display: flex; border-bottom: 1px solid #2a2a4a; }
.ef-modo-aba { padding: 8px 16px; background: none; border: none; color: #888; cursor: pointer; font-size: 12px; transition: all 0.2s; }
.ef-modo-aba.ativa { color: #4fc3f7; border-bottom: 2px solid #4fc3f7; }
.ef-editor-wrap { flex: 1; overflow: hidden; }
.ef-editor { height: 100%; }
.ef-condicional { flex: 1; padding: 16px; overflow-y: auto; }
.ef-condicional-lista { display: flex; flex-direction: column; gap: 8px; margin-bottom: 12px; }
.ef-cond-row { display: flex; gap: 8px; align-items: center; }
.ef-cond-select { padding: 6px 10px; background: #252545; border: 1px solid #3a3a5a; border-radius: 4px; color: #e0e0e0; font-size: 12px; outline: none; }
.ef-cond-select--op { max-width: 140px; }
.ef-cond-input { padding: 6px 10px; background: #252545; border: 1px solid #3a3a5a; border-radius: 4px; color: #e0e0e0; font-size: 12px; outline: none; flex: 1; }
.ef-cond-input:focus { border-color: #4fc3f7; }
.ef-cond-remove { background: none; border: none; color: #f44336; cursor: pointer; padding: 4px; font-size: 16px; }
.ef-cond-resultados { display: flex; flex-direction: column; gap: 8px; margin: 12px 0; }
.ef-cond-resultado { display: flex; align-items: center; gap: 8px; }
.ef-cond-resultado label { font-size: 12px; color: #aaa; min-width: 90px; }
.ef-cond-preview { margin: 12px 0; }
.ef-cond-preview label { font-size: 11px; color: #888; display: block; margin-bottom: 4px; }
.ef-cond-code { display: block; padding: 8px 12px; background: #0d0d1a; border-radius: 4px; font-size: 12px; color: #4fc3f7; word-break: break-all; }
.ef-inferior { border-top: 1px solid #2a2a4a; display: flex; flex-direction: column; max-height: 200px; }
.ef-inferior-abas { display: flex; border-bottom: 1px solid #2a2a4a; }
.ef-inferior-aba { padding: 6px 12px; background: none; border: none; color: #888; cursor: pointer; font-size: 11px; }
.ef-inferior-aba.ativa { color: #4fc3f7; border-bottom: 2px solid #4fc3f7; }
.ef-variaveis { padding: 8px 12px; overflow-y: auto; }
.ef-var-lista { display: flex; flex-wrap: wrap; gap: 6px; }
.ef-var-tag { padding: 3px 8px; background: #252545; border-radius: 4px; font-size: 11px; color: #4fc3f7; font-family: monospace; }
.ef-testar { padding: 8px 12px; overflow-y: auto; }
.ef-resultado-teste { margin-top: 8px; padding: 8px 12px; background: #0d0d1a; border-radius: 4px; font-size: 12px; color: #e0e0e0; font-family: monospace; word-break: break-all; }
.ef-btn { padding: 6px 14px; border: none; border-radius: 6px; cursor: pointer; font-size: 12px; display: inline-flex; align-items: center; gap: 6px; transition: all 0.2s; }
.ef-btn--salvar { background: #4fc3f7; color: #000; font-weight: 600; }
.ef-btn--salvar:hover { background: #81d4fa; }
.ef-btn--fechar { background: none; color: #888; font-size: 18px; padding: 4px 8px; }
.ef-btn--fechar:hover { color: #e0e0e0; }
.ef-btn--add { background: #252545; color: #4fc3f7; border: 1px dashed #3a3a5a; }
.ef-btn--add:hover { background: #2a2a5a; }
.ef-btn--aplicar { background: #4fc3f7; color: #000; font-weight: 600; margin-top: 12px; }
.ef-btn--aplicar:disabled { opacity: 0.4; cursor: not-allowed; }
.ef-btn--testar { background: #4caf50; color: #fff; font-weight: 600; }
.ef-btn--testar:disabled { opacity: 0.4; cursor: not-allowed; }

/* Problem 3: Navigation styles */
.ef-nav-header { display: flex; align-items: center; gap: 8px; padding: 6px 8px; border-bottom: 1px solid #2a2a4a; }
.ef-nav-back { background: none; border: 1px solid #3a3a5a; border-radius: 4px; color: #ccc; cursor: pointer; padding: 2px 6px; font-size: 14px; }
.ef-nav-back:hover { background: #252545; }
.ef-nav-path { font-size: 11px; color: #4fc3f7; }
.ef-campo-row { display: flex; align-items: center; }
.ef-campo-row .ef-campo-item { flex: 1; }
.ef-campo-seta { background: none; border: none; color: #888; cursor: pointer; padding: 6px; font-size: 16px; border-radius: 4px; }
.ef-campo-seta:hover { background: #4fc3f7; color: #000; }

/* Problem 4: Test variable input styles */
.ef-teste-vars { display: flex; flex-direction: column; gap: 6px; margin-bottom: 10px; }
.ef-teste-var-row { display: flex; align-items: center; gap: 8px; }
.ef-teste-var-label { font-size: 12px; font-family: monospace; color: #4fc3f7; min-width: 150px; }
.ef-teste-var-input { flex: 1; padding: 6px 10px; background: #252545; border: 1px solid #3a3a5a; border-radius: 4px; color: #e0e0e0; font-size: 12px; outline: none; }
.ef-teste-var-input:focus { border-color: #4fc3f7; }

@media (max-width: 768px) {
  .ef-modal { width: 100vw; height: 100vh; max-width: 100vw; border-radius: 0; resize: none; }
  .ef-body { flex-direction: column; }
  .ef-sidebar { width: 100%; max-height: 200px; border-right: none; border-bottom: 1px solid #2a2a4a; }
  .ef-cond-row { flex-wrap: wrap; }
}
</style>

<!-- Problem 2: Non-scoped styles for Monaco widgets that render outside component -->
<style>
.monaco-editor .monaco-hover { z-index: 2100 !important; }
.monaco-editor .suggest-widget { z-index: 2100 !important; }
.monaco-editor .parameter-hints-widget { z-index: 2100 !important; }
</style>
