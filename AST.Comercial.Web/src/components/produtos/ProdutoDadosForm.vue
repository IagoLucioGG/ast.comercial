<script setup lang="ts">
import { reactive, computed, ref, watch } from 'vue'
import { odataRequest } from '@/services/api'
import { campo, mensagemErro, type Registro, type MoedaInfo } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import { CATEGORIAS, numeroOuNulo, type Opcao } from './tiposProduto'

/** Aba "Dados" do produto: edita os campos reais de Produto.cs. */
const props = defineProps<{
  produto: Registro
  familias: Opcao[]
  grupos: Opcao[]
  moedas: MoedaInfo[]
}>()
const emit = defineEmits<{ salvo: [] }>()
const toast = useToast()

const salvando = ref(false)
const erros = reactive<Record<string, string>>({})
const form = reactive({
  Nome: '', Codigo: '', Descricao: '', Unidade: '', Categoria: 'Produto',
  Preco: '' as number | string, PrecoCusto: '' as number | string,
  MoedaId: '' as number | string, FamiliaProdutoId: '' as number | string, GrupoProdutoId: '' as number | string,
  Disponivel: true,
})

function preencher() {
  const p = props.produto
  form.Nome = campo(p, 'Nome') ?? ''
  form.Codigo = campo(p, 'Codigo') ?? ''
  form.Descricao = campo(p, 'Descricao') ?? ''
  form.Unidade = campo(p, 'Unidade') ?? ''
  const cat = campo(p, 'Categoria')
  form.Categoria = typeof cat === 'number' ? (CATEGORIAS[cat]?.valor ?? 'Produto') : (cat ?? 'Produto')
  form.Preco = campo(p, 'Preco') ?? 0
  form.PrecoCusto = campo(p, 'PrecoCusto') ?? ''
  form.MoedaId = campo(p, 'MoedaId') ?? ''
  form.FamiliaProdutoId = campo(p, 'FamiliaProdutoId') ?? ''
  form.GrupoProdutoId = campo(p, 'GrupoProdutoId') ?? ''
  form.Disponivel = campo(p, 'Disponivel') !== false
  Object.keys(erros).forEach((k) => delete erros[k])
}
watch(() => props.produto, preencher, { immediate: true })

// Grupos da família escolhida (grupos sem família aparecem sempre).
const gruposVisiveis = computed(() => {
  const fam = numeroOuNulo(form.FamiliaProdutoId)
  return props.grupos.filter((g) => !fam || !g.familiaId || g.familiaId === fam || g.id === numeroOuNulo(form.GrupoProdutoId))
})
function aoMudarFamilia() {
  const g = props.grupos.find((x) => x.id === numeroOuNulo(form.GrupoProdutoId))
  const fam = numeroOuNulo(form.FamiliaProdutoId)
  if (g && fam && g.familiaId && g.familiaId !== fam) form.GrupoProdutoId = ''
}

function validar(): boolean {
  Object.keys(erros).forEach((k) => delete erros[k])
  if (!form.Nome.trim()) erros.Nome = 'Informe o nome.'
  const preco = numeroOuNulo(form.Preco)
  if (preco === null || preco < 0) erros.Preco = 'Informe um preço válido (zero ou maior).'
  const custo = numeroOuNulo(form.PrecoCusto)
  if (custo !== null && custo < 0) erros.PrecoCusto = 'O custo não pode ser negativo.'
  return Object.keys(erros).length === 0
}

async function salvar() {
  if (!validar()) return
  salvando.value = true
  try {
    await odataRequest('PATCH', `Produtos(${campo(props.produto, 'Id')})`, {
      Nome: form.Nome.trim(),
      Codigo: form.Codigo.trim() || null,
      Descricao: form.Descricao.trim() || null,
      Unidade: form.Unidade.trim() || null,
      Categoria: form.Categoria,
      Preco: numeroOuNulo(form.Preco) ?? 0,
      PrecoCusto: numeroOuNulo(form.PrecoCusto),
      MoedaId: numeroOuNulo(form.MoedaId),
      FamiliaProdutoId: numeroOuNulo(form.FamiliaProdutoId),
      GrupoProdutoId: numeroOuNulo(form.GrupoProdutoId),
      Disponivel: form.Disponivel,
    })
    toast.success('Produto salvo.')
    emit('salvo')
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível salvar o produto'))
  } finally {
    salvando.value = false
  }
}
</script>

