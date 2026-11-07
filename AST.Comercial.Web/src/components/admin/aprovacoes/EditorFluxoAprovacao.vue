<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import UiModal from '@/components/ui/UiModal.vue'
import { apiAprov, type CampoFiltro, type Fluxo } from '@/components/aprovacoes/aprovacaoApi'
import { useToast } from '@/composables/useToast'
import '../comum/admin-comum.css'

export interface OpcaoSimples { id: number; nome: string }

const props = defineProps<{
  modelValue: boolean
  /** null = novo fluxo */
  fluxo: Fluxo | null
  campos: CampoFiltro[]
  usuarios: OpcaoSimples[]
  moedas: OpcaoSimples[]
}>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean]; salvo: [] }>()
const toast = useToast()

interface FiltroForm { campo: string; operador: string; valor: string }
interface AprovadorForm { tipo: 'usuario' | 'perfil'; usuarioId: number | null; perfil: string }
interface NivelForm { descricao: string; aprovadores: AprovadorForm[] }

const PERFIS = ['Administrador', 'Gerente', 'Vendedor', 'Visualizador']
const OPERADORES: Record<string, string> = {
  Igual: 'é igual a', Diferente: 'é diferente de', Maior: 'é maior que', Menor: 'é menor que',
  MaiorOuIgual: 'é maior ou igual a', MenorOuIgual: 'é menor ou igual a', Vazio: 'está vazio', NaoVazio: 'não está vazio',
}
const TIPOS = [
  { valor: 'ControleAlcada', rotulo: 'Controle de alçada', ajuda: 'Gera solicitação e exige a aprovação dos níveis.' },
  { valor: 'Bloqueio', rotulo: 'Bloqueio', ajuda: 'Gera solicitação e bloqueia o compartilhamento até a aprovação.' },
  { valor: 'Alerta', rotulo: 'Alerta (não gera solicitação)', ajuda: 'Apenas informativo: nunca cria solicitação.' },
]

const nome = ref('')
const tipo = ref<Fluxo['tipo']>('ControleAlcada')
const mensagem = ref('')
const ordem = ref(0)
const ativo = ref(true)
/** Grupos de condições: E dentro do grupo, OU entre os grupos. */
const grupos = ref<FiltroForm[][]>([])
const niveis = ref<NivelForm[]>([])
const salvando = ref(false)
const erro = ref('')
const tentou = ref(false)

const titulo = computed(() => (props.fluxo?.id ? 'Editar fluxo de aprovação' : 'Novo fluxo de aprovação'))

watch(() => props.modelValue, (aberto) => {
  if (!aberto) return
  erro.value = ''
  tentou.value = false
  const f = props.fluxo
  nome.value = f?.nome ?? ''
  tipo.value = f?.tipo ?? 'ControleAlcada'
  mensagem.value = f?.mensagem ?? ''
  ordem.value = f?.ordem ?? 0
  ativo.value = f?.ativo ?? true
  const porGrupo = new Map<number, FiltroForm[]>()
  for (const x of f?.filtros ?? []) {
    const lista = porGrupo.get(x.grupo) ?? []
    lista.push({ campo: x.campoReferencia, operador: x.operador, valor: x.valor ?? '' })
    porGrupo.set(x.grupo, lista)
  }
  grupos.value = [...porGrupo.keys()].sort((a, b) => a - b).map((k) => porGrupo.get(k)!)
  niveis.value = (f?.niveis ?? []).map((n) => ({
    descricao: n.descricao ?? '',
    aprovadores: n.aprovadores.map((a) => (a.usuarioId
      ? { tipo: 'usuario' as const, usuarioId: a.usuarioId, perfil: 'Gerente' }
      : { tipo: 'perfil' as const, usuarioId: null, perfil: a.perfil ?? 'Gerente' })),
  }))
  if (!f) niveis.value = [{ descricao: '', aprovadores: [{ tipo: 'usuario', usuarioId: null, perfil: 'Gerente' }] }]
}, { immediate: true })

function campoDe(chave: string): CampoFiltro | undefined { return props.campos.find((c) => c.campo === chave) }
function operadoresDe(chave: string): string[] { return campoDe(chave)?.operadores ?? Object.keys(OPERADORES) }
function precisaValor(op: string) { return op !== 'Vazio' && op !== 'NaoVazio' }

