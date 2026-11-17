import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { Botao } from './Botao';
import { Campo } from './Campo';
import { Select } from './Select';

/**
 * Variantes novas da tela 05 (aditivas). Os dois grupos de teste abaixo congelam a marcação das variantes que já
 * existiam: se alguém mudar uma classe do padrão, a tela de outra funcionalidade muda junto, e o teste avisa.
 */

describe('Campo padrão: marcação de sempre (inalterada)', () => {
  it('o controle e o rótulo continuam com as classes antigas, sem prefixo nem sufixo', () => {
    const html = renderToStaticMarkup(<Campo rotulo="Nome" />);

    expect(html).toContain(
      'h-10 w-full rounded-md border bg-branco px-3 text-corpo-denso text-texto transition-colors placeholder:text-texto-suave focus:outline-none focus:ring-2 disabled:cursor-not-allowed disabled:bg-offwhite disabled:text-texto-suave border-borda-forte focus:border-steel focus:ring-steel/30',
    );
    expect(html).toContain('class="text-corpo-denso font-medium text-texto"');
    expect(html).not.toContain('R$');
    expect(html).not.toContain('bg-steel-fundo');
  });
});

describe('Select padrão: marcação de sempre (inalterada)', () => {
  it('a lista continua com a seta à direita em espaço de 40px e a cor steel da seta', () => {
    const html = renderToStaticMarkup(<Select rotulo="Status" opcoes={[{ valor: 'a', rotulo: 'A' }]} />);

    expect(html).toContain('appearance-none pr-10');
    expect(html).toContain('border-borda-forte focus:border-steel focus:ring-steel/30');
    expect(html).toContain('text-steel');
    expect(html).not.toContain('bg-steel-fundo');
  });
});

describe('Botao padrão: marcação de sempre (inalterada)', () => {
  it('a variante primária e o tamanho médio continuam com as classes antigas', () => {
    const html = renderToStaticMarkup(<Botao>Salvar</Botao>);

    expect(html).toContain(
      'inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-md font-semibold transition-colors disabled:cursor-not-allowed disabled:opacity-60 bg-orange text-branco enabled:hover:bg-orange-escuro h-10 px-4 text-corpo-denso',
    );
  });
});

describe('Campo variante desenho (tela 05)', () => {
  it('fundo azul claro sem borda, 40px de altura e canto de 4px, com o foco em anel laranja', () => {
    const html = renderToStaticMarkup(<Campo variante="desenho" rotulo="Nome" />);

    expect(html).toContain('h-10 w-full rounded bg-steel-fundo');
    expect(html).toContain('focus:bg-branco focus:outline-none focus:ring-2 focus:ring-orange-acao');
    expect(html).not.toContain('border-borda');
  });

  it('prefixo "R$" à esquerda com recuo de 44px, e sufixo "KM" à direita com recuo de 48px', () => {
    const html = renderToStaticMarkup(<Campo variante="desenho" rotulo="Diária" prefixo="R$" sufixo="KM" />);

    expect(html).toContain('>R$</span>');
    expect(html).toContain('>KM</span>');
    expect(html).toContain('pl-11');
    expect(html).toContain('pr-12');
  });

  it('sufixo sem recuo (ícone decorativo do chassi) mantém o recuo de 16px à direita', () => {
    const html = renderToStaticMarkup(<Campo variante="desenho" rotulo="Chassi" sufixo="v" sufixoSemRecuo />);

    expect(html).toContain('pr-4');
    expect(html).not.toContain('pr-12');
    expect(html).toContain('>v</span>');
  });

  it('prefixo largo (emblema da placa) recua 56px', () => {
    const html = renderToStaticMarkup(<Campo variante="desenho" rotulo="Placa" prefixo="BR" prefixoLargo />);

    expect(html).toContain('pl-14');
    expect(html).not.toContain('pl-11');
  });

  it('a fonte segue a tipografia pedida: dado (13/18), placa e valor (18/24)', () => {
    expect(renderToStaticMarkup(<Campo variante="desenho" rotulo="R" tipografia="dado" />)).toContain('text-dado-tabular');
    expect(renderToStaticMarkup(<Campo variante="desenho" rotulo="R" tipografia="placa" />)).toContain('text-titulo-md');
    expect(renderToStaticMarkup(<Campo variante="desenho" rotulo="R" tipografia="valor" />)).toContain('text-nome-veiculo');
  });

  it('erro aparece com anel vermelho e a mensagem é anunciada', () => {
    const html = renderToStaticMarkup(<Campo variante="desenho" rotulo="Nome" erro="Campo inválido." />);

    expect(html).toContain('ring-2 ring-status-perigo');
    expect(html).toContain('role="alert"');
    expect(html).toContain('Campo inválido.');
  });

  it('obrigatório com ocultarAsterisco: o aria-required fica, e o asterisco visual não aparece', () => {
    const html = renderToStaticMarkup(<Campo variante="desenho" rotulo="Chassi" obrigatorio ocultarAsterisco />);

    expect(html).toContain('aria-required="true"');
    expect(html).not.toContain('>*</span>');
  });

  it('apoio à direita do rótulo sai em micro texto laranja, como o desenho', () => {
    const html = renderToStaticMarkup(<Campo variante="desenho" rotulo="Placa" apoioRotulo="Padrão Brasil" />);

    expect(html).toContain('text-micro font-normal text-status-pendente');
  });
});

