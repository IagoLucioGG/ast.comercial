<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue'
import { useRoute } from 'vue-router'
import { formatarMoeda } from '@/composables/usePropostaTotais'

interface ItemPublico { descricao?: string | null; quantidade?: number | null; precoUnitario?: number | null; desconto?: number | null; total?: number | null }
interface ParcelaPublica { numero: number; vencimento?: string | null; valor?: number | null }
interface PropostaPublica {
  numero?: number | string | null
  titulo?: string | null
  empresaNome?: string | null
  clienteNome?: string | null
  valor?: number | null
  moeda?: string | null
  validadeAte?: string | null
  status?: string | null
  itens?: ItemPublico[]
  parcelas?: ParcelaPublica[]
  documentoHtml?: string | null
  podeResponder?: boolean
}

type Estado = 'carregando' | 'ok' | 'naoEncontrada' | 'erro'

const route = useRoute()
const chave = computed(() => String(route.params.chave ?? ''))
const estado = ref<Estado>('carregando')
const dados = ref<PropostaPublica | null>(null)
const mensagemErro = ref('')

// resposta do cliente
const modo = ref<'aceitar' | 'recusar' | null>(null)
const nome = ref('')
const email = ref('')
const motivo = ref('')
const enviando = ref(false)
const erroEnvio = ref('')
const resultado = ref('')

// Chamadas diretas (fetch) SEM o cabeçalho Authorization: página pública e anônima
function url(sufixo = ''): string {
  return `/api/publico/propostas/${encodeURIComponent(chave.value)}${sufixo}`
}

async function carregar() {
  estado.value = 'carregando'
  dados.value = null
  resultado.value = ''
  modo.value = null
  try {
    const res = await fetch(url(), { headers: { Accept: 'application/json' } })
    if (res.status === 404) { estado.value = 'naoEncontrada'; return }
    if (!res.ok) throw new Error(`Falha ao carregar a proposta (${res.status}).`)
    dados.value = (await res.json()) as PropostaPublica
    estado.value = 'ok'
  } catch (e) {
    mensagemErro.value = e instanceof Error ? e.message : 'Falha ao carregar a proposta.'
    estado.value = 'erro'
  }
}

async function mensagemDe(res: Response, padrao: string): Promise<string> {
  try {
    const j = (await res.json()) as { erro?: string; message?: string }
    return j.erro ?? j.message ?? padrao
  } catch { return padrao }
}

async function enviar() {
  erroEnvio.value = ''
  if (modo.value === 'aceitar' && !nome.value.trim()) { erroEnvio.value = 'Informe seu nome para aceitar.'; return }
  enviando.value = true
  try {
    const aceitar = modo.value === 'aceitar'
    const corpo = aceitar
      ? { nome: nome.value.trim(), email: email.value.trim() || undefined }
      : { motivo: motivo.value.trim() || undefined }
    const res = await fetch(url(aceitar ? '/aceitar' : '/recusar'), {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
      body: JSON.stringify(corpo),
    })
    if (res.status === 404) { estado.value = 'naoEncontrada'; return }
    if (res.status === 409) {
      // já respondida (por esta ou outra pessoa): recarrega para mostrar o status real
      modo.value = null
      await carregar()
      resultado.value = 'Esta proposta já foi respondida; a resposta anterior foi mantida.'
      return
    }
    if (!res.ok) { erroEnvio.value = await mensagemDe(res, `Não foi possível registrar sua resposta (${res.status}).`); return }
    resultado.value = aceitar ? 'Proposta aceita. Obrigado!' : 'Resposta registrada. Você recusou a proposta.'
    modo.value = null
    if (dados.value) dados.value.podeResponder = false
    // atualiza o status exibido; se falhar, mantém o que já está na tela
    try {
      const r = await fetch(url(), { headers: { Accept: 'application/json' } })
      if (r.ok) dados.value = (await r.json()) as PropostaPublica
    } catch { /* mantém os dados atuais */ }
  } catch {
    erroEnvio.value = 'Sem conexão com o servidor. Tente novamente.'
  } finally {
    enviando.value = false
  }
}

function dinheiro(v: number | null | undefined): string {
  // sem moeda informada pelo servidor mostra só o número (nunca assume R$)
  const m = dados.value?.moeda ?? ''
  return formatarMoeda(v, /^[A-Za-z]{3}$/.test(m) ? m : '')
}
function dataBR(iso?: string | null): string {
  if (!iso) return '-'
  const d = new Date(iso)
  return Number.isNaN(d.getTime()) ? '-' : d.toLocaleDateString('pt-BR', { timeZone: 'UTC' })
}

