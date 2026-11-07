<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, ref } from 'vue'
import { useFunisStore, type EtapaItem } from '@/stores/funis'
import { formatarData, formatarValor, iniciais } from '@/composables/useFunilFormato'
import type { CardNegocio } from '@/composables/useFunilKanban'

/** Progresso do checklist da etapa atual do negócio (só informado quando a etapa tem itens). */
export interface ProgressoChecklist { total: number; concluidos: number; obrigatoriosPendentes: number }

const props = defineProps<{ card: CardNegocio; etapas: EtapaItem[]; arrastando?: boolean; checklist?: ProgressoChecklist }>()
const emit = defineEmits<{
  abrir: [id: number]
  mover: [id: number, etapaId: number]
  ganhar: [id: number]
  perder: [id: number]
  reabrir: [id: number]
  dragstart: [id: number, ev: DragEvent]
  dragend: []
}>()

const store = useFunisStore()
const valorTxt = computed(() => formatarValor(props.card.valor, store.moedaDe(props.card.moedaId)))
const outrasEtapas = computed(() => props.etapas.filter((e) => e.id !== props.card.etapaId))
/** Negócio fechado (ganho/perdido): cartão somente leitura, sem arrastar nem mover. */
const fechado = computed(() => !!props.card.fechadoEm)
const dataFechamento = computed(() => formatarData(props.card.fechadoEm))
const statusInfo = computed(() => store.status.find((s) => s.id === props.card.statusId))
const tagStatus = computed(() => {
  if (!fechado.value) return null
  const tipo = statusInfo.value?.tipoBase
  const texto = statusInfo.value?.nome ?? 'Fechado'
  return { texto, classe: tipo === 1 ? 'nc-tag-ganho' : tipo === 2 ? 'nc-tag-perdido' : '' }
})
const progressoCk = computed(() => {
  const c = props.checklist
  if (!c || c.total <= 0) return null
  return {
    texto: `${c.concluidos}/${c.total}`,
    pct: Math.round((c.concluidos / c.total) * 100),
    bloqueia: c.obrigatoriosPendentes > 0,
    rotulo: `Checklist: ${c.concluidos} de ${c.total} itens concluídos${c.obrigatoriosPendentes ? `, ${c.obrigatoriosPendentes} obrigatório(s) pendente(s)` : ''}`,
  }
})
const respIniciais = computed(() => iniciais(props.card.responsavelNome))
const respRotulo = computed(() => `Responsável: ${props.card.responsavelNome}`)

// ---- menu "Mover para…" (operável só por teclado) ----
const aberto = ref(false)
const botao = ref<HTMLButtonElement | null>(null)
const menu = ref<HTMLElement | null>(null)
const pos = ref({ top: 0, left: 0 })

function itensMenu(): HTMLButtonElement[] {
  return [...(menu.value?.querySelectorAll<HTMLButtonElement>('button[role="menuitem"]') ?? [])]
}

async function abrirMenu() {
  const r = botao.value?.getBoundingClientRect()
  if (r) {
    const largura = 220
    pos.value = {
      top: Math.min(r.bottom + 4, window.innerHeight - 260),
      left: Math.max(8, Math.min(r.right - largura, window.innerWidth - largura - 8)),
    }
  }
  aberto.value = true
  document.addEventListener('mousedown', foraDoMenu)
  await nextTick()
  itensMenu()[0]?.focus()
}

function fecharMenu(devolverFoco = true) {
  aberto.value = false
  document.removeEventListener('mousedown', foraDoMenu)
  if (devolverFoco) botao.value?.focus()
}

function foraDoMenu(e: MouseEvent) {
  const alvo = e.target as Node
  if (menu.value?.contains(alvo) || botao.value?.contains(alvo)) return
  fecharMenu(false)
}

