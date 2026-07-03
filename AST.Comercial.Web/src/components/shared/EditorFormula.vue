<script setup lang="ts">
import { ref, computed, onMounted, watch, nextTick } from 'vue'
import { odataGet } from '@/services/api'

interface CampoEsquema {
  nome: string
  tipo: string
  variavel: string
  grupo: 'fixo' | 'personalizado'
}

interface Condicao {
  campo: string
  operador: string
  valor: string
}

const props = defineProps<{
  modelValue: string
  entidade: string
  titulo?: string
}>()

const emit = defineEmits<{
  'update:modelValue': [v: string]
  fechar: []
  salvar: [v: string]
}>()

const abaAtiva = ref<'simples' | 'condicional'>('simples')
const codigo = ref(props.modelValue || '')
const camposFixos = ref<CampoEsquema[]>([])
const camposPersonalizados = ref<CampoEsquema[]>([])
const buscaCampo = ref('')
const mostrarCampos = ref(false)
const mostrarSnippets = ref(false)
const abaInferior = ref<'variaveis' | 'testar'>('variaveis')
const resultadoTeste = ref('')
const carregando = ref(false)
const editorRef = ref<HTMLTextAreaElement | null>(null)

// Condicional builder
const condicoes = ref<Condicao[]>([{ campo: '', operador: 'igual', valor: '' }])
const resultadoVerdadeiro = ref('')
const resultadoFalso = ref('')

const operadores = [
  { valor: 'igual', label: 'Igual a', js: '===' },
  { valor: 'diferente', label: 'Diferente de', js: '!==' },
  { valor: 'maior', label: 'Maior que', js: '>' },
  { valor: 'menor', label: 'Menor que', js: '<' },
  { valor: 'contem', label: 'Contém', js: '.includes' },
  { valor: 'vazio', label: 'Vazio', js: 'vazio' },
  { valor: 'naoVazio', label: 'Não vazio', js: 'naoVazio' },
]

const snippetsJS = computed(() => {
  const primeiroCampo = camposFixos.value[0]?.nome || 'Campo'
  const segundoCampo = camposFixos.value[1]?.nome || 'Campo2'
  return [
    { label: 'Soma', code: `$registro.${primeiroCampo} + $registro.${segundoCampo}` },
    { label: 'Percentual', code: `$registro.${primeiroCampo} * 0.10` },
    { label: 'Se/Senão', code: `$registro.${primeiroCampo} === 'valor' ? 'resultado' : 'alternativo'` },
    { label: 'Concatenar', code: `\`\${$registro.${primeiroCampo}} - \${$registro.${segundoCampo}}\`` },
    { label: 'Maior que', code: `$registro.${primeiroCampo} > 0` },
    { label: 'Contém texto', code: `$registro.${primeiroCampo}.includes('texto')` },
    { label: 'Data hoje', code: `new Date().toISOString().split('T')[0]` },
    { label: 'Arredondar', code: `Math.round($registro.${primeiroCampo} * 100) / 100` },
  ]
})

const linhas = computed(() => {
  const qtd = (codigo.value || '').split('\n').length
  return Array.from({ length: Math.max(qtd, 10) }, (_, i) => i + 1)
})

const todosCampos = computed(() => [...camposFixos.value, ...camposPersonalizados.value])

const camposFiltrados = computed(() => {
  if (!buscaCampo.value) return todosCampos.value
  const b = buscaCampo.value.toLowerCase()
  return todosCampos.value.filter(c => c.nome.toLowerCase().includes(b) || c.variavel.toLowerCase().includes(b))
})

const variaveisUsadas = computed(() => {
  const regex = /\$(?:registro|campos|usuario|empresa)\.[\w.]+/g
  const matches = codigo.value.match(regex)
  return [...new Set(matches || [])]
})

