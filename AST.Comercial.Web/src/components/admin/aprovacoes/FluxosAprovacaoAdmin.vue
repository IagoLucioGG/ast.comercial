<script setup lang="ts">
import { onMounted, ref } from 'vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import UiModal from '@/components/ui/UiModal.vue'
import EditorFluxoAprovacao, { type OpcaoSimples } from './EditorFluxoAprovacao.vue'
import { apiAprov, type CampoFiltro, type Fluxo } from '@/components/aprovacoes/aprovacaoApi'
import { odataTodos, montarQuery } from '@/services/api'
import { useToast } from '@/composables/useToast'
import '../comum/admin-comum.css'

const toast = useToast()
const fluxos = ref<Fluxo[]>([])
const campos = ref<CampoFiltro[]>([])
const usuarios = ref<OpcaoSimples[]>([])
const moedas = ref<OpcaoSimples[]>([])
const carregando = ref(true)
const erro = ref('')

const editorAberto = ref(false)
const emEdicao = ref<Fluxo | null>(null)
const excluindo = ref<Fluxo | null>(null)
const confirmaAberta = ref(false)
const ocupado = ref(false)

const ROTULO_TIPO: Record<string, string> = { ControleAlcada: 'Controle de alçada', Bloqueio: 'Bloqueio', Alerta: 'Alerta' }

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const [lista, cs, us, ms] = await Promise.all([
      apiAprov<Fluxo[]>('GET', '/api/fluxos-aprovacao'),
      apiAprov<CampoFiltro[]>('GET', '/api/fluxos-aprovacao/campos'),
      odataTodos<{ Id: number; Nome: string }>(`Usuarios${montarQuery({ $select: 'Id,Nome', $orderby: 'Nome' })}`),
      odataTodos<{ Id: number; Codigo: string; Simbolo?: string | null }>(`Moedas${montarQuery({ $orderby: 'Id' })}`),
    ])
    fluxos.value = lista
    campos.value = cs
    usuarios.value = us.map((u) => ({ id: u.Id, nome: u.Nome }))
    moedas.value = ms.map((m) => ({ id: m.Id, nome: m.Simbolo ? `${m.Codigo} (${m.Simbolo})` : m.Codigo }))
  } catch (e) {
    erro.value = e instanceof Error ? e.message : 'Não foi possível carregar os fluxos.'
  } finally {
    carregando.value = false
  }
}

function novo() { emEdicao.value = null; editorAberto.value = true }
function editar(f: Fluxo) { emEdicao.value = f; editorAberto.value = true }

async function alternarAtivo(f: Fluxo) {
  ocupado.value = true
  try {
    await apiAprov('PUT', `/api/fluxos-aprovacao/${f.id}`, { ...f, ativo: !f.ativo })
    toast.success(f.ativo ? 'Fluxo desativado. Solicitações em andamento dele foram canceladas.' : 'Fluxo ativado.')
    await carregar()
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível alterar o fluxo.')
  } finally {
    ocupado.value = false
  }
}

function pedirExclusao(f: Fluxo) { excluindo.value = f; confirmaAberta.value = true }
async function excluir() {
  if (!excluindo.value) return
  ocupado.value = true
  try {
    await apiAprov('DELETE', `/api/fluxos-aprovacao/${excluindo.value.id}`)
    toast.success('Fluxo excluído.')
    confirmaAberta.value = false
    await carregar()
  } catch (e) {
    // 422: fluxo com histórico não pode ser excluído (só desativado)
    toast.error(e instanceof Error ? e.message : 'Não foi possível excluir o fluxo.')
    confirmaAberta.value = false
  } finally {
    ocupado.value = false
  }
}

function resumoFiltros(f: Fluxo): string {
  if (!f.filtros.length) return 'Todas as propostas'
  const grupos = new Set(f.filtros.map((x) => x.grupo)).size
  return `${f.filtros.length} condição(ões)${grupos > 1 ? ` em ${grupos} grupos (OU)` : ''}`
}

onMounted(carregar)
</script>

