<script setup lang="ts">
import { computed, nextTick, onMounted, ref } from 'vue'
import { montarQuery, odataRequest } from '@/services/api'
import { campo, lista, mensagemErro, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'
import FunilEditor, { type FunilReg } from '@/components/admin/funis/FunilEditor.vue'

const toast = useToast()
const funis = ref<FunilReg[]>([])
const carregando = ref(false)
const erro = ref('')
const selecionadoId = ref<number | null>(null)
const novoNome = ref('')
const criando = ref(false)
const campoNovo = ref<HTMLInputElement | null>(null)

const selecionado = computed(() => funis.value.find((f) => f.id === selecionadoId.value) ?? null)

function paraFunil(r: Registro): FunilReg {
  return {
    id: Number(campo(r, 'Id')),
    nome: String(campo(r, 'Nome') ?? ''),
    descricao: String(campo(r, 'Descricao') ?? ''),
    proibirRetorno: !!campo(r, 'ProibirRetornoEtapa'),
    exigirTodas: !!campo(r, 'ExigirTodasEtapas'),
  }
}

async function carregar(manterSelecao = true) {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({ $select: 'Id,Nome,Descricao,Ordem,ProibirRetornoEtapa,ExigirTodasEtapas', $orderby: 'Ordem asc,Id asc', $top: 100 })
    funis.value = lista<Registro>(await odataRequest<unknown>('GET', `Funis${q}`)).map(paraFunil)
    if (!manterSelecao || !funis.value.some((f) => f.id === selecionadoId.value)) selecionadoId.value = funis.value[0]?.id ?? null
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar os funis')
  } finally {
    carregando.value = false
  }
}

/** Recarrega a lista sem mexer na tela (nomes/regras podem ter mudado). */
async function atualizarLista() {
  try {
    const q = montarQuery({ $select: 'Id,Nome,Descricao,Ordem,ProibirRetornoEtapa,ExigirTodasEtapas', $orderby: 'Ordem asc,Id asc', $top: 100 })
    funis.value = lista<Registro>(await odataRequest<unknown>('GET', `Funis${q}`)).map(paraFunil)
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível atualizar a lista de funis'))
  }
}

async function criar() {
  const nome = novoNome.value.trim()
  if (!nome) { toast.error('Informe o nome do funil.'); campoNovo.value?.focus(); return }
  criando.value = true
  try {
    const novo = await odataRequest<Registro>('POST', 'Funis', { Nome: nome })
    novoNome.value = ''
    await carregar()
    selecionadoId.value = Number(campo(novo, 'Id'))
    toast.success('Funil criado. Adicione as etapas abaixo.')
    await nextTick()
  } catch (e) {
    toast.error(mensagemErro(e, 'Não foi possível criar o funil'))
  } finally {
    criando.value = false
  }
}

async function aoArquivar() {
  selecionadoId.value = null
  await carregar(false)
}

onMounted(() => carregar())
</script>

<template>
  <div class="fa">
    <aside class="fa-lista" aria-label="Funis">
      <form class="fa-novo" @submit.prevent="criar">
        <label class="fa-sr" for="fa-novo-nome">Nome do novo funil</label>
        <input id="fa-novo-nome" ref="campoNovo" v-model="novoNome" type="text" maxlength="200" placeholder="Novo funil" />
        <button type="submit" class="fa-add" :disabled="criando" aria-label="Criar funil" title="Criar funil"><i class="mdi" :class="criando ? 'mdi-loading mdi-spin' : 'mdi-plus'" aria-hidden="true"></i></button>
      </form>

      <p v-if="carregando && !funis.length" class="fa-msg" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando...</p>
      <p v-else-if="erro" class="fa-msg fa-erro" role="alert">{{ erro }} <button type="button" class="fa-link" @click="carregar()">Tentar novamente</button></p>
      <p v-else-if="!funis.length" class="fa-msg">Nenhum funil cadastrado. Crie o primeiro acima.</p>
      <ul v-else class="fa-itens">
        <li v-for="f in funis" :key="f.id">
          <button type="button" class="fa-item" :class="{ ativo: f.id === selecionadoId }" :aria-current="f.id === selecionadoId ? 'true' : undefined" @click="selecionadoId = f.id">
            <i class="mdi mdi-filter-variant" aria-hidden="true"></i>
            <span>{{ f.nome }}</span>
          </button>
        </li>
      </ul>
    </aside>

    <section class="fa-editor" aria-live="polite">
      <FunilEditor v-if="selecionado" :key="selecionado.id" :funil="selecionado" @alterado="atualizarLista" @arquivado="aoArquivar" />
      <div v-else-if="!carregando && !erro" class="fa-vazio">
        <i class="mdi mdi-filter-variant-plus" aria-hidden="true"></i>
        <p>Selecione um funil para editar suas etapas, regras e checklists.</p>
      </div>
    </section>
  </div>
</template>

<style scoped>
.fa { display: grid; grid-template-columns: 260px minmax(0, 1fr); gap: 16px; align-items: start; }
.fa-lista { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 12px; padding: 12px; position: sticky; top: 0; }
.fa-novo { display: flex; gap: 6px; margin-bottom: 10px; }
.fa-novo input { flex: 1; min-width: 0; padding: 8px 10px; border: 1px solid var(--border); border-radius: 8px; background: var(--bg-primary); color: var(--text-primary); font-size: 13px; }
.fa-novo input:focus { outline: none; border-color: var(--accent); }
.fa-add { width: 34px; border: 0; border-radius: 8px; background: var(--accent); color: #000; cursor: pointer; font-size: 18px; }
.fa-add:hover:not(:disabled) { background: var(--accent-hover); }
.fa-add:disabled { opacity: .6; cursor: not-allowed; }
.fa-itens { list-style: none; margin: 0; padding: 0; display: flex; flex-direction: column; gap: 4px; }
.fa-item { width: 100%; display: flex; align-items: center; gap: 8px; padding: 9px 10px; border: 1px solid transparent; border-radius: 8px; background: transparent; color: var(--text-secondary); font-size: 13px; text-align: left; cursor: pointer; }
.fa-item span { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.fa-item:hover { background: var(--bg-elevated); color: var(--text-primary); }
.fa-item.ativo { background: var(--bg-elevated); border-color: var(--accent); color: var(--accent); }
.fa-msg { font-size: 13px; color: var(--text-muted); margin: 0; }
.fa-erro { color: var(--danger); }
.fa-link { background: none; border: 0; color: var(--accent); cursor: pointer; text-decoration: underline; font-size: 12px; }
.fa-vazio { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 60px 16px; color: var(--text-muted); font-size: 13px; text-align: center; }
.fa-vazio .mdi { font-size: 36px; }
.fa-sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
@media (max-width: 800px) { .fa { grid-template-columns: 1fr; } .fa-lista { position: static; } }
</style>
