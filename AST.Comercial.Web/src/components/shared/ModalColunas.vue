<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'

interface CampoSchema { nome: string; tipo: string; expandivel?: boolean; caminho?: string }

const props = defineProps<{
  colunas: { campo: string; label: string }[]
  colunasVisiveis: string[]
  entidade: string
}>()

const emit = defineEmits<{
  fechar: []
  salvar: [colunas: string[]]
}>()

const colunasAtivas = ref([...props.colunasVisiveis])
const busca = ref('')
const dropdownAberto = ref(false)
const esquema = ref<any>({})
const camposSchema = ref<CampoSchema[]>([])
const nav = ref<string[]>([])

onMounted(carregarSchema)

async function carregarSchema() {
  try {
    const res = await fetch(`/odata/Campos@Esquema(${props.entidade})`, {
      headers: { Authorization: `Bearer ${localStorage.getItem('token') || ''}` }
    })
    if (res.ok) {
      const data = await res.json()
      esquema.value = data
      camposSchema.value = parsearPropriedades(data['$registro'], '')
    }
  } catch {}

  // Fallback: se não retornou nada, usar as colunas definidas no componente
  if (!camposSchema.value.length) {
    camposSchema.value = props.colunas.map(c => ({ nome: c.campo, tipo: 'string', caminho: c.campo }))
  }
}

function parsearPropriedades(obj: any, prefixo: string): CampoSchema[] {
  const propsObj = obj?.propriedades ?? obj?.Propriedades ?? {}
  const resultado: CampoSchema[] = []
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

const camposNivelAtual = computed(() => {
  if (nav.value.length === 0) {
    const lista = camposSchema.value.filter(c => !colunasAtivas.value.includes(c.caminho ?? c.nome))
    if (!busca.value) return lista
    return lista.filter(c => c.nome.toLowerCase().includes(busca.value.toLowerCase()))
  }
  let current: any = esquema.value['$registro']
  for (const segmento of nav.value) {
    const p = current?.propriedades ?? current?.Propriedades ?? {}
    current = p[segmento]
    if (!current) return []
  }
  const lista = parsearPropriedades(current, nav.value.join('/')).filter(c => !colunasAtivas.value.includes(c.caminho ?? c.nome))
  if (!busca.value) return lista
  return lista.filter(c => c.nome.toLowerCase().includes(busca.value.toLowerCase()))
})

function selecionarCampo(campo: CampoSchema) {
  if (campo.expandivel) {
    nav.value.push(campo.nome)
    busca.value = ''
    return
  }
  colunasAtivas.value.push(campo.caminho ?? campo.nome)
  busca.value = ''
  nav.value = []
  dropdownAberto.value = false
}

function voltarNav() { nav.value.pop() }

function labelDe(campo: string) {
  const found = props.colunas.find(c => c.campo === campo)
  if (found) return found.label
  return campo.includes('/') ? campo.split('/').pop()! : campo
}

function removerColuna(campo: string) {
  colunasAtivas.value = colunasAtivas.value.filter(c => c !== campo)
}

function salvar() {
  emit('salvar', colunasAtivas.value)
}
</script>

<template>
  <div class="mc-overlay">
    <div class="mc-modal">
      <div class="mc-header">
        <h3>Configurar colunas da tabela</h3>
        <button @click="emit('fechar')"><i class="mdi mdi-close"></i></button>
      </div>
      <div class="mc-body">
        <table class="mc-table">
          <thead>
            <tr>
              <th></th>
              <th>Entidade</th>
              <th>Coluna</th>
              <th>Destacar</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="col in colunasAtivas" :key="col">
              <td class="mc-drag"><i class="mdi mdi-drag-vertical"></i></td>
              <td>{{ col.includes('/') ? col.split('/')[0] : entidade }}</td>
              <td>{{ labelDe(col) }}</td>
              <td>Não</td>
              <td>
                <button class="mc-del" @click="removerColuna(col)">
                  <i class="mdi mdi-delete-outline"></i>
                </button>
              </td>
            </tr>
          </tbody>
        </table>
        <!-- Add row -->
        <div class="mc-add-row">
          <span class="mc-add-entidade">{{ entidade }}</span>
          <div class="mc-add-campo">
            <input v-model="busca" placeholder="Buscar coluna..." @focus="dropdownAberto = true" />
            <div v-if="dropdownAberto" class="mc-dropdown">
              <div v-if="nav.length" class="mc-dropdown-nav">
                <button @click="voltarNav()"><i class="mdi mdi-arrow-left"></i></button>
                <span>{{ nav.join(' → ') }}</span>
              </div>
              <div class="mc-dropdown-list">
                <button v-for="c in camposNivelAtual" :key="c.nome" :class="{ expandivel: c.expandivel }" @click="selecionarCampo(c)">
                  <i class="mdi" :class="c.expandivel ? 'mdi-chevron-right' : 'mdi-format-text'"></i>
                  <span>{{ c.nome }}</span>
                  <span v-if="!c.expandivel" class="mc-tipo">{{ c.tipo }}</span>
                  <i v-if="c.expandivel" class="mdi mdi-arrow-right mc-seta"></i>
                </button>
                <p v-if="!camposNivelAtual.length" class="mc-vazio">Nenhum campo disponível</p>
              </div>
            </div>
          </div>
          <button class="mc-confirm" @click="dropdownAberto = false"><i class="mdi mdi-check"></i></button>
        </div>
      </div>
      <div class="mc-footer">
        <button class="mc-btn-salvar" @click="salvar"><i class="mdi mdi-content-save"></i> Salvar</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.mc-overlay {
  position: fixed;
  inset: 0;
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.6);
  backdrop-filter: blur(2px);
}