<template>
  <form class="pd-form" novalidate @submit.prevent="salvar">
    <div class="pd-grade">
      <label class="pd-campo pd-largo">
        <span>Nome *</span>
        <input v-model="form.Nome" maxlength="200" :aria-invalid="!!erros.Nome" />
        <small v-if="erros.Nome" class="pd-erro" role="alert">{{ erros.Nome }}</small>
      </label>
      <label class="pd-campo"><span>Código</span><input v-model="form.Codigo" maxlength="100" /></label>
      <label class="pd-campo"><span>Unidade</span><input v-model="form.Unidade" maxlength="30" placeholder="un, h, mês..." /></label>
      <label class="pd-campo">
        <span>Categoria</span>
        <select v-model="form.Categoria"><option v-for="c in CATEGORIAS" :key="c.valor" :value="c.valor">{{ c.rotulo }}</option></select>
      </label>
      <label class="pd-campo">
        <span>Moeda</span>
        <select v-model="form.MoedaId">
          <option value="">Sem moeda</option>
          <option v-for="m in moedas" :key="m.id" :value="m.id">{{ m.simbolo }} · {{ m.codigo }}</option>
        </select>
      </label>
      <label class="pd-campo">
        <span>Preço base *</span>
        <input v-model="form.Preco" type="number" min="0" step="0.01" :aria-invalid="!!erros.Preco" />
        <small v-if="erros.Preco" class="pd-erro" role="alert">{{ erros.Preco }}</small>
      </label>
      <label class="pd-campo">
        <span>Preço de custo</span>
        <input v-model="form.PrecoCusto" type="number" min="0" step="0.01" :aria-invalid="!!erros.PrecoCusto" />
        <small v-if="erros.PrecoCusto" class="pd-erro" role="alert">{{ erros.PrecoCusto }}</small>
      </label>
      <label class="pd-campo">
        <span>Família</span>
        <select v-model="form.FamiliaProdutoId" @change="aoMudarFamilia">
          <option value="">Sem família</option>
          <option v-for="f in familias" :key="f.id" :value="f.id">{{ f.nome }}</option>
        </select>
      </label>
      <label class="pd-campo">
        <span>Grupo</span>
        <select v-model="form.GrupoProdutoId">
          <option value="">Sem grupo</option>
          <option v-for="g in gruposVisiveis" :key="g.id" :value="g.id">{{ g.nome }}</option>
        </select>
      </label>
      <label class="pd-campo pd-check"><input v-model="form.Disponivel" type="checkbox" /> <span>Disponível para venda</span></label>
      <label class="pd-campo pd-largo pd-total">
        <span>Descrição</span>
        <textarea v-model="form.Descricao" rows="4" maxlength="2000"></textarea>
      </label>
    </div>
    <div class="pd-acoes">
      <button type="button" class="pd-btn" :disabled="salvando" @click="preencher">Descartar alterações</button>
      <button type="submit" class="pd-btn pd-prim" :disabled="salvando">
        <i class="mdi" :class="salvando ? 'mdi-loading mdi-spin' : 'mdi-content-save-outline'"></i> Salvar
      </button>
    </div>
  </form>
</template>

<style scoped>
.pd-form { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 12px; padding: 18px; }
.pd-grade { display: grid; grid-template-columns: repeat(auto-fill, minmax(240px, 1fr)); gap: 14px; }
.pd-campo { display: flex; flex-direction: column; gap: 5px; font-size: 12px; color: var(--text-muted); }
.pd-campo input, .pd-campo select, .pd-campo textarea { padding: 8px 10px; background: var(--bg-primary); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); font-size: 13px; font-family: inherit; }
.pd-campo input:focus, .pd-campo select:focus, .pd-campo textarea:focus { outline: none; border-color: var(--accent); }
.pd-campo [aria-invalid='true'] { border-color: var(--danger); }
.pd-largo { grid-column: span 2; }
.pd-total { grid-column: 1 / -1; }
.pd-check { flex-direction: row; align-items: center; gap: 8px; color: var(--text-secondary); font-size: 13px; }
.pd-check input { width: 16px; height: 16px; accent-color: var(--accent); }
.pd-erro { color: var(--danger); font-size: 11px; }
.pd-acoes { display: flex; justify-content: flex-end; gap: 8px; margin-top: 18px; }
.pd-btn { display: inline-flex; align-items: center; gap: 6px; padding: 8px 16px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.pd-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.pd-btn:disabled { opacity: .6; cursor: default; }
.pd-prim { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.pd-prim:hover:not(:disabled) { background: var(--accent-hover); color: #000; }
@media (max-width: 640px) { .pd-largo { grid-column: 1 / -1; } }
</style>
