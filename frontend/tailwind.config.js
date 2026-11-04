/**
 * Design system da AutoVia.
 *
 * A paleta padrão do Tailwind foi SUBSTITUÍDA (theme.colors, não theme.extend.colors):
 * só existem as cores abaixo. Use sempre o token (bg-navy, text-texto-suave,
 * bg-status-sucesso-fundo...) e nunca uma cor literal no componente.
 *
 * Espaçamento: grade de 8px. Prefira as classes pares do Tailwind
 * (2 = 8px, 4 = 16px, 6 = 24px, 8 = 32px, 12 = 48px).
 *
 * @type {import('tailwindcss').Config}
 */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    colors: {
      transparent: 'transparent',
      current: 'currentColor',
      inherit: 'inherit',

      /** Cor principal: barra lateral do back office, cabeçalhos e títulos. */
      navy: {
        DEFAULT: '#10243E',
        claro: '#1C3A5E',
        escuro: '#0A1829',
      },
      /** Cor secundária: ícones e elementos de apoio. */
      steel: {
        DEFAULT: '#5C80A8',
        claro: '#E3EBF5',
      },
      /** Apenas ações primárias, estados ativos e destaques. */
      orange: {
        DEFAULT: '#FF6B35',
        escuro: '#E5551F',
        claro: '#FFF0EA',
      },
      /** Fundo das páginas. */
      offwhite: '#F7F8FA',
      /** Fundo dos cartões. */
      branco: '#FFFFFF',
      texto: {
        DEFAULT: '#1A1A2E',
        suave: '#6B7789',
      },
      borda: {
        DEFAULT: '#E1E5EB',
        forte: '#C9D1DC',
      },
      /**
       * Tons de status. Sempre usados junto com TEXTO (componente Etiqueta),
       * nunca como única forma de informação.
       */
      status: {
        /** Verde: concluído ou disponível. */
        sucesso: {
          DEFAULT: '#17663A',
          fundo: '#E3F4EA',
        },
        /** Laranja: pendente ou em andamento. */
        pendente: {
          DEFAULT: '#A3400B',
          fundo: '#FFEDE3',
        },
        /** Vermelho: atrasado ou bloqueado. */
        perigo: {
          DEFAULT: '#B42318',
          escuro: '#912018',
          fundo: '#FDE8E7',
        },
        /** Cinza: cancelado. */
        neutro: {
          DEFAULT: '#4B5563',
          fundo: '#EEF0F3',
        },
      },
    },
    extend: {
      fontFamily: {
        sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'],
      },
      fontSize: {
        /** Título de página: semibold grande. */
        titulo: ['1.75rem', { lineHeight: '2.25rem', fontWeight: '600', letterSpacing: '-0.01em' }],
        /** Título de seção: médio. */
        secao: ['1.25rem', { lineHeight: '1.75rem', fontWeight: '600' }],
        /** Corpo confortável (área pública). */
        corpo: ['1rem', { lineHeight: '1.5rem' }],
        /** Corpo denso (tabelas e formulários do back office). */
        'corpo-denso': ['0.875rem', { lineHeight: '1.25rem' }],
        /** Legendas, rótulos de coluna e etiquetas. */
        legenda: ['0.75rem', { lineHeight: '1rem' }],
      },
      borderRadius: {
        cartao: '8px',
      },
      boxShadow: {
        cartao: '0 1px 2px 0 rgb(16 36 62 / 0.06), 0 1px 3px 0 rgb(16 36 62 / 0.10)',
        elevado: '0 12px 32px -8px rgb(16 36 62 / 0.30)',
      },
    },
  },
  plugins: [],
}
