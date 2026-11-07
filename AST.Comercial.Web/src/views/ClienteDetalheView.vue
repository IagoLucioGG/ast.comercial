<script setup lang="ts">
import { ref, computed, watch, onMounted } from 'vue'
import { useRoute, RouterLink } from 'vue-router'
import { odataRequest, montarQuery } from '@/services/api'
import { useEntidadeDetalhe, campo, lista, inicialDe, type Registro } from '@/composables/useEntidadeDetalhe'
import UiTabs, { type AbaConfig } from '@/components/ui/UiTabs.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import DadosEntidade, { type CampoDef } from '@/components/detalhe/DadosEntidade.vue'
import ContatosEntidade from '@/components/detalhe/ContatosEntidade.vue'
import NegociosEntidade from '@/components/detalhe/NegociosEntidade.vue'
import PropostasEntidade from '@/components/detalhe/PropostasEntidade.vue'
import AtividadesEntidade from '@/components/detalhe/AtividadesEntidade.vue'
import TimelineEntidade from '@/components/detalhe/TimelineEntidade.vue'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'

const route = useRoute()
const id = computed(() => Number(route.params.id))

const { registro: cliente, carregando, erro, naoEncontrado, carregar } = useEntidadeDetalhe('Clientes', 'StatusCliente($select=Id,Nome,Cor)')

const abaAtiva = ref('dados')
const chaveTimeline = ref(0)
const chaveContatos = ref(0)
const auxPronto = ref(false)
const principais = ref<Registro[]>([])
const opcoes = ref<Record<string, { id: number; nome: string }[]>>({})

const abas: AbaConfig[] = [
  { chave: 'dados', rotulo: 'Dados', icone: 'mdi-form-select' },
  { chave: 'contatos', rotulo: 'Contatos', icone: 'mdi-account-multiple-outline' },
  { chave: 'negocios', rotulo: 'Negócios', icone: 'mdi-handshake-outline' },
  { chave: 'propostas', rotulo: 'Propostas', icone: 'mdi-file-document-outline' },
  { chave: 'atividades', rotulo: 'Atividades', icone: 'mdi-calendar-check-outline' },
  { chave: 'historico', rotulo: 'Histórico', icone: 'mdi-history' },
]

const camposPadrao = computed<CampoDef[]>(() => [
  { nome: 'Nome', rotulo: 'Nome', tipo: 'texto', obrigatorio: true },
  { nome: 'RazaoSocial', rotulo: 'Razão social', tipo: 'texto' },
  { nome: 'Documento', rotulo: 'Documento (CNPJ)', tipo: 'texto' },
  { nome: 'Email', rotulo: 'E-mail', tipo: 'texto' },
  { nome: 'Telefone', rotulo: 'Telefone', tipo: 'texto' },
  { nome: 'Site', rotulo: 'Site', tipo: 'texto' },
  { nome: 'StatusClienteId', rotulo: 'Status', tipo: 'select', opcoes: opcoes.value.status ?? [] },
  { nome: 'OrigemId', rotulo: 'Origem', tipo: 'select', opcoes: opcoes.value.origem ?? [] },
  { nome: 'RamoAtividadeId', rotulo: 'Ramo de atividade', tipo: 'select', opcoes: opcoes.value.ramo ?? [] },
  { nome: 'PorteEmpresaId', rotulo: 'Porte', tipo: 'select', opcoes: opcoes.value.porte ?? [] },
  { nome: 'Cep', rotulo: 'CEP', tipo: 'texto' },
  { nome: 'Endereco', rotulo: 'Endereço', tipo: 'texto' },
  { nome: 'Cidade', rotulo: 'Cidade', tipo: 'texto' },
  { nome: 'Estado', rotulo: 'Estado', tipo: 'texto' },
  { nome: 'Observacoes', rotulo: 'Observações', tipo: 'area' },
])

const nomesTimeline = computed(() => {
  const mapa = (k: string) => Object.fromEntries((opcoes.value[k] ?? []).map((o) => [o.id, o.nome]))
  return { StatusClienteId: mapa('status'), OrigemId: mapa('origem'), RamoAtividadeId: mapa('ramo'), PorteEmpresaId: mapa('porte') }
})

