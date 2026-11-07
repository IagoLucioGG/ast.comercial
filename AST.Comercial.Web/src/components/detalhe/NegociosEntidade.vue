<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { odataRequest, montarQuery } from '@/services/api'
import { campo, lista, mensagemErro, formatarData, formatarMoeda, type Registro } from '@/composables/useEntidadeDetalhe'
import UiBadge from '@/components/ui/UiBadge.vue'
import DetalheEstado from './DetalheEstado.vue'

/** Negócios de um cliente. */
const props = defineProps<{ clienteId: number }>()

const itens = ref<Registro[]>([])
const carregando = ref(false)
const erro = ref('')

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({
      $filter: `ClienteId eq ${props.clienteId}`,
      $expand: 'Etapa($select=Nome),Status($select=Nome,TipoBase)',
      $orderby: 'CriadoEm desc',
      $top: 100,
    })
    itens.value = lista<Registro>(await odataRequest<unknown>('GET', `Negocios${q}`))
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar os negócios')
  } finally {
    carregando.value = false
  }
}

function tomStatus(n: Registro): 'sucesso' | 'perigo' | 'info' {
  const t = campo(campo(n, 'Status'), 'TipoBase')
  if (t === 1 || t === 'Ganho') return 'sucesso'
  if (t === 2 || t === 'Perdido') return 'perigo'
  return 'info'
}

watch(() => props.clienteId, carregar)
onMounted(carregar)
</script>

<template>
  <div>
    <div class="ne-barra">
      <RouterLink class="ne-novo" :to="{ path: '/negocios', query: { novo: '1', clienteId: String(clienteId) } }"><i class="mdi mdi-plus"></i> Novo negócio</RouterLink>
    </div>
    <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!itens.length" texto-vazio="Nenhum negócio para este cliente." icone="mdi-handshake-outline" @tentar="carregar">
      <div class="ne-wrap">
        <table class="ne-tab">
          <thead><tr><th>Título</th><th>Etapa</th><th>Status</th><th class="num">Valor</th><th>Criado em</th></tr></thead>
          <tbody>
            <tr v-for="n in itens" :key="campo(n, 'Id')">
              <td><RouterLink class="ne-link" :to="`/negocios/${campo(n, 'Id')}`">{{ campo(n, 'Titulo') }}</RouterLink></td>
              <td>{{ campo(campo(n, 'Etapa'), 'Nome') ?? '-' }}</td>
              <td><UiBadge :tom="tomStatus(n)">{{ campo(campo(n, 'Status'), 'Nome') ?? 'Aberto' }}</UiBadge></td>
              <td class="num">{{ formatarMoeda(campo(n, 'Valor')) }}</td>
              <td>{{ formatarData(campo(n, 'CriadoEm')) }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </DetalheEstado>
  </div>
</template>

<style scoped>
.ne-barra { display: flex; justify-content: flex-end; margin-bottom: 12px; }
.ne-novo { display: inline-flex; align-items: center; gap: 4px; padding: 7px 14px; border-radius: 8px; background: var(--accent); color: #000; font-size: 13px; font-weight: 600; text-decoration: none; }
.ne-novo:hover { background: var(--accent-hover); }
.ne-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 10px; }
.ne-tab { width: 100%; border-collapse: collapse; font-size: 13px; }
.ne-tab th { text-align: left; padding: 10px 14px; background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: .5px; border-bottom: 1px solid var(--border); }
.ne-tab td { padding: 10px 14px; border-bottom: 1px solid var(--border); color: var(--text-secondary); }
.ne-tab tr:last-child td { border-bottom: 0; }
.ne-tab .num { text-align: right; }
.ne-link { color: var(--accent); text-decoration: none; font-weight: 500; }
.ne-link:hover { text-decoration: underline; }
</style>
