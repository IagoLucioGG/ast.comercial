<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { odataGet } from '@/services/api'
interface Etiqueta { Id: number; Nome: string; Cor: string | null; Ativo: boolean }
const itens = ref<Etiqueta[]>([]); const carregando = ref(false)
async function carregar() { carregando.value = true; try { const r = await odataGet<Etiqueta>('Etiquetas', { orderby: 'Nome asc', select: 'Id,Nome,Cor,Ativo' }); itens.value = r.value } finally { carregando.value = false } }
onMounted(carregar)
</script>
<template><div class="tc"><div class="tw"><table><thead><tr><th>Cor</th><th>Nome</th><th>Status</th></tr></thead><tbody><tr v-if="carregando"><td colspan="3" class="tm">Carregando...</td></tr><tr v-else-if="!itens.length"><td colspan="3" class="tm">Nenhuma etiqueta</td></tr><tr v-for="e in itens" :key="e.Id"><td><span class="cor-dot" :style="{background:e.Cor??'#666'}"></span></td><td class="tp">{{e.Nome}}</td><td><span class="dot" :class="{on:e.Ativo}"></span>{{e.Ativo?'Ativa':'Inativa'}}</td></tr></tbody></table></div></div></template>
<style scoped>.tc{display:flex;flex-direction:column;height:100%}.tw{flex:1;overflow:auto;border:1px solid var(--border);border-radius:10px}table{width:100%;border-collapse:collapse;font-size:13px}thead{position:sticky;top:0;z-index:1}th{background:var(--bg-secondary);color:var(--text-muted);font-weight:600;text-transform:uppercase;font-size:11px;padding:12px 14px;text-align:left;border-bottom:1px solid var(--border)}td{padding:10px 14px;border-bottom:1px solid var(--border);color:var(--text-secondary)}tr:hover td{background:var(--bg-elevated)}.tm{text-align:center;padding:40px;color:var(--text-muted)}.tp{color:var(--text-primary);font-weight:500}.cor-dot{display:inline-block;width:14px;height:14px;border-radius:4px}.dot{display:inline-block;width:8px;height:8px;border-radius:50%;background:#ef4444;margin-right:6px}.dot.on{background:#22c55e}</style>
