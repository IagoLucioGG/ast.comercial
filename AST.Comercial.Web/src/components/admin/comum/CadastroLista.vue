<script setup lang="ts">
// Tela genérica de cadastro simples: lista + busca + criar/editar (modal) + excluir (confirmação).
// Usada por Cargos, Departamentos, Etiquetas, Moedas (leitura) e pelas listas de apoio de clientes/vendas/produtos.
import { ref, computed, onMounted } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'
import { useToast } from '@/composables/useToast'
import { msgErro, type ApiCadastro, type CampoCadastro } from './adminApi'
import './admin-comum.css'

const props = defineProps<{
  /** Nome no singular, ex.: "Cargo". */
  singular: string
  api: ApiCadastro
  campos: CampoCadastro[] | ((itens: Record<string, unknown>[]) => CampoCadastro[])
  vazio?: string
  aviso?: string
  /** Artigo para mensagens ("o"/"a"). */
  artigo?: 'o' | 'a'
}>()

const toast = useToast()
const itens = ref<Record<string, unknown>[]>([])
const carregando = ref(false)
const erro = ref('')
const busca = ref('')

const camposAtuais = computed<CampoCadastro[]>(() =>
  typeof props.campos === 'function' ? props.campos(itens.value) : props.campos,
)
const colunas = computed(() => camposAtuais.value.filter((c) => c.lista))
const podeEditar = computed(() => !!props.api.atualizar)
const podeCriar = computed(() => !!props.api.criar)
const podeExcluir = computed(() => !!props.api.remover)
const art = computed(() => props.artigo ?? 'o')
const artigoNovo = computed(() => (art.value === 'a' ? 'Nova' : 'Novo'))

const filtrados = computed(() => {
  const q = busca.value.trim().toLowerCase()
  if (!q) return itens.value
  return itens.value.filter((i) =>
    colunas.value.some((c) => textoCelula(c, i).toLowerCase().includes(q)),
  )
})

function textoCelula(c: CampoCadastro, item: Record<string, unknown>): string {
  const v = item[c.chave]
  if (c.formatar) return c.formatar(v, item)
  if (v === null || v === undefined || v === '') return '-'
  if (c.tipo === 'select') return c.opcoes?.find((o) => String(o.valor) === String(v))?.rotulo ?? String(v)
  if (c.tipo === 'checkbox') return v ? 'Sim' : 'Não'
  return String(v)
}

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    itens.value = await props.api.carregar()
  } catch (e) {
    erro.value = msgErro(e, 'Não foi possível carregar a lista.')
    toast.error(erro.value)
  } finally {
    carregando.value = false
  }
}
onMounted(carregar)

// ---- formulário
const modalAberto = ref(false)
const editandoId = ref<number | null>(null)
const dados = ref<Record<string, unknown>>({})
const erros = ref<Record<string, string>>({})
const salvando = ref(false)

function abrirNovo() {
  editandoId.value = null
  const d: Record<string, unknown> = {}
  for (const c of camposAtuais.value) {
    d[c.chave] = c.valorPadrao ?? (c.tipo === 'checkbox' ? false : c.tipo === 'numero' ? 0 : c.tipo === 'select' ? '' : '')
  }
  dados.value = d
  erros.value = {}
  modalAberto.value = true
}

function abrirEdicao(item: Record<string, unknown>) {
  editandoId.value = Number(item.id ?? item.Id)
  const d: Record<string, unknown> = {}
  for (const c of camposAtuais.value) {
    const v = item[c.chave]
    d[c.chave] = v ?? (c.tipo === 'checkbox' ? false : c.tipo === 'numero' ? 0 : '')
  }
  dados.value = d
  erros.value = {}
  modalAberto.value = true
}

