<script setup lang="ts">
import { onMounted, ref, watch } from 'vue'
import { montarQuery, odataRequest } from '@/services/api'
import { campo, lista, mensagemErro, type Registro } from '@/composables/useEntidadeDetalhe'
import { useToast } from '@/composables/useToast'

const props = defineProps<{ etapaId: number; etapaNome: string }>()
const emit = defineEmits<{ alterado: [] }>()

interface Item { id: number; texto: string; obrigatorio: boolean; ordem: number }
interface Checklist { id: number; nome: string; ordem: number; itens: Item[]; novoTexto: string; novoObrigatorio: boolean }

const toast = useToast()
const carregando = ref(false)
const erro = ref('')
const checklists = ref<Checklist[]>([])
const novoNome = ref('')
const criando = ref(false)

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({
      $filter: `EtapaFunilId eq ${props.etapaId}`,
      $expand: 'Itens($orderby=Ordem asc,Id asc)',
      $orderby: 'Ordem asc,Id asc',
      $top: 100,
    })
    checklists.value = lista<Registro>(await odataRequest<unknown>('GET', `ChecklistsEtapa${q}`)).map((c) => ({
      id: Number(campo(c, 'Id')),
      nome: String(campo(c, 'Nome') ?? ''),
      ordem: Number(campo(c, 'Ordem') ?? 0),
      novoTexto: '',
      novoObrigatorio: true,
      itens: lista<Registro>(campo(c, 'Itens')).map((i) => ({
        id: Number(campo(i, 'Id')), texto: String(campo(i, 'Texto') ?? ''), obrigatorio: !!campo(i, 'Obrigatorio'), ordem: Number(campo(i, 'Ordem') ?? 0),
      })),
    }))
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar os checklists')
  } finally {
    carregando.value = false
  }
}

async function executar(acao: () => Promise<unknown>, msgErro: string, recarregar = true) {
  try {
    await acao()
    if (recarregar) await carregar()
    emit('alterado')
  } catch (e) {
    toast.error(mensagemErro(e, msgErro))
    await carregar()
  }
}

async function criarChecklist() {
  const nome = novoNome.value.trim()
  if (!nome) { toast.error('Informe o nome do checklist.'); return }
  criando.value = true
  await executar(async () => {
    await odataRequest('POST', 'ChecklistsEtapa', { EtapaFunilId: props.etapaId, Nome: nome })
    novoNome.value = ''
  }, 'Não foi possível criar o checklist')
  criando.value = false
}

function renomearChecklist(c: Checklist) {
  const nome = c.nome.trim()
  if (!nome) { toast.error('Informe o nome do checklist.'); carregar(); return }
  executar(() => odataRequest('PATCH', `ChecklistsEtapa(${c.id})`, { Nome: nome }), 'Não foi possível renomear o checklist', false)
}

function removerChecklist(c: Checklist) {
  if (!window.confirm(`Remover o checklist "${c.nome}" e seus itens? Negócios que já responderam não perdem o histórico.`)) return
  executar(() => odataRequest('DELETE', `ChecklistsEtapa(${c.id})`), 'Não foi possível remover o checklist')
}

function adicionarItem(c: Checklist) {
  const texto = c.novoTexto.trim()
  if (!texto) { toast.error('Informe o texto do item.'); return }
  executar(
    () => odataRequest('POST', 'ItensChecklistEtapa', { ChecklistEtapaId: c.id, Texto: texto, Obrigatorio: c.novoObrigatorio }),
    'Não foi possível adicionar o item',
  )
}

function salvarItem(i: Item) {
  const texto = i.texto.trim()
  if (!texto) { toast.error('Informe o texto do item.'); carregar(); return }
  executar(() => odataRequest('PATCH', `ItensChecklistEtapa(${i.id})`, { Texto: texto, Obrigatorio: i.obrigatorio }), 'Não foi possível salvar o item', false)
}

function removerItem(i: Item) {
  if (!window.confirm(`Remover o item "${i.texto}"?`)) return
  executar(() => odataRequest('DELETE', `ItensChecklistEtapa(${i.id})`), 'Não foi possível remover o item')
}

/** Reordena renumerando os itens 0..n-1 (só envia os que mudaram de posição). */
function moverItem(c: Checklist, idx: number, delta: -1 | 1) {
  const j = idx + delta
  if (j < 0 || j >= c.itens.length) return
  const nova = [...c.itens]
  const a = nova[idx]
  const b = nova[j]
  if (!a || !b) return
  nova[idx] = b
  nova[j] = a
  c.itens = nova
  const mudados = nova.map((it, pos) => ({ it, pos })).filter(({ it, pos }) => it.ordem !== pos)
  executar(
    () => Promise.all(mudados.map(({ it, pos }) => odataRequest('PATCH', `ItensChecklistEtapa(${it.id})`, { Ordem: pos }))),
    'Não foi possível reordenar os itens',
  )
}

watch(() => props.etapaId, carregar)
onMounted(carregar)
</script>