<template>
  <div class="adm-tela">
    <div class="adm-barra">
      <button type="button" class="adm-btn primario" @click="novo"><i class="mdi mdi-plus"></i> Novo fluxo</button>
      <span class="adm-total">{{ fluxos.length }} fluxo(s)</span>
    </div>
    <div class="adm-aviso">
      <strong>Como funciona:</strong> ao salvar uma proposta (ou ao pedir aprovação), os fluxos ativos são verificados pela ordem.
      O primeiro que se enquadrar cria a solicitação no nível 1. Proposta aguardando aprovação ou rejeitada não pode ser compartilhada.
    </div>

    <p v-if="carregando" class="adm-vazio" role="status">Carregando...</p>
    <div v-else-if="erro" class="adm-erro" role="alert">
      <p>{{ erro }}</p>
      <button type="button" class="adm-btn" @click="carregar">Tentar novamente</button>
    </div>
    <p v-else-if="!fluxos.length" class="adm-vazio">Nenhum fluxo de aprovação. Crie o primeiro para exigir aprovação de propostas (por exemplo, desconto acima de 10%).</p>

    <div v-else class="adm-tabela-wrap">
      <table class="adm-tabela">
        <thead>
          <tr><th>Ordem</th><th>Nome</th><th>Tipo</th><th>Condições</th><th>Níveis</th><th>Situação</th><th></th></tr>
        </thead>
        <tbody>
          <tr v-for="f in fluxos" :key="f.id ?? 0">
            <td>{{ f.ordem }}</td>
            <td class="adm-principal">{{ f.nome }}<div v-if="f.mensagem" class="adm-sub">{{ f.mensagem }}</div></td>
            <td>{{ ROTULO_TIPO[f.tipo] ?? f.tipo }}</td>
            <td>{{ resumoFiltros(f) }}</td>
            <td>
              <span v-for="(n, i) in f.niveis" :key="i" class="nivel-res">
                {{ i + 1 }}º: {{ n.aprovadores.map((a) => a.usuarioNome ?? a.perfil).join(', ') }}
              </span>
              <span v-if="!f.niveis.length" class="adm-sub">-</span>
            </td>
            <td><UiBadge :tom="f.ativo ? 'sucesso' : 'neutro'">{{ f.ativo ? 'Ativo' : 'Inativo' }}</UiBadge></td>
            <td class="adm-acoes">
              <button type="button" class="adm-icone" :aria-label="`Editar ${f.nome}`" @click="editar(f)"><i class="mdi mdi-pencil-outline"></i></button>
              <button type="button" class="adm-icone" :disabled="ocupado" :aria-label="f.ativo ? `Desativar ${f.nome}` : `Ativar ${f.nome}`" @click="alternarAtivo(f)">
                <i class="mdi" :class="f.ativo ? 'mdi-pause-circle-outline' : 'mdi-play-circle-outline'"></i>
              </button>
              <button type="button" class="adm-icone perigo" :aria-label="`Excluir ${f.nome}`" @click="pedirExclusao(f)"><i class="mdi mdi-delete-outline"></i></button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <EditorFluxoAprovacao
      v-model="editorAberto" :fluxo="emEdicao" :campos="campos" :usuarios="usuarios" :moedas="moedas" @salvo="carregar"
    />

    <UiModal v-model="confirmaAberta" titulo="Excluir fluxo" largura="440px">
      <p class="confirma">Excluir o fluxo <strong>{{ excluindo?.nome }}</strong>? Fluxos que já têm solicitações no histórico não podem ser excluídos; desative-os.</p>
      <template #rodape>
        <button type="button" class="adm-btn" :disabled="ocupado" @click="confirmaAberta = false">Cancelar</button>
        <button type="button" class="adm-btn perigo" :disabled="ocupado" @click="excluir">Excluir</button>
      </template>
    </UiModal>
  </div>
</template>

<style scoped>
.nivel-res { display: block; font-size: 12px; overflow-wrap: anywhere; }
.confirma { margin: 0; font-size: 13px; color: var(--text-secondary); }
</style>
