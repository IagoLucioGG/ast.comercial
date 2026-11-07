<script setup lang="ts">
import { ref, watch, onMounted } from 'vue'
import { odataRequest, montarQuery } from '@/services/api'
import { campo, lista, mensagemErro, formatarDataHora, type Registro } from '@/composables/useEntidadeDetalhe'
import DetalheEstado from '@/components/detalhe/DetalheEstado.vue'

/**
 * Histórico simples do produto (odata/RegistrosAlteracao). Não usa TimelineEntidade
 * porque ela só aceita Cliente/Negócio/Contato; aqui basta quem, quando e o quê.
 */
const props = defineProps<{ produtoId: number }>()

const itens = ref<Registro[]>([])
const carregando = ref(false)
const erro = ref('')

const ACOES: Record<string, string> = { Criar: 'Criou o produto', Criacao: 'Criou o produto', Alteracao: 'Alterou o produto', Atualizar: 'Alterou o produto', Atualizacao: 'Alterou o produto', Excluir: 'Excluiu o produto', Exclusao: 'Excluiu o produto' }

async function carregar() {
  carregando.value = true
  erro.value = ''
  try {
    const q = montarQuery({
      $filter: `EntidadeAlvo eq 'Produto' and RegistroId eq ${props.produtoId}`,
      $orderby: 'DataHora desc,Id desc',
      $select: 'Id,Acao,UsuarioNome,Usuario,DataHora',
      $top: 50,
    })
    itens.value = lista<Registro>(await odataRequest<unknown>('GET', `RegistrosAlteracao${q}`))
  } catch (e) {
    erro.value = mensagemErro(e, 'Não foi possível carregar o histórico')
  } finally {
    carregando.value = false
  }
}

watch(() => props.produtoId, carregar)
onMounted(carregar)
</script>

<template>
  <DetalheEstado :carregando="carregando" :erro="erro" :vazio="!itens.length" texto-vazio="Nenhuma alteração registrada." icone="mdi-history" @tentar="carregar">
    <ol class="ph-lista">
      <li v-for="h in itens" :key="campo(h, 'Id')">
        <i class="mdi mdi-circle-small" aria-hidden="true"></i>
        <div>
          <strong>{{ ACOES[String(campo(h, 'Acao'))] ?? campo(h, 'Acao') }}</strong>
          <span> por {{ campo(h, 'UsuarioNome') || campo(h, 'Usuario') || 'sistema' }}</span>
          <small>{{ formatarDataHora(campo(h, 'DataHora')) }}</small>
        </div>
      </li>
    </ol>
  </DetalheEstado>
</template>

<style scoped>
.ph-lista { list-style: none; margin: 0; padding: 0; background: var(--bg-surface); border: 1px solid var(--border); border-radius: 10px; }
.ph-lista li { display: flex; align-items: flex-start; gap: 4px; padding: 10px 14px; border-bottom: 1px solid var(--border); font-size: 13px; color: var(--text-secondary); }
.ph-lista li:last-child { border-bottom: 0; }
.ph-lista .mdi { color: var(--accent); font-size: 22px; line-height: 1; }
.ph-lista strong { color: var(--text-primary); font-weight: 600; }
.ph-lista small { display: block; color: var(--text-muted); font-size: 11px; margin-top: 2px; }
</style>
