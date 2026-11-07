<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import UiSlideOver from '@/components/ui/UiSlideOver.vue'
import { ApiError, odataRequest } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { METRICAS, infoMetrica, type Metrica, type ProgressoMeta } from './tipos'

interface Opcao { Id: number; Nome: string }
interface MoedaOpcao { Id: number; Codigo: string; Simbolo?: string | null }

const props = defineProps<{
  modelValue: boolean
  meta: ProgressoMeta | null
  usuarios: Opcao[]
  equipes: Opcao[]
  funis: Opcao[]
  moedas: MoedaOpcao[]
  inicioPadrao: string
  fimPadrao: string
}>()
const emit = defineEmits<{ 'update:modelValue': [v: boolean]; salvo: [] }>()
const toast = useToast()

type Responsavel = 'usuario' | 'equipe' | 'empresa'
const nome = ref('')
const metrica = ref<Metrica>('ValorGanho')
const alvo = ref<number | null>(null)
const moedaId = ref<number | null>(null)
const responsavel = ref<Responsavel>('usuario')
const usuarioId = ref<number | null>(null)
const equipeId = ref<number | null>(null)
const funilId = ref<number | null>(null)
const inicio = ref('')
const fim = ref('')
const salvando = ref(false)
const erro = ref('')

const monetaria = computed(() => infoMetrica(metrica.value).monetaria)
const titulo = computed(() => (props.meta ? 'Editar meta' : 'Nova meta'))

const dia = (iso: string) => iso.slice(0, 10)

watch(() => props.modelValue, (aberto) => {
  if (!aberto) return
  erro.value = ''
  const m = props.meta
  nome.value = m?.nome ?? ''
  metrica.value = m?.metrica ?? 'ValorGanho'
  alvo.value = m?.alvo ?? null
  moedaId.value = m?.moedaId ?? props.moedas[0]?.Id ?? null
  responsavel.value = m ? m.escopo : 'usuario'
  usuarioId.value = m?.usuarioId ?? null
  equipeId.value = m?.equipeId ?? null
  funilId.value = m?.funilId ?? null
  inicio.value = dia(m?.inicio ?? props.inicioPadrao)
  fim.value = dia(m?.fim ?? props.fimPadrao)
})

function fechar() { emit('update:modelValue', false) }

async function salvar() {
  erro.value = ''
  if (!nome.value.trim()) { erro.value = 'Informe o nome da meta.'; return }
  if (!alvo.value || alvo.value <= 0) { erro.value = 'O alvo deve ser maior que zero.'; return }
  if (monetaria.value && !moedaId.value) { erro.value = 'Escolha a moeda.'; return }
  if (responsavel.value === 'usuario' && !usuarioId.value) { erro.value = 'Escolha o usuário.'; return }
  if (responsavel.value === 'equipe' && !equipeId.value) { erro.value = 'Escolha a equipe.'; return }
  if (!inicio.value || !fim.value) { erro.value = 'Informe o período.'; return }
  if (fim.value < inicio.value) { erro.value = 'O fim deve ser posterior ao início.'; return }
  const corpo = {
    Nome: nome.value.trim(),
    Metrica: metrica.value,
    Alvo: alvo.value,
    MoedaId: monetaria.value ? moedaId.value : null,
    UsuarioId: responsavel.value === 'usuario' ? usuarioId.value : null,
    EquipeId: responsavel.value === 'equipe' ? equipeId.value : null,
    FunilId: funilId.value,
    Inicio: `${inicio.value}T00:00:00Z`,
    Fim: `${fim.value}T23:59:59.999Z`,
  }
  salvando.value = true
  try {
    if (props.meta) await odataRequest('PATCH', `Metas(${props.meta.id})`, corpo)
    else await odataRequest('POST', 'Metas', corpo)
    toast.success(props.meta ? 'Meta atualizada.' : 'Meta criada.')
    emit('salvo')
    fechar()
  } catch (e) {
    const msg = e instanceof ApiError ? e.message : 'Não foi possível salvar a meta.'
    erro.value = msg
    toast.error(msg)
  } finally {
    salvando.value = false
  }
}
</script>

