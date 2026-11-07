<script setup lang="ts">
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { odataRequest } from '@/services/api'
import { campo, mensagemErro, carregarMoedas, type Registro, type MoedaInfo } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'
import UiTabs, { type AbaConfig } from '@/components/ui/UiTabs.vue'
import UiModal from '@/components/ui/UiModal.vue'
import ListasPrecoPainel from '@/components/produtos/ListasPrecoPainel.vue'
import { numeroOuNulo } from '@/components/produtos/tiposProduto'

const route = useRoute()
const router = useRouter()
const toast = useToast()

const colunas: ColunaConfig[] = [
  { campo: 'Codigo', label: 'Código' },
  { campo: 'Nome', label: 'Nome', tipo: 'avatar' },
  { campo: 'FamiliaProduto/Nome', label: 'Família' },
  { campo: 'GrupoProduto/Nome', label: 'Grupo' },
  { campo: 'Preco', label: 'Preço', tipo: 'moeda' },
  { campo: 'Disponivel', label: 'Disponível', tipo: 'booleano' },
  { campo: 'Ativo', label: 'Status', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]

const abas: AbaConfig[] = [
  { chave: 'produtos', rotulo: 'Produtos', icone: 'mdi-package-variant-closed' },
  { chave: 'listas', rotulo: 'Listas de preço', icone: 'mdi-tag-multiple-outline' },
]
// A aba fica na URL (?aba=listas) para sobreviver a recarregar a página.
const aba = computed({
  get: () => (route.query.aba === 'listas' ? 'listas' : 'produtos'),
  set: (v: string) => {
    const { aba: _a, ...resto } = route.query
    router.replace({ query: v === 'listas' ? { ...resto, aba: 'listas' } : resto })
  },
})

// ----- Criação rápida (já leva ao detalhe) -----
const criarAberto = ref(false)
const criando = ref(false)
const moedas = ref<MoedaInfo[]>([])
const novo = reactive({ Nome: '', Codigo: '', Preco: '' as number | string, MoedaId: '' as number | string })
const erros = reactive<Record<string, string>>({})

async function abrirNovo() {
  novo.Nome = ''; novo.Codigo = ''; novo.Preco = ''
  Object.keys(erros).forEach((k) => delete erros[k])
  moedas.value = await carregarMoedas()
  novo.MoedaId = moedas.value.find((m) => m.codigo === 'BRL')?.id ?? ''
  criarAberto.value = true
}

async function criar() {
  Object.keys(erros).forEach((k) => delete erros[k])
  if (!novo.Nome.trim()) erros.Nome = 'Informe o nome do produto.'
  const preco = novo.Preco === '' ? 0 : numeroOuNulo(novo.Preco)
  if (preco === null || preco < 0) erros.Preco = 'Informe um preço válido (zero ou maior).'
  if (Object.keys(erros).length) return
  criando.value = true
  try {
    const criado = await odataRequest<Registro>('POST', 'Produtos', {
      Nome: novo.Nome.trim(),
      Codigo: novo.Codigo.trim() || null,
      Preco: preco,
      MoedaId: numeroOuNulo(novo.MoedaId),
      Disponivel: true,
    })
    criarAberto.value = false
    toast.success('Produto criado.')
    router.push(`/produtos/${campo(criado, 'Id')}`)
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível criar o produto'))
  } finally {
    criando.value = false
  }
}

function abrirDetalhe(id: number) { router.push(`/produtos/${id}`) }

// ?novo=1 abre a criação rápida (e limpa o parâmetro).
function verificarNovo() {
  if (route.query.novo === '1') {
    const { novo: _n, ...resto } = route.query
    router.replace({ query: resto })
    abrirNovo()
  }
}
watch(() => route.query.novo, verificarNovo)
onMounted(verificarNovo)
</script>

<template>
  <div class="pv">
    <UiTabs v-model="aba" :abas="abas" />
    <div class="pv-corpo">
      <TabelaEntidade
        v-if="aba === 'produtos'"
        entidade="Produto"
        endpoint="Produtos"
        titulo="Produto"
        :colunas="colunas"
        :campos-busca="['Nome', 'Codigo']"
        placeholder-busca="Buscar produto..."
        ordenacao-padrao="Nome asc"
        sem-form-interno
        @novo="abrirNovo"
        @editar="abrirDetalhe"
      />
      <ListasPrecoPainel v-else />
    </div>

    <UiModal v-model="criarAberto" titulo="Novo produto" largura="460px">
      <form class="pv-form" novalidate @submit.prevent="criar">
        <label class="pv-campo">
          <span>Nome *</span>
          <input v-model="novo.Nome" maxlength="200" autofocus :aria-invalid="!!erros.Nome" />
          <small v-if="erros.Nome" class="pv-erro" role="alert">{{ erros.Nome }}</small>
        </label>
        <label class="pv-campo"><span>Código</span><input v-model="novo.Codigo" maxlength="100" /></label>
        <div class="pv-linha">
          <label class="pv-campo">
            <span>Moeda</span>
            <select v-model="novo.MoedaId">
              <option value="">Sem moeda</option>
              <option v-for="m in moedas" :key="m.id" :value="m.id">{{ m.simbolo }} · {{ m.codigo }}</option>
            </select>
          </label>
          <label class="pv-campo">
            <span>Preço base</span>
            <input v-model="novo.Preco" type="number" min="0" step="0.01" :aria-invalid="!!erros.Preco" />
            <small v-if="erros.Preco" class="pv-erro" role="alert">{{ erros.Preco }}</small>
          </label>
        </div>
        <button type="submit" hidden>Criar</button>
      </form>
      <template #rodape>
        <button class="pv-btn" @click="criarAberto = false">Cancelar</button>
        <button class="pv-btn pv-prim" :disabled="criando" @click="criar">
          <i class="mdi" :class="criando ? 'mdi-loading mdi-spin' : 'mdi-check'"></i> Criar e abrir
        </button>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.pv-corpo { padding-top: 12px; }
.pv-form { display: flex; flex-direction: column; gap: 12px; }
.pv-linha { display: grid; grid-template-columns: 1fr 1fr; gap: 12px; }
.pv-campo { display: flex; flex-direction: column; gap: 5px; font-size: 12px; color: var(--text-muted); }
.pv-campo input, .pv-campo select { padding: 8px 10px; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 13px; }
.pv-campo input:focus, .pv-campo select:focus { outline: none; border-color: var(--accent); }
.pv-campo [aria-invalid='true'] { border-color: var(--danger); }
.pv-erro { color: var(--danger); font-size: 11px; }
.pv-btn { padding: 7px 14px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-secondary); font-size: 13px; cursor: pointer; display: inline-flex; align-items: center; gap: 6px; }
.pv-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.pv-btn:disabled { opacity: .6; cursor: default; }
.pv-prim { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.pv-prim:hover:not(:disabled) { background: var(--accent-hover); color: #000; }
</style>
