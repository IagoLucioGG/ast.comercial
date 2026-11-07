<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TabelaEntidade, { type ColunaConfig } from '@/components/shared/TabelaEntidade.vue'
import FunilSelector from '@/components/funil/FunilSelector.vue'
import KanbanBoard from '@/components/funil/KanbanBoard.vue'
import NovoNegocioModal from '@/components/negocio/NovoNegocioModal.vue'
import { useFunisStore } from '@/stores/funis'
import { useAuthStore } from '@/stores/auth'
import type { FiltroStatus } from '@/composables/useFunilKanban'

type Modo = 'funil' | 'tabela'

const route = useRoute()
const router = useRouter()
const store = useFunisStore()
const auth = useAuthStore()

const colunas: ColunaConfig[] = [
  { campo: 'Titulo', label: 'Título', tipo: 'avatar' },
  { campo: 'Valor', label: 'Valor', tipo: 'moeda' },
  { campo: 'PrevisaoFechamento', label: 'Previsão', tipo: 'data' },
  { campo: 'Ativo', label: 'Situação', tipo: 'status' },
  { campo: 'CriadoEm', label: 'Criado em', tipo: 'data' },
]

// ---- preferências por usuário ----
const chavePrefs = computed(() => `ast.negocios.prefs.${auth.usuario?.usuarioId ?? 'anon'}`)

function lerPrefs(): { modo?: Modo; funilId?: number } {
  try { return JSON.parse(localStorage.getItem(chavePrefs.value) ?? '{}') } catch { return {} }
}

const prefsIniciais = lerPrefs()
const modo = ref<Modo>(prefsIniciais.modo === 'tabela' ? 'tabela' : 'funil')
const funilId = ref<number | null>(prefsIniciais.funilId ?? null)

watch([modo, funilId], () => {
  localStorage.setItem(chavePrefs.value, JSON.stringify({ modo: modo.value, funilId: funilId.value }))
})

// ---- busca com debounce de 300 ms ----
const buscaDigitada = ref('')
const busca = ref('')
let timer: ReturnType<typeof setTimeout> | undefined
watch(buscaDigitada, (v) => {
  clearTimeout(timer)
  timer = setTimeout(() => { busca.value = v.trim() }, 300)
})
onBeforeUnmount(() => clearTimeout(timer))

// ---- filtro de responsável (Todos / Meus / um usuário) e de status ----
const responsavelSel = ref<string>('todos')
const responsavelId = computed<number | null>(() => {
  if (responsavelSel.value === 'todos') return null
  if (responsavelSel.value === 'meus') return auth.usuario?.usuarioId ?? null
  const n = Number(responsavelSel.value)
  return Number.isFinite(n) ? n : null
})
const statusSel = ref<FiltroStatus>('abertos')
const opcoesStatus: { valor: FiltroStatus; rotulo: string }[] = [
  { valor: 'abertos', rotulo: 'Abertos' },
  { valor: 'ganhos', rotulo: 'Ganhos' },
  { valor: 'perdidos', rotulo: 'Perdidos' },
  { valor: 'todos', rotulo: 'Todos' },
]

// ---- funis ----
function garantirFunil() {
  if (!store.funis.length) { funilId.value = null; return }
  if (!store.funis.some((f) => f.id === funilId.value)) funilId.value = store.funis[0]?.id ?? null
}

async function iniciar(forcar = false) {
  await store.carregar(forcar)
  garantirFunil()
}

// ---- novo negócio ----
const quadro = ref<InstanceType<typeof KanbanBoard> | null>(null)
const tabela = ref<InstanceType<typeof TabelaEntidade> | null>(null)
const novoAberto = ref(false)
const etapaInicial = ref<number | null>(null)

function abrirNovo(etapaId: number | null = null) {
  etapaInicial.value = etapaId
  novoAberto.value = true
}

function aoCriar(etapaId: number) {
  if (modo.value === 'funil') quadro.value?.recarregarEtapa(etapaId)
  else tabela.value?.carregar?.()
}

function abrirPorAtalho() {
  if (route.query.novo !== '1') return
  abrirNovo(null)
  const { novo: _descartado, ...resto } = route.query
  router.replace({ query: resto })
}

onMounted(async () => {
  await iniciar()
  abrirPorAtalho()
})
watch(() => route.query.novo, abrirPorAtalho)
</script>