// ---- grupos e filtros ----
function novoFiltro(): FiltroForm {
  const c = props.campos[0]
  return { campo: c?.campo ?? 'DescontoPercentual', operador: c?.operadores[2] ?? 'Maior', valor: '' }
}
function addGrupo() { grupos.value.push([novoFiltro()]) }
function addFiltro(g: number) { grupos.value[g]?.push(novoFiltro()) }
function removerFiltro(g: number, i: number) {
  const lista = grupos.value[g]
  if (!lista) return
  lista.splice(i, 1)
  if (lista.length === 0) grupos.value.splice(g, 1)
}
function mudouCampo(f: FiltroForm) {
  if (!operadoresDe(f.campo).includes(f.operador)) f.operador = operadoresDe(f.campo)[0] ?? 'Igual'
  f.valor = ''
}

// ---- níveis e aprovadores ----
function addNivel() { niveis.value.push({ descricao: '', aprovadores: [{ tipo: 'usuario', usuarioId: null, perfil: 'Gerente' }] }) }
function removerNivel(i: number) { niveis.value.splice(i, 1) }
function moverNivel(i: number, d: -1 | 1) {
  const j = i + d
  const a = niveis.value[i]
  const b = niveis.value[j]
  if (!a || !b) return
  niveis.value[i] = b
  niveis.value[j] = a
}
function addAprovador(n: NivelForm) { n.aprovadores.push({ tipo: 'usuario', usuarioId: null, perfil: 'Gerente' }) }
function removerAprovador(n: NivelForm, i: number) { n.aprovadores.splice(i, 1) }

// ---- validação e envio ----
const problemas = computed(() => {
  const p: string[] = []
  if (!nome.value.trim()) p.push('Informe o nome do fluxo.')
  if (tipo.value !== 'Alerta' && niveis.value.length === 0) p.push('Inclua ao menos um nível de aprovação.')
  niveis.value.forEach((n, i) => {
    if (n.aprovadores.length === 0) p.push(`Nível ${i + 1}: inclua ao menos um aprovador.`)
    else if (n.aprovadores.some((a) => a.tipo === 'usuario' && !a.usuarioId)) p.push(`Nível ${i + 1}: escolha o usuário de cada aprovador.`)
  })
  grupos.value.forEach((g, gi) => g.forEach((f, fi) => {
    if (precisaValor(f.operador) && !f.valor.trim()) p.push(`Condição ${gi + 1}.${fi + 1}: informe o valor.`)
  }))
  return p
})

async function salvar() {
  tentou.value = true
  erro.value = ''
  if (problemas.value.length) return
  const corpo: Fluxo = {
    id: props.fluxo?.id ?? null,
    nome: nome.value.trim(),
    tipo: tipo.value,
    mensagem: mensagem.value.trim() || null,
    ordem: Number(ordem.value) || 0,
    ativo: ativo.value,
    filtros: grupos.value.flatMap((g, gi) => g.map((f) => ({
      grupo: gi, campoReferencia: f.campo, operador: f.operador, valor: precisaValor(f.operador) ? f.valor.trim() : null,
    }))),
    niveis: niveis.value.map((n) => ({
      descricao: n.descricao.trim() || null,
      aprovadores: n.aprovadores.map((a) => (a.tipo === 'usuario'
        ? { usuarioId: a.usuarioId, perfil: null }
        : { usuarioId: null, perfil: a.perfil })),
    })),
  }
  salvando.value = true
  try {
    if (props.fluxo?.id) await apiAprov('PUT', `/api/fluxos-aprovacao/${props.fluxo.id}`, corpo)
    else await apiAprov('POST', '/api/fluxos-aprovacao', corpo)
    toast.success('Fluxo salvo.')
    emit('salvo')
    emit('update:modelValue', false)
  } catch (e) {
    erro.value = e instanceof Error ? e.message : 'Não foi possível salvar o fluxo.'
    toast.error(erro.value)
  } finally {
    salvando.value = false
  }
}
</script>