const corStatus = computed(() => campo<string>(campo(cliente.value, 'StatusCliente'), 'Cor') ?? null)
const inativo = computed(() => campo(cliente.value, 'Ativo') === false)

async function carregarAuxiliares() {
  const rotas: [string, string][] = [
    ['status', 'Clientes@StatusClientes'], ['origem', 'Clientes@Origens'],
    ['ramo', 'Clientes@RamosAtividade'], ['porte', 'Clientes@Portes'],
  ]
  const resultados = await Promise.allSettled(rotas.map(([, rota]) => odataRequest<unknown>('GET', rota)))
  const novo: Record<string, { id: number; nome: string }[]> = {}
  resultados.forEach((r, i) => {
    const chave = rotas[i]![0]
    novo[chave] = r.status === 'fulfilled'
      ? lista<Registro>(r.value).filter((x) => campo(x, 'Ativo') !== false).map((x) => ({ id: Number(campo(x, 'Id')), nome: String(campo(x, 'Nome')) }))
      : []
  })
  opcoes.value = novo
}

async function carregarPrincipais() {
  try {
    const q = montarQuery({ $filter: `ClienteId eq ${id.value}`, $orderby: 'Decisor desc,Nome asc', $top: 3 })
    principais.value = lista<Registro>(await odataRequest<unknown>('GET', `PessoasContato${q}`))
  } catch {
    principais.value = []
  }
}

async function iniciar() {
  principais.value = []
  auxPronto.value = false
  await carregar(id.value)
  if (cliente.value) await Promise.all([carregarAuxiliares(), carregarPrincipais()])
  auxPronto.value = true
}

async function recarregar() {
  await carregar(id.value, true)
  chaveTimeline.value++
}

function aoAlterarContatos() {
  chaveTimeline.value++
  carregarPrincipais()
}

watch(id, () => { abaAtiva.value = 'dados'; iniciar() })
onMounted(iniciar)
</script>

<template>
  <div class="cd">
    <DetalheEstado :carregando="carregando" :erro="erro" @tentar="iniciar">
      <div v-if="naoEncontrado" class="cd-nf" role="alert">
        <i class="mdi mdi-domain-off"></i>
        <h2>Cliente não encontrado</h2>
        <p>O cliente solicitado não existe ou você não tem acesso a ele.</p>
        <RouterLink class="cd-voltar" to="/clientes"><i class="mdi mdi-arrow-left"></i> Voltar para clientes</RouterLink>
      </div>

      <template v-else-if="cliente">
        <nav class="cd-trilha" aria-label="Navegação">
          <RouterLink to="/clientes"><i class="mdi mdi-arrow-left"></i> Clientes</RouterLink>
        </nav>

        <header class="cd-cab">
          <div class="cd-avatar" aria-hidden="true">{{ inicialDe(campo(cliente, 'Nome')) }}</div>
          <div class="cd-cab-corpo">
            <div class="cd-titulo">
              <h1>{{ campo(cliente, 'Nome') }}</h1>
              <UiBadge v-if="campo(campo(cliente, 'StatusCliente'), 'Nome')" tom="destaque">
                <span v-if="corStatus" class="cd-cor" :style="{ background: corStatus }"></span>{{ campo(campo(cliente, 'StatusCliente'), 'Nome') }}
              </UiBadge>
              <UiBadge v-if="inativo" tom="neutro">Inativo</UiBadge>
            </div>
            <div class="cd-meta">
              <span v-if="campo(cliente, 'Documento')"><i class="mdi mdi-card-account-details-outline"></i> {{ campo(cliente, 'Documento') }}</span>
              <span v-if="campo(cliente, 'Email')"><i class="mdi mdi-email-outline"></i> {{ campo(cliente, 'Email') }}</span>
              <span v-if="campo(cliente, 'Telefone')"><i class="mdi mdi-phone-outline"></i> {{ campo(cliente, 'Telefone') }}</span>
              <span v-if="campo(cliente, 'Cidade')"><i class="mdi mdi-map-marker-outline"></i> {{ campo(cliente, 'Cidade') }}<template v-if="campo(cliente, 'Estado')"> / {{ campo(cliente, 'Estado') }}</template></span>
            </div>
            <div v-if="principais.length" class="cd-contatos">
              <span class="cd-rotulo">Contatos principais</span>
              <button v-for="p in principais" :key="campo(p, 'Id')" class="cd-chip" type="button" @click="abaAtiva = 'contatos'">
                <i class="mdi mdi-account-outline"></i> {{ campo(p, 'Nome') }}<small v-if="campo(p, 'Cargo')"> · {{ campo(p, 'Cargo') }}</small>
              </button>
            </div>
          </div>
        </header>

        <UiTabs v-model="abaAtiva" :abas="abas" />

        <section class="cd-aba" :aria-label="abas.find((a) => a.chave === abaAtiva)?.rotulo">
          <DetalheEstado v-if="abaAtiva === 'dados' && !auxPronto" carregando />
          <DadosEntidade v-else-if="abaAtiva === 'dados'" entidade="Cliente" endpoint="Clientes" :registro="cliente" :padrao="camposPadrao" @salvo="recarregar" />
          <ContatosEntidade v-else-if="abaAtiva === 'contatos'" :key="chaveContatos" :cliente-id="id" editavel @alterado="aoAlterarContatos" />
          <NegociosEntidade v-else-if="abaAtiva === 'negocios'" :cliente-id="id" />
          <PropostasEntidade v-else-if="abaAtiva === 'propostas'" filtro-campo="ClienteId" :pai-id="id" :nova-query="{ clienteId: id }" />
          <AtividadesEntidade v-else-if="abaAtiva === 'atividades'" endpoint="Clientes" :pai-id="id" :vinculo="{ ClienteId: id }" @alterado="chaveTimeline++" />
          <TimelineEntidade v-else :key="chaveTimeline" entidade="Cliente" endpoint="Clientes" :registro-id="id" :nomes="nomesTimeline" />
        </section>
      </template>
    </DetalheEstado>
  </div>
