<script setup lang="ts">
// Registro de atividades da empresa, com filtros por tipo, vínculo (entidade), situação e período, mais paginação.
import { ref, computed, onMounted } from 'vue'
import { odataQuery, montarQuery } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { msgErro, dataBr } from './comum/adminApi'
import './comum/admin-comum.css'

interface Atividade {
  Id: number; Titulo: string; Tipo: string; DataVencimento: string | null; Concluida: boolean
  ConcluidaEm: string | null; CriadoEm: string
  ClienteId: number | null; PessoaContatoId: number | null; NegocioId: number | null
}

const toast = useToast()
const itens = ref<Atividade[]>([])
const total = ref(0)
const carregando = ref(false)
const erro = ref('')
const pagina = ref(1)
const porPagina = ref(20)

const filtros = ref({ busca: '', tipo: '', vinculo: '', situacao: '', de: '', ate: '' })
const TIPOS = ['Tarefa', 'Ligacao', 'Email', 'Reuniao', 'Nota']
const ROTULO_TIPO: Record<string, string> = { Tarefa: 'Tarefa', Ligacao: 'Ligação', Email: 'E-mail', Reuniao: 'Reunião', Nota: 'Nota' }

const totalPaginas = computed(() => Math.max(1, Math.ceil(total.value / porPagina.value)))
const temFiltro = computed(() => Object.values(filtros.value).some((v) => v !== ''))

function limitesData(dia: string, fimDoDia: boolean): string {
  // 'YYYY-MM-DD' no fuso local -> instante UTC em ISO
  const [a, m, d] = dia.split('-').map(Number)
  const dt = new Date(a ?? 1970, (m ?? 1) - 1, (d ?? 1) + (fimDoDia ? 1 : 0))
  return dt.toISOString()
}

function montarFiltro(): string {
  const f = filtros.value
  const partes: string[] = []
  if (f.busca.trim()) partes.push(`contains(tolower(Titulo),'${f.busca.trim().toLowerCase().replace(/'/g, "''")}')`)
  if (TIPOS.includes(f.tipo)) partes.push(`Tipo eq '${f.tipo}'`)
  if (f.vinculo === 'cliente') partes.push('ClienteId ne null')
  else if (f.vinculo === 'contato') partes.push('PessoaContatoId ne null')
  else if (f.vinculo === 'negocio') partes.push('NegocioId ne null')
  else if (f.vinculo === 'nenhum') partes.push('ClienteId eq null and PessoaContatoId eq null and NegocioId eq null')
  if (f.situacao === 'concluida') partes.push('Concluida eq true')
  else if (f.situacao === 'pendente') partes.push('Concluida eq false')
  if (f.de) partes.push(`CriadoEm ge ${limitesData(f.de, false)}`)
  if (f.ate) partes.push(`CriadoEm lt ${limitesData(f.ate, true)}`)
  return partes.join(' and ')
}

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({
      $top: porPagina.value,
      $skip: (pagina.value - 1) * porPagina.value,
      $orderby: 'CriadoEm desc',
      $count: true,
      $filter: montarFiltro(),
      $select: 'Id,Titulo,Tipo,DataVencimento,Concluida,ConcluidaEm,CriadoEm,ClienteId,PessoaContatoId,NegocioId',
    })
    const r = await odataQuery<Atividade>(`Atividades${q}`)
    itens.value = r.value
    total.value = r['@odata.count'] ?? r.value.length
  } catch (e) {
    erro.value = msgErro(e, 'Não foi possível carregar as atividades.')
    toast.error(erro.value)
  } finally {
    carregando.value = false
  }
}

function aplicar() { pagina.value = 1; void carregar() }
function limpar() {
  filtros.value = { busca: '', tipo: '', vinculo: '', situacao: '', de: '', ate: '' }
  aplicar()
}
function mudarPagina(p: number) { pagina.value = Math.min(Math.max(1, p), totalPaginas.value); void carregar() }

function vinculo(a: Atividade): string {
  const v: string[] = []
  if (a.ClienteId) v.push('Cliente')
  if (a.PessoaContatoId) v.push('Contato')
  if (a.NegocioId) v.push('Negócio')
  return v.join(', ') || '-'
}

onMounted(carregar)
</script>

