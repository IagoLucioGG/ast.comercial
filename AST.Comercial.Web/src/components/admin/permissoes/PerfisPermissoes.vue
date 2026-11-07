<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import UiTabs from '@/components/ui/UiTabs.vue'
import UiBadge from '@/components/ui/UiBadge.vue'
import { useToast } from '@/composables/useToast'
import { usePermissoes } from '@/composables/usePermissoes'
import '../comum/admin-comum.css'
import {
  PERFIS, ESCOPOS, carregarCatalogo, carregarMatriz, salvarMatriz,
  type EntidadeCatalogo, type LinhaPermissao, type PerfilChave, type EscopoChave,
} from './permissoesApi'

type CampoAcao = 'podeVer' | 'podeCriar' | 'podeEditar' | 'podeExcluir'

const toast = useToast()
const permissoesProprias = usePermissoes()

const carregando = ref(true)
const erro = ref('')
const salvando = ref(false)
const catalogo = ref<EntidadeCatalogo[]>([])
const original = ref<Record<string, LinhaPermissao>>({})
const atual = ref<Record<string, LinhaPermissao>>({})
const perfilAtivo = ref<PerfilChave>('Vendedor')

const chaveLinha = (perfil: string, entidade: string) => `${perfil}|${entidade}`

const abas = computed(() => PERFIS.map((p) => ({
  chave: p.chave,
  rotulo: p.rotulo,
  icone: p.icone,
  contador: contarAlteracoes(p.chave) || undefined,
})))

const perfilInfo = computed(() => PERFIS.find((p) => p.chave === perfilAtivo.value)!)
const somenteLeitura = computed(() => perfilAtivo.value === 'Administrador')

const grupos = computed(() => {
  const rotulos: Record<string, string> = { Comercial: 'Comercial', Administracao: 'Administração' }
  return ['Comercial', 'Administracao']
    .map((g) => ({
      chave: g,
      rotulo: rotulos[g] ?? g,
      entidades: catalogo.value.filter((e) => e.grupo === g),
    }))
    .filter((g) => g.entidades.length > 0)
})

function igual(a: LinhaPermissao | undefined, b: LinhaPermissao | undefined): boolean {
  if (!a || !b) return a === b
  return a.podeVer === b.podeVer && a.podeCriar === b.podeCriar && a.podeEditar === b.podeEditar
    && a.podeExcluir === b.podeExcluir && a.escopo === b.escopo
}

function mudou(perfil: string, entidade: string): boolean {
  const k = chaveLinha(perfil, entidade)
  return !igual(original.value[k], atual.value[k])
}

function contarAlteracoes(perfil?: string): number {
  return Object.keys(atual.value).filter((k) => (!perfil || k.startsWith(`${perfil}|`)) && !igual(original.value[k], atual.value[k])).length
}

const totalAlteracoes = computed(() => contarAlteracoes())

function linha(entidade: string): LinhaPermissao {
  return atual.value[chaveLinha(perfilAtivo.value, entidade)] as LinhaPermissao
}

function alternar(entidade: string, campo: CampoAcao, valor: boolean) {
  if (somenteLeitura.value) return
  const l = linha(entidade)
  if (!l) return
  l[campo] = valor
  // Ver é pré-requisito das demais ações: tirar Ver zera o resto; marcar qualquer ação liga Ver.
  if (campo === 'podeVer' && !valor) {
    l.podeCriar = false
    l.podeEditar = false
    l.podeExcluir = false
  } else if (campo !== 'podeVer' && valor) {
    l.podeVer = true
  }
}

function definirEscopo(entidade: string, escopo: EscopoChave) {
  if (somenteLeitura.value) return
  const l = linha(entidade)
  if (l) l.escopo = escopo
}

function indexar(linhas: LinhaPermissao[]): Record<string, LinhaPermissao> {
  const mapa: Record<string, LinhaPermissao> = {}
  for (const l of linhas) mapa[chaveLinha(l.perfil, l.entidade)] = { ...l }
  return mapa
}

function aplicarDoServidor(linhas: LinhaPermissao[]) {
  original.value = indexar(linhas)
  atual.value = indexar(linhas)
}

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const [cat, matriz] = await Promise.all([carregarCatalogo(), carregarMatriz()])
    catalogo.value = cat
    aplicarDoServidor(matriz)
  } catch (e) {
    erro.value = e instanceof Error ? e.message : 'Não foi possível carregar as permissões.'
  } finally {
    carregando.value = false
  }
}

function desfazer() {
  atual.value = indexar(Object.values(original.value))
}

async function salvar() {
  const alteradas = Object.keys(atual.value)
    .filter((k) => !igual(original.value[k], atual.value[k]))
    .map((k) => atual.value[k] as LinhaPermissao)
  if (alteradas.length === 0) return
  salvando.value = true
  try {
    const nova = await salvarMatriz(alteradas)
    aplicarDoServidor(nova)
    toast.success(`Permissões salvas (${alteradas.length} ${alteradas.length === 1 ? 'alteração' : 'alterações'}).`)
    void permissoesProprias.carregar(true)
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível salvar as permissões.')
  } finally {
    salvando.value = false
  }
}

onMounted(carregar)
</script>

