<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref, watch } from 'vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import { useToast } from '@/composables/useToast'
import {
  ENTIDADES, LIMITE_BYTES, ROTULO_STATUS, TOM_STATUS,
  analisar, baixarErros, baixarModelo, buscar, executar, formatarBytes, validar,
  type AnaliseImportacao, type EntidadeImportavel, type Importacao, type ModoDuplicidade, type ValidacaoImportacao,
} from './importacaoApi'

const emit = defineEmits<{ concluida: [importacao: Importacao] }>()
const toast = useToast()

type Passo = 'entidade' | 'arquivo' | 'mapear' | 'validar' | 'importar'
const PASSOS: { chave: Passo; rotulo: string }[] = [
  { chave: 'entidade', rotulo: 'Entidade' },
  { chave: 'arquivo', rotulo: 'Arquivo' },
  { chave: 'mapear', rotulo: 'Colunas' },
  { chave: 'validar', rotulo: 'Validação' },
  { chave: 'importar', rotulo: 'Importação' },
]

const passo = ref<Passo>('entidade')
const entidade = ref<EntidadeImportavel>('Cliente')
const arquivo = ref<File | null>(null)
const analise = ref<AnaliseImportacao | null>(null)
const mapa = reactive<Record<number, string>>({})
const validacao = ref<ValidacaoImportacao | null>(null)

const baixandoModelo = ref(false)
const analisando = ref(false)
const validando = ref(false)
const erroArquivo = ref('')
const erroValidacao = ref('')
const arrastando = ref(false)
const entradaArquivo = ref<HTMLInputElement | null>(null)
const mostrarPrevia = ref(false)
const errosVisiveis = ref(50)

const duplicidade = ref<ModoDuplicidade>('Permitir')
const chaveDuplicidade = ref('')
const duplicadas = ref<number | null>(null)
const checandoDuplicadas = ref(false)

const iniciando = ref(false)
const importacao = ref<Importacao | null>(null)
let temporizador: ReturnType<typeof setInterval> | null = null

const indicePasso = computed(() => PASSOS.findIndex((p) => p.chave === passo.value))
const entidadeAtual = computed(() => ENTIDADES.find((e) => e.chave === entidade.value)!)

// ---------------------------------------------------------------- passo 1: entidade e modelo

async function baixarModeloCsv() {
  baixandoModelo.value = true
  try {
    await baixarModelo(entidade.value)
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível baixar o modelo.')
  } finally {
    baixandoModelo.value = false
  }
}

function escolherEntidade(chave: EntidadeImportavel) {
  if (chave === entidade.value) return
  entidade.value = chave
  limparArquivo()
}

// ---------------------------------------------------------------- passo 2: arquivo

function abrirSeletor() {
  entradaArquivo.value?.click()
}

function aoEscolher(ev: Event) {
  const input = ev.target as HTMLInputElement
  const f = input.files?.[0]
  input.value = ''
  if (f) receberArquivo(f)
}

function aoSoltar(ev: DragEvent) {
  arrastando.value = false
  const arquivos = ev.dataTransfer?.files
  if (!arquivos || arquivos.length === 0) return
  if (arquivos.length > 1) {
    erroArquivo.value = 'Envie um arquivo por vez.'
    return
  }
  const f = arquivos[0]
  if (f) receberArquivo(f)
}

async function receberArquivo(f: File) {
  erroArquivo.value = ''
  if (!/\.csv$/i.test(f.name)) {
    erroArquivo.value = 'Formato não aceito. Envie um arquivo com extensão .csv.'
    return
  }
  if (f.size === 0) {
    erroArquivo.value = 'O arquivo está vazio.'
    return
  }
  if (f.size > LIMITE_BYTES) {
    erroArquivo.value = `O arquivo (${formatarBytes(f.size)}) excede o limite de 5 MB.`
    return
  }

  analisando.value = true
  try {
    const resultado = await analisar(f, entidade.value)
    arquivo.value = f
    analise.value = resultado
    validacao.value = null
    reiniciarMapa(resultado)
    duplicidade.value = 'Permitir'
    chaveDuplicidade.value = ''
    duplicadas.value = null
    importacao.value = null
  } catch (e) {
    arquivo.value = null
    analise.value = null
    erroArquivo.value = e instanceof Error ? e.message : 'Não foi possível ler o arquivo.'
  } finally {
    analisando.value = false
  }
}

function limparArquivo() {
  arquivo.value = null
  analise.value = null
  validacao.value = null
  erroArquivo.value = ''
  importacao.value = null
  for (const k of Object.keys(mapa)) delete mapa[Number(k)]
}

// ---------------------------------------------------------------- passo 3: mapeamento

