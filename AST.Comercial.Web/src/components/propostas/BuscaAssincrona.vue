<script setup lang="ts">
import { ref, watch, onBeforeUnmount } from 'vue'

export interface OpcaoBusca {
  id: number
  texto: string
  extra?: unknown
}

const props = withDefaults(defineProps<{
  /** Texto exibido no campo (nome do item já selecionado) */
  texto?: string
  placeholder?: string
  /** Função que consulta o servidor para o termo digitado */
  buscar: (termo: string) => Promise<OpcaoBusca[]>
  rotulo?: string
  desabilitado?: boolean
}>(), { texto: '', placeholder: 'Buscar...', rotulo: 'Buscar', desabilitado: false })

const emit = defineEmits<{ selecionar: [opcao: OpcaoBusca]; digitar: [texto: string] }>()

const termo = ref(props.texto)
const opcoes = ref<OpcaoBusca[]>([])
const aberto = ref(false)
const carregando = ref(false)
const erro = ref(false)
let temporizador: ReturnType<typeof setTimeout> | null = null
let seq = 0

watch(() => props.texto, (t) => { termo.value = t })

async function consultar() {
  const meu = ++seq
  carregando.value = true
  erro.value = false
  try {
    const r = await props.buscar(termo.value.trim())
    if (meu === seq) opcoes.value = r
  } catch {
    if (meu === seq) { opcoes.value = []; erro.value = true }
  } finally {
    if (meu === seq) carregando.value = false
  }
}

function aoDigitar() {
  emit('digitar', termo.value)
  aberto.value = true
  if (temporizador) clearTimeout(temporizador)
  temporizador = setTimeout(consultar, 250)
}

function aoFocar() {
  aberto.value = true
  consultar()
}

function escolher(o: OpcaoBusca) {
  termo.value = o.texto
  aberto.value = false
  emit('selecionar', o)
}

function aoSair() {
  // atraso para permitir o clique numa opção
  setTimeout(() => { aberto.value = false }, 150)
}

onBeforeUnmount(() => { if (temporizador) clearTimeout(temporizador) })
</script>

<template>
  <div class="busca">
    <input
      v-model="termo"
      class="busca-input"
      type="text"
      autocomplete="off"
      :placeholder="placeholder"
      :aria-label="rotulo"
      :disabled="desabilitado"
      @input="aoDigitar"
      @focus="aoFocar"
      @blur="aoSair"
      @keydown.esc="aberto = false"
    />
    <ul v-if="aberto" class="busca-lista" role="listbox">
      <li v-if="carregando" class="busca-aviso">Buscando...</li>
      <li v-else-if="erro" class="busca-aviso erro">Não foi possível buscar.</li>
      <li v-else-if="!opcoes.length" class="busca-aviso">Nenhum resultado.</li>
      <li
        v-for="o in opcoes"
        :key="o.id"
        role="option"
        class="busca-item"
        @mousedown.prevent="escolher(o)"
      >{{ o.texto }}</li>
    </ul>
  </div>
</template>

<style scoped>
.busca { position: relative; width: 100%; }
.busca-input { width: 100%; padding: 7px 10px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); font-size: 13px; }
.busca-input:focus { outline: none; border-color: var(--accent); }
.busca-lista { position: absolute; z-index: 30; left: 0; right: 0; top: calc(100% + 2px); max-height: 220px; overflow-y: auto; margin: 0; padding: 4px; list-style: none; background: var(--bg-elevated); border: 1px solid var(--border); border-radius: 6px; box-shadow: 0 8px 24px rgba(0, 0, 0, .35); }
.busca-item { padding: 7px 10px; border-radius: 4px; font-size: 13px; color: var(--text-primary); cursor: pointer; }
.busca-item:hover { background: var(--bg-surface); color: var(--accent); }
.busca-aviso { padding: 7px 10px; font-size: 12px; color: var(--text-muted); }
.busca-aviso.erro { color: var(--danger); }
</style>