const codigoCondicional = computed(() => {
  const condsValidas = condicoes.value.filter(c => c.campo && c.operador)
  if (!condsValidas.length) return ''

  const partes = condsValidas.map(c => {
    const campo = c.campo
    if (c.operador === 'vazio') return `(${campo} == null || ${campo} === '')`
    if (c.operador === 'naoVazio') return `(${campo} != null && ${campo} !== '')`
    if (c.operador === 'contem') return `${campo}.includes('${c.valor}')`
    const op = operadores.find(o => o.valor === c.operador)
    const jsOp = op?.js || '==='
    const val = isNaN(Number(c.valor)) ? `'${c.valor}'` : c.valor
    return `${campo} ${jsOp} ${val}`
  })

  const cond = partes.length > 1 ? `(${partes.join(' && ')})` : partes[0]
  const verd = isNaN(Number(resultadoVerdadeiro.value)) ? `'${resultadoVerdadeiro.value}'` : resultadoVerdadeiro.value
  const falso = isNaN(Number(resultadoFalso.value)) ? `'${resultadoFalso.value}'` : resultadoFalso.value

  return `${cond} ? ${verd || "''"} : ${falso || "''"}`
})

async function carregarEsquema() {
  carregando.value = true
  try {
    const token = localStorage.getItem('token')
    const res = await fetch(`/odata/Campos@Esquema(${props.entidade})`, {
      headers: { 'Authorization': `Bearer ${token}` }
    })
    if (res.ok) {
      const schema = await res.json()
      const propsRegistro = schema['$registro']?.Propriedades ?? {}
      camposFixos.value = Object.entries(propsRegistro)
        .filter(([nome]) => !['SenhaHash', 'ChaveExterna', 'Id', 'EmpresaId', 'CriadoEm', 'AtualizadoEm', 'Ativo'].includes(nome))
        .map(([nome, info]: [string, any]) => ({
          nome,
          tipo: info.Tipo || 'string',
          variavel: `$registro.${nome}`,
          grupo: 'fixo' as const
        }))
    }
  } catch { /* schema load failed silently */ }

  try {
    const res = await odataGet<any>('Campos', {
      filter: `EntidadeAlvo eq '${props.entidade}'`,
      select: 'Id,Nome,Chave,Tipo',
      orderby: 'Nome asc',
      top: 100
    })
    camposPersonalizados.value = res.value.map((c: any) => ({
      nome: c.Nome,
      tipo: c.Tipo ?? 'Texto',
      variavel: `$campos.${c.Chave}`,
      grupo: 'personalizado' as const
    }))
  } catch { camposPersonalizados.value = [] }

  carregando.value = false
}

function inserirCampo(campo: CampoEsquema) {
  inserirNoCursor(campo.variavel)
  mostrarCampos.value = false
}

function inserirSnippet(code: string) {
  inserirNoCursor(code)
  mostrarSnippets.value = false
}

function inserirNoCursor(texto: string) {
  const el = editorRef.value
  if (!el) { codigo.value += texto; return }
  const start = el.selectionStart
  const end = el.selectionEnd
  codigo.value = codigo.value.substring(0, start) + texto + codigo.value.substring(end)
  nextTick(() => { el.selectionStart = el.selectionEnd = start + texto.length; el.focus() })
}

function adicionarCondicao() {
  condicoes.value.push({ campo: '', operador: 'igual', valor: '' })
}

function removerCondicao(idx: number) {
  condicoes.value.splice(idx, 1)
  if (!condicoes.value.length) condicoes.value.push({ campo: '', operador: 'igual', valor: '' })
}

function limpar() {
  if (abaAtiva.value === 'simples') {
    codigo.value = ''
  } else {
    condicoes.value = [{ campo: '', operador: 'igual', valor: '' }]
    resultadoVerdadeiro.value = ''
    resultadoFalso.value = ''
  }
}

function salvar() {
  const formulaFinal = abaAtiva.value === 'simples' ? codigo.value : codigoCondicional.value
  emit('salvar', formulaFinal)
  emit('update:modelValue', formulaFinal)
}

