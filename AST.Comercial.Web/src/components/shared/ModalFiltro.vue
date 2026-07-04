<script setup lang="ts">
import { ref, computed, onMounted, watch } from 'vue'

interface FiltroSalvo { Id: number; Nome: string; Filtro: string | null }
interface CampoDisponivel { nome: string; tipo: string; expandivel?: boolean; caminho?: string }

const props = defineProps<{
  aberto: boolean
  entidade: string
  filtrosSalvos: FiltroSalvo[]
}>()

const emit = defineEmits<{
  fechar: []
  aplicar: [filtro: string | null]
}>()

const mostrarNovo = ref(false)
const condicoes = ref([{ campo: '', operador: 'eq', valor: '', dropdownAberto: false, busca: '', nav: [] as string[] }])
const campos = ref<CampoDisponivel[]>([])
const esquema = ref<any>({})

const operadores = [
  { valor: 'eq', label: 'Igual a' },
  { valor: 'ne', label: 'Diferente de' },
  { valor: 'contains', label: 'Contém' },
  { valor: 'gt', label: 'Maior que' },
  { valor: 'lt', label: 'Menor que' },
  { valor: 'ge', label: 'Maior ou igual' },
  { valor: 'le', label: 'Menor ou igual' },
]

async function carregarCampos() {
  if (!props.entidade) return
  try {
    const res = await fetch(`/odata/Campos@Esquema(${props.entidade})`, {
      headers: { Authorization: `Bearer ${localStorage.getItem('token') || ''}` }
    })
    if (res.ok) {
      const data = await res.json()
      esquema.value = data
      campos.value = parsearPropriedades(data['$registro'], '')
    }
  } catch { campos.value = [] }
}

function parsearPropriedades(obj: any, prefixo: string): CampoDisponivel[] {
  const propsObj = obj?.propriedades ?? obj?.Propriedades ?? {}
  const resultado: CampoDisponivel[] = []
  for (const [nome, info] of Object.entries(propsObj) as [string, any][]) {
    const subProps = info?.propriedades ?? info?.Propriedades
    const caminho = prefixo ? `${prefixo}/${nome}` : nome
    if (subProps && Object.keys(subProps).length > 0) {
      resultado.push({ nome, tipo: info.tipo ?? 'objeto', expandivel: true, caminho })
    } else {
      resultado.push({ nome, tipo: info.tipo ?? info.Tipo ?? 'string', caminho })
    }
  }
  return resultado
}

function getCamposParaNivel(condIdx: number): CampoDisponivel[] {
  const cond = condicoes.value[condIdx]
  const data = esquema.value

  if (cond.nav.length === 0) {
    // Root level - return $registro properties + navigations
    return campos.value
  }

  // Navigate into the path
  let current: any = data['$registro']
  for (const segmento of cond.nav) {
    const props = current?.propriedades ?? current?.Propriedades ?? {}
    current = props[segmento]
    if (!current) return []
  }

  return parsearPropriedades(current, cond.nav.join('/'))
}

function camposFiltradosParaNivel(condIdx: number): CampoDisponivel[] {
  const cond = condicoes.value[condIdx]
  const lista = getCamposParaNivel(condIdx)
  if (!cond.busca) return lista
  return lista.filter(c => c.nome.toLowerCase().includes(cond.busca.toLowerCase()))
}

function selecionarCampo(idx: number, campo: CampoDisponivel) {
  if (campo.expandivel) {
    condicoes.value[idx].nav.push(campo.nome)
    condicoes.value[idx].busca = ''
    return
  }
  condicoes.value[idx].campo = campo.caminho ?? campo.nome
  condicoes.value[idx].dropdownAberto = false
  condicoes.value[idx].busca = ''
  condicoes.value[idx].nav = []
}

function voltarNavegacao(idx: number) {
  condicoes.value[idx].nav.pop()
}

function aplicarSalvo(filtro: FiltroSalvo) {
  emit('aplicar', filtro.Filtro)
  emit('fechar')
}

function adicionarCondicao() {
  condicoes.value.push({ campo: '', operador: 'eq', valor: '', dropdownAberto: false, busca: '', nav: [] })
}

function removerCondicao(idx: number) {
  condicoes.value.splice(idx, 1)
  if (!condicoes.value.length) condicoes.value.push({ campo: '', operador: 'eq', valor: '', dropdownAberto: false, busca: '', nav: [] })
}

function aplicarNovo() {
  const partes = condicoes.value
    .filter(c => c.campo && c.valor)
    .map(c => {
      if (c.operador === 'contains') return `contains(${c.campo},'${c.valor}')`
      return `${c.campo} ${c.operador} '${c.valor}'`
    })
  const filtro = partes.length ? partes.join(' and ') : null
  emit('aplicar', filtro)
  emit('fechar')
}

function limpar() {
  emit('aplicar', null)
  emit('fechar')
}

