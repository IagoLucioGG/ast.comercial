<script setup lang="ts">
import { ref, onMounted, onBeforeUnmount, watch } from 'vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import { useToast } from '@/composables/useToast'
import { odataRequest, montarQuery } from '@/services/api'
import { automacaoApi, mensagensDoErro, formatarData, type Catalogo } from './automacaoApi'

interface ItemFila {
  id: number; automacaoId: number; registroId: number | null; entidadeAlvo: string
  gatilho: string; status: string; tentativas: number; maxTentativas: number
  criadoEm: string; proximaTentativa: string | null; ultimoErro: string | null
}
const catalogo = ref<Catalogo | null>(null)
const toast = useToast()
const nomesStatus = ['Pendente', 'EmExecucao', 'Concluido', 'Falha', 'FalhaDefinitiva', 'Cancelado']
const rotulos: Record<string, string> = {
  Pendente: 'Pendente', EmExecucao: 'Em execução', Concluido: 'Concluído', Falha: 'Falha',
  FalhaDefinitiva: 'Falha definitiva', Cancelado: 'Cancelado',
}
const tons: Record<string, 'sucesso' | 'perigo' | 'aviso' | 'info' | 'neutro'> = {
  Pendente: 'aviso', EmExecucao: 'info', Concluido: 'sucesso', Falha: 'perigo', FalhaDefinitiva: 'perigo', Cancelado: 'neutro',
}

const itens = ref<ItemFila[]>([])
const temMais = ref(false)
const pagina = ref(1)
const porPagina = 25
const filtro = ref('')
const carregando = ref(false)
const falhou = ref(false)
const auto = ref(true)
const atualizadoEm = ref<Date | null>(null)
const nomes = ref<Map<number, string>>(new Map())
let timer: ReturnType<typeof setInterval> | null = null

// Enums chegam como número em Automacoes@Fila (confirmado em HTTP real): TipoGatilho e EntidadeAlvo do domínio.
const nomesGatilho = ['Criado', 'Editado', 'CampoAlterado', 'Removido', 'EtapaMudou', 'StatusMudou', 'Concluido', 'Vencido',
  'PropostaCriada', 'PropostaAprovada', 'PropostaEnviada', 'PropostaExpirada', 'EtiquetaAdicionada', 'Periodicamente']
const nomesEntidade = ['Cliente', 'Negocio', 'Funil', 'EtapaFunil', 'Atividade', 'Produto', 'Empresa', 'Usuario', 'Equipe',
  'Departamento', 'Cargo', 'Proposta', 'ItemProposta', 'SecaoProposta', 'ParcelaProposta', 'PessoaContato']
function nomeEnum(v: unknown, lista: string[]): string {
  return typeof v === 'number' ? (lista[v] ?? String(v)) : String(v ?? '')
}
const rotuloGatilho = (g: string) => catalogo.value?.gatilhos.find((x) => x.valor === g)?.nome ?? g
const rotuloEntidade = (e: string) => catalogo.value?.entidades.find((x) => x.valor === e)?.nome ?? e

// A API OData pode devolver PascalCase ou camelCase; aceita os dois.
function pegar(o: Record<string, unknown>, k: string): unknown {
  return o[k] ?? o[k.charAt(0).toLowerCase() + k.slice(1)]
}
function normalizar(o: Record<string, unknown>): ItemFila {
  const st = pegar(o, 'Status')
  return {
    id: Number(pegar(o, 'Id')), automacaoId: Number(pegar(o, 'AutomacaoId')),
    registroId: (pegar(o, 'RegistroId') as number | null) ?? null,
    entidadeAlvo: nomeEnum(pegar(o, 'EntidadeAlvo'), nomesEntidade), gatilho: nomeEnum(pegar(o, 'Gatilho'), nomesGatilho),
    status: typeof st === 'number' ? (nomesStatus[st] ?? String(st)) : String(st ?? ''),
    tentativas: Number(pegar(o, 'Tentativas') ?? 0), maxTentativas: Number(pegar(o, 'MaxTentativas') ?? 0),
    criadoEm: String(pegar(o, 'CriadoEm') ?? ''), proximaTentativa: (pegar(o, 'ProximaTentativa') as string | null) ?? null,
    ultimoErro: (pegar(o, 'UltimoErro') as string | null) ?? null,
  }
}

// A fila não expande a navegação (com $expand a rota devolve []), então o nome vem da listagem (máx. 200 por chamada).
async function carregarNomes() {
  try {
    const r = await automacaoApi.listar({ top: 200 })
    nomes.value = new Map(r.itens.map((a) => [a.id, a.nome]))
  } catch { /* o nome é opcional: cai para #id */ }
}

// Automacoes@Fila devolve array puro, sem @odata.count: pede uma linha a mais para saber se há próxima página.
// O servidor limita $top a 100; porPagina + 1 fica abaixo disso.
async function consultar(): Promise<Record<string, unknown>[]> {
  const q = montarQuery({
    '$top': porPagina + 1, '$skip': (pagina.value - 1) * porPagina, '$orderby': 'CriadoEm desc',
    '$filter': filtro.value ? `Status eq '${filtro.value}'` : undefined,
  })
  const r = await odataRequest<unknown>('GET', `Automacoes@Fila${q}`)
  return Array.isArray(r) ? (r as Record<string, unknown>[]) : ((r as { value?: Record<string, unknown>[] } | null)?.value ?? [])
}

