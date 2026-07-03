<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import logoAst from '@/assets/logo-menor.png'
import logoMobile from '@/assets/logo-menor.png'

const router = useRouter()
const auth = useAuthStore()

const login = ref('')
const senha = ref('')
const mostrarSenha = ref(false)
const carregando = ref(false)
const erro = ref('')

async function entrar() {
  erro.value = ''
  if (!login.value || !senha.value) {
    erro.value = 'Preencha todos os campos'
    return
  }

  carregando.value = true
  try {
    const response = await fetch('/api/Autenticacao/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ email: login.value, senha: senha.value })
    })

    if (!response.ok) {
      erro.value = 'Credenciais inválidas'
      return
    }

    const data = await response.json()
    auth.login({
      usuarioId: data.usuarioId,
      empresaId: data.empresaId,
      nome: data.nome,
      perfil: data.perfil,
      token: data.token,
      refreshToken: data.refreshToken,
    })
    router.push({ name: 'home' })
  } catch {
    erro.value = 'Erro ao conectar com o servidor'
  } finally {
    carregando.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <div class="login-container">
      <!-- Painel esquerdo -->
      <aside class="painel-hero">
        <div class="hero-glow"></div>
        <div class="hero-content">
          <img :src="logoAst" alt="AST Comercial" class="logo" />
          <h1>Consulte <span class="destaque">negócios</span>, gerencie regras e otimize suas vendas.</h1>
          <p class="descricao">CRM integrado ao seu dia a dia — funil de vendas, automações, propostas e muito mais.</p>
          <ul class="features">
            <li><span class="check-icon">✓</span> Funil de vendas em tempo real</li>
            <li><span class="check-icon">✓</span> Automações e integrações</li>
            <li><span class="check-icon">✓</span> Propostas e aprovações</li>
          </ul>
        </div>
      </aside>

      <!-- Painel do formulário -->
      <main class="painel-form">
        <div class="form-wrapper">
          <img :src="logoMobile" alt="AST Comercial" class="logo-mobile" />

          <div class="badge">
            <span class="badge-dot"></span>
            Acessando: <strong>AST Comercial</strong>
          </div>

          <h2>Bem-vindo(a)</h2>
          <p class="subtitulo">Faça login para acessar sua conta</p>

          <form @submit.prevent="entrar">
            <div class="campo">
              <label for="login">Email</label>
              <input
                id="login"
                v-model="login"
                type="email"
                placeholder="Digite seu email"
                autocomplete="username"
              />
            </div>

            <div class="campo">
              <label for="senha">Senha</label>
              <div class="campo-senha">
                <input
                  id="senha"
                  v-model="senha"
                  :type="mostrarSenha ? 'text' : 'password'"
                  placeholder="Digite sua senha"
                  autocomplete="current-password"
                />
                <button
                  type="button"
                  class="btn-olho"
                  :aria-label="mostrarSenha ? 'Ocultar senha' : 'Mostrar senha'"
                  @click="mostrarSenha = !mostrarSenha"
                >
                  <svg v-if="!mostrarSenha" xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg>
                  <svg v-else xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></svg>
                </button>
              </div>
            </div>

            <p v-if="erro" class="erro" role="alert">{{ erro }}</p>

            <button type="submit" class="btn-entrar" :disabled="carregando">
              {{ carregando ? 'Entrando...' : 'Entrar' }}
            </button>
          </form>

          <p class="rodape">Use as mesmas credenciais do seu acesso ao sistema</p>
          <p class="rodape-marca">AST Comercial · Iago Lucio</p>
        </div>
      </main>
    </div>
  </div>
</template>

<style scoped>
.login-page {
  position: fixed;
  inset: 0;
  background: #000;
  overflow: hidden;
}

.login-container {
  display: flex;
  width: 100%;
  height: 100vh;
}

/* === PAINEL HERO === */
.painel-hero {
  width: 55%;
  height: 100%;
  background: #000;
  position: relative;
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

/* Grid pattern sutil */
.painel-hero::before {
  content: '';
  position: absolute;
  inset: 0;
  background-image:
    linear-gradient(rgba(6, 182, 212, 0.03) 1px, transparent 1px),
    linear-gradient(90deg, rgba(6, 182, 212, 0.03) 1px, transparent 1px);
  background-size: 60px 60px;
  pointer-events: none;
}

/* Gradiente inferior */
.painel-hero::after {
  content: '';
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  height: 40%;
  background: linear-gradient(to top, rgba(6, 182, 212, 0.06), transparent);
  pointer-events: none;
}

.hero-glow {
  position: absolute;
  top: -150px;
  left: -100px;
  width: 700px;
  height: 700px;
  background: radial-gradient(circle, rgba(6, 182, 212, 0.12) 0%, rgba(6, 182, 212, 0.04) 40%, transparent 70%);
  pointer-events: none;
  animation: pulseGlow 8s ease-in-out infinite;
}

/* Segundo orb no canto inferior direito */
.hero-glow::after {
  content: '';
  position: absolute;
  bottom: -400px;
  right: -300px;
  width: 500px;
  height: 500px;
  background: radial-gradient(circle, rgba(139, 92, 246, 0.08) 0%, transparent 70%);
  pointer-events: none;
}

@keyframes pulseGlow {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.7; transform: scale(1.05); }
}

.hero-content {
  position: relative;
  z-index: 1;
  max-width: 520px;
  padding: 0 40px 0 40px;
}

.logo {
  width: 340px;
  height: auto;
  margin-bottom: 48px;
}

h1 {
  font-size: 40px;
  font-weight: 800;
  color: #ffffff;
  line-height: 1.25;
  margin-bottom: 20px;
  letter-spacing: -0.5px;
}

.destaque {
  color: #06b6d4;
}

.descricao {
  color: #666;
  font-size: 18px;
  margin-bottom: 40px;
  line-height: 1.7;
}

.features {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.features li {
  color: #888;
  font-size: 16px;
  display: flex;
  align-items: center;
  gap: 10px;
}

.check-icon {
  color: #06b6d4;
  font-weight: 700;
  font-size: 18px;
}

/* === PAINEL FORM === */
.painel-form {
  width: 45%;
  height: 100%;
  background: #000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 40px;
  position: relative;
  border-left: 1px solid #141414;
}

/* Linha gradiente na borda */
.painel-form::before {
  content: '';
  position: absolute;
  left: 0;
  top: 20%;
  bottom: 20%;
  width: 1px;
  background: linear-gradient(to bottom, transparent, #06b6d4, rgba(139, 92, 246, 0.5), transparent);
  pointer-events: none;
}

.form-wrapper {
  width: 360px;
}

.logo-mobile {
  display: none;
}

.badge {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  background: #0a0a0a;
  border: 1px solid #1a1a1a;
  border-radius: 100px;
  padding: 8px 16px;
  font-size: 13px;
  color: #888;
  margin-bottom: 32px;
}

.badge strong {
  color: #ccc;
}

.badge-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #06b6d4;
  box-shadow: 0 0 6px rgba(6, 182, 212, 0.5);
  display: inline-block;
}

h2 {
  font-size: 28px;
  font-weight: 700;
  color: #fff;
  margin-bottom: 6px;
  letter-spacing: -0.3px;
}

.subtitulo {
  color: #555;
  font-size: 15px;
  margin-bottom: 32px;
}

.campo {
  margin-bottom: 20px;
}

.campo label {
  display: block;
  color: #bbb;
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.campo input {
  width: 100%;
  background: #0a0a0a;
  border: 1px solid #1f1f1f;
  border-radius: 10px;
  padding: 14px 16px;
  color: #fff;
  font-size: 15px;
  outline: none;
  transition: border-color 0.2s, box-shadow 0.2s;
}

.campo input:focus {
  border-color: #06b6d4;
  box-shadow: 0 0 0 3px rgba(6, 182, 212, 0.1);
}

.campo input::placeholder {
  color: #333;
}

.campo-senha {
  position: relative;
}

.campo-senha input {
  padding-right: 44px;
}

.btn-olho {
  position: absolute;
  right: 14px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  cursor: pointer;
  color: #444;
  padding: 4px;
  display: flex;
  align-items: center;
  transition: color 0.2s;
}

.btn-olho:hover {
  color: #06b6d4;
}

.erro {
  color: #ef4444;
  font-size: 13px;
  margin-bottom: 10px;
  padding: 8px 12px;
  background: rgba(239, 68, 68, 0.08);
  border-radius: 6px;
  border: 1px solid rgba(239, 68, 68, 0.2);
}

.btn-entrar {
  width: 100%;
  background: #06b6d4;
  color: #000;
  border: none;
  border-radius: 10px;
  padding: 14px;
  font-size: 16px;
  font-weight: 700;
  cursor: pointer;
  transition: background 0.2s, box-shadow 0.2s, transform 0.1s;
  margin-top: 12px;
  letter-spacing: 0.2px;
}

.btn-entrar:hover:not(:disabled) {
  background: #22d3ee;
  box-shadow: 0 4px 20px rgba(6, 182, 212, 0.3);
  transform: translateY(-1px);
}

.btn-entrar:active:not(:disabled) {
  transform: translateY(0);
}

.btn-entrar:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.rodape {
  text-align: center;
  color: #444;
  font-size: 12px;
  margin-top: 28px;
}

.rodape-marca {
  text-align: center;
  color: #2a2a2a;
  font-size: 11px;
  margin-top: 8px;
}

/* === MOBILE === */
@media (max-width: 768px) {
  .login-page {
    position: relative;
    overflow: auto;
  }

  .login-container {
    flex-direction: column;
    height: auto;
    min-height: 100vh;
  }

  .painel-hero {
    display: none;
  }

  .painel-form {
    width: 100%;
    height: auto;
    min-height: 100vh;
    padding: 60px 28px 40px;
    border-left: none;
  }

  .logo-mobile {
    display: block;
    width: 100px;
    height: auto;
    margin-bottom: 32px;
  }

  .form-wrapper {
    width: 100%;
    max-width: 400px;
  }
}
</style>