/** Setas, Home/End navegam; Esc e Tab fecham devolvendo o foco ao botão; Enter/Espaço ativam o item focado. */
function teclado(e: KeyboardEvent) {
  if (e.key === 'Escape') { e.preventDefault(); e.stopPropagation(); fecharMenu(); return }
  if (e.key === 'Tab') { e.preventDefault(); fecharMenu(); return }
  const itens = itensMenu()
  if (!itens.length) return
  const i = itens.indexOf(document.activeElement as HTMLButtonElement)
  let prox = -1
  if (e.key === 'ArrowDown') prox = (i + 1) % itens.length
  else if (e.key === 'ArrowUp') prox = (i - 1 + itens.length) % itens.length
  else if (e.key === 'Home') prox = 0
  else if (e.key === 'End') prox = itens.length - 1
  if (prox < 0) return
  e.preventDefault()
  itens[prox]?.focus()
}

/** O botão que abre o menu também abre com seta para baixo. */
function tecladoBotao(e: KeyboardEvent) {
  if (e.key === 'ArrowDown' && !aberto.value) { e.preventDefault(); abrirMenu() }
}

function escolher(fn: () => void) { fecharMenu(false); fn() }

onBeforeUnmount(() => document.removeEventListener('mousedown', foraDoMenu))
</script>

<template>
  <article
    class="nc" :class="{ 'nc-arrastando': arrastando, 'nc-somente-leitura': fechado }" :draggable="!fechado"
    @dragstart="emit('dragstart', card.id, $event)" @dragend="emit('dragend')"
    @click="emit('abrir', card.id)"
  >
    <div class="nc-topo">
      <button class="nc-titulo" type="button" @click.stop="emit('abrir', card.id)">{{ card.titulo }}</button>
      <button
        ref="botao" class="nc-menu-btn" type="button" aria-haspopup="menu" :aria-expanded="aberto"
        :aria-label="`Ações do negócio ${card.titulo}`" @click.stop="aberto ? fecharMenu() : abrirMenu()" @keydown="tecladoBotao"
      ><i class="mdi mdi-dots-vertical" aria-hidden="true"></i></button>
    </div>
    <p v-if="card.clienteNome" class="nc-cliente"><i class="mdi mdi-domain" aria-hidden="true"></i> {{ card.clienteNome }}</p>
    <div v-if="progressoCk" class="nc-ck" role="img" :aria-label="progressoCk.rotulo" :title="progressoCk.rotulo">
      <i class="mdi" :class="progressoCk.bloqueia ? 'mdi-lock-outline' : 'mdi-checkbox-marked-outline'" aria-hidden="true"></i>
      <span class="nc-ck-barra"><span :style="{ width: progressoCk.pct + '%' }"></span></span>
      <span class="nc-ck-txt">{{ progressoCk.texto }}</span>
    </div>
    <div class="nc-base">
      <span class="nc-valor">{{ valorTxt }}</span>
      <span class="nc-direita">
        <span v-if="tagStatus" class="nc-tag" :class="tagStatus.classe">{{ tagStatus.texto }}</span>
        <span v-if="dataFechamento" class="nc-data" :title="`Fechado em ${dataFechamento}`">{{ dataFechamento }}</span>
        <span v-if="card.responsavelNome" class="nc-avatar" role="img" :title="card.responsavelNome" :aria-label="respRotulo">{{ respIniciais }}</span>
      </span>
    </div>

    <Teleport to="body">
      <div v-if="aberto" ref="menu" class="nc-menu" role="menu" :aria-label="`Ações do negócio ${card.titulo}`" :style="{ top: pos.top + 'px', left: pos.left + 'px' }" @keydown="teclado" @click.stop>
        <template v-if="!fechado">
          <p class="nc-menu-rotulo" aria-hidden="true">Mover para…</p>
          <button v-for="e in outrasEtapas" :key="e.id" class="nc-item" type="button" role="menuitem" @click="escolher(() => emit('mover', card.id, e.id))">
            <i class="mdi mdi-arrow-right-thin" aria-hidden="true"></i> {{ e.nome }}
          </button>
          <p v-if="!outrasEtapas.length" class="nc-menu-vazio">Sem outras etapas</p>
          <hr />
          <button class="nc-item nc-ganhar" type="button" role="menuitem" @click="escolher(() => emit('ganhar', card.id))">
            <i class="mdi mdi-trophy-outline" aria-hidden="true"></i> Ganhar
          </button>
          <button class="nc-item nc-perder" type="button" role="menuitem" @click="escolher(() => emit('perder', card.id))">
            <i class="mdi mdi-close-circle-outline" aria-hidden="true"></i> Perder
          </button>
        </template>
        <button v-else class="nc-item" type="button" role="menuitem" @click="escolher(() => emit('reabrir', card.id))">
          <i class="mdi mdi-restore" aria-hidden="true"></i> Reabrir
        </button>
      </div>
    </Teleport>
  </article>