<template>
  <div class="perm-tela adm-tela">
    <div v-if="carregando" class="adm-vazio" role="status">Carregando permissões…</div>

    <div v-else-if="erro" class="adm-erro" role="alert">
      <p>{{ erro }}</p>
      <button type="button" class="adm-btn" @click="carregar">Tentar de novo</button>
    </div>

    <template v-else>
      <div class="adm-aviso">
        <strong>Como funciona:</strong> o servidor passa a aplicar as mudanças assim que você salva.
        Quem já está com o sistema aberto vê os menus e botões novos ao recarregar a página ou entrar de novo.
        O perfil Administrador sempre tem acesso total.
      </div>

      <UiTabs :model-value="perfilAtivo" :abas="abas" @update:model-value="perfilAtivo = $event as PerfilChave" />

      <p class="perm-descricao">
        <i class="mdi" :class="perfilInfo.icone" aria-hidden="true"></i>
        {{ perfilInfo.descricao }}
        <UiBadge v-if="somenteLeitura" tom="destaque">Somente leitura</UiBadge>
      </p>

      <div class="adm-tabela-wrap">
        <table class="adm-tabela perm-tabela">
          <caption class="sr-only">Permissões do perfil {{ perfilInfo.rotulo }} por entidade</caption>
          <thead>
            <tr>
              <th scope="col">Entidade</th>
              <th scope="col" class="perm-col">Ver</th>
              <th scope="col" class="perm-col">Criar</th>
              <th scope="col" class="perm-col">Editar</th>
              <th scope="col" class="perm-col">Excluir</th>
              <th scope="col" class="perm-escopo-col">Quais registros</th>
            </tr>
          </thead>
          <tbody v-for="g in grupos" :key="g.chave">
            <tr class="perm-grupo">
              <th scope="rowgroup" colspan="6">{{ g.rotulo }}</th>
            </tr>
            <tr v-for="e in g.entidades" :key="e.chave" :class="{ alterada: mudou(perfilAtivo, e.chave) }">
              <th scope="row" class="adm-principal perm-nome">
                <span v-if="mudou(perfilAtivo, e.chave)" class="perm-marca" title="Alterado e ainda não salvo"></span>
                {{ e.rotulo }}
              </th>
              <td v-for="c in ([['podeVer', 'Ver'], ['podeCriar', 'Criar'], ['podeEditar', 'Editar'], ['podeExcluir', 'Excluir']] as const)"
                  :key="c[0]" class="perm-col">
                <input
                  type="checkbox"
                  :checked="linha(e.chave)?.[c[0]] ?? false"
                  :disabled="somenteLeitura"
                  :aria-label="`${e.rotulo}: ${c[1]}`"
                  @change="alternar(e.chave, c[0], ($event.target as HTMLInputElement).checked)"
                />
              </td>
              <td class="perm-escopo-col">
                <select
                  v-if="e.suportaEscopo"
                  class="adm-select perm-escopo"
                  :value="linha(e.chave)?.escopo ?? 'Todos'"
                  :disabled="somenteLeitura || !linha(e.chave)?.podeVer"
                  :aria-label="`${e.rotulo}: quais registros o perfil enxerga`"
                  @change="definirEscopo(e.chave, ($event.target as HTMLSelectElement).value as EscopoChave)"
                >
                  <option v-for="s in ESCOPOS" :key="s.chave" :value="s.chave">{{ s.rotulo }}</option>
                </select>
                <span v-else class="perm-sem-escopo" title="O escopo só se aplica a Negócios e Atividades">—</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="perm-rodape">
        <span class="perm-contagem" role="status">
          {{ totalAlteracoes === 0 ? 'Nenhuma alteração pendente.' : `${totalAlteracoes} ${totalAlteracoes === 1 ? 'alteração não salva' : 'alterações não salvas'}.` }}
        </span>
        <button type="button" class="adm-btn" :disabled="totalAlteracoes === 0 || salvando" @click="desfazer">
          <i class="mdi mdi-undo" aria-hidden="true"></i> Desfazer
        </button>
        <button type="button" class="adm-btn primario" :disabled="totalAlteracoes === 0 || salvando" @click="salvar">
          <i class="mdi mdi-content-save-outline" aria-hidden="true"></i>
          {{ salvando ? 'Salvando…' : 'Salvar permissões' }}
        </button>
      </div>
    </template>
  </div>
</template>

<style scoped>
.perm-tela { max-width: 980px; }
.perm-descricao { display: flex; align-items: center; gap: 8px; font-size: 13px; color: var(--text-secondary); }
.perm-descricao .mdi { font-size: 18px; color: var(--accent); }
.perm-tabela { table-layout: auto; }
.perm-col { width: 76px; text-align: center; }
.perm-escopo-col { width: 220px; }
.perm-grupo th { background: var(--bg-secondary); color: var(--text-muted); font-size: 11px; text-transform: uppercase; letter-spacing: .5px; text-align: left; padding: 8px 14px; }
.perm-nome { text-align: left; font-weight: 500; }
.perm-marca { display: inline-block; width: 6px; height: 6px; border-radius: 50%; background: var(--accent); margin-right: 8px; vertical-align: middle; }
tr.alterada td, tr.alterada th.perm-nome { background: rgba(6, 182, 212, .06); }
input[type='checkbox'] { width: 16px; height: 16px; accent-color: var(--accent); cursor: pointer; }
input[type='checkbox']:disabled { cursor: not-allowed; }
input[type='checkbox']:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }
.perm-escopo { padding: 5px 8px; font-size: 12px; }
.perm-sem-escopo { color: var(--text-muted); }
.perm-rodape { display: flex; align-items: center; gap: 10px; justify-content: flex-end; flex-wrap: wrap; }
.perm-contagem { margin-right: auto; font-size: 12px; color: var(--text-muted); }
.sr-only { position: absolute; width: 1px; height: 1px; overflow: hidden; clip: rect(0 0 0 0); white-space: nowrap; }
</style>