function testarFormula() {
  try {
    const mockRegistro: Record<string, any> = {}
    camposFixos.value.forEach(c => {
      if (c.tipo === 'number') mockRegistro[c.nome] = 1000
      else if (c.tipo === 'boolean') mockRegistro[c.nome] = true
      else if (c.tipo === 'datetime') mockRegistro[c.nome] = '2024-01-15'
      else mockRegistro[c.nome] = `Teste_${c.nome}`
    })
    const $registro = mockRegistro
    const $campos: Record<string, any> = {}
    camposPersonalizados.value.forEach(c => { $campos[c.variavel.replace('$campos.', '')] = 'valor_teste' })
    const $usuario = { Nome: 'Admin', Email: 'admin@empresa.com', Perfil: 'Administrador' }
    const $empresa = { Nome: 'Empresa Teste' }
    const fn = new Function('$registro', '$campos', '$usuario', '$empresa', `return ${codigo.value}`)
    const resultado = fn($registro, $campos, $usuario, $empresa)
    resultadoTeste.value = `✓ Resultado: ${JSON.stringify(resultado)}`
  } catch (e: any) {
    resultadoTeste.value = `✗ Erro: ${e.message}`
  }
}

function iconeParaTipo(tipo: string): string {
  const map: Record<string, string> = {
    string: 'mdi-format-text', number: 'mdi-numeric', boolean: 'mdi-checkbox-marked-outline',
    datetime: 'mdi-calendar', Texto: 'mdi-format-text', TextoLongo: 'mdi-text-long',
    Numero: 'mdi-numeric', Decimal: 'mdi-decimal', Moeda: 'mdi-currency-usd',
    Lista: 'mdi-format-list-bulleted', MultiLista: 'mdi-format-list-checks',
    Data: 'mdi-calendar', DataHora: 'mdi-clock-outline', Booleano: 'mdi-checkbox-marked-outline',
    Email: 'mdi-email-outline', Telefone: 'mdi-phone-outline'
  }
  return map[tipo] || 'mdi-code-braces'
}

watch(() => props.modelValue, (v) => { codigo.value = v || '' })
onMounted(carregarEsquema)
</script>

