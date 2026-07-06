<script setup lang="ts">
import { ref, computed } from 'vue'
import { odataGet, odataPost } from '@/services/api'

interface PloomesEntidade { Id: number; Name: string }
interface PloomesCampo {
  Id: number; Key: string; Name: string; TypeId: number; Dynamic: boolean
  Required: boolean; InternalFormula: string | null; FieldHideFormula: string | null
  FieldDisableFormula: string | null; Hidden: boolean; Disabled: boolean; Multiple: boolean
  StringLength: number | null
}

const apiKey = ref(localStorage.getItem('ploomes_api_key') || '')
const entidades = ref<PloomesEntidade[]>([])
const entidadeSelecionada = ref<PloomesEntidade | null>(null)
const campos = ref<PloomesCampo[]>([])
const camposSelecionados = ref<Set<number>>(new Set())
const carregando = ref(false)
const importando = ref(false)
const mensagem = ref('')
const etapa = ref<'config' | 'entidades' | 'campos'>('config')

const baseUrl = 'https://api2-s08-public.ploomes.com'

function salvarApiKey() {
  localStorage.setItem('ploomes_api_key', apiKey.value)
  carregarEntidades()
}

async function carregarEntidades() {
  carregando.value = true; mensagem.value = ''
  try {
    const res = await fetch(`${baseUrl}/Fields@Entities@LastUpdates?$expand=Entity`, {
      headers: { 'User-Key': apiKey.value }
    })
    if (!res.ok) { mensagem.value = `Erro ${res.status}: verifique sua API Key`; return }
    const data = await res.json()
    entidades.value = data.value
      .filter((e: any) => e.Entity)
      .map((e: any) => ({ Id: e.EntityId, Name: e.Entity.Name }))
      .sort((a: PloomesEntidade, b: PloomesEntidade) => a.Name.localeCompare(b.Name))
    etapa.value = 'entidades'
  } catch (e: any) { mensagem.value = e.message } finally { carregando.value = false }
}

async function selecionarEntidade(ent: PloomesEntidade) {
  entidadeSelecionada.value = ent
  carregando.value = true; mensagem.value = ''
  try {
    const res = await fetch(`${baseUrl}/Fields?$filter=EntityId eq ${ent.Id}&$top=200&$select=Id,Key,Name,TypeId,Dynamic,Required,InternalFormula,FieldHideFormula,FieldDisableFormula,Hidden,Disabled,Multiple,StringLength`, {
      headers: { 'User-Key': apiKey.value }
    })
    if (!res.ok) { mensagem.value = `Erro ao carregar campos`; return }
    const data = await res.json()
    campos.value = data.value.filter((c: PloomesCampo) => !c.Hidden)
    camposSelecionados.value = new Set(campos.value.filter(c => c.Dynamic).map(c => c.Id))
    etapa.value = 'campos'
  } catch (e: any) { mensagem.value = e.message } finally { carregando.value = false }
}

function toggleCampo(id: number) {
  if (camposSelecionados.value.has(id)) camposSelecionados.value.delete(id)
  else camposSelecionados.value.add(id)
  camposSelecionados.value = new Set(camposSelecionados.value)
}

function toggleTodos() {
  if (camposSelecionados.value.size === campos.value.length) camposSelecionados.value = new Set()
  else camposSelecionados.value = new Set(campos.value.map(c => c.Id))
}

function mapearTipo(typeId: number): string {
  const mapa: Record<number, string> = { 1: 'Texto', 2: 'TextoLongo', 3: 'Texto', 4: 'Numero', 5: 'Decimal', 6: 'Moeda', 7: 'Lista', 8: 'DataHora', 9: 'Data', 10: 'Booleano', 11: 'Email', 12: 'Cnpj', 13: 'Percentagem', 14: 'Telefone', 15: 'Cpf', 16: 'Cep', 17: 'Endereco', 18: 'Imagem', 19: 'Html' }
  return mapa[typeId] || 'Texto'
}

function converterFormula(formula: string | null): string {
  if (!formula) return ''
  return formula.replace(/\[([^\]]+)\]/g, (_, ref: string) => {
    const partes = ref.split('.')
    if (partes.length >= 2) {
      const entidade = partes[0].trim().toLowerCase().replace(/\s+/g, '')
      const campo = partes.slice(1).join('.').trim().replace(/\s+/g, '')
      const mapaEnt: Record<string, string> = { 'produto': '$registro', 'proposta': '$proposta', 'cliente': '$contato', 'negócio': '$negocio', 'contatodevenda': '$negocio', 'contato': '$contato', 'criador': '$usuario', 'perfildeusuário': '$usuario', 'bloco': '$registro', 'produtodaproposta': '$registro' }
      const prefixo = mapaEnt[entidade] || `$${entidade}`
      return `${prefixo}.${campo}`
    }
    return `$registro.${ref.trim()}`
  })
}

