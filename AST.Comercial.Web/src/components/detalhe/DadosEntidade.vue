<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { odataRequest, montarQuery } from '@/services/api'
import { lista, mensagemErro, lit, formatarData, formatarValorMoeda, type Registro, type MoedaInfo } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import DetalheEstado from './DetalheEstado.vue'
import CampoMoeda from './CampoMoeda.vue'

/**
 * Aba "Dados": mostra e edita os campos do registro.
 * Segue o mesmo modelo do FormularioDinamico (Formulário padrão da entidade com
 * seções e campos), porém embutido na página. Quando não há formulário padrão
 * utilizável para a entidade, usa a lista de campos `padrao` informada pela tela.
 */
export interface CampoDef {
  nome: string
  rotulo: string
  /** `moeda` = valor monetário, formatado com o símbolo da moeda do registro (campo MoedaId). */
  tipo: 'texto' | 'area' | 'numero' | 'moeda' | 'data' | 'bool' | 'select'
  opcoes?: { id: number; nome: string }[]
  somenteLeitura?: boolean
  obrigatorio?: boolean
}

interface SecaoForm { Id: number; Nome: string; Ordem: number; Colunas: number; Colapsavel: boolean; IniciaColapsada: boolean }
interface CampoForm { Id: number; NomeCampoFixo: string | null; Rotulo: string | null; Placeholder: string | null; Dica: string | null; Ordem: number; Obrigatorio: boolean; Visivel: boolean; SomenteLeitura: boolean; SecaoFormularioId: number | null }
interface SecaoView { id: number | null; nome: string; colunas: number; campos: CampoDef[]; dicas: Record<string, string> }

const props = defineProps<{
  entidade: 'Cliente' | 'Negocio'
  endpoint: 'Clientes' | 'Negocios'
  registro: Registro
  padrao: CampoDef[]
  /** Ocultar a edição (ex.: sem permissão). */
  somenteLeitura?: boolean
  /** Moedas (odata/Moedas) para o símbolo dos campos monetários. */
  moedas?: MoedaInfo[]
}>()
const emit = defineEmits<{ salvo: [] }>()

const toast = useToast()
const carregando = ref(false)
const erro = ref('')
const editando = ref(false)
const salvando = ref(false)
const secoes = ref<SecaoView[]>([])
const valores = ref<Record<string, any>>({})
const tiposEsquema = ref<Record<string, string>>({})
const colapsadas = ref<Set<number>>(new Set())

const BLOQUEADOS = new Set(['Id', 'EmpresaId', 'CriadoEm', 'AtualizadoEm', 'Ativo', 'ChaveExterna', 'SenhaHash'])

function paraEntrada(def: CampoDef, v: unknown): any {
  if (def.tipo === 'data') return typeof v === 'string' && v ? v.slice(0, 10) : ''
  if (def.tipo === 'bool') return !!v
  if (v === null || v === undefined) return ''
  return v
}

const ISO_DATA = /^\d{4}-\d{2}-\d{2}(T|$)/
const NOME_MONETARIO = /^(Valor|Preco|Custo|Desconto|Total|Receita|Faturamento)/


function defDoNome(nome: string, rotulo?: string | null, somenteLeitura = false, obrigatorio = false): CampoDef {
  const base = props.padrao.find((c) => c.nome === nome)
  if (base) return { ...base, rotulo: rotulo || base.rotulo, somenteLeitura: base.somenteLeitura || somenteLeitura, obrigatorio: base.obrigatorio || obrigatorio }
  const t = tiposEsquema.value[nome] ?? typeof props.registro[nome]
  const bruto = props.registro[nome]
  let tipo: CampoDef['tipo'] = t === 'number' ? 'numero' : t === 'boolean' ? 'bool' : t === 'datetime' ? 'data' : 'texto'
  if (tipo === 'texto' && typeof bruto === 'string' && ISO_DATA.test(bruto)) tipo = 'data'
  if (tipo === 'texto' && typeof bruto === 'boolean') tipo = 'bool'
  if (tipo === 'texto' && typeof bruto === 'number') tipo = 'numero'
  if (tipo === 'numero' && props.entidade === 'Negocio' && NOME_MONETARIO.test(nome)) tipo = 'moeda'
  return { nome, rotulo: rotulo || nome, tipo, somenteLeitura, obrigatorio }
}

