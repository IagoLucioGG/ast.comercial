<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import UiBadge from '@/components/ui/UiBadge.vue'
import PedidoItensTabela from '@/components/pedidos/PedidoItensTabela.vue'
import {
  buscarMoeda, carregarPedido, listarEtapas, moverParaEtapa, numeroDoPedido,
  type EtapaPedido, type Pedido,
} from '@/components/pedidos/pedidoApi'
import { ApiError } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { usePermissoes } from '@/composables/usePermissoes'
import { formatarMoeda, registrarSimbolos } from '@/composables/usePropostaTotais'

const route = useRoute()
const router = useRouter()
const toast = useToast()
const { pode } = usePermissoes()

const id = computed(() => Number(route.params.id))
const pedido = ref<Pedido | null>(null)
const etapas = ref<EtapaPedido[]>([])
const moeda = ref<string | null>(null)
const carregando = ref(true)
const erro = ref('')
const movendo = ref(false)

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const [p, e] = await Promise.all([carregarPedido(id.value), listarEtapas()])
    pedido.value = p
    etapas.value = e
    moeda.value = null
    if (p.MoedaId) {
      const m = await buscarMoeda(p.MoedaId).catch(() => null)
      if (m) { registrarSimbolos([m]); moeda.value = m.Codigo }
    }
  } catch (e) {
    erro.value = e instanceof ApiError && e.status === 404 ? 'Pedido não encontrado.' : 'Não foi possível carregar o pedido.'
  } finally {
    carregando.value = false
  }
}

async function mover(etapa: EtapaPedido) {
  if (!pedido.value || etapa.Id === pedido.value.EtapaPedidoId) return
  movendo.value = true
  try {
    await moverParaEtapa(pedido.value.Id, etapa.Id)
    pedido.value.EtapaPedidoId = etapa.Id
    pedido.value.Etapa = etapa
    toast.success(`Pedido movido para "${etapa.Nome}".`)
  } catch (e) {
    toast.error(e instanceof ApiError ? e.message : 'Não foi possível mover o pedido.')
  } finally {
    movendo.value = false
  }
}

const data = computed(() => (pedido.value ? new Date(pedido.value.Data).toLocaleDateString('pt-BR') : ''))
const podeEditar = computed(() => pode('Pedido', 'editar'))

onMounted(carregar)
watch(id, carregar)
</script>