const camposPorChave = computed(() => new Map((analise.value?.campos ?? []).map((c) => [c.chave, c])))
const camposNativos = computed(() => (analise.value?.campos ?? []).filter((c) => !c.personalizado))
const camposPersonalizados = computed(() => (analise.value?.campos ?? []).filter((c) => c.personalizado))
const camposUsados = computed(() => new Set(Object.values(mapa).filter(Boolean)))
const obrigatoriosFaltando = computed(() =>
  (analise.value?.campos ?? []).filter((c) => c.obrigatorio && !camposUsados.value.has(c.chave)),
)
const colunasMapeadas = computed(() => camposUsados.value.size)

function amostraDaColuna(i: number): string[] {
  return (analise.value?.amostra ?? []).map((l) => l[i] ?? '').filter((v) => v !== '').slice(0, 3)
}

/** Todas as colunas começam como Ignorar (valor vazio); a sugestão do servidor preenche as que reconheceu. */
function reiniciarMapa(a: AnaliseImportacao) {
  for (const k of Object.keys(mapa)) delete mapa[Number(k)]
  a.cabecalhos.forEach((_, i) => { mapa[i] = '' })
  for (const s of a.sugestao) mapa[s.coluna] = s.campo
}

function restaurarSugestao() {
  if (analise.value) reiniciarMapa(analise.value)
}

function ignorarTodas() {
  for (const k of Object.keys(mapa)) mapa[Number(k)] = ''
}

// ---------------------------------------------------------------- passo 4: validação

async function executarValidacao() {
  if (!arquivo.value) return
  validando.value = true
  erroValidacao.value = ''
  validacao.value = null
  errosVisiveis.value = 50
  try {
    validacao.value = await validar(arquivo.value, entidade.value, { ...mapa })
  } catch (e) {
    erroValidacao.value = e instanceof Error ? e.message : 'Não foi possível validar o arquivo.'
    toast.error(erroValidacao.value)
  } finally {
    validando.value = false
  }
}

// ---------------------------------------------------------------- passo 5: importação

const CAMPO_DA_CHAVE: Record<string, string> = { email: 'Email', documento: 'Documento', codigo: 'Codigo' }
const chavesDisponiveis = computed(() =>
  (analise.value?.chavesDuplicidade ?? []).filter((k) => {
    const campo = k.chave === 'nome' ? (entidade.value === 'Negocio' ? 'Titulo' : 'Nome') : CAMPO_DA_CHAVE[k.chave]
    return campo ? camposUsados.value.has(campo) : false
  }),
)

watch([duplicidade, chaveDuplicidade], async () => {
  duplicadas.value = null
  if (duplicidade.value === 'Permitir' || !chaveDuplicidade.value || !arquivo.value) return
  checandoDuplicadas.value = true
  try {
    const r = await validar(arquivo.value, entidade.value, { ...mapa }, chaveDuplicidade.value)
    duplicadas.value = r.duplicadas
  } catch {
    duplicadas.value = null
  } finally {
    checandoDuplicadas.value = false
  }
})

watch(duplicidade, (modo) => {
  if (modo !== 'Permitir' && !chaveDuplicidade.value) chaveDuplicidade.value = chavesDisponiveis.value[0]?.chave ?? ''
})

const podeImportar = computed(() =>
  !!validacao.value && validacao.value.validas > 0 && !iniciando.value && !importacao.value
  && (duplicidade.value === 'Permitir' || !!chaveDuplicidade.value),
)

const emAndamento = computed(() => importacao.value?.status === 'Processando' || importacao.value?.status === 'Pendente')
const percentual = computed(() => {
  const imp = importacao.value
  if (!imp || imp.totalLinhas === 0) return 0
  return Math.min(100, Math.round((imp.processadas / imp.totalLinhas) * 100))
})

async function iniciarImportacao() {
  if (!arquivo.value || !podeImportar.value) return
  iniciando.value = true
  try {
    const chave = duplicidade.value === 'Permitir' ? undefined : chaveDuplicidade.value
    importacao.value = await executar(arquivo.value, entidade.value, { ...mapa }, duplicidade.value, chave)
    acompanhar()
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível iniciar a importação.')
  } finally {
    iniciando.value = false
  }
}

function pararAcompanhamento() {
  if (temporizador) clearInterval(temporizador)
  temporizador = null
}

function acompanhar() {
  pararAcompanhamento()
  if (!importacao.value || !emAndamento.value) {
    finalizar()
    return
  }
  const id = importacao.value.id
  temporizador = setInterval(async () => {
    try {
      const atual = await buscar(id)
      importacao.value = atual
      if (atual.status !== 'Processando' && atual.status !== 'Pendente') {
        pararAcompanhamento()
        finalizar()
      }
    } catch {
      // falha momentânea de rede: tenta de novo no próximo ciclo
    }
  }, 800)
}

function finalizar() {
  if (importacao.value) emit('concluida', importacao.value)
}

async function baixarRelatorio() {
  if (!importacao.value) return
  try {
    await baixarErros(importacao.value.id)
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível baixar o relatório.')
  }
}

