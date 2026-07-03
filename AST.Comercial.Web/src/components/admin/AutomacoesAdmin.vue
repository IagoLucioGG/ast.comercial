<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { odataGet, type ODataParams } from '@/services/api'
import TabsFiltro from '@/components/shared/TabsFiltro.vue'
interface Automacao { Id: number; Nome: string; Descricao: string | null; Gatilho: string; EntidadeAlvo: string; Ativo: boolean; CriadoEm: string }
const itens = ref<Automacao[]>([]); const total = ref(0); const carregando = ref(false); const pagina = ref(1); const porPagina = ref(20); const filtroAba = ref<string | null>(null)
async function carregar() { carregando.value = true; try { const p: ODataParams = { top: porPagina.value, skip: (pagina.value - 1) * porPagina.value, orderby: 'Nome asc', count: true, select: 'Id,Nome,Descricao,Gatilho,EntidadeAlvo,Ativo,CriadoEm' }; if (filtroAba.value) p.filter = filtroAba.value; const r = await odataGet<Automacao>('Automacoes', p); itens.value = r.value; total.value = r['@odata.count'] ?? 0 } finally { carregando.value = false } }
function onFiltro(f: string | null, _o: string | null, pp: number) { filtroAba.value = f; porPagina.value = pp; pagina.value = 1; carregar() }
function pg(p: number) { pagina.value = p; carregar() }
const tp = () => Math.ceil(total.value / porPagina.value)
onMounted(carregar)
</script>
<template>
    <div class="tc">
        <TabsFiltro entidade="Automacao" @filtro-alterado="onFiltro" />
        <div class="tb"><span class="tt">{{ total }} automações</span></div>
        <div class="tw">
            <table>
                <thead>
                    <tr>
                        <th>Nome</th>
                        <th>Gatilho</th>
                        <th>Entidade</th>
                        <th>Status</th>
                    </tr>
                </thead>
                <tbody>
                    <tr v-if="carregando">
                        <td colspan="4" class="tm">Carregando...</td>
                    </tr>
                    <tr v-else-if="!itens.length">
                        <td colspan="4" class="tm">Nenhuma automação</td>
                    </tr>
                    <tr v-for="e in itens" :key="e.Id">
                        <td class="tp">{{ e.Nome }}</td>
                        <td><span class="badge">{{ e.Gatilho }}</span></td>
                        <td>{{ e.EntidadeAlvo }}</td>
                        <td><span class="dot" :class="{ on: e.Ativo }"></span>{{ e.Ativo ? 'Ativa' : 'Inativa' }}</td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div v-if="tp() > 1" class="tpg"><button :disabled="pagina <= 1" @click="pg(pagina - 1)"><i
                    class="mdi mdi-chevron-left"></i></button><span>{{ pagina }}/{{ tp() }}</span><button
                :disabled="pagina >= tp()" @click="pg(pagina + 1)"><i class="mdi mdi-chevron-right"></i></button></div>
    </div>
</template>
<style
    scoped>
    .tc {
        display: flex;
        flex-direction: column;
        height: 100%
    }

    .tb {
        margin-bottom: 12px
    }

    .tt {
        color: var(--text-muted);
        font-size: 12px
    }

    .tw {
        flex: 1;
        overflow: auto;
        border: 1px solid var(--border);
        border-radius: 10px
    }

    table {
        width: 100%;
        border-collapse: collapse;
        font-size: 13px
    }

    thead {
        position: sticky;
        top: 0;
        z-index: 1
    }

    th {
        background: var(--bg-secondary);
        color: var(--text-muted);
        font-weight: 600;
        text-transform: uppercase;
        font-size: 11px;
        padding: 12px 14px;
        text-align: left;
        border-bottom: 1px solid var(--border)
    }

    td {
        padding: 10px 14px;
        border-bottom: 1px solid var(--border);
        color: var(--text-secondary)
    }

    tr:hover td {
        background: var(--bg-elevated)
    }

    .tm {
        text-align: center;
        padding: 40px;
        color: var(--text-muted)
    }

    .tp {
        color: var(--text-primary);
        font-weight: 500
    }

    .badge {
        background: var(--bg-elevated);
        border: 1px solid var(--border);
        padding: 2px 8px;
        border-radius: 4px;
        font-size: 11px
    }

    .dot {
        display: inline-block;
        width: 8px;
        height: 8px;
        border-radius: 50%;
        background: #ef4444;
        margin-right: 6px
    }

    .dot.on {
        background: #22c55e
    }

    .tpg {
        display: flex;
        align-items: center;
        justify-content: center;
        gap: 12px;
        padding: 12px 0
    }

    .tpg button {
        width: 32px;
        height: 32px;
        border-radius: 6px;
        border: 1px solid var(--border);
        background: var(--bg-surface);
        color: var(--text-secondary);
        cursor: pointer;
        display: flex;
        align-items: center;
        justify-content: center
    }

    .tpg button:disabled {
        opacity: .3
    }

    .tpg span {
        color: var(--text-muted);
        font-size: 13px
    }
</style>
