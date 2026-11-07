import { createRouter, createWebHistory } from 'vue-router'
import AppLayout from '@/layouts/AppLayout.vue'

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
        { path: 'clientes', name: 'clientes', component: () => import('../views/ClientesView.vue') },
        { path: 'clientes/:id(\\d+)', name: 'cliente-detalhe', component: () => import('../views/ClienteDetalheView.vue') },
        { path: 'contatos', name: 'contatos', component: () => import('../views/ContatosView.vue') },
        { path: 'negocios', name: 'negocios', component: () => import('../views/NegociosView.vue') },
        { path: 'negocios/:id(\\d+)', name: 'negocio-detalhe', component: () => import('../views/NegocioDetalheView.vue') },
        { path: 'atividades', name: 'atividades', component: () => import('../views/AtividadesView.vue') },
        { path: 'propostas', name: 'propostas', component: () => import('../views/PropostasView.vue') },
        { path: 'propostas/:id(\\d+)', name: 'proposta-detalhe', component: () => import('../views/PropostaDetalheView.vue') },
        { path: 'produtos', name: 'produtos', component: () => import('../views/ProdutosView.vue') },
        { path: 'relatorios', name: 'relatorios', component: () => import('../views/HomeView.vue') },
        { path: 'admin', name: 'admin', component: () => import('../views/AdminView.vue'),
          children: [
            { path: ':painel', name: 'admin-painel', component: () => import('../views/AdminView.vue') },
          ]
        },
        { path: 'admin/modelo-documento/:id', name: 'editor-modelo', component: () => import('../views/EditorModeloView.vue') },
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

router.beforeEach((to) => {
  const token = localStorage.getItem('token')

  if (!to.meta.public && !token) {
    return { name: 'login' }
  }

  if (to.name === 'login' && token) {
    return { name: 'home' }
  }
})

export default router