<template>
  <UiSlideOver :model-value="modelValue" :titulo="titulo" largura="460px" @update:model-value="emit('update:modelValue', $event)">
    <form id="form-meta" class="fm" @submit.prevent="salvar">
      <label>Nome
        <input v-model="nome" type="text" maxlength="200" required placeholder="Ex.: Receita do trimestre" />
      </label>
      <label>Métrica
        <select v-model="metrica">
          <option v-for="m in METRICAS" :key="m.valor" :value="m.valor">{{ m.rotulo }}</option>
        </select>
      </label>
      <div class="linha">
        <label>Alvo
          <input v-model.number="alvo" type="number" min="0.01" step="any" required />
        </label>
        <label v-if="monetaria">Moeda
          <select v-model="moedaId">
            <option v-for="m in moedas" :key="m.Id" :value="m.Id">{{ m.Codigo }} {{ m.Simbolo ? `(${m.Simbolo})` : '' }}</option>
          </select>
        </label>
      </div>
      <fieldset>
        <legend>Responsável</legend>
        <div class="opcoes">
          <label class="radio"><input v-model="responsavel" type="radio" value="usuario" /> Usuário</label>
          <label class="radio"><input v-model="responsavel" type="radio" value="equipe" /> Equipe</label>
          <label class="radio"><input v-model="responsavel" type="radio" value="empresa" /> Empresa inteira</label>
        </div>
        <select v-if="responsavel === 'usuario'" v-model="usuarioId" aria-label="Usuário">
          <option :value="null" disabled>Selecione o usuário</option>
          <option v-for="u in usuarios" :key="u.Id" :value="u.Id">{{ u.Nome }}</option>
        </select>
        <select v-else-if="responsavel === 'equipe'" v-model="equipeId" aria-label="Equipe">
          <option :value="null" disabled>Selecione a equipe</option>
          <option v-for="e in equipes" :key="e.Id" :value="e.Id">{{ e.Nome }}</option>
        </select>
      </fieldset>
      <label>Funil (opcional)
        <select v-model="funilId">
          <option :value="null">Todos os funis</option>
          <option v-for="f in funis" :key="f.Id" :value="f.Id">{{ f.Nome }}</option>
        </select>
      </label>
      <div class="linha">
        <label>Início <input v-model="inicio" type="date" required /></label>
        <label>Fim <input v-model="fim" type="date" required /></label>
      </div>
      <p v-if="erro" class="erro" role="alert">{{ erro }}</p>
    </form>
    <template #rodape>
      <button type="button" class="b sec" @click="fechar">Cancelar</button>
      <button type="submit" form="form-meta" class="b pri" :disabled="salvando">{{ salvando ? 'Salvando…' : 'Salvar' }}</button>
    </template>
  </UiSlideOver>
</template>

<style scoped>
.fm { display: flex; flex-direction: column; gap: 14px; }
label { display: flex; flex-direction: column; gap: 5px; font-size: 12px; color: var(--text-secondary); flex: 1; }
input, select { background: var(--bg-secondary); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); padding: 8px 10px; font-size: 13px; font-family: inherit; }
input:focus, select:focus { outline: 2px solid var(--accent); outline-offset: -1px; }
.linha { display: flex; gap: 12px; }
fieldset { border: 1px solid var(--border); border-radius: 8px; padding: 10px 12px; display: flex; flex-direction: column; gap: 10px; }
legend { font-size: 12px; color: var(--text-secondary); padding: 0 4px; }
.opcoes { display: flex; gap: 14px; flex-wrap: wrap; }
.radio { flex-direction: row; align-items: center; gap: 6px; color: var(--text-primary); flex: none; }
.erro { color: var(--danger); font-size: 12px; margin: 0; }
.b { border-radius: 6px; padding: 8px 16px; font-size: 13px; cursor: pointer; border: 1px solid var(--border); font-family: inherit; }
.b.sec { background: none; color: var(--text-secondary); }
.b.pri { background: var(--accent); color: #04121a; border-color: var(--accent); font-weight: 600; }
.b.pri:hover { background: var(--accent-hover); }
.b:disabled { opacity: .6; cursor: default; }
</style>