.mc-modal {
  background: var(--bg-primary);
  border: 1px solid var(--border);
  border-radius: 12px;
  width: 700px;
  max-width: 95vw;
  max-height: 80vh;
  display: flex;
  flex-direction: column;
  box-shadow: 0 16px 48px rgba(0, 0, 0, 0.5);
  overflow: hidden;
}

.mc-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px;
  border-bottom: 1px solid var(--border);
}

.mc-header h3 {
  margin: 0;
  font-size: 15px;
  font-weight: 600;
  color: var(--text-primary);
}

.mc-header button {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30px;
  height: 30px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: var(--text-muted);
  font-size: 18px;
  cursor: pointer;
  transition: all 0.15s;
}

.mc-header button:hover {
  background: var(--bg-elevated);
  color: var(--text-primary);
}

.mc-body {
  flex: 1;
  overflow-y: auto;
  padding: 16px 20px;
}

.mc-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.mc-table thead th {
  text-align: left;
  padding: 8px 12px;
  font-size: 10px;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--text-muted);
  border-bottom: 1px solid var(--border);
}

.mc-table tbody td {
  padding: 10px 12px;
  color: var(--text-secondary);
  border-bottom: 1px solid var(--border);
}

.mc-table tbody tr:hover td {
  background: var(--bg-elevated);
}

.mc-drag {
  color: var(--text-muted);
  cursor: grab;
  font-size: 16px;
  width: 30px;
}

.mc-drag:active {
  cursor: grabbing;
}

.mc-del {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 28px;
  height: 28px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: var(--text-muted);
  font-size: 16px;
  cursor: pointer;
  transition: all 0.15s;
}

.mc-del:hover {
  background: rgba(239, 68, 68, 0.1);
  color: #ef4444;
}

.mc-add-row {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 12px;
  margin-top: 8px;
  border: 1px dashed var(--border);
  border-radius: 8px;
  background: var(--bg-surface);
}

.mc-add-entidade {
  font-size: 13px;
  color: var(--text-secondary);
  font-weight: 500;
  min-width: 80px;
}

.mc-add-campo {
  position: relative;
  flex: 1;
}

.mc-add-campo input {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid var(--border);
  border-radius: 6px;
  background: var(--bg-primary);
  color: var(--text-primary);
  font-size: 13px;
  outline: none;
  transition: border-color 0.15s;
}

.mc-add-campo input::placeholder {
  color: var(--text-muted);
}

.mc-add-campo input:focus {
  border-color: var(--accent);
}

.mc-dropdown {
  position: absolute;
  top: calc(100% + 4px);
  left: 0;
  right: 0;
  z-index: 10;
  background: var(--bg-primary);
  border: 1px solid var(--border);
  border-radius: 8px;
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.4);
  max-height: 200px;
  overflow-y: auto;
  padding: 4px;
}

.mc-dropdown button {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  text-align: left;
  padding: 8px 12px;
  border: none;
  border-radius: 6px;
  background: transparent;
  color: var(--text-secondary);
  font-size: 13px;
  cursor: pointer;
  transition: all 0.1s;
}

.mc-dropdown button:hover {
  background: rgba(0, 188, 212, 0.1);
  color: var(--accent);
}

.mc-dropdown button.expandivel {
  color: var(--accent);
}

.mc-dropdown-nav {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  border-bottom: 1px solid var(--border);
  font-size: 11px;
  color: var(--accent);
}

.mc-dropdown-nav button {
  background: none;
  border: 1px solid var(--border);
  border-radius: 4px;
  color: var(--text-secondary);
  cursor: pointer;
  padding: 2px 6px;
  font-size: 12px;
  width: auto;
}

.mc-dropdown-nav button:hover {
  background: var(--bg-elevated);
}

.mc-dropdown-list {
  max-height: 200px;
  overflow-y: auto;
  padding: 4px;
}

.mc-tipo {
  font-size: 10px;
  color: var(--text-muted);
  margin-left: auto;
}

.mc-seta {
  font-size: 12px;
  color: var(--text-muted);
  margin-left: auto;
}

.mc-vazio {
  text-align: center;
  color: var(--text-muted);
  font-size: 12px;
  padding: 16px;
  margin: 0;
}

.mc-confirm {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 32px;
  height: 32px;
  border: 1px solid var(--border);
  border-radius: 6px;
  background: var(--bg-primary);
  color: var(--accent);
  font-size: 18px;
  cursor: pointer;
  transition: all 0.15s;
}

.mc-confirm:hover {
  background: var(--accent);
  color: #000;
  border-color: var(--accent);
}

.mc-footer {
  display: flex;
  justify-content: flex-end;
  padding: 14px 20px;
  border-top: 1px solid var(--border);
}

.mc-btn-salvar {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 9px 20px;
  border: none;
  border-radius: 8px;
  background: var(--accent);
  color: #000;
  font-size: 13px;
  font-weight: 600;
  cursor: pointer;
  transition: background 0.15s;
}

.mc-btn-salvar:hover {
  background: var(--accent-hover);
}

@media (max-width: 768px) {
  .mc-modal {
    width: 100%;
    max-width: 100%;
    max-height: 100%;
    border-radius: 0;
  }

  .mc-add-row {
    flex-wrap: wrap;
  }

  .mc-add-campo {
    width: 100%;
  }
}
</style>