async function carregar(silencioso = false) {
  if (!silencioso) carregando.value = true
  falhou.value = false
  try {
    const lista = await consultar()
    temMais.value = lista.length > porPagina
    itens.value = lista.slice(0, porPagina).map(normalizar)
    if (itens.value.some((x) => !nomes.value.has(x.automacaoId))) await carregarNomes()
    atualizadoEm.value = new Date()
  } catch (e) {
    falhou.value = true
    if (!silencioso) toast.error(mensagensDoErro(e)[0] ?? 'Falha ao carregar a fila.')
  } finally { carregando.value = false }
}
function agendar() {
  if (timer) clearInterval(timer)
  timer = auto.value ? setInterval(() => { if (!document.hidden) carregar(true) }, 10000) : null
}
watch(auto, agendar)
watch(filtro, () => { pagina.value = 1; carregar() })
function ir(p: number) { pagina.value = p; carregar() }
onMounted(() => {
  carregar(); agendar()
  automacaoApi.catalogo().then((c) => { catalogo.value = c }).catch(() => { /* rótulos caem para o nome técnico */ })
})
onBeforeUnmount(() => { if (timer) clearInterval(timer) })
</script>

<template>
  <div class="fila">
    <div class="barra">
      <label class="f">Status
        <select v-model="filtro">
          <option value="">Todos</option>
          <option v-for="s in nomesStatus" :key="s" :value="s">{{ rotulos[s] }}</option>
        </select>
      </label>
      <span class="total">{{ itens.length }} {{ itens.length === 1 ? 'item' : 'itens' }} nesta página{{ temMais ? ' (há mais)' : '' }}</span>
      <span class="grow"></span>
      <span v-if="atualizadoEm" class="mut" aria-live="off">Atualizado às {{ atualizadoEm.toLocaleTimeString('pt-BR') }}</span>
      <button class="btn" :aria-pressed="auto" @click="auto = !auto">
        <i class="mdi" :class="auto ? 'mdi-pause' : 'mdi-play'"></i>{{ auto ? 'Pausar atualização' : 'Retomar atualização' }}
      </button>
      <button class="btn" aria-label="Atualizar agora" @click="carregar()"><i class="mdi mdi-refresh"></i></button>
    </div>

    <p v-if="carregando && !itens.length" class="estado" role="status"><i class="mdi mdi-loading mdi-spin"></i> Carregando...</p>
    <p v-else-if="falhou && !itens.length" class="estado erro" role="alert">Não foi possível carregar a fila. <button class="link" @click="carregar()">Tentar de novo</button></p>
    <p v-else-if="!itens.length" class="estado">Nenhum item na fila.</p>
    <div v-else class="tw">
      <table>
        <thead><tr><th>Automação</th><th>Gatilho</th><th>Registro</th><th>Status</th><th>Tentativas</th><th>Próxima tentativa</th><th>Criado</th></tr></thead>
        <tbody>
          <tr v-for="x in itens" :key="x.id">
            <td class="forte">{{ nomes.get(x.automacaoId) || `#${x.automacaoId}` }}
              <div v-if="x.ultimoErro" class="erro-linha" :title="x.ultimoErro">{{ x.ultimoErro }}</div></td>
            <td>{{ rotuloGatilho(x.gatilho) }}</td>
            <td>{{ rotuloEntidade(x.entidadeAlvo) }}{{ x.registroId ? ` #${x.registroId}` : '' }}</td>
            <td><UiBadge :tom="tons[x.status] ?? 'neutro'">{{ rotulos[x.status] ?? x.status }}</UiBadge></td>
            <td>{{ x.tentativas }} / {{ x.maxTentativas }}</td>
            <td>{{ formatarData(x.proximaTentativa) }}</td>
            <td>{{ formatarData(x.criadoEm) }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-if="pagina > 1 || temMais" class="pg">
      <button class="btn" :disabled="pagina <= 1" aria-label="Página anterior" @click="ir(pagina - 1)"><i class="mdi mdi-chevron-left"></i></button>
      <span>Página {{ pagina }}</span>
      <button class="btn" :disabled="!temMais" aria-label="Próxima página" @click="ir(pagina + 1)"><i class="mdi mdi-chevron-right"></i></button>
    </div>
  </div>
</template>

<style scoped>
.fila { display: flex; flex-direction: column; gap: 12px; height: 100%; }
.barra { display: flex; align-items: flex-end; gap: 10px; flex-wrap: wrap; }
.f { display: flex; flex-direction: column; gap: 4px; font-size: 12px; color: var(--text-muted); }
select { background: var(--bg-secondary); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); padding: 7px 9px; font-size: 13px; }
.total, .mut { font-size: 12px; color: var(--text-muted); padding-bottom: 8px; }
.grow { flex: 1; }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 12px; border-radius: 8px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.btn:hover:not(:disabled) { background: var(--bg-elevated); color: var(--text-primary); }
.btn:disabled { opacity: .3; cursor: default; }
.btn:focus-visible, select:focus-visible, .link:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }
.estado { text-align: center; padding: 40px; color: var(--text-muted); font-size: 13px; }
.estado.erro { color: var(--danger); }
.link { background: none; border: 0; color: var(--accent); cursor: pointer; }
.tw { flex: 1; overflow: auto; border: 1px solid var(--border); border-radius: 10px; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
th { position: sticky; top: 0; background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; text-align: left; padding: 10px 12px; border-bottom: 1px solid var(--border); }
td { padding: 9px 12px; border-bottom: 1px solid var(--border); color: var(--text-secondary); }
tr:hover td { background: var(--bg-elevated); }
.forte { color: var(--text-primary); font-weight: 500; }
.erro-linha { color: var(--danger); font-size: 11px; font-weight: 400; max-width: 320px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.pg { display: flex; justify-content: center; align-items: center; gap: 12px; font-size: 13px; color: var(--text-muted); }
</style>