async function carregarLayout() {
  carregando.value = true
  erro.value = ''
  try {
    const esquema = await odataRequest<any>('GET', `Campos@Esquema(${props.entidade})`).catch(() => null)
    const prop = esquema?.['$registro']?.propriedades ?? {}
    tiposEsquema.value = Object.fromEntries(Object.entries(prop).map(([k, v]: any) => [k, String(v?.tipo ?? '')]))

    let montado: SecaoView[] = []
    const forms = lista<Registro>(await odataRequest<unknown>('GET', `Formularios${montarQuery({ $filter: `EntidadeAlvo eq '${lit(props.entidade)}' and Padrao eq true`, $top: 1 })}`).catch(() => null))
    const formId = forms[0]?.Id as number | undefined
    if (formId) {
      const [rs, rc] = await Promise.all([
        odataRequest<unknown>('GET', `SecoesFormulario${montarQuery({ $filter: `FormularioId eq ${formId}`, $orderby: 'Ordem asc', $top: 100 })}`),
        odataRequest<unknown>('GET', `CamposFormulario${montarQuery({ $filter: `FormularioId eq ${formId}`, $orderby: 'Ordem asc', $top: 100 })}`),
      ])
      const sec = lista<SecaoForm>(rs)
      // Só campos que existem no registro como valor simples (o formulário padrão pode citar campos de outras origens).
      const cps = lista<CampoForm>(rc).filter((c) => {
        const n = c.NomeCampoFixo
        if (!c.Visivel || !n || !(n in props.registro) || BLOQUEADOS.has(n)) return false
        const v = props.registro[n]
        return v === null || typeof v !== 'object'
      })
      const grupos: { id: number | null; nome: string; colunas: number; colapsavel: boolean; inicia: boolean }[] = [
        { id: null, nome: 'Informações gerais', colunas: 2, colapsavel: false, inicia: false },
        ...sec.sort((a, b) => a.Ordem - b.Ordem).map((s) => ({ id: s.Id, nome: s.Nome, colunas: s.Colunas || 2, colapsavel: s.Colapsavel, inicia: s.IniciaColapsada })),
      ]
      for (const g of grupos) {
        const doGrupo = cps.filter((c) => (c.SecaoFormularioId ?? null) === g.id).sort((a, b) => a.Ordem - b.Ordem)
        if (!doGrupo.length) continue
        const dicas: Record<string, string> = {}
        const campos = doGrupo.map((c) => {
          if (c.Dica) dicas[c.NomeCampoFixo!] = c.Dica
          return defDoNome(c.NomeCampoFixo!, c.Rotulo, c.SomenteLeitura, c.Obrigatorio)
        })
        montado.push({ id: g.id, nome: g.nome, colunas: g.colunas, campos, dicas })
        if (g.inicia && g.id !== null) colapsadas.value.add(g.id)
      }
    }
    if (!montado.length) {
      montado = [{ id: null, nome: 'Informações gerais', colunas: 2, campos: props.padrao, dicas: {} }]
    } else {
      // Campos importantes da entidade que o formulário padrão não cobre (ex.: status, origem).
      const usados = new Set(montado.flatMap((s) => s.campos.map((c) => c.nome)))
      const restantes = props.padrao.filter((c) => !usados.has(c.nome))
      if (restantes.length) montado.push({ id: null, nome: 'Outros dados', colunas: 2, campos: restantes, dicas: {} })
    }
    secoes.value = montado
    reiniciarValores()
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar os campos')
  } finally {
    carregando.value = false
  }
}

function todosCampos(): CampoDef[] {
  return secoes.value.flatMap((s) => s.campos)
}

function reiniciarValores() {
  const v: Record<string, any> = {}
  for (const c of todosCampos()) v[c.nome] = paraEntrada(c, props.registro[c.nome])
  valores.value = v
}

function nomeOpcao(def: CampoDef, v: unknown): string {
  return def.opcoes?.find((o) => o.id === Number(v))?.nome ?? '-'
}

