import { createRouter, createWebHistory } from 'vue-router'
import AppLayout from '@/layouts/AppLayout.vue'
import { usePermissoes } from '@/composables/usePermissoes'
import { useToast } from '@/composables/useToast'

declare module 'vue-router' {
  interface RouteMeta {
    public?: boolean
    /** Chave da entidade do catálogo de permissões: sem permissão de Ver, a rota é barrada. */
    entidade?: string
    /** Nome da tela, usado na mensagem de bloqueio. */
    titulo?: string
    /** Só o perfil Administrador entra. */
    somenteAdmin?: boolean
  }
}

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('../views/LoginView.vue'),
      meta: { public: true },
    },
    {
      path: '/',
      component: AppLayout,
      children: [
        { path: '', name: 'home', component: () => import('../views/HomeView.vue') },
        { path: 'clientes', name: 'clientes', component: () => import('../views/ClientesView.vue'), meta: { entidade: 'Cliente', titulo: 'Clientes' } },
        { path: 'clientes/:id(\\d+)', name: 'cliente-detalhe', component: () => import('../views/ClienteDetalheView.vue'), meta: { entidade: 'Cliente', titulo: 'Clientes' } },
        { path: 'contatos', name: 'contatos', component: () => import('../views/ContatosView.vue'), meta: { entidade: 'PessoaContato', titulo: 'Contatos' } },
        { path: 'negocios', name: 'negocios', component: () => import('../views/NegociosView.vue'), meta: { entidade: 'Negocio', titulo: 'Negócios' } },
        { path: 'negocios/:id(\\d+)', name: 'negocio-detalhe', component: () => import('../views/NegocioDetalheView.vue'), meta: { entidade: 'Negocio', titulo: 'Negócios' } },
        { path: 'atividades', name: 'atividades', component: () => import('../views/AtividadesView.vue'), meta: { entidade: 'Atividade', titulo: 'Atividades' } },
        { path: 'propostas', name: 'propostas', component: () => import('../views/PropostasView.vue'), meta: { entidade: 'Proposta', titulo: 'Propostas' } },
        { path: 'propostas/:id(\\d+)', name: 'proposta-detalhe', component: () => import('../views/PropostaDetalheView.vue'), meta: { entidade: 'Proposta', titulo: 'Propostas' } },
        { path: 'pedidos', name: 'pedidos', component: () => import('../views/PedidosView.vue'), meta: { entidade: 'Pedido', titulo: 'Pedidos' } },
        { path: 'pedidos/:id(\\d+)', name: 'pedido-detalhe', component: () => import('../views/PedidoDetalheView.vue'), meta: { entidade: 'Pedido', titulo: 'Pedidos' } },
        { path: 'produtos', name: 'produtos', component: () => import('../views/ProdutosView.vue'), meta: { entidade: 'Produto', titulo: 'Produtos' } },
        { path: 'produtos/:id(\\d+)', name: 'produto-detalhe', component: () => import('../views/ProdutoDetalheView.vue'), meta: { entidade: 'Produto', titulo: 'Produtos' } },
        { path: 'relatorios', name: 'relatorios', component: () => import('../views/RelatoriosView.vue'), meta: { entidade: 'PainelRelatorio', titulo: 'Relatórios' } },
        { path: 'relatorios/:id(\\d+)', name: 'relatorio-painel', component: () => import('../views/RelatoriosView.vue'), meta: { entidade: 'PainelRelatorio', titulo: 'Relatórios' } },
        { path: 'metas', name: 'metas', component: () => import('../views/MetasView.vue'), meta: { entidade: 'Meta', titulo: 'Metas' } },
        // Caixa de aprovações: aberta a qualquer usuário (o servidor confere se ele é aprovador).
        { path: 'aprovacoes', name: 'aprovacoes', component: () => import('../views/AprovacoesView.vue') },
        { path: 'admin', name: 'admin', component: () => import('../views/AdminView.vue'), meta: { somenteAdmin: true, titulo: 'Administração' },
          children: [
            { path: ':painel', name: 'admin-painel', component: () => import('../views/AdminView.vue') },
          ]
        },
        { path: 'admin/modelo-documento/:id', name: 'editor-modelo', component: () => import('../views/EditorModeloView.vue'), meta: { somenteAdmin: true, titulo: 'Administração' } },
      ],
    },
    {
      // Página pública de aceite da proposta (sem login e sem layout)
      path: '/p/:chave',
      name: 'proposta-publica',
      component: () => import('../views/PropostaPublicaView.vue'),
      meta: { public: true },
    },
    {
      path: '/:pathMatch(.*)*',
      redirect: '/',
    },
  ],
})

router.beforeEach(async (to) => {
  const token = localStorage.getItem('token')

  if (!to.meta.public && !token) {
    return { name: 'login' }
  }

  if (to.name === 'login' && token) {
    return { name: 'home' }
  }

  // Permissões do perfil: rota sem permissão de Ver volta ao Resumo com aviso.
  // O servidor também barra (403); isto só evita abrir telas que não vão funcionar.
  if (token && !to.meta.public) {
    const perm = usePermissoes()
    await perm.carregar()
    if (perm.carregado.value) {
      const bloqueado =
        (to.meta.somenteAdmin === true && !perm.administrador.value) ||
        (!!to.meta.entidade && !perm.pode(to.meta.entidade, 'ver'))
      if (bloqueado) {
        useToast().warning(`Sem permissão para acessar ${to.meta.titulo ?? 'esta tela'}.`)
        return { name: 'home' }
      }
    }
  }
})

export default router