<template>
  <div class="nv">
    <div class="nv-barra">
      <div class="nv-modos" role="group" aria-label="Modo de visualização">
        <button type="button" :class="{ ativo: modo === 'funil' }" :aria-pressed="modo === 'funil'" @click="modo = 'funil'">
          <i class="mdi mdi-view-column" aria-hidden="true"></i> Funil
        </button>
        <button type="button" :class="{ ativo: modo === 'tabela' }" :aria-pressed="modo === 'tabela'" @click="modo = 'tabela'">
          <i class="mdi mdi-table" aria-hidden="true"></i> Tabela
        </button>
      </div>

      <template v-if="modo === 'funil'">
        <FunilSelector v-if="store.funis.length" v-model="funilId" :funis="store.funis" />
        <div class="nv-busca">
          <i class="mdi mdi-magnify" aria-hidden="true"></i>
          <input v-model="buscaDigitada" type="search" placeholder="Buscar por título ou cliente..." aria-label="Buscar negócios" />
        </div>
        <label class="nv-resp">
          <span class="nv-rotulo">Responsável</span>
          <select v-model="responsavelSel" aria-label="Filtrar por responsável">
            <option value="todos">Todos</option>
            <option value="meus">Meus</option>
            <option v-for="u in store.usuarios" :key="u.id" :value="String(u.id)">{{ u.nome }}</option>
          </select>
        </label>
        <div class="nv-modos" role="group" aria-label="Situação dos negócios">
          <button
            v-for="o in opcoesStatus" :key="o.valor" type="button"
            :class="{ ativo: statusSel === o.valor }" :aria-pressed="statusSel === o.valor" @click="statusSel = o.valor"
          >{{ o.rotulo }}</button>
        </div>
        <span class="nv-espaco"></span>
        <button class="nv-novo" type="button" @click="abrirNovo(null)"><i class="mdi mdi-plus" aria-hidden="true"></i> Novo negócio</button>
      </template>
    </div>

    <template v-if="modo === 'funil'">
      <div v-if="store.carregando && !store.carregado" class="nv-esqueleto" aria-busy="true" aria-label="Carregando funil">
        <div v-for="n in 4" :key="n" class="nv-sk-col"></div>
      </div>
      <div v-else-if="store.erro" class="nv-estado" role="alert">
        <i class="mdi mdi-alert-circle-outline" aria-hidden="true"></i>
        <p>{{ store.erro }}</p>
        <button type="button" class="nv-novo" @click="iniciar(true)">Tentar de novo</button>
      </div>
      <div v-else-if="!store.funis.length" class="nv-estado">
        <i class="mdi mdi-filter-off-outline" aria-hidden="true"></i>
        <p>Nenhum funil cadastrado. Crie um funil na área de administração.</p>
      </div>
      <KanbanBoard v-else ref="quadro" :funil-id="funilId" :busca="busca" :status="statusSel" :responsavel-id="responsavelId" @novo="abrirNovo" />
    </template>

    <div v-else class="nv-tabela">
      <TabelaEntidade
        ref="tabela"
        entidade="Negocio"
        endpoint="Negocios"
        titulo="Negócio"
        :colunas="colunas"
        :campos-busca="['Titulo']"
        placeholder-busca="Buscar negócio..."
        ordenacao-padrao="CriadoEm desc"
        sem-form-interno
        @novo="abrirNovo(null)"
        @editar="(id: number) => router.push(`/negocios/${id}`)"
      />
    </div>

    <NovoNegocioModal v-model="novoAberto" :funil-id="funilId" :etapa-id="etapaInicial" @criado="aoCriar" />
  </div>
</template>

<style scoped>
.nv { display: flex; flex-direction: column; height: 100%; min-height: 0; }
.nv-barra { display: flex; align-items: center; gap: 12px; padding: 12px 20px; background: var(--bg-primary); border-bottom: 1px solid var(--border); flex-shrink: 0; flex-wrap: wrap; }
.nv-modos { display: inline-flex; border: 1px solid var(--border); border-radius: 8px; overflow: hidden; background: var(--bg-surface); }
.nv-modos button { display: flex; align-items: center; gap: 6px; padding: 8px 14px; border: 0; background: none; color: var(--text-secondary); font-size: 12px; font-weight: 600; cursor: pointer; }
.nv-modos button:hover { color: var(--text-primary); background: var(--bg-elevated); }
.nv-modos button.ativo { background: var(--accent); color: #000; }
.nv-busca { display: flex; align-items: center; gap: 8px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 8px; padding: 8px 14px; width: 300px; color: var(--text-muted); }
.nv-busca:focus-within { border-color: var(--accent); }
.nv-busca input { border: 0; background: transparent; color: var(--text-primary); font-size: 13px; outline: none; width: 100%; }
.nv-resp { display: flex; align-items: center; gap: 8px; font-size: 12px; color: var(--text-secondary); }
.nv-resp select { padding: 8px 10px; border-radius: 8px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-primary); font-size: 12px; max-width: 200px; }
.nv-resp select:focus { outline: none; border-color: var(--accent); }
.nv-espaco { flex: 1; }
.nv-novo { display: flex; align-items: center; gap: 5px; padding: 8px 16px; border: 0; border-radius: 8px; background: var(--accent); color: #000; font-size: 12px; font-weight: 600; cursor: pointer; }
.nv-novo:hover { background: var(--accent-hover); }
.nv-tabela { flex: 1; min-height: 0; display: flex; flex-direction: column; }
.nv-esqueleto { flex: 1; display: flex; gap: 12px; padding: 16px 20px; overflow: hidden; }
.nv-sk-col { flex: 0 0 300px; border-radius: 12px; background: var(--bg-secondary); border: 1px solid var(--border); }
.nv-estado { flex: 1; display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 10px; color: var(--text-muted); font-size: 13px; text-align: center; padding: 24px; }
.nv-estado i { font-size: 36px; }
</style>