<template>
  <div class="adm-tela">
    <form class="filtros" @submit.prevent="aplicar">
      <div class="adm-campo grande">
        <label for="ra-busca">Título</label>
        <input id="ra-busca" v-model="filtros.busca" class="adm-input" type="search" placeholder="Buscar pelo título..." />
      </div>
      <div class="adm-campo">
        <label for="ra-tipo">Tipo</label>
        <select id="ra-tipo" v-model="filtros.tipo" class="adm-select" @change="aplicar">
          <option value="">Todos</option>
          <option v-for="t in TIPOS" :key="t" :value="t">{{ ROTULO_TIPO[t] }}</option>
        </select>
      </div>
      <div class="adm-campo">
        <label for="ra-vinculo">Entidade vinculada</label>
        <select id="ra-vinculo" v-model="filtros.vinculo" class="adm-select" @change="aplicar">
          <option value="">Todas</option>
          <option value="cliente">Cliente</option>
          <option value="contato">Contato</option>
          <option value="negocio">Negócio</option>
          <option value="nenhum">Sem vínculo</option>
        </select>
      </div>
      <div class="adm-campo">
        <label for="ra-sit">Situação</label>
        <select id="ra-sit" v-model="filtros.situacao" class="adm-select" @change="aplicar">
          <option value="">Todas</option>
          <option value="pendente">Pendentes</option>
          <option value="concluida">Concluídas</option>
        </select>
      </div>
      <div class="adm-campo">
        <label for="ra-de">Criada de</label>
        <input id="ra-de" v-model="filtros.de" class="adm-input" type="date" @change="aplicar" />
      </div>
      <div class="adm-campo">
        <label for="ra-ate">até</label>
        <input id="ra-ate" v-model="filtros.ate" class="adm-input" type="date" @change="aplicar" />
      </div>
      <div class="botoes">
        <button type="submit" class="adm-btn primario">Filtrar</button>
        <button type="button" class="adm-btn" :disabled="!temFiltro" @click="limpar">Limpar</button>
      </div>
    </form>

    <div class="adm-barra">
      <span class="adm-total" style="margin-left: 0">{{ total }} atividade(s)</span>
      <label class="por-pagina">Por página
        <select v-model.number="porPagina" class="adm-select" aria-label="Itens por página" @change="aplicar">
          <option :value="10">10</option><option :value="20">20</option><option :value="50">50</option><option :value="100">100</option>
        </select>
      </label>
    </div>

    <div class="adm-tabela-wrap">
      <table class="adm-tabela">
        <thead><tr><th>Título</th><th>Tipo</th><th>Vínculo</th><th>Vencimento</th><th>Situação</th><th>Criada em</th></tr></thead>
        <tbody>
          <tr v-if="carregando"><td colspan="6" class="adm-vazio">Carregando...</td></tr>
          <tr v-else-if="erro"><td colspan="6" class="adm-erro">{{ erro }}<br /><button class="adm-btn" @click="carregar">Tentar novamente</button></td></tr>
          <tr v-else-if="!itens.length"><td colspan="6" class="adm-vazio">{{ temFiltro ? 'Nenhuma atividade para os filtros escolhidos.' : 'Nenhuma atividade registrada.' }}</td></tr>
          <template v-else>
            <tr v-for="a in itens" :key="a.Id">
              <td class="adm-principal">{{ a.Titulo }}</td>
              <td><span class="badge-tipo">{{ ROTULO_TIPO[a.Tipo] ?? a.Tipo }}</span></td>
              <td>{{ vinculo(a) }}</td>
              <td>{{ dataBr(a.DataVencimento) }}</td>
              <td><span class="adm-dot" :class="{ on: a.Concluida }" :style="a.Concluida ? '' : 'background:#f59e0b'"></span>{{ a.Concluida ? 'Concluída' : 'Pendente' }}</td>
              <td>{{ dataBr(a.CriadoEm) }}</td>
            </tr>
          </template>
        </tbody>
      </table>
    </div>

    <div v-if="totalPaginas > 1" class="adm-pag">
      <button :disabled="pagina <= 1" aria-label="Página anterior" @click="mudarPagina(pagina - 1)"><i class="mdi mdi-chevron-left"></i></button>
      <span>{{ pagina }} / {{ totalPaginas }}</span>
      <button :disabled="pagina >= totalPaginas" aria-label="Próxima página" @click="mudarPagina(pagina + 1)"><i class="mdi mdi-chevron-right"></i></button>
    </div>
    <p class="adm-sub">O filtro por usuário depende de a atividade registrar o responsável, o que a API ainda não faz.</p>
  </div>
</template>

<style scoped>
.filtros { display: flex; flex-wrap: wrap; gap: 10px; align-items: flex-end; }
.filtros .adm-campo { width: 150px; }
.filtros .adm-campo.grande { width: 220px; }
.botoes { display: flex; gap: 8px; }
.badge-tipo { background: var(--bg-elevated); border: 1px solid var(--border); padding: 2px 8px; border-radius: 4px; font-size: 11px; }
.por-pagina { display: inline-flex; align-items: center; gap: 8px; margin-left: auto; font-size: 12px; color: var(--text-muted); }
.por-pagina select { width: 80px; padding: 4px 8px; }
</style>