function novaImportacao() {
  pararAcompanhamento()
  limparArquivo()
  duplicidade.value = 'Permitir'
  chaveDuplicidade.value = ''
  duplicadas.value = null
  passo.value = 'entidade'
}

// ---------------------------------------------------------------- navegação

const podeAvancar = computed(() => {
  switch (passo.value) {
    case 'entidade': return true
    case 'arquivo': return !!analise.value && !analisando.value
    case 'mapear': return obrigatoriosFaltando.value.length === 0 && colunasMapeadas.value > 0
    case 'validar': return !!validacao.value && validacao.value.validas > 0 && !validando.value
    default: return false
  }
})

function avancar() {
  const prox = PASSOS[indicePasso.value + 1]
  if (!prox || !podeAvancar.value) return
  irPara(prox.chave)
}

function voltar() {
  const ant = PASSOS[indicePasso.value - 1]
  if (ant && !emAndamento.value && !importacao.value) irPara(ant.chave)
}

function irPara(destino: Passo) {
  passo.value = destino
  if (destino === 'validar') executarValidacao()
}

function podeIrParaPasso(i: number): boolean {
  if (emAndamento.value || importacao.value) return false
  return i < indicePasso.value
}

onBeforeUnmount(pararAcompanhamento)

const rotuloImportar = computed(() => {
  const n = validacao.value?.validas ?? 0
  return `Importar ${n.toLocaleString('pt-BR')} ${n === 1 ? 'linha' : 'linhas'}`
})
</script>

