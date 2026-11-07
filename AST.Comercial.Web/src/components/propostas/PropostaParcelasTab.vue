<script setup lang="ts">
import { computed, ref } from 'vue'
import { odataRequest } from '@/services/api'
import { useToast } from '@/composables/useToast'
import { paraInputData, deInputData, type ParcelaProposta } from '@/composables/usePropostaApi'
import { arredondar, formatarMoeda } from '@/composables/usePropostaTotais'

const props = defineProps<{ propostaId: number; moeda: string; total: number; somenteLeitura?: boolean }>()
const parcelas = defineModel<ParcelaProposta[]>({ required: true })
const emit = defineEmits<{ mudou: [] }>()

const toast = useToast()
const adicionando = ref(false)

const soma = computed(() => arredondar(parcelas.value.reduce((s, p) => s + Number(p.Valor ?? 0), 0)))
const diferenca = computed(() => arredondar(props.total - soma.value))

function corpo(p: ParcelaProposta) {
  return {
    Numero: p.Numero,
    DataVencimento: p.DataVencimento ?? null,
    Valor: arredondar(Number(p.Valor ?? 0)),
    Descricao: p.Descricao ?? null,
  }
}

async function adicionar() {
  adicionando.value = true
  try {
    const numero = parcelas.value.reduce((m, p) => Math.max(m, p.Numero), 0) + 1
    // sugere o saldo restante como valor da nova parcela
    const sugestao = Math.max(0, diferenca.value)
    const criada = await odataRequest<ParcelaProposta>('POST', 'Propostas@Parcelas', {
      PropostaId: props.propostaId,
      Numero: numero,
      Valor: sugestao,
    })
    parcelas.value = [...parcelas.value, { ...criada }]
    emit('mudou')
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível adicionar a parcela.')
  } finally {
    adicionando.value = false
  }
}

async function salvar(p: ParcelaProposta) {
  try {
    await odataRequest('PATCH', `Propostas@Parcelas(${p.Id})`, corpo(p))
    emit('mudou')
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível salvar a parcela.')
  }
}

async function remover(p: ParcelaProposta) {
  try {
    await odataRequest('DELETE', `Propostas@Parcelas(${p.Id})`)
    parcelas.value = parcelas.value.filter((x) => x.Id !== p.Id)
    emit('mudou')
  } catch (e) {
    toast.error(e instanceof Error ? e.message : 'Não foi possível remover a parcela.')
  }
}

function aoMudarData(p: ParcelaProposta, ev: Event) {
  p.DataVencimento = deInputData((ev.target as HTMLInputElement).value)
  salvar(p)
}
function aoMudarValor(p: ParcelaProposta, ev: Event) {
  const v = Number((ev.target as HTMLInputElement).value)
  p.Valor = Number.isFinite(v) && v >= 0 ? v : 0
}
</script>

<template>
  <div class="parcelas">
    <div class="tabela-wrap">
      <table class="tabela">
        <thead>
          <tr>
            <th class="c-n">Nº</th>
            <th>Vencimento</th>
            <th class="c-val">Valor</th>
            <th>Descrição</th>
            <th class="c-acao"></th>
          </tr>
        </thead>
        <tbody>
          <tr v-if="!parcelas.length"><td colspan="5" class="vazio">Nenhuma parcela cadastrada.</td></tr>
          <tr v-for="p in parcelas" :key="p.Id">
            <td class="c-n">{{ p.Numero }}</td>
            <td><input type="date" aria-label="Vencimento" :value="paraInputData(p.DataVencimento)" :disabled="somenteLeitura" @change="aoMudarData(p, $event)" /></td>
            <td class="c-val"><input type="number" min="0" step="0.01" aria-label="Valor da parcela" :value="p.Valor ?? 0" :disabled="somenteLeitura" @input="aoMudarValor(p, $event)" @change="salvar(p)" /></td>
            <td><input type="text" maxlength="120" aria-label="Descrição da parcela" v-model="p.Descricao" :disabled="somenteLeitura" @change="salvar(p)" /></td>
            <td class="c-acao">
              <button v-if="!somenteLeitura" type="button" class="icone" aria-label="Remover parcela" @click="remover(p)"><i class="mdi mdi-delete-outline"></i></button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
    <div class="rodape">
      <button v-if="!somenteLeitura" type="button" class="btn" :disabled="adicionando" @click="adicionar"><i class="mdi mdi-plus"></i> Adicionar parcela</button>
      <span class="soma" :class="{ alerta: parcelas.length > 0 && diferenca !== 0 }">
        Soma das parcelas: {{ formatarMoeda(soma, moeda) }}
        <template v-if="parcelas.length && diferenca !== 0"> · diferença de {{ formatarMoeda(diferenca, moeda) }} em relação ao total</template>
      </span>
    </div>
  </div>
</template>

<style scoped>
.parcelas { display: flex; flex-direction: column; gap: 12px; }
.tabela-wrap { overflow-x: auto; border: 1px solid var(--border); border-radius: 8px; }
.tabela { width: 100%; border-collapse: collapse; min-width: 560px; }
th { text-align: left; font-size: 11px; text-transform: uppercase; letter-spacing: .04em; color: var(--text-muted); padding: 8px 10px; background: var(--bg-secondary); }
td { padding: 6px 10px; border-top: 1px solid var(--border); }
.c-n { width: 50px; color: var(--text-secondary); }
.c-val { width: 140px; }
.c-acao { width: 44px; text-align: center; }
td input { width: 100%; padding: 6px 8px; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 6px; color: var(--text-primary); font-size: 13px; }
td input:focus { outline: none; border-color: var(--accent); }
.vazio { text-align: center; color: var(--text-muted); font-size: 13px; padding: 24px; }
.icone { background: none; border: 0; color: var(--text-secondary); cursor: pointer; font-size: 18px; padding: 4px; border-radius: 4px; }
.icone:hover { color: var(--danger); }
.rodape { display: flex; align-items: center; justify-content: space-between; flex-wrap: wrap; gap: 10px; }
.soma { font-size: 12px; color: var(--text-secondary); }
.soma.alerta { color: #f59e0b; }
.btn { display: inline-flex; align-items: center; gap: 6px; padding: 7px 14px; border-radius: 6px; border: 1px solid var(--border); background: var(--bg-elevated); color: var(--text-primary); font-size: 13px; cursor: pointer; }
.btn:hover { border-color: var(--accent); color: var(--accent); }
.btn:disabled { opacity: .6; cursor: default; }
</style>