/** Moeda do registro; em edição acompanha a moeda escolhida no formulário. */
const moedaAtual = computed(() => {
  const id = editando.value && 'MoedaId' in valores.value ? valores.value.MoedaId : props.registro.MoedaId
  return props.moedas?.find((m) => m.id === Number(id))
})

function exibir(def: CampoDef): string {
  const v = props.registro[def.nome]
  if (def.tipo === 'bool') return v ? 'Sim' : 'Não'
  if (v === null || v === undefined || v === '') return '-'
  if (def.tipo === 'data') return formatarData(String(v))
  if (def.tipo === 'moeda') {
    const m = props.moedas?.find((x) => x.id === Number(props.registro.MoedaId))
    return formatarValorMoeda(Number(v), m?.simbolo, m?.codigo)
  }
  if (def.tipo === 'numero') return Number(v).toLocaleString('pt-BR')
  if (def.tipo === 'select') return nomeOpcao(def, v)
  return String(v)
}

function toggleSecao(id: number) {
  if (colapsadas.value.has(id)) colapsadas.value.delete(id)
  else colapsadas.value.add(id)
  colapsadas.value = new Set(colapsadas.value)
}

function iniciarEdicao() { reiniciarValores(); editando.value = true }
function cancelar() { reiniciarValores(); editando.value = false }

const alterados = computed(() => {
  const dif: Record<string, any> = {}
  for (const c of todosCampos()) {
    if (c.somenteLeitura) continue
    const novo = valores.value[c.nome]
    const antigo = paraEntrada(c, props.registro[c.nome])
    if (novo === antigo) continue
    let envio: any = novo
    if (c.tipo === 'data') envio = novo ? `${novo}T00:00:00Z` : null
    else if (c.tipo === 'numero' || c.tipo === 'moeda' || c.tipo === 'select') envio = novo === '' || novo === null ? null : Number(novo)
    else if (c.tipo === 'texto' || c.tipo === 'area') envio = novo === '' ? null : novo
    dif[c.nome] = envio
  }
  return dif
})

async function salvar() {
  for (const c of todosCampos()) {
    if (c.obrigatorio && !c.somenteLeitura && (valores.value[c.nome] === '' || valores.value[c.nome] === null)) {
      toast.error(`Preencha o campo "${c.rotulo}".`)
      return
    }
  }
  const payload = alterados.value
  if (!Object.keys(payload).length) { editando.value = false; return }
  salvando.value = true
  try {
    await odataRequest('PATCH', `${props.endpoint}(${props.registro.Id})`, payload)
    toast.success('Dados salvos.')
    editando.value = false
    emit('salvo')
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível salvar'))
  } finally {
    salvando.value = false
  }
}

watch(() => props.registro, () => { if (!editando.value && secoes.value.length) reiniciarValores() })
onMounted(carregarLayout)
</script>

