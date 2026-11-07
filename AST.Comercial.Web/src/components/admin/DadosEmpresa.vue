<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { odataQuery, odataRequest, montarQuery } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { msgErro } from './comum/adminApi'
import './comum/admin-comum.css'

interface Empresa {
  Id: number; Nome: string; RazaoSocial: string | null; Cnpj: string | null; Email: string | null
  Telefone: string | null; Endereco: string | null; Cidade: string | null; Estado: string | null
  Cep: string | null; LogoUrl: string | null
}
type CampoEmpresa = Exclude<keyof Empresa, 'Id'>

const CAMPOS: { chave: CampoEmpresa; rotulo: string; max: number; obrig?: boolean; largo?: boolean; tipo?: string }[] = [
  { chave: 'Nome', rotulo: 'Nome', max: 200, obrig: true },
  { chave: 'RazaoSocial', rotulo: 'Razão social', max: 200 },
  { chave: 'Cnpj', rotulo: 'CNPJ', max: 20 },
  { chave: 'Email', rotulo: 'E-mail', max: 150, tipo: 'email' },
  { chave: 'Telefone', rotulo: 'Telefone', max: 30, tipo: 'tel' },
  { chave: 'Cep', rotulo: 'CEP', max: 15 },
  { chave: 'Endereco', rotulo: 'Endereço', max: 300, largo: true },
  { chave: 'Cidade', rotulo: 'Cidade', max: 100 },
  { chave: 'Estado', rotulo: 'Estado', max: 50 },
  { chave: 'LogoUrl', rotulo: 'URL da logo', max: 500, largo: true },
]

const toast = useToast()
const original = ref<Empresa | null>(null)
const form = ref<Record<string, string>>({})
const carregando = ref(false)
const erro = ref('')
const salvando = ref(false)
const erros = ref<Record<string, string>>({})

function paraForm(e: Empresa) {
  const f: Record<string, string> = {}
  for (const c of CAMPOS) f[c.chave] = (e[c.chave] ?? '') as string
  form.value = f
}

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const r = await odataQuery<Empresa>(`Empresas${montarQuery({ $top: 1 })}`)
    original.value = r.value[0] ?? null
    if (original.value) paraForm(original.value)
  } catch (e) {
    erro.value = msgErro(e, 'Não foi possível carregar os dados da empresa.')
    toast.error(erro.value)
  } finally {
    carregando.value = false
  }
}
onMounted(carregar)

// Só envia o que mudou.
const alterados = computed(() => {
  const o = original.value
  if (!o) return {} as Record<string, string | null>
  const r: Record<string, string | null> = {}
  for (const c of CAMPOS) {
    const novo = (form.value[c.chave] ?? '').trim()
    const antigo = (o[c.chave] ?? '') as string
    if (novo !== antigo) r[c.chave] = novo === '' ? null : novo
  }
  return r
})
const sujo = computed(() => Object.keys(alterados.value).length > 0)

function validar(): boolean {
  const e: Record<string, string> = {}
  for (const c of CAMPOS) {
    const v = (form.value[c.chave] ?? '').trim()
    if (c.obrig && !v) e[c.chave] = `${c.rotulo} é obrigatório.`
    if (v.length > c.max) e[c.chave] = `${c.rotulo} deve ter no máximo ${c.max} caracteres.`
  }
  const email = (form.value.Email ?? '').trim()
  if (email && !email.includes('@')) e.Email = 'E-mail inválido.'
  const logo = (form.value.LogoUrl ?? '').trim()
  if (logo && !/^(https?:\/\/|\/)/.test(logo)) e.LogoUrl = 'Use uma URL começando com http://, https:// ou /.'
  erros.value = e
  return Object.keys(e).length === 0
}

async function salvar() {
  if (!original.value || !sujo.value || salvando.value || !validar()) return
  salvando.value = true
  try {
    const atualizado = await odataRequest<Empresa>('PATCH', `Empresas(${original.value.Id})`, alterados.value)
    original.value = { ...original.value, ...atualizado }
    paraForm(original.value)
    toast.success('Dados da empresa salvos.')
  } catch (e) {
    toast.error(msgErro(e))
  } finally {
    salvando.value = false
  }
}

function descartar() {
  if (original.value) paraForm(original.value)
  erros.value = {}
}

const logoOk = computed(() => /^(https?:\/\/|\/)/.test((form.value.LogoUrl ?? '').trim()))
</script>

<template>
  <div class="adm-tela">
    <div v-if="carregando" class="adm-vazio">Carregando...</div>
    <div v-else-if="erro" class="adm-erro">{{ erro }}<br /><button class="adm-btn" @click="carregar">Tentar novamente</button></div>
    <div v-else-if="!original" class="adm-vazio">Nenhuma empresa configurada.</div>
    <form v-else class="empresa-form" @submit.prevent="salvar">
      <div v-if="logoOk" class="logo-previa">
        <img :src="(form.LogoUrl ?? '').trim()" alt="Logo da empresa" />
      </div>
      <div class="adm-form adm-form-grade">
        <div v-for="c in CAMPOS" :key="c.chave" class="adm-campo" :class="{ largo: c.largo }">
          <label :for="`emp-${c.chave}`">{{ c.rotulo }} <span v-if="c.obrig" class="adm-obrig" aria-hidden="true">*</span></label>
          <input :id="`emp-${c.chave}`" v-model="form[c.chave]" class="adm-input" :type="c.tipo ?? 'text'" :maxlength="c.max" />
          <span v-if="erros[c.chave]" class="adm-invalido" role="alert">{{ erros[c.chave] }}</span>
        </div>
      </div>
      <div class="empresa-acoes">
        <button type="button" class="adm-btn" :disabled="!sujo || salvando" @click="descartar">Descartar alterações</button>
        <button type="submit" class="adm-btn primario" :disabled="!sujo || salvando">{{ salvando ? 'Salvando...' : 'Salvar' }}</button>
      </div>
    </form>
  </div>
</template>

<style scoped>
.empresa-form { max-width: 720px; display: flex; flex-direction: column; gap: 18px; }
.empresa-acoes { display: flex; justify-content: flex-end; gap: 8px; }
.logo-previa { width: 96px; height: 96px; border: 1px solid var(--border); border-radius: 12px; background: var(--bg-surface); display: flex; align-items: center; justify-content: center; overflow: hidden; }
.logo-previa img { max-width: 100%; max-height: 100%; object-fit: contain; }
</style>