</template>

<style scoped>
.cd { max-width: 1100px; margin: 0 auto; }
.cd-trilha { margin-bottom: 10px; font-size: 13px; }
.cd-trilha a, .cd-voltar { color: var(--text-secondary); text-decoration: none; display: inline-flex; align-items: center; gap: 4px; }
.cd-trilha a:hover, .cd-voltar:hover { color: var(--accent); }
.cd-nf { display: flex; flex-direction: column; align-items: center; gap: 8px; padding: 60px 16px; color: var(--text-secondary); text-align: center; }
.cd-nf .mdi { font-size: 44px; color: var(--text-muted); }
.cd-nf h2 { color: var(--text-primary); font-size: 18px; }
.cd-cab { display: flex; gap: 16px; align-items: flex-start; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 14px; padding: 18px 20px; margin-bottom: 16px; }
.cd-avatar { flex: 0 0 52px; height: 52px; border-radius: 14px; display: flex; align-items: center; justify-content: center; background: var(--bg-elevated); border: 1px solid var(--border); color: var(--accent); font-size: 22px; font-weight: 700; }
.cd-cab-corpo { flex: 1; min-width: 0; }
.cd-titulo { display: flex; align-items: center; gap: 10px; flex-wrap: wrap; }
.cd-titulo h1 { font-size: 20px; font-weight: 600; color: var(--text-primary); margin: 0; overflow-wrap: anywhere; }
.cd-cor { display: inline-block; width: 8px; height: 8px; border-radius: 50%; }
.cd-meta { display: flex; flex-wrap: wrap; gap: 6px 18px; margin-top: 8px; font-size: 13px; color: var(--text-secondary); }
.cd-meta .mdi { color: var(--text-muted); margin-right: 3px; }
.cd-contatos { display: flex; flex-wrap: wrap; align-items: center; gap: 6px; margin-top: 12px; }
.cd-rotulo { font-size: 11px; text-transform: uppercase; letter-spacing: .5px; color: var(--text-muted); margin-right: 4px; }
.cd-chip { display: inline-flex; align-items: center; gap: 4px; padding: 4px 10px; border: 1px solid var(--border); border-radius: 999px; background: var(--bg-primary); color: var(--text-primary); font-size: 12px; cursor: pointer; }
.cd-chip:hover { border-color: var(--accent); }
.cd-chip small { color: var(--text-muted); }
.cd-aba { padding: 18px 2px; }
</style>