onMounted(carregar)
watch(chave, carregar)
</script>

<template>
  <div class="publica">
    <main class="pagina">
      <p v-if="estado === 'carregando'" class="estado" role="status">Carregando proposta...</p>

      <div v-else-if="estado === 'naoEncontrada'" class="estado cartao" role="alert">
        <h1>Proposta não encontrada</h1>
        <p>O link pode estar incorreto ou a proposta expirou. Entre em contato com quem enviou o link.</p>
      </div>

      <div v-else-if="estado === 'erro'" class="estado cartao" role="alert">
        <h1>Não foi possível carregar</h1>
        <p>{{ mensagemErro }}</p>
        <button type="button" class="btn" @click="carregar">Tentar novamente</button>
      </div>

      <template v-else-if="dados">
        <header class="topo">
          <p v-if="dados.empresaNome" class="empresa">{{ dados.empresaNome }}</p>
          <p class="numero">Proposta <span v-if="dados.numero">nº {{ dados.numero }}</span></p>
          <h1>{{ dados.titulo || 'Proposta comercial' }}</h1>
          <p v-if="dados.clienteNome" class="cliente">Para: {{ dados.clienteNome }}</p>
          <p v-if="dados.status" class="selo">{{ dados.status }}</p>
        </header>

        <p v-if="resultado" class="aviso ok" role="status">{{ resultado }}</p>

        <section class="cartao resumo">
          <div><small>Valor total</small><strong>{{ dinheiro(dados.valor) }}</strong></div>
          <div><small>Válida até</small><strong>{{ dataBR(dados.validadeAte) }}</strong></div>
        </section>

        <section v-if="dados.itens?.length" class="cartao">
          <h2>Itens</h2>
          <div class="rolagem">
            <table>
              <thead>
                <tr><th>Descrição</th><th class="n">Qtd.</th><th class="n">Preço</th><th class="n">Desc.</th><th class="n">Total</th></tr>
              </thead>
              <tbody>
                <tr v-for="(i, idx) in dados.itens" :key="idx">
                  <td>{{ i.descricao }}</td>
                  <td class="n">{{ i.quantidade }}</td>
                  <td class="n">{{ dinheiro(i.precoUnitario) }}</td>
                  <td class="n">{{ i.desconto ? `${i.desconto}%` : '-' }}</td>
                  <td class="n">{{ dinheiro(i.total) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section v-if="dados.parcelas?.length" class="cartao">
          <h2>Parcelas</h2>
          <div class="rolagem">
            <table>
              <thead><tr><th>Nº</th><th>Vencimento</th><th class="n">Valor</th></tr></thead>
              <tbody>
                <tr v-for="p in dados.parcelas" :key="p.numero">
                  <td>{{ p.numero }}</td><td>{{ dataBR(p.vencimento) }}</td><td class="n">{{ dinheiro(p.valor) }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </section>

        <section v-if="dados.documentoHtml" class="cartao">
          <h2>Documento</h2>
          <!-- HTML do servidor só dentro de iframe com sandbox (sem scripts) -->
          <iframe class="doc" sandbox="" :srcdoc="dados.documentoHtml" title="Documento da proposta"></iframe>
        </section>

        <section v-if="dados.podeResponder" class="cartao resposta">
          <template v-if="!modo">
            <h2>Sua resposta</h2>
            <div class="botoes">
              <button type="button" class="btn aceitar" @click="modo = 'aceitar'; erroEnvio = ''">Aceitar proposta</button>
              <button type="button" class="btn recusar" @click="modo = 'recusar'; erroEnvio = ''">Recusar</button>
            </div>
          </template>

          <form v-else class="form" @submit.prevent="enviar">
            <h2>{{ modo === 'aceitar' ? 'Confirmar aceite' : 'Recusar proposta' }}</h2>
            <template v-if="modo === 'aceitar'">
              <label><span>Seu nome *</span><input v-model="nome" type="text" maxlength="120" autocomplete="name" required /></label>
              <label><span>Seu e-mail (opcional)</span><input v-model="email" type="email" maxlength="160" autocomplete="email" /></label>
              <p class="dica">Ao confirmar, você aceita esta proposta nas condições acima.</p>
            </template>
            <label v-else><span>Motivo (opcional)</span><textarea v-model="motivo" rows="3" maxlength="1000"></textarea></label>
            <p v-if="erroEnvio" class="aviso erro" role="alert">{{ erroEnvio }}</p>
            <div class="botoes">
              <button type="button" class="btn" :disabled="enviando" @click="modo = null">Voltar</button>
              <button type="submit" class="btn" :class="modo === 'aceitar' ? 'aceitar' : 'recusar'" :disabled="enviando">
                {{ enviando ? 'Enviando...' : modo === 'aceitar' ? 'Confirmar aceite' : 'Confirmar recusa' }}
              </button>
            </div>
          </form>
        </section>
        <p v-else-if="!resultado" class="aviso">{{ ['Aceita', 'Recusada'].includes(dados.status ?? '') ? `Esta proposta já foi respondida (${dados.status}).` : 'Esta proposta não está recebendo respostas no momento.' }}</p>
      </template>
    </main>
  </div>
</template>

<style scoped>
/* Tema próprio e claro, independente do tema do aplicativo */
.publica {
  --p-bg: #f3f4f6; --p-card: #ffffff; --p-borda: #e5e7eb; --p-texto: #111827; --p-suave: #6b7280; --p-acento: #0e7490;
  min-height: 100vh; background: var(--p-bg); color: var(--p-texto); color-scheme: light;
  font-family: system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif; overflow-y: auto; position: fixed; inset: 0;
}
.pagina { max-width: 860px; margin: 0 auto; padding: 20px 14px 48px; display: flex; flex-direction: column; gap: 14px; }
.estado { text-align: center; color: var(--p-suave); margin-top: 60px; }
.cartao { background: var(--p-card); border: 1px solid var(--p-borda); border-radius: 12px; padding: 16px; }
.estado.cartao { margin-top: 60px; }
.topo { padding: 4px 2px; }
.empresa { margin: 0; font-size: 13px; font-weight: 600; color: var(--p-acento); text-transform: uppercase; letter-spacing: .05em; }
.numero { margin: 2px 0 0; font-size: 13px; color: var(--p-suave); }
h1 { margin: 4px 0; font-size: 24px; line-height: 1.25; overflow-wrap: anywhere; }
h2 { margin: 0 0 10px; font-size: 15px; }
.cliente { margin: 0; color: var(--p-suave); font-size: 14px; }
.selo { display: inline-block; margin: 8px 0 0; padding: 2px 10px; border-radius: 999px; background: #e0f2fe; color: #075985; font-size: 12px; font-weight: 600; }
.resumo { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 12px; }
.resumo small { display: block; font-size: 11px; color: var(--p-suave); text-transform: uppercase; letter-spacing: .04em; }
.resumo strong { font-size: 20px; font-variant-numeric: tabular-nums; }
.rolagem { overflow-x: auto; }
table { width: 100%; border-collapse: collapse; font-size: 14px; min-width: 420px; }
th { text-align: left; font-size: 11px; color: var(--p-suave); text-transform: uppercase; padding: 6px 8px; border-bottom: 1px solid var(--p-borda); }
td { padding: 8px; border-bottom: 1px solid var(--p-borda); }
.n { text-align: right; font-variant-numeric: tabular-nums; white-space: nowrap; }
.doc { width: 100%; height: 70vh; min-height: 360px; border: 1px solid var(--p-borda); border-radius: 8px; background: #fff; }
.form { display: flex; flex-direction: column; gap: 12px; }
.form label { display: flex; flex-direction: column; gap: 4px; font-size: 13px; color: var(--p-suave); }
.form input, .form textarea { padding: 10px; border: 1px solid #cbd5e1; border-radius: 8px; font-size: 16px; color: var(--p-texto); background: #fff; font-family: inherit; }
.form input:focus, .form textarea:focus { outline: 2px solid var(--p-acento); outline-offset: 0; }
.dica { margin: 0; font-size: 12px; color: var(--p-suave); }
.botoes { display: flex; gap: 10px; flex-wrap: wrap; }
.btn { flex: 1 1 140px; padding: 12px 16px; border-radius: 8px; border: 1px solid #cbd5e1; background: #fff; color: var(--p-texto); font-size: 15px; font-weight: 600; cursor: pointer; }
.btn:disabled { opacity: .6; cursor: default; }
.btn.aceitar { background: #15803d; border-color: #15803d; color: #fff; }
.btn.recusar { background: #fff; border-color: #b91c1c; color: #b91c1c; }
.aviso { margin: 0; padding: 10px 12px; border-radius: 8px; background: #f3f4f6; color: var(--p-suave); font-size: 14px; text-align: center; }
.aviso.ok { background: #dcfce7; color: #166534; }
.aviso.erro { background: #fee2e2; color: #991b1b; text-align: left; }
@media (max-width: 520px) { h1 { font-size: 20px; } .resumo strong { font-size: 18px; } }
</style>
