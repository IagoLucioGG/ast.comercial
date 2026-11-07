<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import UiModal from '@/components/ui/UiModal.vue'
import { useToast } from '@/composables/useToast'
import EditorAutomacao from './automacoes/EditorAutomacao.vue'
import HistoricoExecucoes from './automacoes/HistoricoExecucoes.vue'
import { automacaoApi, mensagensDoErro, formatarData, type AutomacaoDto, type Catalogo } from './automacoes/automacaoApi'

const toast = useToast()
const catalogo = ref<Catalogo | null>(null)
const entidade = ref('')
const busca = ref('')
const itens = ref<AutomacaoDto[]>([])
const total = ref(0)
const carregandoCat = ref(true)
const carregando = ref(false)
const erro = ref(false)
const editorAberto = ref(false)
const editandoId = ref<number | null>(null)
const historico = ref<AutomacaoDto | null>(null)
const historicoAberto = ref(false)
const excluindo = ref<AutomacaoDto | null>(null)
const ocupados = ref<Set<number>>(new Set())
let debounce: ReturnType<typeof setTimeout> | null = null

const entidadeAtual = computed(() => catalogo.value?.entidades.find((e) => e.valor === entidade.value))
const nomeGatilho = (v: string) => catalogo.value?.gatilhos.find((g) => g.valor === v)?.nome ?? v

async function carregarCatalogo() {
  carregandoCat.value = true
  try {
    catalogo.value = await automacaoApi.catalogo()
    entidade.value = catalogo.value.entidades[0]?.valor ?? ''
  } catch (e) {
    erro.value = true
    toast.error(mensagensDoErro(e)[0] ?? 'Falha ao carregar o catálogo de automações.')
  } finally { carregandoCat.value = false }
}

async function carregar() {
  if (!entidade.value) return
  carregando.value = true; erro.value = false
  try {
    const r = await automacaoApi.listar({ entidadeAlvo: entidade.value, busca: busca.value.trim() })
    itens.value = r.itens; total.value = r.total
  } catch (e) {
    erro.value = true; itens.value = []
    toast.error(mensagensDoErro(e)[0] ?? 'Falha ao listar automações.')
  } finally { carregando.value = false }
}

watch(entidade, carregar)
watch(busca, () => { if (debounce) clearTimeout(debounce); debounce = setTimeout(carregar, 300) })
onMounted(async () => { await carregarCatalogo(); await carregar() })

function nova() { editandoId.value = null; editorAberto.value = true }
function editar(a: AutomacaoDto) { editandoId.value = a.id; editorAberto.value = true }
function abrirHistorico(a: AutomacaoDto) { historico.value = a; historicoAberto.value = true }

async function alternar(a: AutomacaoDto) {
  if (ocupados.value.has(a.id)) return
  ocupados.value.add(a.id)
  try {
    const r = a.ativo ? await automacaoApi.desativar(a.id) : await automacaoApi.ativar(a.id)
    Object.assign(a, r)
    toast.success(r.ativo ? 'Automação ativada.' : 'Automação desativada.')
  } catch (e) { toast.error(mensagensDoErro(e)[0] ?? 'Não foi possível alterar o estado.') }
  finally { ocupados.value.delete(a.id) }
}

async function confirmarExclusao() {
  const a = excluindo.value
  if (!a) return
  try {
    await automacaoApi.remover(a.id)
    toast.success('Automação excluída.')
    excluindo.value = null
    await carregar()
  } catch (e) { toast.error(mensagensDoErro(e)[0] ?? 'Não foi possível excluir.') }
}
</script>