watch(() => props.aberto, (v) => { if (v) carregarCampos() })
onMounted(() => { if (props.aberto) carregarCampos() })
</script>

<template>
  <div v-if="aberto" class="mf-dropdown" @click.stop>
    <div class="mf-header">
      <span>Ações de filtros</span>
      <button @click="emit('fechar')"><i class="mdi mdi-close"></i></button>
    </div>

    <button class="mf-item mf-novo" @click="mostrarNovo = !mostrarNovo">
      <i class="mdi mdi-plus"></i> Novo filtro
    </button>

    <div v-if="filtrosSalvos.length" class="mf-section">
      <p class="mf-section-title">Filtros salvos</p>
      <button v-for="f in filtrosSalvos" :key="f.Id" class="mf-item" @click="aplicarSalvo(f)">
        <i class="mdi mdi-filter-outline"></i> {{ f.Nome }}
      </button>
    </div>

    <button class="mf-item mf-limpar" @click="limpar">
      <i class="mdi mdi-filter-remove-outline"></i> Limpar filtros
    </button>

    <div v-if="mostrarNovo" class="mf-builder">
      <p class="mf-builder-title"><i class="mdi mdi-filter"></i> Bloco de condição</p>
      <div v-for="(cond, idx) in condicoes" :key="idx" class="mf-cond">
        <div class="mf-campo-wrap">
          <button class="mf-campo-btn" @click.stop="cond.dropdownAberto = !cond.dropdownAberto">
            {{ cond.campo || 'Selecione um campo' }}
            <i class="mdi mdi-chevron-down"></i>
          </button>
          <div v-if="cond.dropdownAberto" class="mf-campo-dropdown" @click.stop>
            <div class="mf-campo-dropdown-header">
              <span>{{ entidade }}</span>
              <button @click="cond.dropdownAberto = false"><i class="mdi mdi-close"></i></button>
            </div>
            <div v-if="cond.nav.length" class="mf-campo-nav">
              <button @click="voltarNavegacao(idx)"><i class="mdi mdi-arrow-left"></i></button>
              <span>{{ cond.nav.join(' → ') }}</span>
            </div>
            <input v-model="cond.busca" class="mf-campo-busca" placeholder="Busque um campo" />
            <div class="mf-campo-lista">
              <button v-for="c in camposFiltradosParaNivel(idx)" :key="c.nome" class="mf-campo-item" :class="{ expandivel: c.expandivel }" @click="selecionarCampo(idx, c)">
                <i class="mdi" :class="c.expandivel ? 'mdi-chevron-right' : 'mdi-format-text'"></i>
                <span>{{ c.nome }}</span>
                <span v-if="!c.expandivel" class="mf-campo-tipo">{{ c.tipo }}</span>
                <i v-if="c.expandivel" class="mdi mdi-arrow-right mf-campo-seta"></i>
              </button>
              <p v-if="!camposFiltradosParaNivel(idx).length" class="mf-campo-vazio">Nenhum campo</p>
            </div>
          </div>
        </div>
        <select v-model="cond.operador" class="mf-select">
          <option v-for="op in operadores" :key="op.valor" :value="op.valor">{{ op.label }}</option>
        </select>
        <input v-model="cond.valor" placeholder="Valor..." class="mf-input" />
        <button class="mf-cond-del" @click="removerCondicao(idx)"><i class="mdi mdi-close"></i></button>
      </div>
      <button class="mf-add-cond" @click="adicionarCondicao"><i class="mdi mdi-plus"></i> Condição</button>
      <div class="mf-builder-actions">
        <button class="mf-btn-aplicar" @click="aplicarNovo"><i class="mdi mdi-filter-check"></i> Aplicar</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.mf-dropdown{position:absolute;right:0;top:calc(100% + 6px);z-index:100;background:var(--bg-primary);border:1px solid var(--border);box-shadow:0 8px 32px rgba(0,0,0,.4);width:400px;max-height:600px;overflow-y:auto;border-radius:10px;padding:8px;animation:mfFadeIn .12s ease;resize:both;min-width:320px;min-height:200px}