<template>
  <UiModal :model-value="modelValue" :titulo="titulo" largura="780px" :fechar-ao-clicar-fora="false" @update:model-value="emit('update:modelValue', $event)">
    <form class="adm-form" @submit.prevent="salvar">
      <div class="adm-form-grade">
        <div class="adm-campo largo">
          <label for="fa-nome">Nome <span class="adm-obrig">*</span></label>
          <input id="fa-nome" v-model="nome" class="adm-input" maxlength="300" placeholder="Ex.: Desconto acima de 10%" />
        </div>
        <div class="adm-campo">
          <label for="fa-tipo">Tipo</label>
          <select id="fa-tipo" v-model="tipo" class="adm-select">
            <option v-for="t in TIPOS" :key="t.valor" :value="t.valor">{{ t.rotulo }}</option>
          </select>
          <span class="adm-ajuda">{{ TIPOS.find((t) => t.valor === tipo)?.ajuda }}</span>
        </div>
        <div class="adm-campo">
          <label for="fa-ordem">Ordem de verificação</label>
          <input id="fa-ordem" v-model.number="ordem" type="number" min="0" max="100000" class="adm-input" />
          <span class="adm-ajuda">Da menor para a maior; o primeiro fluxo que se enquadrar vale.</span>
        </div>
        <div class="adm-campo largo">
          <label for="fa-msg">Mensagem ao solicitante (opcional)</label>
          <input id="fa-msg" v-model="mensagem" class="adm-input" maxlength="500" />
        </div>
        <label class="check largo"><input v-model="ativo" type="checkbox" /> Fluxo ativo</label>
      </div>

      <section class="bloco" aria-label="Condições">
        <header class="bloco-cab">
          <h4>Condições</h4>
          <button type="button" class="adm-btn" @click="addGrupo"><i class="mdi mdi-plus"></i> Adicionar grupo</button>
        </header>
        <p class="adm-ajuda">Dentro de um grupo todas as condições valem (E). Basta um grupo se cumprir (OU). Sem condições, o fluxo vale para toda proposta.</p>
        <p v-if="!grupos.length" class="vazio">Sem condições: o fluxo se aplica a todas as propostas.</p>
        <div v-for="(g, gi) in grupos" :key="gi" class="grupo">
          <div class="grupo-tit">{{ gi === 0 ? 'Grupo 1' : `OU grupo ${gi + 1}` }}</div>
          <div v-for="(f, fi) in g" :key="fi" class="filtro">
            <select v-model="f.campo" class="adm-select" :aria-label="`Campo da condição ${gi + 1}.${fi + 1}`" @change="mudouCampo(f)">
              <option v-for="c in campos" :key="c.campo" :value="c.campo">{{ c.rotulo }}</option>
            </select>
            <select v-model="f.operador" class="adm-select" :aria-label="`Operador da condição ${gi + 1}.${fi + 1}`">
              <option v-for="o in operadoresDe(f.campo)" :key="o" :value="o">{{ OPERADORES[o] ?? o }}</option>
            </select>
            <template v-if="precisaValor(f.operador)">
              <select v-if="campoDe(f.campo)?.tipo === 'usuario'" v-model="f.valor" class="adm-select" :aria-label="`Valor da condição ${gi + 1}.${fi + 1}`">
                <option value="">Escolha o usuário</option>
                <option v-for="u in usuarios" :key="u.id" :value="String(u.id)">{{ u.nome }}</option>
              </select>
              <select v-else-if="campoDe(f.campo)?.tipo === 'moeda'" v-model="f.valor" class="adm-select" :aria-label="`Valor da condição ${gi + 1}.${fi + 1}`">
                <option value="">Escolha a moeda</option>
                <option v-for="m in moedas" :key="m.id" :value="String(m.id)">{{ m.nome }}</option>
              </select>
              <input
                v-else v-model="f.valor" class="adm-input" :aria-label="`Valor da condição ${gi + 1}.${fi + 1}`"
                :inputmode="campoDe(f.campo)?.tipo === 'numero' ? 'decimal' : 'numeric'"
                :placeholder="campoDe(f.campo)?.tipo === 'numero' ? 'Ex.: 10' : 'ID (vários separados por vírgula)'"
              />
            </template>
            <span v-else class="sem-valor"></span>
            <button type="button" class="adm-icone perigo" aria-label="Remover condição" @click="removerFiltro(gi, fi)"><i class="mdi mdi-close"></i></button>
          </div>
          <button type="button" class="link" @click="addFiltro(gi)"><i class="mdi mdi-plus"></i> E outra condição</button>
        </div>
      </section>

      <section class="bloco" aria-label="Níveis de aprovação">
        <header class="bloco-cab">
          <h4>Níveis de aprovação</h4>
          <button type="button" class="adm-btn" @click="addNivel"><i class="mdi mdi-plus"></i> Adicionar nível</button>
        </header>
        <p class="adm-ajuda">Os níveis são aprovados em sequência, do 1º ao último. Em cada nível, qualquer um dos aprovadores listados pode decidir.</p>
        <p v-if="!niveis.length" class="vazio">Nenhum nível.</p>
        <div v-for="(n, ni) in niveis" :key="ni" class="nivel">
          <div class="nivel-cab">
            <span class="nivel-num">{{ ni + 1 }}º</span>
            <input v-model="n.descricao" class="adm-input" maxlength="200" :aria-label="`Descrição do nível ${ni + 1}`" placeholder="Descrição (opcional)" />
            <button type="button" class="adm-icone" :disabled="ni === 0" :aria-label="`Subir nível ${ni + 1}`" @click="moverNivel(ni, -1)"><i class="mdi mdi-arrow-up"></i></button>
            <button type="button" class="adm-icone" :disabled="ni === niveis.length - 1" :aria-label="`Descer nível ${ni + 1}`" @click="moverNivel(ni, 1)"><i class="mdi mdi-arrow-down"></i></button>
            <button type="button" class="adm-icone perigo" :aria-label="`Remover nível ${ni + 1}`" @click="removerNivel(ni)"><i class="mdi mdi-delete-outline"></i></button>
          </div>
          <div v-for="(a, ai) in n.aprovadores" :key="ai" class="aprovador">
            <select v-model="a.tipo" class="adm-select tipo" :aria-label="`Tipo do aprovador ${ai + 1} do nível ${ni + 1}`">
              <option value="usuario">Usuário</option>
              <option value="perfil">Qualquer usuário do perfil</option>
            </select>
            <select v-if="a.tipo === 'usuario'" v-model="a.usuarioId" class="adm-select" :aria-label="`Usuário aprovador ${ai + 1} do nível ${ni + 1}`">
              <option :value="null">Escolha o usuário</option>
              <option v-for="u in usuarios" :key="u.id" :value="u.id">{{ u.nome }}</option>
            </select>
            <select v-else v-model="a.perfil" class="adm-select" :aria-label="`Perfil aprovador ${ai + 1} do nível ${ni + 1}`">
              <option v-for="p in PERFIS" :key="p" :value="p">{{ p }}</option>
            </select>
            <button type="button" class="adm-icone perigo" aria-label="Remover aprovador" @click="removerAprovador(n, ai)"><i class="mdi mdi-close"></i></button>
          </div>
          <button type="button" class="link" @click="addAprovador(n)"><i class="mdi mdi-plus"></i> Outro aprovador</button>
        </div>
      </section>

      <div v-if="tentou && problemas.length" class="adm-aviso" role="alert">
        <ul><li v-for="p in problemas" :key="p">{{ p }}</li></ul>
      </div>
      <p v-if="erro" class="adm-invalido" role="alert">{{ erro }}</p>
    </form>
    <template #rodape>
      <button type="button" class="adm-btn" :disabled="salvando" @click="emit('update:modelValue', false)">Cancelar</button>
      <button type="button" class="adm-btn primario" :disabled="salvando" @click="salvar">{{ salvando ? 'Salvando...' : 'Salvar fluxo' }}</button>
    </template>
  </UiModal>
