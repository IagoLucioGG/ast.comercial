<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { useEntidadeDetalhe, campo, inicialDe, carregarMoedas, formatarValorMoeda, mensagemErro, type MoedaInfo } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import UiTabs, { type AbaConfig } from '@/components/ui/UiTabs.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'
import ProdutoDadosForm from '@/components/produtos/ProdutoDadosForm.vue'
import ProdutoPrecosTab from '@/components/produtos/ProdutoPrecosTab.vue'
import ProdutoNegociosTab from '@/components/produtos/ProdutoNegociosTab.vue'
import ProdutoPropostasTab from '@/components/produtos/ProdutoPropostasTab.vue'
import ProdutoHistoricoTab from '@/components/produtos/ProdutoHistoricoTab.vue'
import { carregarFamiliasEGrupos, type Opcao } from '@/components/produtos/tiposProduto'

const route = useRoute()
const toast = useToast()
const id = computed(() => Number(route.params.id))
const { registro: produto, carregando, erro, naoEncontrado, carregar } = useEntidadeDetalhe(
  'Produtos', 'FamiliaProduto($select=Id,Nome),GrupoProduto($select=Id,Nome)',
)

const abaAtiva = ref('dados')
const chaveHistorico = ref(0)
const chavePrecos = ref(0)
const familias = ref<Opcao[]>([])
const grupos = ref<Opcao[]>([])
const moedas = ref<MoedaInfo[]>([])
const auxPronto = ref(false)

const abas: AbaConfig[] = [
  { chave: 'dados', rotulo: 'Dados', icone: 'mdi-form-select' },
  { chave: 'precos', rotulo: 'Preços', icone: 'mdi-tag-multiple-outline' },
  { chave: 'negocios', rotulo: 'Negócios', icone: 'mdi-handshake-outline' },
  { chave: 'propostas', rotulo: 'Propostas', icone: 'mdi-file-document-outline' },
  { chave: 'historico', rotulo: 'Histórico', icone: 'mdi-history' },
]

const moeda = computed(() => moedas.value.find((m) => m.id === campo(produto.value, 'MoedaId')))
const disponivel = computed(() => campo(produto.value, 'Disponivel') !== false)
const inativo = computed(() => campo(produto.value, 'Ativo') === false)

async function carregarAuxiliares() {
  try {
    const [fg, m] = await Promise.all([carregarFamiliasEGrupos(), carregarMoedas()])
    familias.value = fg.familias
    grupos.value = fg.grupos
    moedas.value = m
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível carregar famílias e grupos'))
  }
}

async function iniciar() {
  auxPronto.value = false
  await carregar(id.value)
  if (produto.value) await carregarAuxiliares()
  auxPronto.value = true
}
async function aoSalvar() {
  await carregar(id.value, true)
  chaveHistorico.value++
  chavePrecos.value++
}

watch(id, () => { abaAtiva.value = 'dados'; iniciar() })
onMounted(iniciar)
</script>