<template>
  <div class="detalhe">
    <button type="button" class="voltar" @click="router.push('/pedidos')"><i class="mdi mdi-arrow-left"></i> Pedidos</button>

    <p v-if="carregando" class="estado">Carregando pedido...</p>
    <div v-else-if="erro" class="estado erro" role="alert">
      <p>{{ erro }}</p>
      <button type="button" class="btn" @click="carregar">Tentar novamente</button>
    </div>

    <template v-else-if="pedido">
      <header class="cabecalho">
        <div class="titulos">
          <span class="numero">{{ numeroDoPedido(pedido) }}</span>
          <h1>{{ pedido.Titulo || 'Sem título' }}</h1>
          <div class="meta">
            <span v-if="pedido.Cliente?.Nome"><i class="mdi mdi-domain"></i> {{ pedido.Cliente.Nome }}</span>
            <UiBadge v-if="pedido.Etapa" tom="info">{{ pedido.Etapa.Nome }}</UiBadge>
            <span>{{ data }}</span>
          </div>
        </div>
        <div class="valor">
          <small>Valor total</small>
          <strong>{{ formatarMoeda(pedido.Valor, moeda) }}</strong>
        </div>
      </header>

      <section aria-label="Etapa do pedido" class="bloco">
        <h2>Etapa</h2>
        <div class="etapas" role="group" aria-label="Mover para etapa">
          <button
            v-for="e in etapas" :key="e.Id" type="button" class="etapa"
            :class="{ ativa: e.Id === pedido.EtapaPedidoId }" :aria-pressed="e.Id === pedido.EtapaPedidoId"
            :disabled="movendo || !podeEditar" @click="mover(e)"
          >{{ e.Nome }}</button>
        </div>
      </section>

      <section aria-label="Condições" class="bloco info">
        <div><small>Forma de pagamento</small><span>{{ pedido.FormaPagamento || '-' }}</span></div>
        <div><small>Parcelas</small><span>{{ pedido.NumeroParcelas ?? '-' }}</span></div>
        <div><small>Desconto</small><span>{{ formatarMoeda(pedido.Desconto, moeda) }}</span></div>
        <div><small>Frete</small><span>{{ formatarMoeda(pedido.CustoFrete, moeda) }}</span></div>
        <div v-if="pedido.PropostaId">
          <small>Proposta de origem</small>
          <a href="#" @click.prevent="router.push(`/propostas/${pedido.PropostaId}`)">Abrir proposta</a>
        </div>
        <div v-if="pedido.NegocioId">
          <small>Negócio</small>
          <a href="#" @click.prevent="router.push(`/negocios/${pedido.NegocioId}`)">Abrir negócio</a>
        </div>
      </section>

      <section aria-label="Itens" class="bloco">
        <h2>Itens</h2>
        <PedidoItensTabela :itens="pedido.Itens ?? []" :moeda="moeda" />
      </section>

      <section v-if="pedido.Observacoes" aria-label="Observações" class="bloco">
        <h2>Observações</h2>
        <p class="obs">{{ pedido.Observacoes }}</p>
      </section>
    </template>
  </div>
</template>

<style scoped>
.detalhe { padding: 16px 20px; display: flex; flex-direction: column; gap: 16px; }
.voltar { align-self: flex-start; background: none; border: 0; color: var(--text-muted); cursor: pointer; font-size: 13px; display: inline-flex; gap: 6px; align-items: center; }
.voltar:hover { color: var(--text-primary); }
.estado { color: var(--text-muted); }
.estado.erro { color: var(--danger); }
.cabecalho { display: flex; flex-wrap: wrap; gap: 16px; align-items: flex-start; justify-content: space-between; }
.numero { font-size: 12px; color: var(--text-muted); font-weight: 600; }
h1 { margin: 2px 0 6px; font-size: 22px; color: var(--text-primary); }
h2 { margin: 0 0 10px; font-size: 14px; color: var(--text-primary); }
.meta { display: flex; flex-wrap: wrap; gap: 10px; align-items: center; font-size: 13px; color: var(--text-secondary); }
.valor { text-align: right; }
.valor small { display: block; font-size: 11px; color: var(--text-muted); }
.valor strong { font-size: 22px; color: var(--text-primary); }
.bloco { background: var(--bg-secondary); border: 1px solid var(--border); border-radius: 10px; padding: 14px 16px; }
.etapas { display: flex; flex-wrap: wrap; gap: 8px; }
.etapa { padding: 6px 14px; border-radius: 999px; border: 1px solid var(--border); background: var(--bg-primary); color: var(--text-secondary); font-size: 13px; cursor: pointer; }
.etapa:hover:not(:disabled) { border-color: var(--accent); color: var(--text-primary); }
.etapa.ativa { background: var(--accent); border-color: var(--accent); color: #000; font-weight: 600; }
.etapa:disabled { cursor: default; opacity: .7; }
.info { display: grid; grid-template-columns: repeat(auto-fit, minmax(170px, 1fr)); gap: 14px; }
.info small { display: block; font-size: 11px; color: var(--text-muted); margin-bottom: 2px; }
.info span, .info a { font-size: 13px; color: var(--text-primary); }
.info a { color: var(--accent); }
.obs { margin: 0; white-space: pre-wrap; font-size: 13px; color: var(--text-secondary); }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; }
@media (max-width: 640px) { .valor { text-align: left; width: 100%; } }
</style>
