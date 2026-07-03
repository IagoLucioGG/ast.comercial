<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { odataGet, type ODataParams } from '@/services/api'

interface UsuarioInteg { Id: number; Nome: string; Email: string; TokenIntegracao: string | null; DescricaoIntegracao: string | null; Ativo: boolean; CriadoEm: string }

const itens = ref<UsuarioInteg[]>([])
const total = ref(0)
const carregando = ref(false)
const pagina = ref(1)
const porPagina = 20

async function carregar() {
  carregando.value = true
  try {
    const params: ODataParams = { top: porPagina, skip: (pagina.value - 1) * porPagina, orderby: 'Nome asc', count: true, filter: "Tipo eq 'Integracao'", select: 'Id,Nome,Email,TokenIntegracao,DescricaoIntegracao,Ativo,CriadoEm' }
    const res = await odataGet<UsuarioInteg>('Usuarios', params)
    itens.value = res.value
    total.value = res['@odata.count'] ?? 0
  } finally { carregando.value = false }
}

function mudarPagina(p: number) { pagina.value = p; carregar() }
const totalPaginas = () => Math.ceil(total.value / porPagina)
onMounted(carregar)
</script>

<template>
  <div class="tabela-container">
    <div class="tabela-toolbar"><span class="total-registros">{{ total }} usuários de integração</span></div>
    <div class="tabela-wrapper">
      <table>
        <thead><tr><th>Nome</th><th>Email</th><th>Descrição</th><th>Token (API Key)</th><th>Status</th></tr></thead>
        <tbody>
          <tr v-if="carregando"><td colspan="5" class="loading-cell">Carregando...</td></tr>
          <tr v-else-if="itens.length === 0"><td colspan="5" class="empty-cell">Nenhum usuário de integração</td></tr>
          <tr v-for="u in itens" :key="u.Id">
            <td class="cell-primary">{{ u.Nome }}</td>
            <td>{{ u.Email }}</td>
            <td>{{ u.DescricaoIntegracao ?? '-' }}</td>
            <td class="cell-token">{{ u.TokenIntegracao ? u.TokenIntegracao.substring(0, 16) + '...' : '-' }}</td>
            <td><span class="status-dot" :class="{ ativo: u.Ativo }"></span>{{ u.Ativo ? 'Ativo' : 'Inativo' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
    <div v-if="totalPaginas() > 1" class="paginacao">
      <button :disabled="pagina <= 1" @click="mudarPagina(pagina - 1)"><i class="mdi mdi-chevron-left"></i></button>
      <span>{{ pagina }} / {{ totalPaginas() }}</span>
      <button :disabled="pagina >= totalPaginas()" @click="mudarPagina(pagina + 1)"><i class="mdi mdi-chevron-right"></i></button>
    </div>
  </div>
</template>

<style scoped>
.tabela-container { display: flex; flex-direction: column; height: 100%; }
.tabela-toolbar { margin-bottom: 16px; }
.total-registros { color: var(--text-muted); font-size: 12px; }
.tabela-wrapper { flex: 1; overflow: auto; border: 1px solid var(--border); border-radius: 10px; }
table { width: 100%; border-collapse: collapse; font-size: 13px; }
thead { position: sticky; top: 0; z-index: 1; }
th { background: var(--bg-secondary); color: var(--text-muted); font-weight: 600; text-transform: uppercase; font-size: 11px; letter-spacing: 0.5px; padding: 12px 14px; text-align: left; border-bottom: 1px solid var(--border); }
td { padding: 10px 14px; border-bottom: 1px solid var(--border); color: var(--text-secondary); }
tr:hover td { background: var(--bg-elevated); }
.loading-cell, .empty-cell { text-align: center; padding: 40px; color: var(--text-muted); }
.cell-primary { color: var(--text-primary); font-weight: 500; }
.cell-token { font-family: monospace; font-size: 12px; color: var(--text-muted); }
.status-dot { display: inline-block; width: 8px; height: 8px; border-radius: 50%; background: #ef4444; margin-right: 6px; }
.status-dot.ativo { background: #22c55e; }
.paginacao { display: flex; align-items: center; justify-content: center; gap: 12px; padding: 12px 0; }
.paginacao button { width: 32px; height: 32px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-surface); color: var(--text-secondary); cursor: pointer; display: flex; align-items: center; justify-content: center; }
.paginacao button:hover:not(:disabled) { background: var(--bg-elevated); }
.paginacao button:disabled { opacity: 0.3; cursor: not-allowed; }
.paginacao span { color: var(--text-muted); font-size: 13px; }
</style>