</template>

<style scoped>
.nc { background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; padding: 10px 12px; cursor: grab; transition: border-color .15s, box-shadow .15s; }
.nc:hover { border-color: var(--accent); box-shadow: 0 2px 10px rgba(0, 0, 0, .18); }
.nc:active { cursor: grabbing; }
.nc-arrastando { opacity: .4; }
.nc-topo { display: flex; align-items: flex-start; gap: 4px; }
.nc-titulo { flex: 1; text-align: left; background: none; border: 0; padding: 0; color: var(--text-primary); font-size: 13px; font-weight: 600; line-height: 1.35; cursor: pointer; word-break: break-word; }
.nc-titulo:hover { color: var(--accent); }
.nc-menu-btn { flex-shrink: 0; width: 24px; height: 24px; border: 0; border-radius: 6px; background: none; color: var(--text-muted); font-size: 16px; cursor: pointer; display: flex; align-items: center; justify-content: center; }
.nc-menu-btn:hover, .nc-menu-btn:focus-visible { background: var(--bg-elevated); color: var(--text-primary); }
.nc-cliente { margin-top: 4px; font-size: 12px; color: var(--text-secondary); overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
.nc-ck { display: flex; align-items: center; gap: 6px; margin-top: 8px; font-size: 11px; color: var(--text-muted); }
.nc-ck .mdi { font-size: 14px; }
.nc-ck-barra { flex: 1; height: 4px; border-radius: 2px; background: var(--bg-elevated); overflow: hidden; }
.nc-ck-barra span { display: block; height: 100%; background: var(--accent); }
.nc-base { display: flex; align-items: center; justify-content: space-between; margin-top: 8px; gap: 8px; }
.nc-valor { font-size: 13px; font-weight: 700; color: var(--accent); }
.nc-tag { font-size: 10px; padding: 2px 8px; border-radius: 4px; background: var(--bg-elevated); border: 1px solid var(--border); color: var(--text-secondary); white-space: nowrap; }
.nc-tag-ganho { color: #22c55e; border-color: #22c55e; }
.nc-tag-perdido { color: var(--danger); border-color: var(--danger); }
.nc-direita { display: flex; align-items: center; gap: 6px; min-width: 0; }
.nc-data { font-size: 11px; color: var(--text-muted); white-space: nowrap; }
.nc-avatar { flex-shrink: 0; width: 22px; height: 22px; border-radius: 50%; background: var(--accent); color: #000; font-size: 10px; font-weight: 700; display: flex; align-items: center; justify-content: center; cursor: default; }
.nc-somente-leitura, .nc-somente-leitura:active { cursor: default; }
.nc-menu { position: fixed; z-index: 3000; width: 220px; max-height: 300px; overflow-y: auto; padding: 6px; background: var(--bg-elevated); border: 1px solid var(--border); border-radius: 10px; box-shadow: 0 12px 32px rgba(0, 0, 0, .4); }
.nc-menu-rotulo { padding: 4px 8px; font-size: 10px; font-weight: 600; text-transform: uppercase; letter-spacing: .6px; color: var(--text-muted); }
.nc-menu-vazio { padding: 6px 8px; font-size: 12px; color: var(--text-muted); }
.nc-menu hr { border: 0; border-top: 1px solid var(--border); margin: 6px 0; }
.nc-item { width: 100%; display: flex; align-items: center; gap: 8px; text-align: left; padding: 7px 8px; border: 0; border-radius: 6px; background: none; color: var(--text-primary); font-size: 13px; cursor: pointer; }
.nc-item:hover, .nc-item:focus, .nc-item:focus-visible { background: var(--bg-surface); outline: none; color: var(--accent); }
.nc-ganhar { color: #22c55e; }
.nc-perder { color: var(--danger); }
</style>