function validar(): boolean {
  const e: Record<string, string> = {}
  for (const c of camposAtuais.value) {
    const v = dados.value[c.chave]
    if (c.obrigatorio && (v === '' || v === null || v === undefined || (typeof v === 'string' && !v.trim())))
      e[c.chave] = `${c.rotulo} é obrigatório.`
    if (c.maxLength && typeof v === 'string' && v.length > c.maxLength)
      e[c.chave] = `${c.rotulo} deve ter no máximo ${c.maxLength} caracteres.`
    if (c.tipo === 'cor' && typeof v === 'string' && v && !/^#[0-9a-fA-F]{6}$/.test(v))
      e[c.chave] = 'Use o formato #RRGGBB.'
  }
  erros.value = e
  return Object.keys(e).length === 0
}

function montarPayload(): Record<string, unknown> {
  const p: Record<string, unknown> = {}
  for (const c of camposAtuais.value) {
    let v = dados.value[c.chave]
    if (c.tipo === 'numero') v = v === '' || v === null ? 0 : Number(v)
    else if (c.tipo === 'select') v = v === '' || v === null ? null : (typeof v === 'string' && /^-?\d+$/.test(v) && c.opcoes?.some((o) => typeof o.valor === 'number') ? Number(v) : v)
    else if (typeof v === 'string') v = v.trim()
    p[c.chave] = v
  }
  return p
}

async function salvar() {
  if (!validar() || salvando.value) return
  salvando.value = true
  try {
    const payload = montarPayload()
    if (editandoId.value === null) {
      await props.api.criar?.(payload)
      toast.success(`${props.singular} criad${art.value}.`)
    } else {
      await props.api.atualizar?.(editandoId.value, payload)
      toast.success(`${props.singular} atualizad${art.value}.`)
    }
    modalAberto.value = false
    await carregar()
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    salvando.value = false
  }
}

// ---- exclusão
const excluindo = ref<Record<string, unknown> | null>(null)
const confirmAberto = ref(false)
const removendo = ref(false)

function pedirExclusao(item: Record<string, unknown>) {
  excluindo.value = item
  confirmAberto.value = true
}
async function confirmarExclusao() {
  if (!excluindo.value || removendo.value) return
  removendo.value = true
  try {
    await props.api.remover?.(Number(excluindo.value.id ?? excluindo.value.Id))
    toast.success(`${props.singular} excluíd${art.value}.`)
    confirmAberto.value = false
    await carregar()
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    removendo.value = false
  }
}
const nomeExcluindo = computed(() => String(excluindo.value?.nome ?? excluindo.value?.Nome ?? ''))
</script>

<template>
  <div class="adm-tela">
    <div v-if="aviso" class="adm-aviso">{{ aviso }}</div>
    <div class="adm-barra">
      <div class="adm-busca">
        <i class="mdi mdi-magnify"></i>
        <input v-model="busca" type="search" placeholder="Buscar..." :aria-label="`Buscar ${singular.toLowerCase()}`" />
      </div>
      <button v-if="podeCriar" class="adm-btn primario" @click="abrirNovo">
        <i class="mdi mdi-plus"></i> {{ artigoNovo }} {{ singular }}
      </button>
      <span class="adm-total">{{ filtrados.length }} registro(s)</span>
    </div>

    <div class="adm-tabela-wrap">
      <table class="adm-tabela">
        <thead>
          <tr>
            <th v-for="c in colunas" :key="c.chave">{{ c.rotulo }}</th>
            <th v-if="podeEditar || podeExcluir"><span class="sr-only">Ações</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="carregando"><td :colspan="colunas.length + 1" class="adm-vazio">Carregando...</td></tr>
          <tr v-else-if="erro">
            <td :colspan="colunas.length + 1" class="adm-erro">
              {{ erro }}<br /><button class="adm-btn" @click="carregar">Tentar novamente</button>
            </td>
          </tr>
          <tr v-else-if="!filtrados.length">
            <td :colspan="colunas.length + 1" class="adm-vazio">{{ busca ? 'Nada encontrado para a busca.' : (vazio ?? 'Nenhum registro cadastrado.') }}</td>
          </tr>
          <template v-else>
          <tr v-for="item in filtrados" :key="String(item.id ?? item.Id)">
            <td v-for="(c, i) in colunas" :key="c.chave" :class="{ 'adm-principal': i === 0 }">
              <span v-if="c.tipo === 'cor'">
                <span class="adm-cor" :style="{ background: (item[c.chave] as string) || 'transparent' }"></span>
                <span v-if="item[c.chave]" style="margin-left: 8px">{{ item[c.chave] }}</span>
              </span>
              <template v-else>{{ textoCelula(c, item) }}</template>
            </td>
            <td v-if="podeEditar || podeExcluir" class="adm-acoes">
              <button v-if="podeEditar" class="adm-icone" :aria-label="`Editar ${String(item.nome ?? item.Nome ?? '')}`" @click="abrirEdicao(item)"><i class="mdi mdi-pencil-outline"></i></button>
              <button v-if="podeExcluir" class="adm-icone perigo" :aria-label="`Excluir ${String(item.nome ?? item.Nome ?? '')}`" @click="pedirExclusao(item)"><i class="mdi mdi-trash-can-outline"></i></button>
            </td>
          </tr>
          </template>
        </tbody>
      </table>
    </div>

    <UiModal v-model="modalAberto" :titulo="editandoId === null ? `Novo ${singular}` : `Editar ${singular}`" largura="520px">
      <form id="form-cadastro" class="adm-form adm-form-grade" @submit.prevent="salvar">
        <div v-for="c in camposAtuais" :key="c.chave" class="adm-campo" :class="{ largo: c.largo || c.tipo === 'textarea' }">
          <label :for="`cad-${c.chave}`">{{ c.rotulo }} <span v-if="c.obrigatorio" class="adm-obrig" aria-hidden="true">*</span></label>
          <textarea v-if="c.tipo === 'textarea'" :id="`cad-${c.chave}`" v-model="(dados[c.chave] as string)" class="adm-textarea" :maxlength="c.maxLength"></textarea>
          <select v-else-if="c.tipo === 'select'" :id="`cad-${c.chave}`" v-model="dados[c.chave]" class="adm-select">
            <option value="">(nenhum)</option>
            <option v-for="o in c.opcoes" :key="String(o.valor)" :value="o.valor">{{ o.rotulo }}</option>
          </select>
          <label v-else-if="c.tipo === 'checkbox'" class="adm-check"><input :id="`cad-${c.chave}`" v-model="dados[c.chave]" type="checkbox" /> Sim</label>
          <div v-else-if="c.tipo === 'cor'" style="display:flex;gap:8px;align-items:center">
            <input :id="`cad-${c.chave}`" v-model="(dados[c.chave] as string)" class="adm-input" placeholder="#RRGGBB" maxlength="7" />
            <input type="color" :value="/^#[0-9a-fA-F]{6}$/.test(String(dados[c.chave] ?? '')) ? String(dados[c.chave]) : '#06b6d4'" :aria-label="`Escolher ${c.rotulo}`" @input="dados[c.chave] = ($event.target as HTMLInputElement).value" />
          </div>
          <input v-else :id="`cad-${c.chave}`" v-model="dados[c.chave]" class="adm-input" :type="c.tipo === 'numero' ? 'number' : 'text'" :maxlength="c.maxLength" :min="c.tipo === 'numero' ? 0 : undefined" />
          <span v-if="c.ajuda" class="adm-ajuda">{{ c.ajuda }}</span>
          <span v-if="erros[c.chave]" class="adm-invalido" role="alert">{{ erros[c.chave] }}</span>
        </div>
      </form>
      <template #rodape>
        <button class="adm-btn" type="button" @click="modalAberto = false">Cancelar</button>
        <button class="adm-btn primario" type="submit" form="form-cadastro" :disabled="salvando">{{ salvando ? 'Salvando...' : 'Salvar' }}</button>
      </template>
    </UiModal>

    <UiModal v-model="confirmAberto" :titulo="`Excluir ${singular}`" largura="440px">
      <p>Excluir {{ art }} {{ singular.toLowerCase() }} <strong>{{ nomeExcluindo }}</strong>? A exclusão é lógica e o registro deixa de aparecer nas listas.</p>
      <template #rodape>
        <button class="adm-btn" @click="confirmAberto = false">Cancelar</button>
        <button class="adm-btn perigo" :disabled="removendo" @click="confirmarExclusao">{{ removendo ? 'Excluindo...' : 'Excluir' }}</button>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.adm-check { display: inline-flex; align-items: center; gap: 8px; font-size: 13px; color: var(--text-secondary); text-transform: none; letter-spacing: 0; font-weight: 400; }
.sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
p { font-size: 14px; color: var(--text-secondary); line-height: 1.5; }
</style>
