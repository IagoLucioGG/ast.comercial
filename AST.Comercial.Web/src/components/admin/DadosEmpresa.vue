<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { odataGet } from '@/services/api'
interface Empresa { Id: number; Nome: string; RazaoSocial: string|null; Cnpj: string|null; Email: string|null; Telefone: string|null; Cidade: string|null; Estado: string|null }
const empresa = ref<Empresa|null>(null); const carregando = ref(false)
async function carregar() { carregando.value = true; try { const r = await odataGet<Empresa>('Empresas', { top: 1, select: 'Id,Nome,RazaoSocial,Cnpj,Email,Telefone,Cidade,Estado' }); empresa.value = r.value[0] ?? null } finally { carregando.value = false } }
onMounted(carregar)
</script>
<template><div class="tc"><div v-if="carregando" class="tm">Carregando...</div><div v-else-if="!empresa" class="tm">Nenhuma empresa configurada</div><div v-else class="grid"><div class="campo"><label>Nome</label><span>{{empresa.Nome}}</span></div><div class="campo"><label>Razão Social</label><span>{{empresa.RazaoSocial??'-'}}</span></div><div class="campo"><label>CNPJ</label><span>{{empresa.Cnpj??'-'}}</span></div><div class="campo"><label>Email</label><span>{{empresa.Email??'-'}}</span></div><div class="campo"><label>Telefone</label><span>{{empresa.Telefone??'-'}}</span></div><div class="campo"><label>Cidade/Estado</label><span>{{[empresa.Cidade,empresa.Estado].filter(Boolean).join('/')||'-'}}</span></div></div></div></template>
<style scoped>.tc{height:100%}.tm{text-align:center;padding:40px;color:var(--text-muted)}.grid{display:grid;grid-template-columns:1fr 1fr;gap:20px;max-width:600px}.campo{display:flex;flex-direction:column;gap:4px}.campo label{font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:.5px}.campo span{font-size:14px;color:var(--text-primary)}</style>