</template>

<style scoped>
.check { display: flex; align-items: center; gap: 8px; font-size: 13px; color: var(--text-secondary); }
.bloco { border: 1px solid var(--border); border-radius: 10px; padding: 12px; display: flex; flex-direction: column; gap: 10px; }
.bloco-cab { display: flex; align-items: center; justify-content: space-between; gap: 8px; }
.bloco-cab h4 { margin: 0; font-size: 13px; font-weight: 600; color: var(--text-primary); }
.vazio { margin: 0; font-size: 12px; color: var(--text-muted); }
.grupo { background: var(--bg-elevated); border-radius: 8px; padding: 10px; display: flex; flex-direction: column; gap: 8px; }
.grupo-tit { font-size: 11px; font-weight: 700; color: var(--accent); text-transform: uppercase; letter-spacing: .05em; }
.filtro { display: grid; grid-template-columns: minmax(150px, 1.2fr) minmax(140px, 1fr) minmax(130px, 1fr) 30px; gap: 8px; align-items: center; }
.sem-valor { display: block; }
.nivel { background: var(--bg-elevated); border-radius: 8px; padding: 10px; display: flex; flex-direction: column; gap: 8px; }
.nivel-cab { display: flex; align-items: center; gap: 6px; }
.nivel-num { font-weight: 700; color: var(--accent); min-width: 28px; }
.aprovador { display: grid; grid-template-columns: 200px 1fr 30px; gap: 8px; align-items: center; margin-left: 34px; }
.link { align-self: flex-start; background: none; border: 0; padding: 0; color: var(--text-secondary); font-size: 12px; cursor: pointer; font-family: inherit; }
.link:hover { color: var(--accent); }
.link:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.adm-aviso ul { margin: 0; padding-left: 18px; }
@media (max-width: 640px) {
  .filtro { grid-template-columns: 1fr 30px; }
  .filtro .adm-select, .filtro .adm-input { grid-column: 1 / 2; }
  .aprovador { grid-template-columns: 1fr 30px; margin-left: 0; }
}
</style>