<template>
  <div class="ef-overlay" @click.self="emit('fechar')">
    <div class="ef-modal">
      <div class="ef-header">
        <h3>{{ titulo || 'Editor de Fórmula' }}</h3>
        <button class="ef-close" @click="emit('fechar')"><i class="mdi mdi-close"></i></button>
      </div>

      <div class="ef-tabs">
        <button :class="{ active: abaAtiva === 'simples' }" @click="abaAtiva = 'simples'">
          <i class="mdi mdi-code-braces"></i> Fórmula simples
        </button>
        <button :class="{ active: abaAtiva === 'condicional' }" @click="abaAtiva = 'condicional'">
          <i class="mdi mdi-swap-horizontal"></i> Fórmula condicional
        </button>
      </div>

      <!-- ABA SIMPLES -->
      <div v-if="abaAtiva === 'simples'" class="ef-body">
        <div class="ef-editor-area">
          <div class="ef-editor-wrap">
            <div class="ef-line-numbers">
              <span v-for="n in linhas" :key="n">{{ n }}</span>
            </div>
            <textarea
              ref="editorRef"
              v-model="codigo"
              class="ef-code"
              spellcheck="false"
              placeholder="// Digite sua fórmula JavaScript aqui..."
            ></textarea>
          </div>
          <div class="ef-sidebar-btns">
            <div class="ef-dd-wrap">
              <button class="ef-sb-btn" @click="mostrarCampos = !mostrarCampos; mostrarSnippets = false">
                <i class="mdi mdi-code-tags"></i> Campos <i class="mdi mdi-chevron-down"></i>
              </button>
              <div v-if="mostrarCampos" class="ef-dd">
                <div class="ef-dd-head">
                  <span>Campos — {{ entidade }}</span>
                  <button @click="mostrarCampos = false"><i class="mdi mdi-close"></i></button>
                </div>
                <input v-model="buscaCampo" class="ef-dd-search" placeholder="Buscar campo..." />
                <div class="ef-dd-list">
                  <div v-if="carregando" class="ef-dd-empty">Carregando...</div>
                  <template v-else-if="camposFiltrados.length">
                    <div v-if="camposFixos.length && !buscaCampo" class="ef-dd-group">Campos fixos ($registro)</div>
                    <button
                      v-for="c in camposFiltrados.filter(x => x.grupo === 'fixo')"
                      :key="c.variavel"
                      class="ef-dd-item"
                      @click="inserirCampo(c)"
                    >
                      <i class="mdi" :class="iconeParaTipo(c.tipo)"></i>
                      <span class="ef-dd-item-nome">{{ c.nome }}</span>
                      <span class="ef-dd-item-tipo">{{ c.tipo }}</span>
                    </button>
                    <div v-if="camposPersonalizados.length && !buscaCampo" class="ef-dd-group">Personalizados ($campos)</div>
                    <button
                      v-for="c in camposFiltrados.filter(x => x.grupo === 'personalizado')"
                      :key="c.variavel"
                      class="ef-dd-item"
                      @click="inserirCampo(c)"
                    >
                      <i class="mdi" :class="iconeParaTipo(c.tipo)"></i>
                      <span class="ef-dd-item-nome">{{ c.nome }}</span>
                      <span class="ef-dd-item-tipo">custom</span>
                    </button>
                  </template>
                  <div v-else class="ef-dd-empty">Nenhum campo encontrado</div>
                </div>
              </div>
            </div>
            <div class="ef-dd-wrap">
              <button class="ef-sb-btn" @click="mostrarSnippets = !mostrarSnippets; mostrarCampos = false">
                <i class="mdi mdi-lightning-bolt"></i> Códigos JS <i class="mdi mdi-chevron-down"></i>
              </button>
              <div v-if="mostrarSnippets" class="ef-dd">
                <div class="ef-dd-head">
                  <span>Snippets</span>
                  <button @click="mostrarSnippets = false"><i class="mdi mdi-close"></i></button>
                </div>
                <div class="ef-dd-list">
                  <button
                    v-for="s in snippetsJS"
                    :key="s.label"
                    class="ef-dd-item ef-dd-snippet"
                    @click="inserirSnippet(s.code)"
                  >
                    <i class="mdi mdi-lightning-bolt"></i>
                    <div class="ef-dd-snippet-info">
                      <span>{{ s.label }}</span>
                      <code>{{ s.code }}</code>
                    </div>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Bottom panel -->
        <div class="ef-bottom">
          <div class="ef-btabs">
            <button :class="{ active: abaInferior === 'variaveis' }" @click="abaInferior = 'variaveis'">
              <i class="mdi mdi-variable"></i> Variáveis da fórmula
            </button>
            <button :class="{ active: abaInferior === 'testar' }" @click="abaInferior = 'testar'">
              <i class="mdi mdi-play-circle-outline"></i> Testar fórmula
            </button>
          </div>
          <div v-if="abaInferior === 'variaveis'" class="ef-vars">
            <table v-if="variaveisUsadas.length">
              <thead><tr><th>Variável</th><th>Referência</th></tr></thead>
              <tbody>
                <tr v-for="v in variaveisUsadas" :key="v">
                  <td class="ef-var-mono">{{ v }}</td>
                  <td>{{ v.startsWith('$campos') ? 'Campo personalizado' : 'Campo fixo' }}</td>
                </tr>
              </tbody>
            </table>
            <p v-else class="ef-vars-empty">Nenhuma variável utilizada ainda.</p>
          </div>
          <div v-if="abaInferior === 'testar'" class="ef-test">
            <p class="ef-test-info">Executa a fórmula com dados fictícios baseados nos campos da entidade.</p>
            <button class="ef-btn-test" @click="testarFormula"><i class="mdi mdi-play"></i> Executar teste</button>
            <div v-if="resultadoTeste" class="ef-result" :class="{ erro: resultadoTeste.startsWith('✗') }">{{ resultadoTeste }}</div>
          </div>
        </div>
      </div>

      <!-- ABA CONDICIONAL -->
      <div v-if="abaAtiva === 'condicional'" class="ef-body">
        <p class="ef-cond-desc">
          Monte a lógica condicional selecionando campos, operadores e valores. O código será gerado automaticamente.
        </p>

        <div class="ef-cond-section">
          <div class="ef-cond-label">Se todas as condições forem verdadeiras:</div>
          <div class="ef-cond-list">
            <div v-for="(cond, idx) in condicoes" :key="idx" class="ef-cond-row">
              <select v-model="cond.campo" class="ef-cond-select ef-cond-campo">
                <option value="" disabled>Campo...</option>
                <optgroup label="Campos fixos ($registro)">
                  <option v-for="c in camposFixos" :key="c.variavel" :value="c.variavel">{{ c.nome }}</option>
                </optgroup>
                <optgroup v-if="camposPersonalizados.length" label="Personalizados ($campos)">
                  <option v-for="c in camposPersonalizados" :key="c.variavel" :value="c.variavel">{{ c.nome }}</option>
                </optgroup>
              </select>
              <select v-model="cond.operador" class="ef-cond-select ef-cond-op">
                <option v-for="op in operadores" :key="op.valor" :value="op.valor">{{ op.label }}</option>
              </select>
              <input
                v-if="cond.operador !== 'vazio' && cond.operador !== 'naoVazio'"
                v-model="cond.valor"
                class="ef-cond-input"
                placeholder="Valor..."
              />
              <div v-else class="ef-cond-input ef-cond-input-disabled"></div>
              <button class="ef-cond-remove" @click="removerCondicao(idx)" title="Remover">
                <i class="mdi mdi-close-circle-outline"></i>
              </button>
            </div>
          </div>
          <button class="ef-cond-add" @click="adicionarCondicao">
            <i class="mdi mdi-plus-circle-outline"></i> Adicionar condição
          </button>
        </div>

        <div class="ef-cond-results">
          <div class="ef-cond-result-row">
            <label>Então retorna:</label>
            <input v-model="resultadoVerdadeiro" class="ef-cond-result-input" placeholder="Valor quando verdadeiro..." />
          </div>
          <div class="ef-cond-result-row">
            <label>Senão retorna:</label>
            <input v-model="resultadoFalso" class="ef-cond-result-input" placeholder="Valor quando falso..." />
          </div>
        </div>

        <div class="ef-cond-preview">
          <div class="ef-cond-preview-label"><i class="mdi mdi-code-braces"></i> Código gerado:</div>
          <code class="ef-cond-preview-code">{{ codigoCondicional || '// Preencha os campos acima' }}</code>
        </div>
      </div>

      <!-- FOOTER -->
      <div class="ef-footer">
        <button class="ef-btn-limpar" @click="limpar">
          <i class="mdi mdi-eraser"></i> Limpar fórmula
        </button>
        <div style="flex:1"></div>
        <button class="ef-btn-salvar" @click="salvar">
          <i class="mdi mdi-content-save"></i> Salvar fórmula
        </button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.ef-overlay { position: fixed; inset: 0; background: rgba(0,0,0,.7); z-index: 2000; display: flex; align-items: center; justify-content: center; }