<template>
  <div class="auto">
    <aside class="ents" aria-label="Entidades">
      <h3>Entidades</h3>
      <p v-if="carregandoCat" class="mut" role="status">Carregando...</p>
      <p v-else-if="!catalogo" class="mut erro">Catálogo indisponível. <button class="link" @click="carregarCatalogo().then(carregar)">Tentar de novo</button></p>
      <ul v-else role="listbox" aria-label="Entidades com automação">
        <li v-for="e in catalogo.entidades" :key="e.valor">
          <button role="option" :aria-selected="e.valor === entidade" :class="{ ativa: e.valor === entidade }" @click="entidade = e.valor">
            {{ e.nome }}
          </button>
        </li>
      </ul>
    </aside>

    <section class="lista">
      <div class="barra">
        <h3>{{ entidadeAtual?.nome ?? 'Automações' }}</h3>
        <div class="busca">
          <i class="mdi mdi-magnify" aria-hidden="true"></i>
          <input v-model="busca" type="search" placeholder="Buscar automação" aria-label="Buscar automação" />
        </div>
        <span class="grow"></span>
        <button class="btn pri" :disabled="!catalogo || !entidade" @click="nova"><i class="mdi mdi-plus"></i> Nova automação</button>
      </div>

      <p v-if="carregando" class="estado" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando...</p>
      <p v-else-if="erro" class="estado erro" role="alert">Não foi possível carregar. <button class="link" @click="carregar">Tentar de novo</button></p>
      <p v-else-if="!itens.length" class="estado">{{ busca ? 'Nenhuma automação encontrada.' : 'Nenhuma automação para esta entidade ainda.' }}</p>
      <div v-else class="tw">
        <table>
          <thead><tr><th>Nome</th><th>Gatilho</th><th>Ativa</th><th>Última execução</th><th>Criada em</th><th>Criada por</th><th><span class="sr">Ações</span></th></tr></thead>
          <tbody>
            <tr v-for="a in itens" :key="a.id">
              <td class="forte">
                <button class="nome" @click="editar(a)">{{ a.nome }}</button>
                <div v-if="a.descricao" class="desc">{{ a.descricao }}</div>
              </td>
              <td><UiBadge tom="destaque">{{ nomeGatilho(a.gatilho) }}</UiBadge></td>
              <td>
                <button class="switch" role="switch" :aria-checked="a.ativo" :aria-label="`${a.ativo ? 'Desativar' : 'Ativar'} ${a.nome}`"
                  :class="{ on: a.ativo }" :disabled="ocupados.has(a.id)" @click="alternar(a)"><span></span></button>
              </td>
              <td>{{ formatarData(a.ultimaExecucao) }}</td>
              <td>{{ formatarData(a.criadoEm) }}</td>
              <td>{{ a.criadoPor || '—' }}</td>
              <td class="acoes">
                <button class="ico" :aria-label="`Histórico de ${a.nome}`" title="Histórico de execuções" @click="abrirHistorico(a)"><i class="mdi mdi-history"></i></button>
                <button class="ico" :aria-label="`Editar ${a.nome}`" title="Editar" @click="editar(a)"><i class="mdi mdi-pencil-outline"></i></button>
                <button class="ico perigo" :aria-label="`Excluir ${a.nome}`" title="Excluir" @click="excluindo = a"><i class="mdi mdi-delete-outline"></i></button>
              </td>
            </tr>
          </tbody>
        </table>
        <p class="rodape">{{ total }} {{ total === 1 ? 'automação' : 'automações' }}</p>
      </div>
    </section>

    <EditorAutomacao v-if="catalogo" v-model="editorAberto" :automacao-id="editandoId" :entidade="entidade" :catalogo="catalogo" @salvo="carregar" />
    <HistoricoExecucoes v-model="historicoAberto" :automacao-id="historico?.id ?? null" :nome="historico?.nome ?? ''" />

    <UiModal :model-value="!!excluindo" titulo="Excluir automação" largura="440px" @update:model-value="excluindo = null">
      <p class="conf">Excluir a automação <strong>{{ excluindo?.nome }}</strong>? Itens pendentes na fila serão cancelados. Esta ação não pode ser desfeita.</p>
      <template #rodape>
        <button class="btn" @click="excluindo = null">Cancelar</button>
        <button class="btn danger" @click="confirmarExclusao">Excluir</button>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.auto { display: flex; gap: 16px; height: 100%; min-height: 0; }
