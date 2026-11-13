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
        /** Texto de apoio sobre fundo escuro (ex.: linha sob a marca na tela de acesso). Passa AA sobre navy. */
        neblina: '#9DB0CC',
        /** Texto corrido claro sobre fundo escuro (ex.: parágrafo do painel da tela de acesso). */
        palido: '#B4C7E9',
        /** Cadastro (designs 20 e 21): fundo azulado mais claro (surface-container-low): cabeçalho do indicador, blocos e caixas. */
        fundo: '#EFF4FF',
        /** Cadastro: fundo azulado de faixas, etiquetas e ícones (surface-container). */
        suave: '#E5EEFF',
        /** Cadastro: círculo de etapa inativa e botão secundário em repouso (surface-container-high). */
        alto: '#DCE9FF',
        /** Cadastro: linha do indicador de etapas e trilho da barra de progresso (surface-variant). */
        linha: '#D3E4FE',
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
        /** Tela de acesso (design 01): título grande do painel da marca (36/44, bold). */
        'titulo-hero': ['2.25rem', { lineHeight: '2.75rem', fontWeight: '700', letterSpacing: '-0.02em' }],
        /** Tela de acesso: título do formulário (30/38, bold). */
        'titulo-acesso': ['1.875rem', { lineHeight: '2.375rem', fontWeight: '700', letterSpacing: '-0.015em' }],
        /** Tela de acesso: nome da marca (22/28, bold). */
        marca: ['1.375rem', { lineHeight: '1.75rem', fontWeight: '700', letterSpacing: '-0.01em' }],
        /** Tela de acesso: microtexto em caixa alta (11/14). */
        micro: ['0.6875rem', { lineHeight: '0.875rem', fontWeight: '500', letterSpacing: '0.03em' }],
        /** Tela de acesso: rótulo curto e link de apoio (12/16, semibold). */
        rotulo: ['0.75rem', { lineHeight: '1rem', fontWeight: '600', letterSpacing: '0.02em' }],
      },
      borderRadius: {
        cartao: '8px',
      },
      boxShadow: {
        cartao: '0 1px 2px 0 rgb(16 36 62 / 0.06), 0 1px 3px 0 rgb(16 36 62 / 0.10)',
        elevado: '0 12px 32px -8px rgb(16 36 62 / 0.30)',
        /** Cadastro (design 20): sombra larga e suave do cartão principal. */
        destaque: '0 12px 40px -8px rgb(16 36 62 / 0.08), 0 2px 8px -2px rgb(16 36 62 / 0.04)',
      },
    },
  },
  plugins: [],
}
