import { ref, watch } from 'vue'
import { defineStore } from 'pinia'

export const useTemaStore = defineStore('tema', () => {
  const dark = ref(localStorage.getItem('tema') !== 'light')

  watch(dark, (v) => {
    document.documentElement.setAttribute('data-theme', v ? 'dark' : 'light')
    localStorage.setItem('tema', v ? 'dark' : 'light')
  }, { immediate: true })

  function alternar() {
    dark.value = !dark.value
  }

  return { dark, alternar }
})