<template>
  <div class="ce" :aria-label="`Checklists da etapa ${etapaNome}`">
    <p v-if="carregando" class="ce-msg" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando checklists...</p>
    <p v-else-if="erro" class="ce-msg ce-erro" role="alert">{{ erro }} <button type="button" class="ce-link" @click="carregar">Tentar novamente</button></p>
    <template v-else>
      <p v-if="!checklists.length" class="ce-msg">Nenhum checklist nesta etapa. Itens obrigatórios impedem o negócio de avançar até serem concluídos.</p>

      <section v-for="c in checklists" :key="c.id" class="ce-bloco">
        <div class="ce-cab">
          <input v-model="c.nome" class="ce-in ce-nome" type="text" maxlength="200" :aria-label="`Nome do checklist ${c.nome}`" @change="renomearChecklist(c)" />
          <button type="button" class="ce-ic ce-del" :aria-label="`Remover checklist ${c.nome}`" title="Remover checklist" @click="removerChecklist(c)"><i class="mdi mdi-delete-outline" aria-hidden="true"></i></button>
        </div>

        <ul class="ce-itens">
          <li v-for="(i, idx) in c.itens" :key="i.id">
            <input v-model="i.texto" class="ce-in ce-texto" type="text" maxlength="500" :aria-label="`Texto do item ${idx + 1}`" @change="salvarItem(i)" />
            <label class="ce-obr"><input v-model="i.obrigatorio" type="checkbox" @change="salvarItem(i)" /> Obrigatório</label>
            <button type="button" class="ce-ic" :disabled="idx === 0" :aria-label="`Subir item ${i.texto}`" title="Subir" @click="moverItem(c, idx, -1)"><i class="mdi mdi-arrow-up" aria-hidden="true"></i></button>
            <button type="button" class="ce-ic" :disabled="idx === c.itens.length - 1" :aria-label="`Descer item ${i.texto}`" title="Descer" @click="moverItem(c, idx, 1)"><i class="mdi mdi-arrow-down" aria-hidden="true"></i></button>
            <button type="button" class="ce-ic ce-del" :aria-label="`Remover item ${i.texto}`" title="Remover item" @click="removerItem(i)"><i class="mdi mdi-close" aria-hidden="true"></i></button>
          </li>
        </ul>

        <form class="ce-novo" @submit.prevent="adicionarItem(c)">
          <input v-model="c.novoTexto" class="ce-in ce-texto" type="text" maxlength="500" placeholder="Novo item do checklist" :aria-label="`Novo item de ${c.nome}`" />
          <label class="ce-obr"><input v-model="c.novoObrigatorio" type="checkbox" /> Obrigatório</label>
          <button type="submit" class="ce-btn"><i class="mdi mdi-plus" aria-hidden="true"></i> Item</button>
        </form>
      </section>

      <form class="ce-novo ce-novo-lista" @submit.prevent="criarChecklist">
        <input v-model="novoNome" class="ce-in ce-texto" type="text" maxlength="200" placeholder="Nome de um novo checklist (ex.: Qualificação)" :aria-label="`Novo checklist da etapa ${etapaNome}`" />
        <button type="submit" class="ce-btn" :disabled="criando"><i class="mdi mdi-playlist-plus" aria-hidden="true"></i> Novo checklist</button>
      </form>
    </template>
  </div>
</template>

<style scoped>
.ce { margin-top: 8px; padding: 12px; background: var(--bg-primary); border: 1px dashed var(--border); border-radius: 10px; }
.ce-msg { font-size: 12px; color: var(--text-muted); margin: 0 0 10px; }
.ce-erro { color: var(--danger); }
.ce-link { background: none; border: 0; color: var(--accent); cursor: pointer; text-decoration: underline; font-size: 12px; }
.ce-bloco { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 8px; padding: 10px; margin-bottom: 10px; }
.ce-cab { display: flex; align-items: center; gap: 6px; margin-bottom: 8px; }
.ce-in { padding: 6px 8px; border: 1px solid var(--border); border-radius: 6px; background: var(--bg-primary); color: var(--text-primary); font-size: 13px; min-width: 0; }
.ce-in:focus { outline: none; border-color: var(--accent); }
.ce-nome { flex: 1; font-weight: 600; }
.ce-texto { flex: 1; }
.ce-itens { list-style: none; margin: 0 0 8px; padding: 0; display: flex; flex-direction: column; gap: 6px; }
.ce-itens li, .ce-novo { display: flex; align-items: center; gap: 6px; flex-wrap: wrap; }
.ce-obr { display: inline-flex; align-items: center; gap: 4px; font-size: 12px; color: var(--text-secondary); white-space: nowrap; cursor: pointer; }
.ce-obr input { accent-color: var(--accent); }
.ce-ic { width: 26px; height: 26px; border: 1px solid transparent; border-radius: 6px; background: transparent; color: var(--text-muted); cursor: pointer; font-size: 15px; }
.ce-ic:hover:not(:disabled), .ce-ic:focus-visible { background: var(--bg-elevated); color: var(--text-primary); border-color: var(--border); outline: none; }
.ce-ic:disabled { opacity: .35; cursor: not-allowed; }
.ce-del:hover:not(:disabled) { color: var(--danger); }
.ce-btn { display: inline-flex; align-items: center; gap: 4px; padding: 6px 12px; border: 1px solid var(--border); border-radius: 6px; background: var(--bg-primary); color: var(--text-secondary); font-size: 12px; font-weight: 600; cursor: pointer; white-space: nowrap; }
.ce-btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.ce-btn:disabled { opacity: .5; cursor: not-allowed; }
.ce-novo-lista { margin-top: 4px; }
</style>