<template>
  <div class="imp-wizard">
    <ol class="imp-passos" aria-label="Etapas da importação">
      <li
        v-for="(p, i) in PASSOS"
        :key="p.chave"
        class="imp-passo"
        :class="{ ativo: p.chave === passo, feito: i < indicePasso }"
        :aria-current="p.chave === passo ? 'step' : undefined"
      >
        <button
          type="button"
          class="imp-passo-btn"
          :disabled="!podeIrParaPasso(i)"
          :aria-label="`Etapa ${i + 1}: ${p.rotulo}`"
          @click="irPara(p.chave)"
        >
          <span class="imp-passo-n"><i v-if="i < indicePasso" class="mdi mdi-check"></i><template v-else>{{ i + 1 }}</template></span>
          <span class="imp-passo-t">{{ p.rotulo }}</span>
        </button>
      </li>
    </ol>

    <!-- 1. Entidade -->
    <section v-if="passo === 'entidade'" class="imp-corpo" aria-labelledby="imp-h-entidade">
      <h3 id="imp-h-entidade" class="imp-titulo">O que você quer importar?</h3>
      <div class="imp-entidades" role="radiogroup" aria-labelledby="imp-h-entidade">
        <button
          v-for="e in ENTIDADES"
          :key="e.chave"
          type="button"
          role="radio"
          class="imp-entidade"
          :class="{ marcada: e.chave === entidade }"
          :aria-checked="e.chave === entidade"
          @click="escolherEntidade(e.chave)"
        >
          <i class="mdi" :class="e.icone"></i>
          <strong>{{ e.rotulo }}</strong>
          <span>{{ e.descricao }}</span>
        </button>
      </div>
      <div class="imp-dica">
        <i class="mdi mdi-information-outline"></i>
        <div>
          Arquivo CSV de até 5 MB e 10.000 linhas, com cabeçalho na primeira linha. Aceita UTF-8 ou Windows-1252 e separador
          ponto e vírgula ou vírgula. Referências (cliente, família, etapa, moeda) são feitas pelo nome já cadastrado.
        </div>
      </div>
      <div class="imp-acoes">
        <button type="button" class="imp-btn-sec" :disabled="baixandoModelo" @click="baixarModeloCsv">
          <i class="mdi mdi-download-outline"></i> {{ baixandoModelo ? 'Baixando…' : `Baixar modelo de ${entidadeAtual.rotulo.toLowerCase()}` }}
        </button>
        <button type="button" class="imp-btn" @click="avancar">Continuar <i class="mdi mdi-arrow-right"></i></button>
      </div>
    </section>

    <!-- 2. Arquivo -->
    <section v-else-if="passo === 'arquivo'" class="imp-corpo" aria-labelledby="imp-h-arquivo">
      <h3 id="imp-h-arquivo" class="imp-titulo">Envie o arquivo de {{ entidadeAtual.rotulo.toLowerCase() }}</h3>
      <div
        class="imp-drop"
        :class="{ arrastando, comArquivo: !!analise }"
        role="button"
        tabindex="0"
        aria-label="Arrastar o arquivo CSV para cá ou escolher no computador"
        @click="abrirSeletor"
        @keydown.enter.prevent="abrirSeletor"
        @keydown.space.prevent="abrirSeletor"
        @dragover.prevent="arrastando = true"
        @dragleave.prevent="arrastando = false"
        @drop.prevent="aoSoltar"
      >
        <input ref="entradaArquivo" type="file" accept=".csv,text/csv" class="imp-input-oculto" tabindex="-1" @change="aoEscolher" />
        <template v-if="analisando">
          <i class="mdi mdi-loading mdi-spin"></i>
          <strong>Lendo o arquivo…</strong>
        </template>
        <template v-else-if="analise && arquivo">
          <i class="mdi mdi-file-delimited-outline"></i>
          <strong>{{ arquivo.name }}</strong>
          <span>
            {{ formatarBytes(arquivo.size) }} · {{ analise.totalLinhas.toLocaleString('pt-BR') }}
            {{ analise.totalLinhas === 1 ? 'linha' : 'linhas' }} · {{ analise.cabecalhos.length }} colunas ·
            {{ analise.codificacao }} · separador “{{ analise.separador }}”
          </span>
          <span class="imp-drop-trocar">Clique ou arraste outro arquivo para trocar</span>
        </template>
        <template v-else>
          <i class="mdi mdi-tray-arrow-up"></i>
          <strong>Arraste o arquivo CSV para cá</strong>
          <span>ou clique para escolher no computador</span>
        </template>
      </div>
      <p v-if="erroArquivo" class="imp-erro" role="alert"><i class="mdi mdi-alert-circle-outline"></i> {{ erroArquivo }}</p>
      <div class="imp-acoes">
        <button type="button" class="imp-btn-sec" @click="voltar"><i class="mdi mdi-arrow-left"></i> Voltar</button>
        <button type="button" class="imp-btn" :disabled="!podeAvancar" @click="avancar">Continuar <i class="mdi mdi-arrow-right"></i></button>
      </div>
    </section>

    <!-- 3. Mapeamento -->
    <section v-else-if="passo === 'mapear' && analise" class="imp-corpo" aria-labelledby="imp-h-mapear">
      <div class="imp-cab">
        <h3 id="imp-h-mapear" class="imp-titulo">Relacione as colunas com os campos</h3>
        <div class="imp-cab-acoes">
          <button type="button" class="imp-btn-link" @click="restaurarSugestao"><i class="mdi mdi-auto-fix"></i> Usar sugestão</button>
          <button type="button" class="imp-btn-link" @click="ignorarTodas"><i class="mdi mdi-close-circle-outline"></i> Limpar</button>
        </div>
      </div>
      <p v-if="obrigatoriosFaltando.length" class="imp-erro" role="alert">
        <i class="mdi mdi-alert-circle-outline"></i>
        Falta relacionar: <strong>{{ obrigatoriosFaltando.map((c) => c.rotulo).join(', ') }}</strong>
      </p>
      <p v-else class="imp-ok"><i class="mdi mdi-check-circle-outline"></i> {{ colunasMapeadas }} de {{ analise.cabecalhos.length }} colunas serão importadas.</p>

      <div class="imp-tabela-wrap">
        <table class="imp-tabela">
          <thead>
            <tr><th scope="col">Coluna do arquivo</th><th scope="col">Amostra</th><th scope="col">Campo de destino</th></tr>
          </thead>
          <tbody>
            <tr v-for="(cab, i) in analise.cabecalhos" :key="i" :class="{ ignorada: !mapa[i] }">
              <td class="imp-col-nome"><span class="imp-n">{{ i + 1 }}</span> {{ cab }}</td>
              <td class="imp-col-amostra">
                <template v-if="amostraDaColuna(i).length">
                  <span v-for="(v, k) in amostraDaColuna(i)" :key="k" class="imp-chip" :title="v">{{ v }}</span>
                </template>
                <span v-else class="imp-vazio">(vazia)</span>
              </td>
              <td class="imp-col-destino">
                <select v-model="mapa[i]" :aria-label="`Campo de destino da coluna ${cab}`" class="imp-select">
                  <option value="">— Ignorar esta coluna —</option>
                  <optgroup label="Campos do sistema">
                    <option
                      v-for="c in camposNativos"
                      :key="c.chave"
                      :value="c.chave"
                      :disabled="camposUsados.has(c.chave) && mapa[i] !== c.chave"
                    >{{ c.rotulo }}{{ c.obrigatorio ? ' *' : '' }}</option>
                  </optgroup>
                  <optgroup v-if="camposPersonalizados.length" label="Campos personalizados">
                    <option
                      v-for="c in camposPersonalizados"
                      :key="c.chave"
                      :value="c.chave"
                      :disabled="camposUsados.has(c.chave) && mapa[i] !== c.chave"
                    >{{ c.rotulo }}</option>
                  </optgroup>
                </select>
                <small v-if="mapa[i] && camposPorChave.get(mapa[i]!)?.dica" class="imp-dica-campo">{{ camposPorChave.get(mapa[i]!)?.dica }}</small>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
      <p class="imp-legenda">* campo obrigatório</p>

      <button type="button" class="imp-btn-link" :aria-expanded="mostrarPrevia" @click="mostrarPrevia = !mostrarPrevia">
        <i class="mdi" :class="mostrarPrevia ? 'mdi-chevron-up' : 'mdi-chevron-down'"></i>
        {{ mostrarPrevia ? 'Ocultar' : 'Ver' }} as primeiras {{ analise.amostra.length }} linhas do arquivo
      </button>
      <div v-if="mostrarPrevia" class="imp-tabela-wrap imp-previa">
        <table class="imp-tabela">
          <thead>
            <tr>
              <th v-for="(cab, i) in analise.cabecalhos" :key="i" scope="col">
                {{ cab }}
                <div class="imp-destino-previa">{{ mapa[i] ? camposPorChave.get(mapa[i]!)?.rotulo : 'ignorada' }}</div>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(linha, r) in analise.amostra" :key="r">
              <td v-for="(cel, c) in linha" :key="c" :class="{ ignorada: !mapa[c] }">{{ cel }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="imp-acoes">
        <button type="button" class="imp-btn-sec" @click="voltar"><i class="mdi mdi-arrow-left"></i> Voltar</button>
        <button type="button" class="imp-btn" :disabled="!podeAvancar" @click="avancar">Validar <i class="mdi mdi-arrow-right"></i></button>
      </div>
    </section>

    <!-- 4. Validação -->
    <section v-else-if="passo === 'validar'" class="imp-corpo" aria-labelledby="imp-h-validar">
      <h3 id="imp-h-validar" class="imp-titulo">Validação do arquivo</h3>
      <div v-if="validando" class="imp-carregando" role="status"><i class="mdi mdi-loading mdi-spin"></i> Validando as linhas…</div>
      <p v-else-if="erroValidacao" class="imp-erro" role="alert"><i class="mdi mdi-alert-circle-outline"></i> {{ erroValidacao }}</p>
      <template v-else-if="validacao">
        <div class="imp-resumo">
          <div class="imp-num"><strong>{{ validacao.totalLinhas.toLocaleString('pt-BR') }}</strong><span>linhas no arquivo</span></div>
          <div class="imp-num ok"><strong>{{ validacao.validas.toLocaleString('pt-BR') }}</strong><span>válidas</span></div>
          <div class="imp-num" :class="{ ruim: validacao.invalidas > 0 }"><strong>{{ validacao.invalidas.toLocaleString('pt-BR') }}</strong><span>com erro</span></div>
        </div>

        <p v-if="validacao.invalidas === 0" class="imp-ok"><i class="mdi mdi-check-circle-outline"></i> Nenhum erro encontrado. O arquivo está pronto para importar.</p>
        <template v-else>
          <p class="imp-aviso">
            <i class="mdi mdi-alert-outline"></i>
            As linhas com erro não serão importadas e entram no relatório de erros. Você pode corrigir o arquivo e enviar de novo, ou seguir só com as válidas.
          </p>
          <div class="imp-tabela-wrap imp-erros">
            <table class="imp-tabela">
              <thead>
                <tr><th scope="col">Linha</th><th scope="col">Campo</th><th scope="col">Problema</th><th scope="col">Valor informado</th></tr>
              </thead>
              <tbody>
                <tr v-for="(e, i) in validacao.erros.slice(0, errosVisiveis)" :key="i">
                  <td class="imp-linha">{{ e.linha }}</td>
                  <td>{{ e.campo }}</td>
                  <td>{{ e.mensagem }}</td>
                  <td class="imp-valor" :title="e.valor ?? ''">{{ e.valor ?? '—' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
          <div class="imp-rodape-erros">
            <span>Mostrando {{ Math.min(errosVisiveis, validacao.erros.length) }} de {{ validacao.totalErros.toLocaleString('pt-BR') }} erros</span>
            <button v-if="errosVisiveis < validacao.erros.length" type="button" class="imp-btn-link" @click="errosVisiveis += 100">Mostrar mais</button>
            <span v-if="validacao.errosTruncados">A lista foi limitada; o relatório completo fica disponível após a importação.</span>
          </div>
        </template>
      </template>

      <div class="imp-acoes">
        <button type="button" class="imp-btn-sec" @click="irPara('mapear')"><i class="mdi mdi-arrow-left"></i> Ajustar colunas</button>
        <button type="button" class="imp-btn-sec" @click="irPara('arquivo')"><i class="mdi mdi-file-replace-outline"></i> Trocar arquivo</button>
        <button type="button" class="imp-btn" :disabled="!podeAvancar" @click="avancar">Continuar <i class="mdi mdi-arrow-right"></i></button>
      </div>
    </section>

    <!-- 5. Importação -->
    <section v-else-if="passo === 'importar'" class="imp-corpo" aria-labelledby="imp-h-importar">
      <h3 id="imp-h-importar" class="imp-titulo">Importar {{ entidadeAtual.rotulo.toLowerCase() }}</h3>

      <template v-if="!importacao">
        <fieldset class="imp-grupo">
          <legend>Se o registro já existir</legend>
          <label class="imp-radio"><input v-model="duplicidade" type="radio" value="Permitir" /> <span><strong>Criar todos</strong> sem verificar duplicidade</span></label>
          <label class="imp-radio"><input v-model="duplicidade" type="radio" value="Ignorar" /> <span><strong>Ignorar</strong> as linhas que já existem</span></label>
          <label class="imp-radio"><input v-model="duplicidade" type="radio" value="Atualizar" /> <span><strong>Atualizar</strong> o registro existente (células vazias não apagam dados)</span></label>
        </fieldset>

        <div v-if="duplicidade !== 'Permitir'" class="imp-chave">
          <label for="imp-chave">Identificar o registro pelo</label>
          <select id="imp-chave" v-model="chaveDuplicidade" class="imp-select">
            <option v-for="k in chavesDisponiveis" :key="k.chave" :value="k.chave">{{ k.rotulo }}</option>
          </select>
          <p v-if="!chavesDisponiveis.length" class="imp-erro">Relacione uma coluna de e-mail, documento, código ou nome para usar essa opção.</p>
          <p v-else-if="checandoDuplicadas" class="imp-vazio"><i class="mdi mdi-loading mdi-spin"></i> Conferindo registros existentes…</p>
          <p v-else-if="duplicadas !== null" class="imp-info">
            <i class="mdi mdi-information-outline"></i>
            {{ duplicadas.toLocaleString('pt-BR') }} {{ duplicadas === 1 ? 'linha corresponde' : 'linhas correspondem' }} a registros já existentes
            ou repetidos no arquivo.
          </p>
        </div>

        <div class="imp-acoes">
          <button type="button" class="imp-btn-sec" @click="voltar"><i class="mdi mdi-arrow-left"></i> Voltar</button>
          <button type="button" class="imp-btn" :disabled="!podeImportar" @click="iniciarImportacao">
            <i class="mdi" :class="iniciando ? 'mdi-loading mdi-spin' : 'mdi-database-import-outline'"></i> {{ iniciando ? 'Enviando…' : rotuloImportar }}
          </button>
        </div>
      </template>

      <template v-else>
        <div v-if="emAndamento" class="imp-progresso" role="status" aria-live="polite">
          <div class="imp-barra" role="progressbar" :aria-valuenow="percentual" aria-valuemin="0" aria-valuemax="100" aria-label="Progresso da importação">
            <div class="imp-barra-fill" :style="{ width: percentual + '%' }"></div>
          </div>
          <p>Importando… {{ importacao.processadas.toLocaleString('pt-BR') }} de {{ importacao.totalLinhas.toLocaleString('pt-BR') }} linhas ({{ percentual }}%)</p>
          <p class="imp-vazio">Você pode sair desta tela; a importação continua e fica no histórico.</p>
        </div>
        <div v-else class="imp-final">
          <div class="imp-final-cab">
            <UiBadge :tom="TOM_STATUS[importacao.status]">{{ ROTULO_STATUS[importacao.status] }}</UiBadge>
            <span>{{ importacao.nomeArquivo }}</span>
          </div>
          <div class="imp-resumo">
            <div class="imp-num"><strong>{{ importacao.totalLinhas.toLocaleString('pt-BR') }}</strong><span>linhas</span></div>
            <div class="imp-num ok"><strong>{{ importacao.criadas.toLocaleString('pt-BR') }}</strong><span>criadas</span></div>
            <div class="imp-num ok"><strong>{{ importacao.atualizadas.toLocaleString('pt-BR') }}</strong><span>atualizadas</span></div>
            <div class="imp-num"><strong>{{ importacao.ignoradas.toLocaleString('pt-BR') }}</strong><span>ignoradas</span></div>
            <div class="imp-num" :class="{ ruim: importacao.falhas > 0 }"><strong>{{ importacao.falhas.toLocaleString('pt-BR') }}</strong><span>com erro</span></div>
          </div>
          <p v-if="importacao.falhas > 0" class="imp-aviso">
            <i class="mdi mdi-alert-outline"></i>
            {{ importacao.falhas }} {{ importacao.falhas === 1 ? 'linha não foi importada' : 'linhas não foram importadas' }}.
            Baixe o relatório, corrija e envie só essas linhas.
          </p>
          <p v-else class="imp-ok"><i class="mdi mdi-check-circle-outline"></i> Importação concluída sem erros.</p>
          <div class="imp-acoes">
            <button v-if="importacao.falhas > 0" type="button" class="imp-btn-sec" @click="baixarRelatorio">
              <i class="mdi mdi-file-alert-outline"></i> Baixar relatório de erros
            </button>
            <button type="button" class="imp-btn" @click="novaImportacao"><i class="mdi mdi-plus"></i> Nova importação</button>
          </div>
        </div>
      </template>
    </section>
  </div>
</template>

<style scoped>
.imp-wizard { display: flex; flex-direction: column; gap: 20px; max-width: 1080px; }

.imp-passos { display: flex; gap: 6px; list-style: none; margin: 0; padding: 0; flex-wrap: wrap; }
.imp-passo { flex: 1; min-width: 110px; }
.imp-passo-btn { width: 100%; display: flex; align-items: center; gap: 8px; padding: 8px 10px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-muted); font-size: 12px; font-weight: 600; cursor: default; text-align: left; }
.imp-passo-btn:not(:disabled) { cursor: pointer; color: var(--text-secondary); }
.imp-passo-btn:not(:disabled):hover { background: var(--bg-elevated); }
.imp-passo-btn:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.imp-passo-n { display: inline-flex; align-items: center; justify-content: center; width: 22px; height: 22px; border-radius: 50%; border: 1px solid var(--border); font-size: 11px; flex-shrink: 0; }
.imp-passo.ativo .imp-passo-btn { border-color: var(--accent); color: var(--text-primary); }
.imp-passo.ativo .imp-passo-n { background: var(--accent); border-color: var(--accent); color: #000; }
.imp-passo.feito .imp-passo-n { background: rgba(34, 197, 94, .16); border-color: transparent; color: #22c55e; }

.imp-corpo { display: flex; flex-direction: column; gap: 14px; }
.imp-titulo { margin: 0; font-size: 16px; font-weight: 600; color: var(--text-primary); }
.imp-cab { display: flex; align-items: center; justify-content: space-between; gap: 12px; flex-wrap: wrap; }
.imp-cab-acoes { display: flex; gap: 14px; }

.imp-entidades { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 10px; }
.imp-entidade { display: flex; flex-direction: column; align-items: flex-start; gap: 4px; padding: 14px 16px; border: 1px solid var(--border); border-radius: 10px; background: var(--bg-surface); color: var(--text-secondary); text-align: left; cursor: pointer; }
.imp-entidade .mdi { font-size: 22px; color: var(--accent); }
.imp-entidade strong { color: var(--text-primary); font-size: 14px; }
.imp-entidade span { font-size: 12px; color: var(--text-muted); }
.imp-entidade:hover { border-color: var(--accent); }
.imp-entidade.marcada { border-color: var(--accent); background: rgba(6, 182, 212, .08); }
.imp-entidade:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }

.imp-dica { display: flex; gap: 10px; padding: 10px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-secondary); color: var(--text-muted); font-size: 12px; line-height: 1.5; }
.imp-dica .mdi { color: var(--accent); font-size: 16px; }

.imp-drop { display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 6px; padding: 36px 20px; border: 2px dashed var(--border); border-radius: 12px; background: var(--bg-surface); color: var(--text-muted); text-align: center; cursor: pointer; transition: border-color .15s, background .15s; }
.imp-drop .mdi { font-size: 38px; color: var(--accent); }
.imp-drop strong { color: var(--text-primary); font-size: 14px; word-break: break-all; }
.imp-drop span { font-size: 12px; }
.imp-drop:hover, .imp-drop.arrastando { border-color: var(--accent); background: rgba(6, 182, 212, .06); }
.imp-drop.comArquivo { border-style: solid; }
.imp-drop:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.imp-drop-trocar { opacity: .8; }
.imp-input-oculto { display: none; }

.imp-tabela-wrap { overflow: auto; border: 1px solid var(--border); border-radius: 8px; max-height: 420px; }
.imp-tabela { width: 100%; border-collapse: collapse; font-size: 12px; }
.imp-tabela thead { position: sticky; top: 0; z-index: 1; }
.imp-tabela th { background: var(--bg-secondary); color: var(--text-muted); font-weight: 600; font-size: 11px; text-transform: uppercase; padding: 9px 12px; text-align: left; border-bottom: 1px solid var(--border); white-space: nowrap; }
.imp-tabela td { padding: 8px 12px; border-bottom: 1px solid var(--border); color: var(--text-secondary); vertical-align: middle; }
.imp-tabela tr:last-child td { border-bottom: none; }
.imp-tabela tr.ignorada td, .imp-tabela td.ignorada { opacity: .5; }
.imp-col-nome { color: var(--text-primary); font-weight: 500; white-space: nowrap; }
.imp-col-amostra { max-width: 340px; }
.imp-col-destino { min-width: 240px; }
.imp-n { display: inline-block; min-width: 18px; margin-right: 6px; color: var(--text-muted); font-size: 11px; font-weight: 400; }
.imp-chip { display: inline-block; max-width: 150px; margin: 2px 4px 2px 0; padding: 2px 8px; border-radius: 4px; background: var(--bg-elevated); border: 1px solid var(--border); font-size: 11px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; vertical-align: middle; }
.imp-vazio { color: var(--text-muted); font-size: 12px; }
.imp-destino-previa { margin-top: 2px; font-size: 10px; color: var(--accent); text-transform: none; font-weight: 500; }
.imp-dica-campo { display: block; margin-top: 3px; font-size: 11px; color: var(--text-muted); }
.imp-legenda { margin: -6px 0 0; font-size: 11px; color: var(--text-muted); }
.imp-linha { font-variant-numeric: tabular-nums; color: var(--text-primary); }
.imp-valor { max-width: 240px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; font-family: monospace; font-size: 11px; }

.imp-select { width: 100%; padding: 7px 10px; border: 1px solid var(--border); border-radius: 6px; background: var(--bg-primary); color: var(--text-primary); font-size: 12px; }
.imp-select:focus { outline: none; border-color: var(--accent); }

.imp-resumo { display: flex; gap: 10px; flex-wrap: wrap; }
.imp-num { min-width: 120px; padding: 12px 16px; border: 1px solid var(--border); border-radius: 10px; background: var(--bg-surface); display: flex; flex-direction: column; gap: 2px; }
.imp-num strong { font-size: 22px; color: var(--text-primary); font-variant-numeric: tabular-nums; }
.imp-num span { font-size: 12px; color: var(--text-muted); }
.imp-num.ok strong { color: #22c55e; }
.imp-num.ruim strong { color: var(--danger); }

.imp-ok, .imp-erro, .imp-aviso, .imp-info { display: flex; align-items: flex-start; gap: 8px; margin: 0; padding: 10px 14px; border-radius: 8px; font-size: 13px; line-height: 1.45; }
.imp-ok { background: rgba(34, 197, 94, .1); color: #22c55e; }
.imp-erro { background: rgba(239, 68, 68, .1); color: var(--danger); }
.imp-aviso { background: rgba(245, 158, 11, .1); color: #f59e0b; }
.imp-info { background: rgba(6, 182, 212, .08); color: var(--accent); }
.imp-rodape-erros { display: flex; gap: 14px; align-items: center; flex-wrap: wrap; font-size: 12px; color: var(--text-muted); }
.imp-carregando { display: flex; align-items: center; gap: 8px; padding: 30px; justify-content: center; color: var(--text-muted); }

.imp-grupo { display: flex; flex-direction: column; gap: 8px; margin: 0; padding: 12px 16px; border: 1px solid var(--border); border-radius: 10px; background: var(--bg-surface); }
.imp-grupo legend { padding: 0 6px; font-size: 12px; font-weight: 600; color: var(--text-muted); }
.imp-radio { display: flex; align-items: center; gap: 10px; font-size: 13px; color: var(--text-secondary); cursor: pointer; }
.imp-radio strong { color: var(--text-primary); }
.imp-radio input { accent-color: var(--accent); }
.imp-chave { display: flex; flex-direction: column; gap: 6px; max-width: 360px; font-size: 12px; color: var(--text-muted); }
.imp-chave .imp-info, .imp-chave .imp-erro { max-width: 640px; }

.imp-progresso { display: flex; flex-direction: column; gap: 8px; padding: 18px; border: 1px solid var(--border); border-radius: 10px; background: var(--bg-surface); }
.imp-progresso p { margin: 0; font-size: 13px; color: var(--text-secondary); }
.imp-barra { height: 10px; border-radius: 999px; background: var(--bg-elevated); overflow: hidden; }
.imp-barra-fill { height: 100%; background: var(--accent); transition: width .3s ease; }
.imp-final { display: flex; flex-direction: column; gap: 14px; }
.imp-final-cab { display: flex; align-items: center; gap: 10px; font-size: 13px; color: var(--text-secondary); }

.imp-acoes { display: flex; gap: 10px; justify-content: flex-end; flex-wrap: wrap; padding-top: 4px; }
.imp-btn { display: inline-flex; align-items: center; gap: 6px; padding: 9px 18px; border: none; border-radius: 8px; background: var(--accent); color: #000; font-size: 13px; font-weight: 600; cursor: pointer; }
.imp-btn:hover:not(:disabled) { background: var(--accent-hover); }
.imp-btn:disabled { opacity: .4; cursor: not-allowed; }
.imp-btn-sec { display: inline-flex; align-items: center; gap: 6px; padding: 9px 16px; border: 1px solid var(--border); border-radius: 8px; background: transparent; color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.imp-btn-sec:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.imp-btn-sec:disabled { opacity: .5; cursor: not-allowed; }
.imp-btn-link { display: inline-flex; align-items: center; gap: 4px; padding: 2px 0; border: none; background: none; color: var(--accent); font-size: 12px; cursor: pointer; }
.imp-btn-link:hover { text-decoration: underline; }
.imp-btn:focus-visible, .imp-btn-sec:focus-visible, .imp-btn-link:focus-visible, .imp-select:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
</style>
