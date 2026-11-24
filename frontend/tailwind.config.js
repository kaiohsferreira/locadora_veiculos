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
        /** Cadastro de veículo (tela 05): ícones de cabeçalho, botão "Veículo Ativado!" e texto da trilha em foco. Retirada (tela 16): selo da situação, interruptor ligado e total previsto. */
        profundo: '#000E23',
        /** Cadastro de veículo (tela 05): hover do botão "Salvar como Rascunho". */
        marinho: '#002542',
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
        /** Cadastro de veículo (tela 05): botão principal e anel de foco do desenho. */
        acao: '#FE6A34',
      },
      /** Fundo das páginas. */
      offwhite: '#F7F8FA',
      /** Fundo dos cartões. */
      branco: '#FFFFFF',
      /** Cadastro de veículo (tela 05): fundo da barra fixa de ações (com 95% de opacidade). */
      superficie: '#F8F9FF',
      /** Cadastro de veículo (tela 05): cor decorativa de bordas, separadores, setas e etiquetas de seção. */
      contorno: '#74777E',
      /** Cadastro de veículo (tela 05): contorno tracejado da zona de envio de fotos (outline-variant do desenho). */
      'contorno-suave': '#C4C6CE',
      /** Amostras de cor externa do seletor (tela 05): as três que não estavam no tema; o branco é `branco`. */
      amostra: {
        preto: '#10141A',
        prata: '#BAC2CB',
        cinza: '#585E67',
      },
      texto: {
        DEFAULT: '#1A1A2E',
        suave: '#6B7789',
        /** Cadastro de veículo (tela 05): texto de campos, títulos de cartão e rótulos. */
        desenho: '#0B1C30',
        /** Cadastro de veículo (tela 05): descrições, prefixo "R$", sufixo "KM" e texto de apoio. */
        apoio: '#44474D',
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
      /**
       * Frota (telas 11 e 12, Gestão de Frota): verdes e laranja-escuro do desenho que não existiam no tema. Valores novos
       * da tela 11 (listados para o Kaio antes do commit). A tela 30 usa frota-oficina e frota-oficina-fundo.
       */
      frota: {
        disponivel: {
          DEFAULT: '#15803D',
          fundo: '#DCFCE7',
          ponto: '#22C55E',
        },
        oficina: {
          DEFAULT: '#AB3500',
          fundo: '#FFDBD0',
        },
      },
      /**
       * Documentos do cliente (tela 21): selo "Enviado" (verde do desenho, que não existia no tema). Valor novo
       * da tela 21, listado para o Kaio antes do commit. As outras telas não usam este token.
       */
      documento: {
        DEFAULT: '#065F46',
        fundo: '#D1FAE5',
      },
      /**
       * Manutenção da frota (tela 14): cores do desenho que não existiam no tema. Valores novos da tela 14, aprovados
       * pelo Kaio em 20/11/2026 ("Aprovo como está"). As outras telas não usam estes tokens.
       */
      manutencao: {
        /** Vermelho do alerta de revisão (iminente ou vencida): selo, barra, km na foto, botão Bloquear e "Crítico". */
        alerta: '#BA1A1A',
        /** Trilho da barra do alerta vermelho. */
        'alerta-trilho': '#FFDAD6',
        /** Hover do botão Bloquear p/ Oficina. */
        'alerta-escuro': '#93000A',
        /** Texto da etiqueta "Em Execução". */
        'execucao-texto': '#832600',
        /** Fundo da etiqueta "Concluída". */
        'concluida-fundo': '#D5E3FF',
        /** Barra superior e de progresso do cartão de alerta azul-cinza (neutro). */
        neutro: '#4D5F7C',
        /** Ícone de km do cartão de alerta azul-cinza (neutro). */
        'neutro-icone': '#A5CAF5',
      },
      /**
       * Pagamentos do cliente (tela 30): cores do desenho que não existiam no tema. Valores novos da tela 30, listados
       * para o Kaio antes do commit. As outras telas não usam estes tokens.
       */
      pagamento: {
        /** Texto do selo "Pendente" (on-secondary-fixed do desenho). */
        'texto-pendente': '#390C00',
        /** Texto do selo "Compensado" (on-primary-fixed do desenho). */
        'texto-compensado': '#061C36',
        /** Ícone de confirmado (emerald-600 do desenho). */
        sucesso: '#059669',
        /** Ponto do cartão de quitadas (emerald-500 do desenho). */
        'sucesso-ponto': '#10B981',
      },
      /**
       * Financeiro (tela 09, painel financeiro): cores do desenho que não existiam no tema. Valores novos da tela 09.
       * Aprovado pelo Kaio em 21/11/2026. As outras telas não usam estes tokens.
       */
      financeiro: {
        /** Selo "Venda" da coluna de origem (tertiary do desenho, #000F1F). */
        'origem-venda': '#000F1F',
        /** Hover do botão "Dar baixa" (on-secondary-container do desenho, #5D1900). Também o texto do selo "Em andamento" da tela 16 (retirada). */
        'acao-hover': '#5D1900',
      },
      /**
       * Retirada do veículo (tela 16): cores do desenho que não existiam no tema. Valores novos, aprovados pelo Kaio em 24/11/2026
       * (locacao.contract.json, tela_16.valoresNovos). Os outros tons da tela 16 reaproveitam tokens
       * que já existem (navy.profundo, frota.oficina, frota.disponivel, financeiro.acao-hover, steel.*, orange.acao, texto.*).
       */
      retirada: {
        /** Faixa azul "BRASIL" da placa Mercosul (bg-[#003399] do desenho, html :116). */
        'placa-faixa': '#003399',
        /** Ícone do selo da situação da reserva (text-secondary-fixed-dim do desenho, html :95). */
        'selo-icone': '#FFB59D',
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
        /** Minhas reservas (design 29): nome do veículo no cartão (18/24, negrito). Autorizado pelo Kaio em 16/11/2026. */
        'nome-veiculo': ['1.125rem', { lineHeight: '1.5rem', fontWeight: '700' }],
        /** Cadastro de veículo (tela 05): placa no campo (16/22, semibold). Autorizado pelo Kaio em 16/11/2026. */
        'titulo-md': ['1rem', { lineHeight: '1.375rem', fontWeight: '600', letterSpacing: '0em' }],
        /** Cadastro de veículo (tela 05): chassi, renavam, anos e quilometragem (13/18, medium). Autorizado pelo Kaio em 16/11/2026. */
        'dado-tabular': ['0.8125rem', { lineHeight: '1.125rem', fontWeight: '500', letterSpacing: '0.02em' }],
        /** Cadastro de veículo (tela 05): texto de ajuda sob os campos (12/16, com respiro de letra). Autorizado pelo Kaio em 16/11/2026. */
        apoio: ['0.75rem', { lineHeight: '1rem', fontWeight: '400', letterSpacing: '0.01em' }],
        /** Cadastro de veículo (tela 05): emblema "BR" da placa (9px). Autorizado pelo Kaio em 16/11/2026. */
        emblema: ['0.5625rem', { lineHeight: '0.875rem', fontWeight: '700' }],
        /** Frota (tela 11): chip de situação dentro do cartão (10/14, negrito). Valor novo da tela 11, listado para o Kaio. */
        'chip-frota': ['0.625rem', { lineHeight: '0.875rem', fontWeight: '700' }],
        /** Frota (tela 11): sigla "BRASIL" do emblema da placa (8/8, negrito). Valor novo da tela 11, listado para o Kaio. */
        'emblema-brasil': ['0.5rem', { lineHeight: '0.5rem', fontWeight: '700' }],
        /** Documentos do cliente (tela 21): título da página (30/38, seminegrito). Valor novo da tela 21, listado para o Kaio. */
        'titulo-documentos': ['1.875rem', { lineHeight: '2.375rem', fontWeight: '600', letterSpacing: '-0.015em' }],
        /** Pagamentos do cliente (tela 30): valor da cobrança e do cartão (18/24, bold). Valor novo da tela 30, listado para o Kaio. */
        'valor-destaque': ['1.125rem', { lineHeight: '1.5rem', fontWeight: '700', letterSpacing: '-0.005em' }],
      },
      borderRadius: {
        cartao: '8px',
      },
      boxShadow: {
        cartao: '0 1px 2px 0 rgb(16 36 62 / 0.06), 0 1px 3px 0 rgb(16 36 62 / 0.10)',
        elevado: '0 12px 32px -8px rgb(16 36 62 / 0.30)',
        /** Cadastro (design 20): sombra larga e suave do cartão principal. */
        destaque: '0 12px 40px -8px rgb(16 36 62 / 0.08), 0 2px 8px -2px rgb(16 36 62 / 0.04)',
        /** Cadastro de veículo (tela 05): sombra da barra fixa de ações, para cima. Autorizado pelo Kaio em 16/11/2026. */
        'barra-inferior': '0 -4px 16px rgba(0, 0, 0, 0.06)',
        /** Cadastro de veículo (tela 05): sombra laranja do botão principal (shadow-md com a cor do botão a 20%). Autorizado pelo Kaio em 16/11/2026. */
        'acao-laranja': '0 4px 6px -1px rgb(254 106 52 / 0.2), 0 2px 4px -2px rgb(254 106 52 / 0.2)',
      },
    },
  },
  plugins: [],
}
