import { fileURLToPath, URL } from 'node:url'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// URL com esquema https e host não vazio ("https://" sozinho não passa).
function ehUrlHttpsValida(valor: string): boolean {
  try {
    const url = new URL(valor)
    return url.protocol === 'https:' && url.hostname !== ''
  } catch {
    return false
  }
}

// Trava de publicação: no Vercel (variável VERCEL definida pelo próprio Vercel) o build de produção
// só passa com VITE_API_URL começando por https://. Sem isso, o site iria ao ar sem conseguir falar com a API.
// Fora do Vercel (máquina do desenvolvedor, testes) esta checagem não roda.
function exigirApiUrlNoVercel(modo: string): void {
  if (!process.env.VERCEL || modo !== 'production') return

  const { VITE_API_URL } = loadEnv(modo, process.cwd(), 'VITE_')
  const apiUrl = (VITE_API_URL ?? '').trim()
  if (!ehUrlHttpsValida(apiUrl)) {
    throw new Error(
      'Build interrompido no Vercel: a variável VITE_API_URL não está definida ou não começa com https://. ' +
        'Defina-a em Project Settings > Environment Variables com a URL https da API (sem barra no fim) e faça o deploy de novo.',
    )
  }
}

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  exigirApiUrlNoVercel(mode)

  return {
    plugins: [react()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      port: 5173,
    },
  }
})