.ef-modal { background: var(--bg-primary); border: 1px solid var(--border); border-radius: 14px; width: 920px; max-width: 95vw; max-height: 90vh; display: flex; flex-direction: column; box-shadow: 0 24px 64px rgba(0,0,0,.5); }
.ef-header { display: flex; align-items: center; justify-content: space-between; padding: 18px 24px; border-bottom: 1px solid var(--border); }
.ef-header h3 { font-size: 17px; font-weight: 600; color: var(--text-primary); margin: 0; }
.ef-close { background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 22px; padding: 4px; border-radius: 6px; }
.ef-close:hover { color: var(--text-primary); background: var(--bg-elevated); }

.ef-tabs { display: flex; gap: 0; padding: 0 24px; border-bottom: 1px solid var(--border); }
.ef-tabs button { display: flex; align-items: center; gap: 6px; padding: 12px 20px; border: 1px solid var(--border); border-bottom: none; background: var(--bg-surface); color: var(--text-secondary); font-size: 13px; cursor: pointer; border-radius: 8px 8px 0 0; margin-bottom: -1px; }
.ef-tabs button.active { background: var(--bg-primary); color: var(--text-primary); font-weight: 600; border-bottom: 1px solid var(--bg-primary); }
.ef-tabs button .mdi { font-size: 15px; }

.ef-body { flex: 1; overflow-y: auto; padding: 20px 24px; display: flex; flex-direction: column; gap: 16px; }