describe('Select variante desenho (tela 05)', () => {
  it('sem borda, com fundo azul claro e espaço de 40px para a seta', () => {
    const html = renderToStaticMarkup(
      <Select variante="desenho" rotulo="Marca" opcoes={[{ valor: 'a', rotulo: 'A' }]} />,
    );

    expect(html).toContain('appearance-none rounded bg-steel-fundo px-4 pr-10');
    expect(html).not.toContain('border-borda');
    expect(html).toContain('text-contorno');
  });

  it('o destaque (status) usa o laranja de pendente e seminegrito', () => {
    const html = renderToStaticMarkup(
      <Select variante="desenho" destaque rotulo="Status" opcoes={[{ valor: 'a', rotulo: 'A' }]} />,
    );

    expect(html).toContain('font-semibold text-status-pendente');
  });

  it('ocultarAsterisco esconde o asterisco e mantém aria-required', () => {
    const html = renderToStaticMarkup(
      <Select variante="desenho" rotulo="Marca" obrigatorio ocultarAsterisco opcoes={[]} />,
    );

    expect(html).toContain('aria-required="true"');
    expect(html).not.toContain('>*</span>');
  });
});

describe('Botao variantes da tela 05', () => {
  it('cinza-azulado: 40px, canto de 2px e fundo steel-suave, sem as cores do primário', () => {
    const html = renderToStaticMarkup(<Botao variante="cinzaAzulado" icone="arrow_back">Cancelar / Voltar</Botao>);

    expect(html).toContain('h-10 items-center justify-center gap-1');
    expect(html).toContain('rounded-sm');
    expect(html).toContain('bg-steel-suave px-4 text-texto-desenho enabled:hover:bg-steel-alto');
    expect(html).not.toContain('bg-orange');
    expect(html).toContain('>arrow_back</span>');
  });

  it('laranja-acao: fundo laranja do desenho, recuo de 24px, sombra laranja e texto em negrito', () => {
    const html = renderToStaticMarkup(<Botao variante="laranjaAcao" icone="check_circle">Salvar</Botao>);

    expect(html).toContain('bg-orange-acao px-6 font-bold text-branco shadow-acao-laranja');
    expect(html).toContain('enabled:hover:bg-status-pendente');
  });

  it('azul-marinho e azul-profundo têm as cores do desenho', () => {
    expect(renderToStaticMarkup(<Botao variante="azulMarinho">R</Botao>)).toContain('bg-navy px-4 text-branco enabled:hover:bg-navy-marinho');
    expect(renderToStaticMarkup(<Botao variante="azulProfundo">R</Botao>)).toContain('bg-navy-profundo');
  });

  it('as variantes do desenho ignoram o tamanho e usam o ícone de 18px', () => {
    const html = renderToStaticMarkup(
      <Botao variante="azulMarinho" tamanho="grande" icone="bookmark">R</Botao>,
    );

    expect(html).not.toContain('h-12');
    expect(html).toContain('font-size:18px');
  });
});