@keyframes mfFadeIn{from{opacity:0;transform:translateY(-4px)}to{opacity:1;transform:translateY(0)}}
.mf-header{display:flex;align-items:center;justify-content:space-between;padding:10px 14px;border-bottom:1px solid var(--border);font-size:13px;font-weight:600;color:var(--text-primary)}
.mf-header button{background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:16px;padding:2px;border-radius:4px}
.mf-header button:hover{color:var(--text-primary)}
.mf-item{display:flex;align-items:center;gap:8px;padding:9px 14px;border-radius:6px;width:100%;text-align:left;background:none;border:none;color:var(--text-secondary);font-size:13px;cursor:pointer;transition:background .1s}
.mf-item:hover{background:var(--bg-elevated)}
.mf-novo{color:var(--accent);font-weight:500}
.mf-limpar{color:var(--text-muted);font-size:12px;border-top:1px solid var(--border);margin-top:4px;padding-top:10px}
.mf-section{padding:4px 0}
.mf-section-title{font-size:10px;font-weight:600;text-transform:uppercase;letter-spacing:.5px;color:var(--text-muted);padding:8px 14px 4px;margin:0}
.mf-builder{padding:12px;border-top:1px solid var(--border);display:flex;flex-direction:column;gap:10px;flex:1}
.mf-builder-title{font-size:12px;font-weight:600;color:var(--text-primary);margin:0 0 4px;display:flex;align-items:center;gap:6px}
.mf-cond{display:flex;gap:6px;align-items:flex-start}
.mf-campo-wrap{position:relative;flex:1.2;min-width:0}
.mf-campo-btn{display:flex;align-items:center;justify-content:space-between;width:100%;padding:6px 10px;background:var(--bg-surface);border:1px solid var(--border);border-radius:6px;color:var(--text-secondary);font-size:12px;cursor:pointer;text-align:left;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.mf-campo-btn:hover{border-color:var(--accent)}
.mf-campo-btn .mdi{font-size:14px;flex-shrink:0}
.mf-campo-dropdown{position:absolute;top:calc(100% + 4px);left:0;z-index:50;background:var(--bg-primary);border:1px solid var(--border);border-radius:10px;box-shadow:0 8px 24px rgba(0,0,0,.4);width:220px;max-height:260px;display:flex;flex-direction:column;overflow:hidden}
.mf-campo-dropdown-header{display:flex;align-items:center;justify-content:space-between;padding:8px 12px;border-bottom:1px solid var(--border);font-size:12px;font-weight:600;color:var(--text-primary)}
.mf-campo-dropdown-header button{background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:14px}
.mf-campo-nav{display:flex;align-items:center;gap:6px;padding:6px 12px;border-bottom:1px solid var(--border);font-size:11px;color:var(--accent)}
.mf-campo-nav button{background:none;border:1px solid var(--border);border-radius:4px;color:var(--text-secondary);cursor:pointer;padding:2px 4px;font-size:12px}
.mf-campo-nav button:hover{background:var(--bg-elevated)}
.mf-campo-busca{border:none;border-bottom:1px solid var(--border);background:transparent;padding:8px 12px;color:var(--text-primary);font-size:12px;outline:none}
.mf-campo-busca::placeholder{color:var(--text-muted)}
.mf-campo-lista{flex:1;overflow-y:auto;padding:4px}
.mf-campo-item{display:flex;align-items:center;gap:8px;width:100%;padding:7px 12px;border:none;background:transparent;color:var(--text-secondary);font-size:12px;cursor:pointer;border-radius:6px;text-align:left}
.mf-campo-item:hover{background:var(--bg-elevated);color:var(--text-primary)}
.mf-campo-item .mdi{color:var(--accent);font-size:14px;flex-shrink:0}
.mf-campo-item span{flex:1}
.mf-campo-item.expandivel{color:var(--accent)}
.mf-campo-tipo{font-size:10px;color:var(--text-muted);margin-left:auto;flex-shrink:0}
.mf-campo-seta{font-size:12px;color:var(--text-muted);margin-left:auto;flex-shrink:0}
.mf-campo-vazio{text-align:center;color:var(--text-muted);font-size:11px;padding:12px}
.mf-select{background:var(--bg-surface);border:1px solid var(--border);border-radius:6px;padding:6px 8px;font-size:11px;color:var(--text-primary);outline:none;cursor:pointer;min-width:80px}
.mf-select:focus{border-color:var(--accent)}
.mf-input{background:var(--bg-surface);border:1px solid var(--border);border-radius:6px;padding:6px 10px;font-size:12px;color:var(--text-primary);outline:none;flex:1;min-width:0}
.mf-input:focus{border-color:var(--accent)}
.mf-input::placeholder{color:var(--text-muted)}
.mf-cond-del{background:none;border:none;color:var(--text-muted);cursor:pointer;font-size:14px;padding:6px 4px;border-radius:4px;flex-shrink:0}
.mf-cond-del:hover{color:#ef4444}
.mf-add-cond{display:flex;align-items:center;gap:4px;background:none;border:none;color:var(--accent);font-size:12px;cursor:pointer;padding:4px 0}
.mf-add-cond:hover{opacity:.8}
.mf-builder-actions{display:flex;justify-content:flex-end;padding-top:4px}
.mf-btn-aplicar{display:flex;align-items:center;gap:5px;padding:7px 14px;border:none;border-radius:6px;background:var(--accent);color:#000;font-size:12px;font-weight:600;cursor:pointer}
.mf-btn-aplicar:hover{background:var(--accent-hover)}
</style>