.ef-editor-area { display: flex; gap: 12px; }
.ef-editor-wrap { flex: 1; display: flex; border: 1px solid var(--border); border-radius: 10px; overflow: hidden; min-height: 220px; background: var(--bg-surface); }
.ef-line-numbers { padding: 12px 0; width: 40px; text-align: right; display: flex; flex-direction: column; user-select: none; border-right: 1px solid var(--border); background: var(--bg-secondary); }
.ef-line-numbers span { font-size: 12px; line-height: 20px; color: var(--text-muted); padding-right: 8px; font-family: 'Fira Code', monospace; }
.ef-code { flex: 1; border: none; background: transparent; padding: 12px 14px; color: var(--text-primary); font-size: 13px; font-family: 'Fira Code', 'Cascadia Code', monospace; line-height: 20px; resize: none; outline: none; min-height: 220px; }
.ef-code::placeholder { color: var(--text-muted); }

.ef-sidebar-btns { display: flex; flex-direction: column; gap: 8px; flex-shrink: 0; }
.ef-dd-wrap { position: relative; }
.ef-sb-btn { display: flex; align-items: center; gap: 6px; padding: 8px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-secondary); font-size: 12px; cursor: pointer; white-space: nowrap; }
.ef-sb-btn:hover { background: var(--bg-elevated); color: var(--text-primary); }

.ef-dd { position: absolute; top: 0; right: 100%; margin-right: 8px; width: 300px; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 12px; box-shadow: 0 12px 40px rgba(0,0,0,.4); z-index: 10; display: flex; flex-direction: column; max-height: 380px; }
.ef-dd-head { display: flex; align-items: center; justify-content: space-between; padding: 12px 14px; border-bottom: 1px solid var(--border); }
.ef-dd-head span { font-size: 14px; font-weight: 600; color: var(--text-primary); }
.ef-dd-head button { background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 18px; }
.ef-dd-search { border: none; border-bottom: 1px solid var(--border); background: transparent; padding: 10px 14px; color: var(--text-primary); font-size: 13px; outline: none; }
.ef-dd-search::placeholder { color: var(--text-muted); }
.ef-dd-list { flex: 1; overflow-y: auto; padding: 6px; }
.ef-dd-group { padding: 8px 12px 4px; font-size: 11px; font-weight: 700; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.5px; }
.ef-dd-item { display: flex; align-items: center; gap: 10px; width: 100%; padding: 9px 12px; border: none; background: transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; border-radius: 6px; text-align: left; }
.ef-dd-item:hover { background: var(--bg-elevated); color: var(--text-primary); }
.ef-dd-item .mdi { font-size: 16px; color: var(--accent); opacity: .7; width: 20px; text-align: center; flex-shrink: 0; }
.ef-dd-item-nome { flex: 1; }
.ef-dd-item-tipo { font-size: 11px; color: var(--text-muted); }
.ef-dd-snippet { align-items: flex-start; }
.ef-dd-snippet-info { display: flex; flex-direction: column; gap: 3px; overflow: hidden; }
.ef-dd-snippet-info span { font-size: 13px; color: var(--text-primary); }
.ef-dd-snippet-info code { font-size: 11px; color: var(--text-muted); font-family: monospace; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; max-width: 200px; }
.ef-dd-empty { padding: 20px; text-align: center; color: var(--text-muted); font-size: 13px; }