<template>
  <div class="pdv">
    <DetalheEstado :carregando="carregando" :erro="erro" @tentar="iniciar">
      <div v-if="naoEncontrado" class="pdv-nf" role="alert">
        <i class="mdi mdi-package-variant-closed-remove"></i>
        <h2>Produto não encontrado</h2>
        <p>O produto solicitado não existe ou você não tem acesso a ele.</p>
        <RouterLink class="pdv-voltar" to="/produtos"><i class="mdi mdi-arrow-left"></i> Voltar para produtos</RouterLink>
      </div>
      <template v-else-if="produto">
        <nav class="pdv-trilha" aria-label="Navegação">
          <RouterLink to="/produtos"><i class="mdi mdi-arrow-left"></i> Produtos</RouterLink>
        </nav>
        <header class="pdv-cab">
          <div class="pdv-avatar" aria-hidden="true">{{ inicialDe(campo(produto, 'Nome')) }}</div>
          <div class="pdv-corpo">
            <div class="pdv-titulo">
              <h1>{{ campo(produto, 'Nome') }}</h1>
              <UiBadge :tom="disponivel ? 'sucesso' : 'neutro'">{{ disponivel ? 'Disponível' : 'Indisponível' }}</UiBadge>
              <UiBadge v-if="inativo" tom="perigo">Excluído</UiBadge>
            </div>
            <div class="pdv-meta">
              <span v-if="campo(produto, 'Codigo')"><i class="mdi mdi-barcode"></i> {{ campo(produto, 'Codigo') }}</span>
              <span v-if="campo(produto, 'Unidade')"><i class="mdi mdi-ruler"></i> {{ campo(produto, 'Unidade') }}</span>
              <span v-if="campo(campo(produto, 'FamiliaProduto'), 'Nome')"><i class="mdi mdi-shape-outline"></i> {{ campo(campo(produto, 'FamiliaProduto'), 'Nome') }}</span>
              <span v-if="campo(campo(produto, 'GrupoProduto'), 'Nome')"><i class="mdi mdi-folder-outline"></i> {{ campo(campo(produto, 'GrupoProduto'), 'Nome') }}</span>
            </div>
          </div>
          <div class="pdv-preco">
            <span>Preço base</span>
            <strong>{{ formatarValorMoeda(campo(produto, 'Preco'), moeda?.simbolo, moeda?.codigo) }}</strong>
          </div>
        </header>
        <UiTabs v-model="abaAtiva" :abas="abas" />
        <section class="pdv-aba" :aria-label="abas.find((a) => a.chave === abaAtiva)?.rotulo">
          <DetalheEstado v-if="abaAtiva === 'dados' && !auxPronto" carregando />
          <ProdutoDadosForm v-else-if="abaAtiva === 'dados'" :produto="produto" :familias="familias" :grupos="grupos" :moedas="moedas" @salvo="aoSalvar" />
          <ProdutoPrecosTab v-else-if="abaAtiva === 'precos'" :key="chavePrecos" :produto-id="id" />
          <ProdutoNegociosTab v-else-if="abaAtiva === 'negocios'" :produto-id="id" />
          <ProdutoPropostasTab v-else-if="abaAtiva === 'propostas'" :produto-id="id" />
          <ProdutoHistoricoTab v-else :key="chaveHistorico" :produto-id="id" />
        </section>
      </template>
    </DetalheEstado>
  </div>
</template>

<style scoped>
.pdv { max-width: 1100px; margin: 0 auto; }
.pdv-trilha { margin-bottom: 10px; font-size: 13px; }
.pdv-trilha a, .pdv-voltar { color: var(--text-secondary); text-decoration: none; display: inline-flex; align-items: center; gap: 4px; }
.pdv-trilha a:hover, .pdv-voltar:hover { color: var(--accent); }
.pdv-nf { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 60px 16px; color: var(--text-secondary); text-align: center; }
.pdv-nf .mdi { font-size: 44px; color: var(--text-muted); }
.pdv-nf h2 { color: var(--text-primary); font-size: 18px; }
.pdv-cab { display: flex; gap: 16px; align-items: flex-start; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 14px; padding: 18px 20px; margin-bottom: 16px; }
.pdv-avatar { flex: 0 0 52px; height: 52px; border-radius: 14px; display: flex; align-items: center; justify-content: center; background: var(--bg-elevated); border: 1px solid var(--border); color: var(--accent); font-size: 22px; font-weight: 700; }
.pdv-corpo { flex: 1; min-width: 0; }
.pdv-titulo { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.pdv-titulo h1 { font-size: 20px; font-weight: 600; color: var(--text-primary); margin: 0; overflow-wrap: anywhere; }
.pdv-meta { display: flex; flex-wrap: wrap; gap: 6px 18px; margin-top: 8px; font-size: 13px; color: var(--text-secondary); }
.pdv-meta .mdi { color: var(--text-muted); margin-right: 3px; }
.pdv-preco { text-align: right; display: flex; flex-direction: column; gap: 2px; }
.pdv-preco span { font-size: 11px; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); }
.pdv-preco strong { font-size: 20px; color: var(--text-primary); white-space: nowrap; }
.pdv-aba { padding: 18px 2px; }
@media (max-width: 640px) { .pdv-cab { flex-wrap: wrap; } .pdv-preco { text-align: left; width: 100%; } }
</style>
