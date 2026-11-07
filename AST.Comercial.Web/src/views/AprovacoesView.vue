<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import UiTabs, { type AbaConfig } from '@/components/ui/UiTabs.vue'
import AprovacaoCard from '@/components/aprovacoes/AprovacaoCard.vue'
import DecisaoModal from '@/components/aprovacoes/DecisaoModal.vue'
import { apiAprov, numeroProposta, type Solicitacao } from '@/components/aprovacoes/aprovacaoApi'
import { useToast } from '@/composables/useToast'

type Aba = 'pendentes' | 'solicitadas' | 'historico'
const ROTAS: Record<Aba, string> = {
  pendentes: '/api/aprovacoes/minhas',
  solicitadas: '/api/aprovacoes/solicitadas',
  historico: '/api/aprovacoes/historico',
}

const toast = useToast()
const aba = ref<Aba>('pendentes')
const listas = ref<Record<Aba, Solicitacao[] | null>>({ pendentes: null, solicitadas: null, historico: null })
const carregando = ref(false)
const erro = ref('')

const abas = computed<AbaConfig[]>(() => [
  { chave: 'pendentes', rotulo: 'Pendentes para mim', icone: 'mdi-inbox-arrow-down-outline', contador: listas.value.pendentes?.length },
  { chave: 'solicitadas', rotulo: 'Solicitadas por mim', icone: 'mdi-send-outline' },
  { chave: 'historico', rotulo: 'Histórico', icone: 'mdi-history' },
])

const itens = computed(() => listas.value[aba.value] ?? [])

async function carregar(qual: Aba = aba.value) {
  carregando.value = true
  erro.value = ''
  try {
    listas.value[qual] = await apiAprov<Solicitacao[]>('GET', ROTAS[qual])
  } catch (e) {
    erro.value = e instanceof Error ? e.message : 'Não foi possível carregar as aprovações.'
  } finally {
    carregando.value = false
  }
}

// ---------- decisão ----------
const alvo = ref<Solicitacao | null>(null)
const modo = ref<'aprovar' | 'rejeitar'>('aprovar')
const modalAberto = ref(false)
const enviando = ref(false)

function abrir(s: Solicitacao, m: 'aprovar' | 'rejeitar') {
  alvo.value = s
  modo.value = m
  modalAberto.value = true
}

const contextoModal = computed(() => {
  const p = alvo.value?.proposta
  if (!p) return ''
  return `${numeroProposta(p)} · ${p.titulo ?? 'Sem título'} (nível ${alvo.value?.nivelAtual}/${alvo.value?.totalNiveis})`
})

async function confirmar(comentario: string) {
  if (!alvo.value) return
  enviando.value = true
  try {
    const r = await apiAprov<Solicitacao>('POST', `/api/aprovacoes/${alvo.value.id}/${modo.value}`, { comentario })
    toast.success(
      modo.value === 'rejeitar' ? 'Proposta rejeitada.'
        : r.situacao === 'Aprovada' ? 'Aprovação concluída.' : `Nível aprovado. Segue para o nível ${r.nivelAtual}.`,
    )
    modalAberto.value = false
    listas.value.historico = null
    listas.value.solicitadas = null
    await carregar('pendentes')
  } catch (e) {
    // 403 (não é aprovador) e 422 (regra) chegam com a mensagem do servidor
    toast.error(e instanceof Error ? e.message : 'Não foi possível registrar a decisão.')
    modalAberto.value = false
    await carregar(aba.value)
  } finally {
    enviando.value = false
  }
}

const vazio = computed(() => ({
  pendentes: 'Nenhuma aprovação aguardando você.',
  solicitadas: 'Você ainda não solicitou aprovações.',
  historico: 'Ainda não há aprovações concluídas.',
}[aba.value]))

onMounted(() => carregar('pendentes'))
watch(aba, (a) => { if (listas.value[a] === null) carregar(a) })
</script>

<template>
  <div class="pagina">
    <header class="cab">
      <h1>Aprovações</h1>
      <button type="button" class="btn" :disabled="carregando" @click="carregar()"><i class="mdi mdi-refresh"></i> Atualizar</button>
    </header>

    <UiTabs v-model="aba" :abas="abas" />

    <p v-if="carregando && !itens.length" class="estado" role="status">Carregando...</p>
    <div v-else-if="erro" class="estado erro" role="alert">
      <p>{{ erro }}</p>
      <button type="button" class="btn" @click="carregar()">Tentar novamente</button>
    </div>
    <p v-else-if="!itens.length" class="estado"><i class="mdi mdi-check-all"></i> {{ vazio }}</p>

    <div v-else class="lista">
      <AprovacaoCard
        v-for="s in itens" :key="s.id" :solicitacao="s" :com-acoes="aba === 'pendentes'" :ocupado="enviando"
        @aprovar="abrir($event, 'aprovar')" @rejeitar="abrir($event, 'rejeitar')"
      />
    </div>

    <DecisaoModal
      v-model="modalAberto" :modo="modo" :obrigatorio="modo === 'rejeitar'" :enviando="enviando"
      :contexto="contextoModal" @confirmar="confirmar"
    />
  </div>
</template>

<style scoped>
.pagina { display: flex; flex-direction: column; gap: 14px; color: var(--text-primary); }
.cab { display: flex; align-items: center; justify-content: space-between; gap: 12px; }
h1 { margin: 0; font-size: 20px; font-weight: 600; }
.lista { display: flex; flex-direction: column; gap: 12px; }
.estado { color: var(--text-secondary); font-size: 14px; padding: 24px 0; text-align: center; }
.estado.erro { color: var(--danger); }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; font-family: inherit; }
.btn:hover:not(:disabled) { border-color: var(--accent); }
.btn:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.btn:disabled { opacity: .5; cursor: default; }
</style>
