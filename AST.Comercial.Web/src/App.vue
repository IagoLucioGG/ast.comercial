<script setup lang="ts">
import { RouterView } from 'vue-router'
</script>

<template>
  <RouterView />
</template>

<style>
:root {
  --bg-primary: #000000;
  --bg-secondary: #080808;
  --bg-surface: #111111;
  --bg-elevated: #1a1a1a;
  --border: #262626;
  --text-primary: #fafafa;
  --text-secondary: #a3a3a3;
  --text-muted: #525252;
  --accent: #06b6d4;
  --accent-hover: #22d3ee;
  --danger: #ef4444;
  color-scheme: dark;
}

[data-theme="light"] {
  --bg-primary: #ffffff;
  --bg-secondary: #f5f5f5;
  --bg-surface: #fafafa;
  --bg-elevated: #ffffff;
  --border: #e5e5e5;
  --text-primary: #171717;
  --text-secondary: #525252;
  --text-muted: #a3a3a3;
  --accent: #0891b2;
  --accent-hover: #06b6d4;
  --danger: #dc2626;
  color-scheme: light;
}

*,
*::before,
*::after {
  margin: 0;
  padding: 0;
  box-sizing: border-box;
}

html {
  -webkit-text-size-adjust: 100%;
}

body {
  font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
  background: var(--bg-primary);
  color: var(--text-primary);
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
  line-height: 1.5;
  min-height: 100vh;
  min-height: 100dvh;
}

::-webkit-scrollbar { width: 6px; height: 6px; }
::-webkit-scrollbar-track { background: transparent; }
::-webkit-scrollbar-thumb { background: var(--border); border-radius: 3px; }
::-webkit-scrollbar-thumb:hover { background: var(--text-muted); }
::-webkit-scrollbar-corner { background: transparent; }
* { scrollbar-width: thin; scrollbar-color: var(--border) transparent; }
</style>