function mapearEntidadeAlvo(nome: string): string {
  const mapa: Record<string, string> = { 'Cliente': 'Cliente', 'Contato de Venda': 'Negocio', 'Produto': 'Produto', 'Proposta': 'Proposta', 'Atividade': 'Atividade', 'Empresa': 'Empresa', 'Usuário': 'Usuario', 'Equipe': 'Equipe' }
  return mapa[nome] || 'Cliente'
}

async function importarCampos() {
  if (!entidadeSelecionada.value) return
  importando.value = true; mensagem.value = ''
  const entAlvo = mapearEntidadeAlvo(entidadeSelecionada.value.Name)
  let importados = 0; let erros = 0; let duplicados = 0

  // Carregar nomes existentes para evitar duplicidade
  let nomesExistentes = new Set<string>()
  try {
    const res = await odataGet<any>('Campos', { filter: `EntidadeAlvo eq '${entAlvo}'`, select: 'Nome', top: 1000 })
    nomesExistentes = new Set(res.value.map((c: any) => (c.Nome as string).toLowerCase()))
  } catch {}

  for (const campo of campos.value) {
    if (!camposSelecionados.value.has(campo.Id)) continue
    try {
      // Verificar duplicidade por nome
      if (nomesExistentes.has(campo.Name.toLowerCase())) { duplicados++; continue }
      const payload: any = { Nome: campo.Name, Tipo: mapearTipo(campo.TypeId), EntidadeAlvo: entAlvo, Obrigatorio: campo.Required, Visivel: true, Ativo: !campo.Disabled, PermiteMultiplosValores: campo.Multiple }
      if (campo.StringLength) payload.TamanhoMaximo = campo.StringLength
      const fc = converterFormula(campo.InternalFormula)
      const fv = converterFormula(campo.FieldHideFormula)
      const fb = converterFormula(campo.FieldDisableFormula)
      if (fc) payload.Formula = fc
      if (fv) payload.FormulaVisibilidade = fv
      if (fb) payload.FormulaSomenteLeitura = fb
      await odataPost('Campos', payload)
      importados++
      nomesExistentes.add(campo.Name.toLowerCase())
    } catch (e: any) {
      erros++
      console.error(`[Importador] Falha ao importar "${campo.Name}" (Key: ${campo.Key}, Type: ${campo.TypeId}):`, e.message)
    }
  }
  const partes = [`✓ ${importados} importados`]
  if (duplicados) partes.push(`${duplicados} já existentes`)
  if (erros) partes.push(`${erros} erros`)
  mensagem.value = partes.join(', ')
  importando.value = false
}

function voltarEntidades() { etapa.value = 'entidades'; campos.value = []; entidadeSelecionada.value = null }
const totalSelecionados = computed(() => camposSelecionados.value.size)
</script>

<template>
<div class="ip-root">
  <div v-if="etapa === 'config'" class="ip-config">
    <div class="ip-icon"><i class="mdi mdi-cloud-download-outline"></i></div>
    <h3>Importar campos da Ploomes</h3>
    <p>Informe sua API Key da Ploomes para conectar</p>
    <div class="ip-field"><input v-model="apiKey" type="password" placeholder="User-Key da Ploomes" @keyup.enter="salvarApiKey" /></div>
    <button class="ip-btn" :disabled="!apiKey.trim() || carregando" @click="salvarApiKey"><i class="mdi mdi-connection"></i> {{ carregando ? 'Conectando...' : 'Conectar' }}</button>
  </div>

  <div v-else-if="etapa === 'entidades'" class="ip-entidades">
    <div class="ip-head"><h3>Selecionar entidade</h3><button class="ip-btn-sec" @click="etapa = 'config'"><i class="mdi mdi-key-outline"></i> Alterar API Key</button></div>
    <div class="ip-grid">
      <button v-for="ent in entidades" :key="ent.Id" class="ip-card" @click="selecionarEntidade(ent)"><i class="mdi mdi-database-outline"></i><span>{{ ent.Name }}</span></button>
    </div>
  </div>

  <div v-else-if="etapa === 'campos'" class="ip-campos">
    <div class="ip-head">
      <button class="ip-btn-back" @click="voltarEntidades"><i class="mdi mdi-arrow-left"></i></button>
      <h3>{{ entidadeSelecionada?.Name }} — {{ campos.length }} campos</h3>
      <span class="ip-sel">{{ totalSelecionados }} selecionados</span>
      <button class="ip-btn" :disabled="!totalSelecionados || importando" @click="importarCampos"><i class="mdi mdi-import"></i> {{ importando ? 'Importando...' : 'Importar' }}</button>
    </div>
    <div v-if="carregando" class="ip-loading">Carregando campos...</div>
    <div v-else class="ip-table-wrap">
      <table class="ip-table">
        <thead><tr><th><input type="checkbox" :checked="totalSelecionados === campos.length" @change="toggleTodos" /></th><th>Nome</th><th>Tipo</th><th>Chave</th><th>Fórmulas</th><th>Obrig.</th></tr></thead>
        <tbody>
          <tr v-for="c in campos" :key="c.Id" :class="{ dim: !camposSelecionados.has(c.Id) }">
            <td><input type="checkbox" :checked="camposSelecionados.has(c.Id)" @change="toggleCampo(c.Id)" /></td>
            <td class="ip-nome">{{ c.Name }}</td>
            <td><span class="ip-badge">{{ mapearTipo(c.TypeId) }}</span></td>
            <td class="ip-chave">{{ c.Key }}</td>
            <td class="ip-formula">{{ c.InternalFormula ? '✓ Cálculo' : '' }}{{ c.FieldHideFormula ? ' ✓ Visib.' : '' }}{{ c.FieldDisableFormula ? ' ✓ Bloq.' : '' }}</td>
            <td>{{ c.Required ? 'Sim' : '' }}</td>
          </tr>
        </tbody>
      </table>
    </div>
  </div>
  <p v-if="mensagem" class="ip-msg" :class="{ erro: mensagem.includes('Erro') }">{{ mensagem }}</p>
