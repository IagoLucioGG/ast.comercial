<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { odataGet } from '@/services/api'
interface Funil { Id: number; Nome: string; Ordem: number; Ativo: boolean }
const itens = ref<Funil[]>([]); const carregando = ref(false)
async function carregar() { carregando.value = true; try { const r = await odataGet<Funil>('Funis', { orderby: 'Ordem asc', select: 'Id,Nome,Ordem,Ativo' }); itens.value = r.value } finally { carregando.value = false } }
onMounted(carregar)
</script>
<template><div class="tc"><div class="tw"><table><thead><tr><th>Nome</th><th>Ordem</th><th>Status</th></tr></thead><tbody><tr v-if="carregando"><td colspan="3" class="tm">Carregando...</td></tr><tr v-else-if="!itens.length"><td colspan="3" class="tm">Nenhum funil cadastrado</td></tr><tr v-for="e in itens" :key="e.Id"><td class="tp">{{e.Nome}}</td><td>{{e.Ordem}}</td><td><span class="dot" :class="{on:e.Ativo}"></span>{{e.Ativo?'Ativo':'Inativo'}}</td></tr></tbody></table></div></div></template>
<style scoped>.tc{display:flex;flex-direction:column;height:100%}.tw{flex:1;overflow:auto;border:1px solid var(--border);border-radius:10px}table{width:100%;border-collapse:collapse;font-size:13px}thead{position:sticky;top:0;z-index:1}th{background:var(--bg-secondary);color:var(--text-muted);font-weight:600;text-transform:uppercase;font-size:11px;padding:12px 14px;text-align:left;border-bottom:1px solid var(--border)}td{padding:10px 14px;border-bottom:1px solid var(--border);color:var(--text-secondary)}tr:hover td{background:var(--bg-elevated)}.tm{text-align:center;padding:40px;color:var(--text-muted)}.tp{color:var(--text-primary);font-weight:500}.dot{display:inline-block;width:8px;height:8px;border-radius:50%;background:#ef4444;margin-right:6px}.dot.on{background:#22c55e}</style>