.ents { width: 210px; flex-shrink: 0; border: 1px solid var(--border); border-radius: 10px; background: var(--bg-surface); padding: 12px 8px; overflow-y: auto; }
.ents h3, .barra h3 { font-size: 13px; font-weight: 600; color: var(--text-primary); }
.ents h3 { padding: 0 8px 8px; text-transform: uppercase; font-size: 11px; color: var(--text-muted); }
.ents ul { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 2px; }
.ents li button { width: 100%; text-align: left; background: none; border: 0; border-radius: 6px; padding: 8px 10px; font-size: 13px; color: var(--text-secondary); cursor: pointer; }
.ents li button:hover { background: var(--bg-elevated); }
.ents li button.ativa { background: var(--bg-elevated); color: var(--accent); font-weight: 600; }
.lista { flex: 1; min-width: 0; display: flex; flex-direction: column; gap: 12px; }
.barra { display: flex; align-items: center; gap: 12px; }
.barra h3 { font-size: 15px; }
.busca { position: relative; }
.busca .mdi { position: absolute; left: 9px; top: 50%; transform: translateY(-50%); color: var(--text-muted); }
.busca input { background: var(--bg-secondary); border: 1px solid var(--border); border-radius: 8px; color: var(--text-primary); padding: 7px 10px 7px 30px; font-size: 13px; width: 240px; }
.grow { flex: 1; }
.mut { font-size: 12px; color: var(--text-muted); padding: 0 8px; }
.mut.erro, .estado.erro { color: var(--danger); }
.estado { text-align: center; padding: 48px; color: var(--text-muted); font-size: 13px; }
.tw { flex: 1; overflow: auto; border: 1px solid var(--border); border-radius: 10px; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th { position: sticky; top: 0; z-index: 1; background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; text-align: left; padding: 11px 14px; border-bottom: 1px solid var(--border); }
td { padding: 10px 14px; border-bottom: 1px solid var(--border); color: var(--text-secondary); vertical-align: middle; }
tr:hover td { background: var(--bg-elevated); }
.forte { color: var(--text-primary); }
.nome { background: none; border: 0; padding: 0; color: var(--text-primary); font-weight: 500; font-size: 13px; cursor: pointer; text-align: left; }
.nome:hover { color: var(--accent); }
.desc { font-size: 11px; color: var(--text-muted); max-width: 360px; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.acoes { white-space: nowrap; text-align: right; }
.rodape { padding: 10px 14px; font-size: 12px; color: var(--text-muted); }
.sr { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); }
.switch { width: 36px; height: 20px; border-radius: 999px; border: 1px solid var(--border); background: var(--bg-elevated); position: relative; cursor: pointer; padding: 0; transition: background .15s; }
.switch span { position: absolute; top: 2px; left: 2px; width: 14px; height: 14px; border-radius: 50%; background: var(--text-muted); transition: transform .15s, background .15s; }
.switch.on { background: var(--accent); border-color: var(--accent); }
.switch.on span { transform: translateX(16px); background: #fff; }
.switch:disabled { opacity: .5; cursor: default; }
.ico { width: 30px; height: 30px; border-radius: 6px; border: 1px solid transparent; background: none; color: var(--text-secondary); cursor: pointer; }
.ico:hover { background: var(--bg-surface); border-color: var(--border); color: var(--text-primary); }
.ico.perigo:hover { color: var(--danger); border-color: var(--danger); }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 8px 14px; border-radius: 8px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.btn.pri { background: var(--accent); border-color: var(--accent); color: #fff; }
.btn.pri:hover:not(:disabled) { background: var(--accent-hover); }
.btn.danger { background: var(--danger); border-color: var(--danger); color: #fff; }
.btn:disabled { opacity: .5; cursor: default; }
.link { background: none; border: 0; color: var(--accent); cursor: pointer; }
.conf { font-size: 14px; color: var(--text-secondary); line-height: 1.5; }
button:focus-visible, input:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
@media (max-width: 760px) { .auto { flex-direction: column; } .ents { width: auto; } }
</style>