</div>
</template>

<style scoped>
.ip-root{display:flex;flex-direction:column;height:100%;overflow:hidden}
.ip-config{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:12px;padding:60px 20px;text-align:center}
.ip-config h3{font-size:18px;font-weight:600;color:var(--text-primary);margin:0}
.ip-config p{font-size:13px;color:var(--text-muted);margin:0}
.ip-icon .mdi{font-size:48px;color:var(--accent);opacity:.6}
.ip-field{width:100%;max-width:400px}
.ip-field input{width:100%;padding:12px 16px;border:1px solid var(--border);border-radius:8px;background:var(--bg-surface);color:var(--text-primary);font-size:14px;outline:none;box-sizing:border-box}
.ip-field input:focus{border-color:var(--accent)}
.ip-btn{display:flex;align-items:center;gap:6px;padding:10px 20px;border:none;border-radius:8px;background:var(--accent);color:#000;font-size:13px;font-weight:600;cursor:pointer}.ip-btn:hover{background:var(--accent-hover)}.ip-btn:disabled{opacity:.4;cursor:not-allowed}
.ip-btn-sec{display:flex;align-items:center;gap:4px;padding:6px 12px;border:1px solid var(--border);border-radius:6px;background:transparent;color:var(--text-secondary);font-size:12px;cursor:pointer}.ip-btn-sec:hover{background:var(--bg-elevated)}
.ip-btn-back{width:32px;height:32px;border:1px solid var(--border);border-radius:6px;background:var(--bg-surface);color:var(--text-secondary);cursor:pointer;display:flex;align-items:center;justify-content:center}.ip-btn-back:hover{background:var(--bg-elevated)}
.ip-head{display:flex;align-items:center;gap:12px;padding:12px 0;flex-shrink:0}
.ip-head h3{font-size:15px;font-weight:600;color:var(--text-primary);margin:0}
.ip-sel{font-size:12px;color:var(--text-muted);margin-left:auto}
.ip-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(180px,1fr));gap:10px;padding:12px 0}
.ip-card{display:flex;align-items:center;gap:10px;padding:14px 16px;border:1px solid var(--border);border-radius:8px;background:var(--bg-surface);color:var(--text-secondary);font-size:13px;cursor:pointer;text-align:left}.ip-card:hover{border-color:var(--accent);color:var(--text-primary)}
.ip-card .mdi{font-size:18px;color:var(--accent)}
.ip-entidades,.ip-campos{display:flex;flex-direction:column;flex:1;overflow:hidden;padding:0 4px}
.ip-table-wrap{flex:1;overflow:auto;border:1px solid var(--border);border-radius:8px}
.ip-table{width:100%;border-collapse:collapse;font-size:12px}
.ip-table thead{position:sticky;top:0;z-index:1}
.ip-table th{background:var(--bg-secondary);color:var(--text-muted);font-weight:600;text-transform:uppercase;font-size:10px;padding:10px 12px;text-align:left;border-bottom:1px solid var(--border)}
.ip-table td{padding:8px 12px;border-bottom:1px solid var(--border);color:var(--text-secondary)}
.ip-table tr:hover td{background:var(--bg-elevated)}
.ip-table tr.dim{opacity:.4}
.ip-nome{color:var(--text-primary);font-weight:500}
.ip-chave{font-family:monospace;font-size:11px;color:var(--text-muted)}
.ip-badge{background:var(--bg-elevated);border:1px solid var(--border);padding:2px 6px;border-radius:4px;font-size:10px}
.ip-formula{font-size:10px;color:var(--accent)}
.ip-loading{padding:40px;text-align:center;color:var(--text-muted)}
.ip-msg{padding:10px 16px;border-radius:8px;font-size:13px;margin-top:12px;background:rgba(6,182,212,.08);color:var(--accent)}.ip-msg.erro{background:rgba(239,68,68,.08);color:#ef4444}
</style>