<template>
  <DetalheEstado :carregando="carregando" :erro="erro" @tentar="carregarLayout">
    <div class="dd-barra" v-if="!somenteLeitura">
      <template v-if="!editando">
        <button class="dd-btn" @click="iniciarEdicao"><i class="mdi mdi-pencil-outline"></i> Editar</button>
      </template>
      <template v-else>
        <button class="dd-btn" :disabled="salvando" @click="cancelar">Cancelar</button>
        <button class="dd-btn dd-primario" :disabled="salvando" @click="salvar">
          <i class="mdi mdi-check"></i> {{ salvando ? 'Salvando...' : 'Salvar' }}
        </button>
      </template>
    </div>

    <section v-for="(s, i) in secoes" :key="`${s.id}-${i}`" class="dd-secao">
      <button
        v-if="s.id !== null"
        class="dd-secao-head"
        :aria-expanded="!colapsadas.has(s.id)"
        @click="toggleSecao(s.id)"
      >
        <i class="mdi" :class="colapsadas.has(s.id) ? 'mdi-chevron-right' : 'mdi-chevron-down'"></i> {{ s.nome }}
      </button>
      <h4 v-else-if="secoes.length > 1 || i === 0" class="dd-secao-titulo">{{ s.nome }}</h4>
      <div v-if="s.id === null || !colapsadas.has(s.id)" class="dd-grade" :style="{ gridTemplateColumns: `repeat(${s.colunas}, minmax(0, 1fr))` }">
        <div v-for="c in s.campos" :key="c.nome" class="dd-campo" :class="{ largo: c.tipo === 'area' }">
          <label :for="`dd-${c.nome}`" :class="{ obrig: c.obrigatorio && editando }">{{ c.rotulo }}</label>
          <template v-if="editando && !c.somenteLeitura">
            <textarea v-if="c.tipo === 'area'" :id="`dd-${c.nome}`" v-model="valores[c.nome]" rows="3"></textarea>
            <select v-else-if="c.tipo === 'select'" :id="`dd-${c.nome}`" v-model="valores[c.nome]">
              <option value="">Sem valor</option>
              <option v-for="o in c.opcoes ?? []" :key="o.id" :value="o.id">{{ o.nome }}</option>
            </select>
            <label v-else-if="c.tipo === 'bool'" class="dd-check"><input :id="`dd-${c.nome}`" v-model="valores[c.nome]" type="checkbox" /> Sim</label>
            <CampoMoeda v-else-if="c.tipo === 'moeda'" :id="`dd-${c.nome}`" v-model="valores[c.nome]" :simbolo="moedaAtual?.simbolo || moedaAtual?.codigo" :aria-label="c.rotulo" />
            <input v-else-if="c.tipo === 'numero'" :id="`dd-${c.nome}`" v-model="valores[c.nome]" type="number" step="any" />
            <input v-else-if="c.tipo === 'data'" :id="`dd-${c.nome}`" v-model="valores[c.nome]" type="date" />
            <input v-else :id="`dd-${c.nome}`" v-model="valores[c.nome]" type="text" />
            <small v-if="s.dicas[c.nome]">{{ s.dicas[c.nome] }}</small>
          </template>
          <div v-else :id="`dd-${c.nome}`" class="dd-valor">{{ exibir(c) }}</div>
        </div>
      </div>
    </section>
  </DetalheEstado>
</template>

<style scoped>
.dd-barra { display: flex; justify-content: flex-end; gap: 8px; margin-bottom: 14px; }
.dd-btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.dd-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.dd-btn:disabled { opacity: .5; cursor: not-allowed; }
.dd-primario { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.dd-primario:hover:not(:disabled) { background: var(--accent-hover); color: #000; }
.dd-secao { margin-bottom: 18px; }
.dd-secao-titulo { margin: 0 0 10px; font-size: 12px; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); }
.dd-secao-head { display: flex; align-items: center; gap: 6px; width: 100%; padding: 10px 12px; margin-bottom: 10px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-secondary); color: var(--text-primary); font-size: 13px; font-weight: 600; cursor: pointer; text-align: left; }
.dd-grade { display: grid; gap: 14px 18px; }
.dd-campo { display: flex; flex-direction: column; gap: 5px; min-width: 0; }
.dd-campo.largo { grid-column: 1 / -1; }
.dd-campo > label { font-size: 11px; font-weight: 600; text-transform: uppercase; letter-spacing: .4px; color: var(--text-muted); }
.dd-campo > label.obrig::after { content: ' *'; color: var(--danger); }
.dd-valor { font-size: 14px; color: var(--text-primary); min-height: 20px; overflow-wrap: anywhere; white-space: pre-wrap; }
.dd-campo input[type='text'], .dd-campo input[type='number'], .dd-campo input[type='date'], .dd-campo select, .dd-campo textarea { width: 100%; box-sizing: border-box; padding: 9px 11px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-surface); color: var(--text-primary); font-size: 14px; font-family: inherit; outline: none; }
.dd-campo input:focus, .dd-campo select:focus, .dd-campo textarea:focus { border-color: var(--accent); }
.dd-check { display: flex; align-items: center; gap: 8px; font-size: 14px; color: var(--text-primary); text-transform: none; letter-spacing: 0; }
.dd-campo small { font-size: 11px; color: var(--text-muted); }
</style>