.ef-bottom { border: 1px solid var(--border); border-radius: 10px; overflow: hidden; }
.ef-btabs { display: flex; border-bottom: 1px solid var(--border); }
.ef-btabs button { flex: 1; display: flex; align-items: center; justify-content: center; gap: 6px; padding: 10px 16px; border: none; background: var(--bg-surface); color: var(--text-muted); font-size: 12px; font-weight: 500; cursor: pointer; border-bottom: 2px solid transparent; }
.ef-btabs button.active { color: var(--accent); border-bottom-color: var(--accent); background: var(--bg-primary); }
.ef-vars { padding: 12px 16px; min-height: 80px; }
.ef-vars table { width: 100%; font-size: 12px; border-collapse: collapse; }
.ef-vars th { text-align: left; color: var(--text-muted); font-weight: 600; padding: 6px 8px; border-bottom: 1px solid var(--border); }
.ef-vars td { padding: 6px 8px; color: var(--text-secondary); border-bottom: 1px solid var(--border); }
.ef-var-mono { font-family: monospace; color: var(--accent); font-size: 12px; }
.ef-vars-empty { color: var(--text-muted); font-size: 13px; text-align: center; padding: 20px; margin: 0; }
.ef-test { padding: 12px 16px; display: flex; flex-direction: column; gap: 10px; }
.ef-test-info { font-size: 12px; color: var(--text-muted); margin: 0; }
.ef-btn-test { display: flex; align-items: center; gap: 6px; padding: 8px 16px; border: 1px solid var(--accent); border-radius: 8px; background: transparent; color: var(--accent); font-size: 13px; cursor: pointer; width: fit-content; }
.ef-btn-test:hover { background: rgba(6,182,212,.08); }
.ef-result { padding: 10px 14px; border-radius: 8px; font-size: 13px; font-family: monospace; background: rgba(34,197,94,.08); color: #22c55e; border: 1px solid rgba(34,197,94,.2); }
.ef-result.erro { background: rgba(239,68,68,.08); color: #ef4444; border-color: rgba(239,68,68,.2); }

/* Condicional tab */
.ef-cond-desc { font-size: 13px; color: var(--text-secondary); margin: 0; line-height: 1.5; }
.ef-cond-section { display: flex; flex-direction: column; gap: 12px; }
.ef-cond-label { font-size: 13px; font-weight: 600; color: var(--text-primary); }
.ef-cond-list { display: flex; flex-direction: column; gap: 8px; }
.ef-cond-row { display: flex; gap: 8px; align-items: center; }
.ef-cond-select { padding: 8px 12px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-primary); font-size: 13px; outline: none; }
.ef-cond-select:focus { border-color: var(--accent); }
.ef-cond-campo { flex: 2; min-width: 0; }
.ef-cond-op { flex: 1.2; min-width: 0; }
.ef-cond-input { flex: 1.5; padding: 8px 12px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-primary); font-size: 13px; outline: none; }
.ef-cond-input:focus { border-color: var(--accent); }
.ef-cond-input-disabled { opacity: 0.4; pointer-events: none; }
.ef-cond-remove { background: none; border: none; color: var(--text-muted); cursor: pointer; font-size: 18px; padding: 4px; border-radius: 6px; flex-shrink: 0; }
.ef-cond-remove:hover { color: #ef4444; background: rgba(239,68,68,.08); }
.ef-cond-add { display: flex; align-items: center; gap: 6px; padding: 8px 14px; border: 1px dashed var(--border); border-radius: 8px; background: transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; width: fit-content; }
.ef-cond-add:hover { border-color: var(--accent); color: var(--accent); }

.ef-cond-results { display: flex; flex-direction: column; gap: 10px; padding: 16px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; }
.ef-cond-result-row { display: flex; align-items: center; gap: 12px; }
.ef-cond-result-row label { font-size: 13px; font-weight: 600; color: var(--text-secondary); white-space: nowrap; min-width: 110px; }
.ef-cond-result-input { flex: 1; padding: 8px 12px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-primary); font-size: 13px; outline: none; }
.ef-cond-result-input:focus { border-color: var(--accent); }

.ef-cond-preview { padding: 14px; background: var(--bg-secondary); border: 1px solid var(--border); border-radius: 10px; }
.ef-cond-preview-label { display: flex; align-items: center; gap: 6px; font-size: 12px; font-weight: 600; color: var(--text-muted); margin-bottom: 8px; }
.ef-cond-preview-code { display: block; font-size: 12px; font-family: 'Fira Code', monospace; color: var(--accent); line-height: 1.6; word-break: break-all; }

.ef-footer { display: flex; align-items: center; gap: 12px; padding: 16px 24px; border-top: 1px solid var(--border); }
.ef-btn-limpar { display: flex; align-items: center; gap: 6px; padding: 8px 16px; border: 1px solid var(--border); border-radius: 8px; background: transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.ef-btn-limpar:hover { background: var(--bg-elevated); color: var(--text-primary); }
.ef-btn-salvar { display: flex; align-items: center; gap: 6px; padding: 10px 24px; border: none; border-radius: 8px; background: var(--accent); color: #000; font-size: 14px; font-weight: 600; cursor: pointer; }
.ef-btn-salvar:hover { background: var(--accent-hover); }
</style>
