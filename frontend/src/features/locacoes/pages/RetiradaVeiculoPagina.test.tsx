import { beforeEach, describe, expect, it, vi } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { AvisoProvider } from '@/components/ui';
import { ErroApi } from '@/types/api';
import { CARGO_FUNCIONARIO, type CargoFuncionario } from '@/types/enums';
import type { ContextoRetirada } from '@/types/retirada';
import { useAbrirLocacao, useRetirada } from '../hooks/useRetirada';
import { contextoRetiradaComCaucaoTeste as comCaucao, contextoRetiradaTeste as contexto } from '../retirada/contextoRetirada.fixture';
import type { EscolhaSimulacaoCaucao, MemoriaCaucaoRetirada, TentativaCaucao } from '../retirada/regrasCaucaoRetirada';
import { termosCortadosPresentesRetirada, termosDaCaucaoPresentesRetirada } from '../retirada/termosCortadosRetirada';
import type { ConferenciaRetirada } from '../retirada/validacaoRetirada';
import RetiradaVeiculoPagina, { ConteudoRetirada } from './RetiradaVeiculoPagina';

/** Cargo da sessão do dublê: ATENDENTE por padrão (volta a ele antes de cada teste); null = sessão sem cargo (ajuste A1). */
const sessao = vi.hoisted(() => ({ cargo: 'ATENDENTE' as CargoFuncionario | null }));

vi.mock('../hooks/useRetirada', () => ({ useRetirada: vi.fn(), useAbrirLocacao: vi.fn() }));
// Sem AutenticacaoProvider nos testes: o nome e o cargo do funcionário logado vêm de um dublê (usePermissoes lê dele).
vi.mock('@/routes/autenticacao/useAutenticacao', () => ({
  useAutenticacao: () => ({ usuario: { nome: 'Ana Souza', perfil: 'funcionario', cargo: sessao.cargo ?? undefined } }),
}));

beforeEach(() => { sessao.cargo = 'ATENDENTE'; });

type Consulta = ReturnType<typeof useRetirada>;
type Registro = ReturnType<typeof useAbrirLocacao>;

const NADA: ConferenciaRetirada = { cnhOriginal: false, titularidadeCartao: false, comprovanteResidencia: false, termoMultas: false };
/** Os três atestados de hoje ligados (sem caução a titularidade não existe na tela). */
const TUDO: ConferenciaRetirada = { cnhOriginal: true, titularidadeCartao: false, comprovanteResidencia: true, termoMultas: true };
/** Os quatro atestados ligados (caução exigida). */
const TUDO_COM_CAUCAO: ConferenciaRetirada = { ...TUDO, titularidadeCartao: true };
const TEXTO_BOTAO = 'Iniciar locação e ir para vistoria';
const TEXTO_INDISPONIVEL = 'A retirada exige uma reserva ativa ou confirmada e um veículo reservado. Atualize os dados ou volte às retiradas.';
const VOLTAR = 'Voltar às retiradas';
const ENDERECO_ENTRADA = 'href="/back-office/locacoes/retirada"';
/** Caução indisponível: o texto inteiro (só no item da pré-autorização) e o curto (só na caixa da esquerda; ajuste A4). */
const CAUCAO_INDISPONIVEL = 'Pré-autorização indisponível: o provedor de pagamento não está liberado para a caução. A locação não pode ser iniciada.';
const CAUCAO_INDISPONIVEL_CURTO = 'Pré-autorização indisponível.';
/** Aviso do cargo sem permissão com a caução exigida (ajuste A1). */
const CAUCAO_CARGO_SEM_PERMISSAO = 'Com caução, só atendente ou administrador inicia a locação.';
/** Complemento do 409 com a caução exigida e a retirada ainda disponível (encurtado e aprovado em 24/11/2026). */
const CAUCAO_TENTE_NOVAMENTE = 'Escolha de novo o resultado da simulação e tente outra vez.';
/** Cargos que NÃO iniciam a locação com a caução exigida, e a sessão sem cargo. */
const SEM_PERMISSAO_COM_CAUCAO: readonly (CargoFuncionario | null)[] = ['VENDEDOR', 'GESTOR_FROTA', 'FINANCEIRO', 'ADMINISTRATIVO', null];

function mockarHooks(parcial: Partial<Consulta>, registro: Partial<Registro>) {
  vi.mocked(useRetirada).mockReturnValue({ isPending: false, isError: false, isFetching: false, refetch: vi.fn(), data: contexto, ...parcial } as unknown as Consulta);
  vi.mocked(useAbrirLocacao).mockReturnValue({ isPending: false, isError: false, mutateAsync: vi.fn(), ...registro } as unknown as Registro);
}

function renderizarPagina(
  parcial: Partial<Consulta> = {},
  id = '8',
  registro: Partial<Registro> = {},
  memoriaInicial: MemoriaCaucaoRetirada | null = null,
): string {
  mockarHooks(parcial, registro);
  return renderToStaticMarkup(
    <AvisoProvider>
      <MemoryRouter initialEntries={[`/retirada/${id}`]}>
        <Routes><Route path="/retirada/:reservaId" element={<RetiradaVeiculoPagina memoriaInicial={memoriaInicial} />} /></Routes>
      </MemoryRouter>
    </AvisoProvider>,
  );
}

/** Conteúdo da tela com a escolha e a tentativa vindas por props, como a página passa. */
function renderizarConteudo(
  dados: Partial<ContextoRetirada> = {},
  valoresIniciais: ConferenciaRetirada = NADA,
  registro: Partial<Registro> = {},
  atualizando = false,
  escolha: EscolhaSimulacaoCaucao | null = null,
  tentativaAnterior: TentativaCaucao | null = null,
): string {
  mockarHooks({}, registro);
  return renderToStaticMarkup(
    <AvisoProvider>
      <MemoryRouter>
        <ConteudoRetirada
          contexto={{ ...contexto, ...dados }}
          atualizando={atualizando}
          valoresIniciais={valoresIniciais}
          escolha={escolha}
          aoEscolher={vi.fn()}
          tentativaAnterior={tentativaAnterior}
          aoRegistrarTentativa={vi.fn()}
        />
      </MemoryRouter>
    </AvisoProvider>,
  );
}

/** Conteúdo com a caução exigida, disponível e em simulação (fixture com caução por cima dos dados de hoje). */
function renderizarComCaucao(
  dados: Partial<ContextoRetirada> = {},
  valoresIniciais: ConferenciaRetirada = NADA,
  registro: Partial<Registro> = {},
  escolha: EscolhaSimulacaoCaucao | null = null,
  atualizando = false,
  tentativaAnterior: TentativaCaucao | null = null,
): string {
  return renderizarConteudo({ ...comCaucao, ...dados }, valoresIniciais, registro, atualizando, escolha, tentativaAnterior);
}

/** Quantas vezes o texto aparece no HTML. */
function contar(html: string, texto: string): number {
  return html.split(texto).length - 1;
}

/** A linha do erro da abertura: o <div role="alert"> embaixo da lista, com o texto do servidor e o complemento. */
function linhaDoErro(html: string): string {
  const inicio = html.indexOf('<div role="alert"');
  return inicio < 0 ? '' : html.slice(inicio, html.indexOf('</div>', inicio));
}

/** Tag do rádio de uma saída da simulação (a ordem dos atributos é a do React, não a do JSX). */
function radioDaSaida(html: string, valor: EscolhaSimulacaoCaucao): string {
  return (html.match(/<input [^>]*>/g) ?? []).find((tag) => tag.includes(`value="${valor}"`)) ?? '';
}

/** Trecho do item da pré-autorização: do título até o fim da lista. */
function itemPreAutorizacao(html: string): string {
  const inicio = html.indexOf('Pré-autorização da caução (');
  return html.slice(inicio, html.indexOf('</ul>', inicio));
}

/** Abertura da tag do botão que contém o texto (o texto vem antes do ícone da direita). */
function botaoComTexto(html: string, texto: string): string {
  const fim = html.indexOf(texto);
  const inicio = html.lastIndexOf('<button', fim);
  return html.slice(inicio, fim);
}

/** Abertura da tag do link que contém o texto, com o ícone da esquerda (o ícone vem antes do texto). */
function linkComTexto(html: string, texto: string): string {
  const fim = html.indexOf(texto);
  const inicio = html.lastIndexOf('<a ', fim);
  return html.slice(inicio, fim);
}

describe('tela 16: retirada do veículo', () => {
  beforeEach(() => vi.clearAllMocks());

  it('estado inicial: título, subtítulo, trilha, passo a passo, dados reais e conferência desmarcada', () => {
    const html = renderizarPagina();
    for (const texto of [
      'Retirada do veículo',
      'Conferência inicial de documentação e validação de reserva para entrega das chaves.',
      'Despacho', '#AV-00008', 'Reserva Ativa', 'Grupo SUV Médio', 'Jeep Renegade Longitude', '2024', 'BRASIL', 'BRA2E19',
      'Cliente de teste', '123.***.***-09', '*******1234', 'Categoria:', '18/11/2030', 'Cadastro nesta locadora',
      '18/11/2026 → 22/11/2026', '4 diárias completas contratadas', 'AutoVia Congonhas', 'Rua Exemplo, 100 - Centro, São Paulo - SP',
      'Diária:', 'R$ 120,00', 'Total previsto', 'R$ 480,00', 'Checklist de Verificação Documental e Presencial', '0/3 Confirmados',
      'Etapa 1', 'Em andamento', 'Quilometragem atual', '12.000 km', 'Atualizar dados',
    ]) expect(html).toContain(texto);
    expect(html).toContain('href="/back-office/locacoes"');
    expect(html.match(/role="switch"/g)).toHaveLength(3);
    expect(html.match(/aria-checked="false"/g)).toHaveLength(3);
    expect(html).toContain('aria-current="step"');
    expect(html).not.toContain('Pronto para despacho de pátio');
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
    expect(html).not.toContain('type="file"');
    expect(html).not.toContain(VOLTAR);
  });

  it('botão principal desligado fica apagado (disabled:opacity-60) em 0/3 e no indisponível, com sombra e hover do desenho', () => {
    const inicial = botaoComTexto(renderizarPagina(), TEXTO_BOTAO);
    const indisponivel = botaoComTexto(renderizarPagina({ data: { ...contexto, statusVeiculo: 'DISPONIVEL' } }), TEXTO_BOTAO);
    for (const botao of [inicial, indisponivel]) {
      expect(botao).toContain('disabled=""');
      expect(botao).toContain('disabled:opacity-60');
    }
    for (const classe of ['gap-2', '!shadow-md', 'active:scale-95', 'enabled:hover:!bg-frota-oficina']) expect(inicial).toContain(classe);
  });

  it('abaixo de 1280 px as colunas empilham e o passo a passo fica 2 por linha (xl e md:grid-cols-2)', () => {
    const html = renderizarPagina();
    for (const classe of ['xl:grid-cols-12', 'xl:col-span-5', 'xl:col-span-7', 'md:grid-cols-2 xl:grid-cols-4']) expect(html).toContain(classe);
    for (const classe of ['lg:grid-cols-12', 'lg:col-span-5', 'lg:col-span-7', 'md:grid-cols-4']) expect(html).not.toContain(classe);
  });

  it('o ponto do selo Em andamento para quando o sistema pede menos movimento', () => {
    const pontos = renderizarPagina().match(/class="[^"]*animate-ping[^"]*"/g) ?? [];
    expect(pontos).toHaveLength(1);
    expect(pontos[0]).toContain('motion-reduce:animate-none');
  });

  it('ocupa a área como as telas 09, 11 e 18: sem a largura antiga e com a barra de ponta a ponta', () => {
    const html = renderizarPagina();
    expect(html).not.toContain('max-w-5xl');
    expect(html).toMatch(/class="sticky bottom-0 [^"]*-mx-8 [^"]*px-8 /);
  });

  it('nenhuma etapa do passo a passo é link', () => {
    const html = renderizarPagina();
    expect(html).not.toMatch(/<a [^>]*>[^<]*(Conferência|Vistoria|Contrato|Entrega)/);
  });

  it('carregando: sem conferência nem dados antigos', () => {
    const html = renderizarPagina({ isPending: true });
    expect(html).toContain('aria-busy="true"');
    expect(html).not.toContain('role="switch"');
    expect(html).not.toContain('Cliente de teste');
  });

  it.each([401, 403, 500, 0])('erro %s: mensagem do servidor, Tentar novamente e Voltar às retiradas, sem conferência nem dados antigos', (status) => {
    const html = renderizarPagina({ isError: true, error: new ErroApi({ status, mensagem: 'Não foi possível consultar.' }) });
    expect(html).toContain('Não foi possível consultar.');
    expect(html).not.toContain('role="switch"');
    expect(html).not.toContain('Cliente de teste');
    expect(html).toContain('Tentar novamente');
    const voltar = linkComTexto(html, VOLTAR);
    expect(voltar).toContain(ENDERECO_ENTRADA);
    expect(voltar).toContain('>arrow_back</span>');
  });

  it('erro 404 (reserva não encontrada): mensagem do servidor e só o Voltar às retiradas, sem Tentar novamente', () => {
    const html = renderizarPagina({ isError: true, error: new ErroApi({ status: 404, mensagem: 'A reserva não foi encontrada.' }) });
    expect(html).toContain('A reserva não foi encontrada.');
    expect(html).not.toContain('Tentar novamente');
    expect(html).not.toContain('role="switch"');
    expect(html).not.toContain('Cliente de teste');
    expect(linkComTexto(html, VOLTAR)).toContain(ENDERECO_ENTRADA);
  });

  it('id inválido: mensagem própria, Voltar às retiradas e nenhuma consulta com id', () => {
    const html = renderizarPagina({}, '0');
    expect(html).toContain('A reserva informada é inválida. Volte às retiradas e informe o código de novo.');
    expect(html).not.toContain('role="switch"');
    expect(html).not.toContain('Tentar novamente');
    expect(linkComTexto(html, VOLTAR)).toContain(ENDERECO_ENTRADA);
    expect(useRetirada).toHaveBeenCalledWith(null);
  });

  it('locação já iniciada: link para a vistoria, sem conferência, sem barra e sem selo Em andamento', () => {
    const html = renderizarPagina({ data: { ...contexto, idLocacaoExistente: 42, statusVeiculo: 'LOCADO' } });
    for (const texto of ['Locação já iniciada', 'Continuar para vistoria', 'Etapa 1']) expect(html).toContain(texto);
    expect(html).toContain('/back-office/locacoes/42/vistoria');
    for (const ausente of ['role="switch"', 'Pronto para despacho de pátio', TEXTO_BOTAO, 'Em andamento', 'aria-current="step"']) expect(html).not.toContain(ausente);
  });

  it('retirada indisponível: texto do estado, botão desligado e sem selo Em andamento', () => {
    const html = renderizarPagina({ data: { ...contexto, statusVeiculo: 'DISPONIVEL' } });
    expect(html).toContain(TEXTO_INDISPONIVEL);
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
    expect(html).not.toContain('Em andamento');
  });

  it('retirada indisponível: o aviso fica no topo, com role="alert", antes da lista e antes do aviso de CNH vencida', () => {
    const html = renderizarConteudo({ statusReserva: 'CANCELADA', cnhVencida: true, cnhValidadeStr: '17/11/2026' }, NADA);
    const posicao = html.indexOf(TEXTO_INDISPONIVEL);
    expect(html.split(TEXTO_INDISPONIVEL).length - 1).toBe(1);
    const abertura = html.slice(html.lastIndexOf('<p ', posicao), posicao);
    expect(abertura).toContain('role="alert"');
    expect(abertura).toContain('bg-status-perigo-fundo');
    expect(abertura).toContain('>block</span>');
    expect(posicao).toBeLessThan(html.indexOf('CNH vencida:'));
    expect(posicao).toBeLessThan(html.indexOf('<ul'));
    expect(posicao).toBeLessThan(html.indexOf('CNH original física ou digital compatível'));
  });

  it('CNH vencida: aviso com a data, e o botão segue a mesma regra', () => {
    const html = renderizarConteudo({ cnhVencida: true, cnhValidadeStr: '17/11/2026' }, TUDO);
    expect(html).toContain('CNH vencida: a validade terminou em 17/11/2026. O sistema não bloqueia a retirada; confira o documento com o cliente.');
    expect(botaoComTexto(html, TEXTO_BOTAO)).not.toContain('disabled=""');
  });

  it('CNH em dia: sem aviso', () => {
    expect(renderizarConteudo({ cnhVencida: false }, NADA)).not.toContain('CNH vencida');
  });

  it('documentos não enviados pelo site: texto de não enviado nas duas linhas', () => {
    const html = renderizarConteudo({ cnhEnviadaEmStr: '', comprovanteResidenciaEnviadoEmStr: '' }, NADA);
    expect(html).toContain('O cliente não enviou a CNH pelo site.');
    expect(html).toContain('O cliente não enviou o comprovante pelo site.');
  });

  it('documentos enviados: data e hora do envio, sem link para o arquivo', () => {
    const html = renderizarConteudo({}, NADA);
    expect(html).toContain('Enviada pelo cliente em 10/11/2026 10:30.');
    expect(html).toContain('Enviado pelo cliente em 12/11/2026 09:15.');
    expect(html).not.toContain('href="http');
  });

  it('veículo sem foto: ícone de carro', () => {
    expect(renderizarConteudo({ fotoCapa: null, erroFotoCapa: null }, NADA)).toContain('directions_car');
  });

  it('veículo com erro na capa: mostra o texto pronto do servidor', () => {
    const texto = 'Não foi possível carregar as fotos agora. Tente novamente em instantes.';
    expect(renderizarConteudo({ fotoCapa: null, erroFotoCapa: texto }, NADA)).toContain(texto);
  });

  it('sem valor estimado: a linha Total previsto não aparece', () => {
    const html = renderizarConteudo({ valorEstimadoStr: '' }, NADA);
    expect(html).not.toContain('Total previsto');
    expect(html).toContain('Diária:');
  });

  it('três itens marcados: contador 3/3, botão ligado e pronto para despacho', () => {
    const html = renderizarConteudo({}, TUDO);
    expect(html).toContain('3/3 Confirmados');
    expect(html).toContain('Conferido nesta tela por Ana Souza');
    expect(html.match(/aria-checked="true"/g)).toHaveLength(3);
    expect(botaoComTexto(html, TEXTO_BOTAO)).not.toContain('disabled=""');
    expect(html).toContain('Pronto para despacho de pátio');
  });

  it('bloqueia durante atualização e envio', () => {
    expect(renderizarConteudo({}, NADA, {}, true)).toContain('<fieldset disabled=""');
    expect(renderizarConteudo({}, NADA, { isPending: true })).toContain('<fieldset disabled=""');
  });

  it('409 ao abrir: texto do servidor e aviso de conflito, sem o aviso de recuperação', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'A reserva não está em um estado que permita iniciar a locação.' });
    const html = renderizarConteudo({}, TUDO, { isError: true, error: erro });
    expect(html).toContain('A reserva não está em um estado que permita iniciar a locação.');
    expect(html).toContain('Os dados da retirada mudaram. Confira os dados atualizados antes de continuar.');
    expect(html).not.toContain('Não foi possível confirmar a abertura.');
  });

  it('outro erro ao abrir (500): texto do servidor e aviso de recuperação, sem o de conflito', () => {
    const erro = new ErroApi({ status: 500, mensagem: 'Erro interno.' });
    const html = renderizarConteudo({}, TUDO, { isError: true, error: erro });
    expect(html).toContain('Não foi possível confirmar a abertura.');
    expect(html).not.toContain('Os dados da retirada mudaram.');
  });

  it('escapa nome com marcação e mostra sem cadastro ativo', () => {
    const html = renderizarConteudo({ clienteNome: '<script>alert(1)</script>', statusCadastroLocadora: null }, NADA);
    expect(html).toContain('&lt;script&gt;');
    expect(html).toContain('Sem cadastro ativo nesta locadora');
  });

  it('sem caução: o POST não é o da caução, a tela não tem o bloco, a titularidade nem o painel', () => {
    const html = renderizarConteudo({}, TUDO);
    expect(html).not.toContain('type="radio"');
    expect(html).not.toContain('Simulação do cartão');
    expect(html).not.toContain('Titularidade do cartão de crédito');
    expect(html).not.toContain('Garantia / Caução');
    expect(html).not.toContain('/4 Confirmados');
  });

  it('sem caução, com a integração quebrada (campos neutros): a página é a de hoje, com o subtítulo sem "garantias"', () => {
    const html = renderizarPagina({ data: { ...contexto, caucaoDisponivel: false, caucaoModoSimulacao: false } });
    expect(html).toContain('Conferência inicial de documentação e validação de reserva para entrega das chaves.');
    expect(html).not.toContain('garantias');
    expect(html).toContain('0/3 Confirmados');
    expect(html.match(/role="switch"/g)).toHaveLength(3);
  });

  it.each([
    ['estado inicial', () => renderizarPagina()],
    ['três itens marcados', () => renderizarConteudo({}, TUDO)],
    ['locação já iniciada', () => renderizarPagina({ data: { ...contexto, idLocacaoExistente: 42 } })],
    ['indisponível', () => renderizarPagina({ data: { ...contexto, statusVeiculo: 'DISPONIVEL' } })],
    ['carregando', () => renderizarPagina({ isPending: true })],
    ['erro 404', () => renderizarPagina({ isError: true, error: new ErroApi({ status: 404, mensagem: 'A reserva não foi encontrada.' }) })],
    ['erro da consulta (500) com dados antigos', () => renderizarPagina({ isError: true, data: comCaucao, error: new ErroApi({ status: 500, mensagem: 'Erro interno.' }) })],
    ['id inválido', () => renderizarPagina({}, '0')],
    ['409 ao abrir', () => renderizarConteudo({}, TUDO, { isError: true, error: new ErroApi({ status: 409, mensagem: 'Estado inválido.' }) })],
    ['502 ao abrir', () => renderizarConteudo({}, TUDO, { isError: true, error: new ErroApi({ status: 502, mensagem: 'Não foi possível concluir agora.' }) })],
    ['CNH vencida e sem documentos', () => renderizarConteudo({ cnhVencida: true, cnhEnviadaEmStr: '', comprovanteResidenciaEnviadoEmStr: '' }, NADA)],
    ['sem valor estimado', () => renderizarConteudo({ valorEstimadoStr: '' }, NADA)],
    ['sessão VENDEDOR (o aviso do cargo é só da caução)', () => { sessao.cargo = 'VENDEDOR'; return renderizarConteudo({}, TUDO); }],
    ['sessão sem cargo', () => { sessao.cargo = null; return renderizarPagina(); }],
  ])('sem caução ou sem dados, nenhum termo das duas listas aparece: %s', (_nome, renderizar) => {
    const html = renderizar();
    expect(termosCortadosPresentesRetirada(html)).toEqual([]);
    expect(termosDaCaucaoPresentesRetirada(html)).toEqual([]);
  });
});

describe('tela 16 com a caução exigida', () => {
  beforeEach(() => vi.clearAllMocks());

  it('disponível: subtítulo com "garantias", bloco da caução, quatro atestados, 0/4, item da pré-autorização e botão desligado', () => {
    const html = renderizarPagina({ data: comCaucao });
    for (const texto of [
      'Conferência inicial de documentação, garantias e validação de reserva para entrega das chaves.',
      'Garantia / Caução', 'R$ 1.200,00', 'Pré-autorização no cartão do cliente ao iniciar a locação.',
      'Titularidade do cartão de crédito',
      'Titular do cartão corresponde ao condutor principal ou responsável financeiro com procuração reconhecida.',
      '0/4 Confirmados', 'Pré-autorização da caução (R$ 1.200,00)',
      'Bloqueio de garantia financeira no cartão de crédito do cliente, feito quando a locação é iniciada.',
      'Simulação do cartão',
      'Ambiente de simulação: nenhum valor é reservado em cartão de verdade. Escolha o resultado da pré-autorização.',
      'Aprovar', 'Recusar', 'Deixar em análise',
    ]) expect(html).toContain(texto);
    expect(html).not.toContain('Conferência inicial de documentação e validação de reserva para entrega das chaves.');
    expect(html.match(/role="switch"/g)).toHaveLength(4);
    expect(html.match(/type="radio"/g)).toHaveLength(3);
    expect(html).not.toContain('checked=""');
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
    expect(html).not.toContain('Pronto para despacho de pátio');
    expect(html).not.toContain('Pré-autorização indisponível');
  });

  it('o item da pré-autorização não tem interruptor: os quatro role="switch" são dos atestados', () => {
    const item = itemPreAutorizacao(renderizarComCaucao());
    expect(item).toContain('Simulação do cartão');
    expect(item).not.toContain('role="switch"');
    expect(item.match(/type="radio"/g)).toHaveLength(3);
    expect(item).toContain('<legend');
  });

  it('ordem do desenho: CNH, titularidade, comprovante, termo e a pré-autorização por último', () => {
    const html = renderizarComCaucao();
    const posicoes = [
      'CNH original física ou digital compatível', 'Titularidade do cartão de crédito', 'Comprovante de residência atualizado',
      'Termo de responsabilidade sobre multas', 'Pré-autorização da caução (R$ 1.200,00)',
    ].map((texto) => html.indexOf(texto));
    expect(posicoes.every((posicao) => posicao >= 0)).toBe(true);
    expect([...posicoes].sort((a, b) => a - b)).toEqual(posicoes);
    expect(html.indexOf('>credit_card</span>')).toBeGreaterThan(html.indexOf('>badge</span>'));
    expect(html).toContain('>account_balance_wallet</span>');
  });

  it('o bloco da caução fica dentro da Tarifação, depois do Total previsto, com rótulo, valor e situação', () => {
    const html = renderizarComCaucao();
    const posicaoTarifacao = html.indexOf('Tarifação da Reserva');
    const posicaoTotal = html.indexOf('Total previsto');
    const posicaoCaucao = html.indexOf('Garantia / Caução');
    expect(posicaoTarifacao).toBeLessThan(posicaoTotal);
    expect(posicaoTotal).toBeLessThan(posicaoCaucao);
    expect(posicaoCaucao).toBeLessThan(html.indexOf('Pré-autorização no cartão do cliente ao iniciar a locação.'));
    // A caixa branca do desenho (bg-surface-container-lowest) abre depois do Total previsto e antes do rótulo.
    expect(html.lastIndexOf('bg-branco', posicaoCaucao)).toBeGreaterThan(posicaoTotal);
  });

  it('os quatro marcados e uma saída escolhida: 4/4, botão ligado, pronto para despacho e a opção escolhida marcada', () => {
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, {}, 'RECUSAR');
    expect(html).toContain('4/4 Confirmados');
    expect(html.match(/aria-checked="true"/g)).toHaveLength(4);
    expect(html.match(/checked=""/g)).toHaveLength(1);
    expect(radioDaSaida(html, 'RECUSAR')).toContain('checked=""');
    expect(radioDaSaida(html, 'APROVAR')).toContain('type="radio"');
    expect(radioDaSaida(html, 'APROVAR')).not.toContain('checked=""');
    expect(botaoComTexto(html, TEXTO_BOTAO)).not.toContain('disabled=""');
    expect(html).toContain('Pronto para despacho de pátio');
  });

  it('a opção escolhida tem o fundo escuro do interruptor ligado; as livres, o fundo claro', () => {
    const item = itemPreAutorizacao(renderizarComCaucao({}, NADA, {}, 'APROVAR'));
    const rotulos = item.match(/<label class="[^"]*"/g) ?? [];
    expect(rotulos).toHaveLength(3);
    expect(rotulos[0]).toContain('bg-navy-profundo text-branco');
    for (const livre of rotulos.slice(1)) expect(livre).toContain('bg-steel-alto text-texto-desenho');
    for (const rotulo of rotulos) expect(rotulo).toContain('has-[:focus-visible]:ring-orange-acao');
    expect(item).toContain('>radio_button_checked</span>');
    expect(item.match(/>radio_button_unchecked<\/span>/g)).toHaveLength(2);
  });

  it('os quatro marcados sem saída escolhida: 4/4, mas o botão continua desligado e sem pronto para despacho', () => {
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO);
    expect(html).toContain('4/4 Confirmados');
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
    expect(html).not.toContain('Pronto para despacho de pátio');
  });

  it('três de quatro (falta a titularidade) com saída escolhida: 3/4 e botão desligado', () => {
    const html = renderizarComCaucao({}, TUDO, {}, 'APROVAR');
    expect(html).toContain('3/4 Confirmados');
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
  });

  it('a titularidade marcada mostra a linha verde de quem conferiu', () => {
    const html = renderizarComCaucao({}, { ...NADA, titularidadeCartao: true });
    expect(html).toContain('1/4 Confirmados');
    expect(html).toContain('Conferido nesta tela por Ana Souza');
  });

  it('indisponível: o texto curto na caixa e o inteiro no item, sem painel nem rádios, e botão desligado mesmo com tudo marcado', () => {
    const html = renderizarComCaucao({ caucaoDisponivel: false, caucaoModoSimulacao: false }, TUDO_COM_CAUCAO, {}, 'APROVAR');
    expect(contar(html, CAUCAO_INDISPONIVEL)).toBe(1);
    expect(contar(html, CAUCAO_INDISPONIVEL_CURTO)).toBe(1);
    expect(html).toContain('Garantia / Caução');
    expect(html).toContain('4/4 Confirmados');
    expect(html).not.toContain('type="radio"');
    expect(html).not.toContain('Simulação do cartão');
    expect(html).not.toContain('Ambiente de simulação');
    expect(html).not.toContain('Pré-autorização no cartão do cliente ao iniciar a locação.');
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
    expect(html).not.toContain('Pronto para despacho de pátio');
    const item = itemPreAutorizacao(html);
    expect(item).toContain('bg-status-perigo-fundo');
    expect(item).toContain('text-status-perigo');
  });

  it('disponível sem o modo de simulação (cartão real): a tela trata como indisponível', () => {
    const html = renderizarComCaucao({ caucaoDisponivel: true, caucaoModoSimulacao: false }, TUDO_COM_CAUCAO, {}, 'APROVAR');
    expect(html).not.toContain('type="radio"');
    expect(html).toContain('Pré-autorização indisponível');
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
  });

  it('enviando: interruptores, painel e botão desabilitados (o painel fica dentro do fieldset desabilitado)', () => {
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, { isPending: true }, 'APROVAR');
    const inicioFieldset = html.indexOf('<fieldset disabled=""');
    expect(inicioFieldset).toBeGreaterThanOrEqual(0);
    expect(inicioFieldset).toBeLessThan(html.indexOf('type="radio"'));
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
  });

  it('retirada indisponível com caução: o bloco aparece só informativo e o painel fica desabilitado', () => {
    const html = renderizarComCaucao({ statusReserva: 'CANCELADA' }, TUDO_COM_CAUCAO, {}, 'APROVAR');
    expect(html).toContain('Garantia / Caução');
    expect(html).toContain('A retirada exige uma reserva ativa ou confirmada e um veículo reservado. Atualize os dados ou volte às retiradas.');
    expect(html.indexOf('<fieldset disabled=""')).toBeLessThan(html.indexOf('type="radio"'));
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
    expect(html).not.toContain('Pronto para despacho de pátio');
  });

  it('locação já iniciada com caução exigida: sem o bloco da caução, sem painel e sem atestados', () => {
    const html = renderizarPagina({ data: { ...comCaucao, idLocacaoExistente: 42, statusVeiculo: 'LOCADO', caucaoDisponivel: false, caucaoModoSimulacao: false } });
    expect(html).toContain('Locação já iniciada');
    for (const ausente of ['Garantia / Caução', 'R$ 1.200,00', 'Pré-autorização', 'Titularidade do cartão', 'type="radio"', 'role="switch"', TEXTO_BOTAO]) {
      expect(html).not.toContain(ausente);
    }
  });

  it('erro 409 com a retirada ainda disponível: texto do servidor e o complemento da caução', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'O cartão foi recusado na pré-autorização da caução: recusado pelo emissor; tente outro cartão.' });
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, { isError: true, error: erro }, 'RECUSAR');
    expect(html).toContain('O cartão foi recusado na pré-autorização da caução: recusado pelo emissor; tente outro cartão.');
    expect(linhaDoErro(html)).toContain(CAUCAO_TENTE_NOVAMENTE);
    expect(html).not.toContain('A locação não foi iniciada');
    expect(html).not.toContain('Os dados da retirada mudaram.');
    expect(html).not.toContain('Não foi possível confirmar a abertura.');
    // A escolha da simulação continua marcada depois do erro.
    expect(radioDaSaida(html, 'RECUSAR')).toContain('checked=""');
  });

  it.each([
    'A pré-autorização da caução ficou em análise e não pôde ser usada. A locação não foi iniciada: faça uma nova tentativa.',
    'Esta tentativa de pré-autorização da caução não vale mais (foi cancelada, expirou ou foi estornada). A locação não foi iniciada: faça uma nova tentativa.',
  ])('erro 409 de caução (%s): texto do servidor e o complemento da caução, com "A locação não foi iniciada" uma vez só', (mensagem) => {
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, { isError: true, error: new ErroApi({ status: 409, mensagem }) }, 'ANALISE');
    expect(html).toContain(mensagem);
    expect(linhaDoErro(html)).toContain(CAUCAO_TENTE_NOVAMENTE);
    expect(contar(html, 'A locação não foi iniciada')).toBe(1);
  });

  it('erro 409 com a retirada indisponível depois da recarga: texto do servidor e o aviso de conflito', () => {
    const erro = new ErroApi({ status: 409, mensagem: 'A reserva não está em um estado que permita iniciar a locação.' });
    const html = renderizarComCaucao({ statusVeiculo: 'DISPONIVEL' }, TUDO_COM_CAUCAO, { isError: true, error: erro }, 'APROVAR');
    expect(html).toContain('A reserva não está em um estado que permita iniciar a locação.');
    expect(html).toContain('Os dados da retirada mudaram. Confira os dados atualizados antes de continuar.');
    expect(html).not.toContain('Escolha de novo o resultado da simulação');
  });

  it.each([
    [403, 'Seu perfil não tem permissão para acessar este recurso.'],
    [400, 'Esta retirada exige caução: informe o cartão de crédito do cliente.'],
    [429, 'Muitas tentativas. Aguarde alguns instantes e tente novamente.'],
  ])('erro %s com caução: só o texto do servidor, sem complemento', (status, mensagem) => {
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, { isError: true, error: new ErroApi({ status, mensagem }) }, 'APROVAR');
    expect(html).toContain(mensagem);
    for (const complemento of ['Escolha de novo o resultado', 'Os dados da retirada mudaram.', 'Não foi possível confirmar a abertura.']) {
      expect(html).not.toContain(complemento);
    }
  });

  it.each([
    [502, 'O provedor de teste (FAKE) da caução não está liberado neste ambiente.'],
    [0, 'O servidor demorou para responder. Tente novamente.'],
  ])('erro %s com caução (erro do provedor ou sem resposta): texto do servidor e o aviso de recuperação', (status, mensagem) => {
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, { isError: true, error: new ErroApi({ status, mensagem }) }, 'APROVAR');
    expect(html).toContain(mensagem);
    expect(html).toContain('Não foi possível confirmar a abertura.');
    expect(html).not.toContain('Escolha de novo o resultado');
  });

  it('escapa o valor da caução com marcação', () => {
    const html = renderizarComCaucao({ valorCaucaoStr: '<b>R$ 1</b>' });
    expect(html).toContain('&lt;b&gt;R$ 1&lt;/b&gt;');
    expect(html).not.toContain('<b>R$ 1</b>');
  });

  it.each([
    ['disponível, nada marcado', () => renderizarPagina({ data: comCaucao })],
    ['quatro marcados e saída escolhida', () => renderizarComCaucao({}, TUDO_COM_CAUCAO, {}, 'APROVAR')],
    ['indisponível', () => renderizarComCaucao({ caucaoDisponivel: false, caucaoModoSimulacao: false })],
    ['retirada indisponível', () => renderizarComCaucao({ statusReserva: 'CANCELADA' })],
    ['locação já iniciada', () => renderizarPagina({ data: { ...comCaucao, idLocacaoExistente: 42 } })],
    ['enviando', () => renderizarComCaucao({}, TUDO_COM_CAUCAO, { isPending: true }, 'APROVAR')],
    ['409 de recusa', () => renderizarComCaucao({}, TUDO_COM_CAUCAO, { isError: true, error: new ErroApi({ status: 409, mensagem: 'O cartão foi recusado na pré-autorização da caução: recusado pelo emissor; tente outro cartão.' }) }, 'RECUSAR')],
    ['403', () => renderizarComCaucao({}, TUDO_COM_CAUCAO, { isError: true, error: new ErroApi({ status: 403, mensagem: 'Seu perfil não tem permissão para acessar este recurso.' }) }, 'APROVAR')],
    ['502', () => renderizarComCaucao({}, TUDO_COM_CAUCAO, { isError: true, error: new ErroApi({ status: 502, mensagem: 'O provedor de teste (FAKE) da caução não está liberado neste ambiente.' }) }, 'APROVAR')],
    ['cargo sem permissão (VENDEDOR), quatro marcados e saída escolhida', () => { sessao.cargo = 'VENDEDOR'; return renderizarComCaucao({}, TUDO_COM_CAUCAO, {}, 'APROVAR'); }],
    ['sessão sem cargo', () => { sessao.cargo = null; return renderizarPagina({ data: comCaucao }); }],
    ['CNH vencida e sem documentos', () => renderizarComCaucao({ cnhVencida: true, cnhEnviadaEmStr: '', comprovanteResidenciaEnviadoEmStr: '' })],
  ])('com caução, nenhum termo cortado de sempre aparece (bandeira, final, NSU, AutoVia Pay, cartão físico): %s', (_nome, renderizar) => {
    const html = renderizar();
    expect(termosCortadosPresentesRetirada(html)).toEqual([]);
    for (const proibido of ['Mastercard', 'NSU', 'AutoVia Pay', 'Cartão físico', 'final 4018', '849103', 'Pré-autorizado']) {
      expect(html).not.toContain(proibido);
    }
  });
});

describe('tela 16: caução indisponível com o texto curto na caixa da esquerda (ajuste A4)', () => {
  beforeEach(() => vi.clearAllMocks());

  it('a caixa "Garantia / Caução" diz só o texto curto, com o ícone block e a cor de perigo; o texto inteiro aparece UMA vez, no item', () => {
    const html = renderizarComCaucao({ caucaoDisponivel: false, caucaoModoSimulacao: false });
    const posicaoCurto = html.indexOf(CAUCAO_INDISPONIVEL_CURTO);
    expect(contar(html, CAUCAO_INDISPONIVEL_CURTO)).toBe(1);
    expect(contar(html, CAUCAO_INDISPONIVEL)).toBe(1);
    // O curto fica na coluna da esquerda, dentro da caixa da caução e antes da conferência; o inteiro, no item da pré-autorização.
    expect(posicaoCurto).toBeGreaterThan(html.indexOf('Garantia / Caução'));
    expect(posicaoCurto).toBeLessThan(html.indexOf('Checklist de Verificação Documental e Presencial'));
    expect(itemPreAutorizacao(html)).toContain(CAUCAO_INDISPONIVEL);
    expect(itemPreAutorizacao(html)).not.toContain(CAUCAO_INDISPONIVEL_CURTO);
    const linhaDaCaixa = html.slice(html.lastIndexOf('<span class="flex items-start gap-1.5', posicaoCurto), posicaoCurto);
    expect(linhaDaCaixa).toContain('text-status-perigo');
    expect(linhaDaCaixa).toContain('>block</span>');
  });

  it('com a caução disponível, nenhum dos dois textos de indisponível', () => {
    const html = renderizarComCaucao();
    expect(html).not.toContain(CAUCAO_INDISPONIVEL_CURTO);
    expect(html).not.toContain(CAUCAO_INDISPONIVEL);
    expect(html).toContain('Pré-autorização no cartão do cliente ao iniciar a locação.');
  });
});

describe('tela 16: aviso por cargo com a caução exigida (ajuste A1)', () => {
  beforeEach(() => vi.clearAllMocks());

  it.each(SEM_PERMISSAO_COM_CAUCAO)('cargo %s: o aviso do cargo no lugar do painel, botão desligado e sem pronto, mesmo com 4/4 e "Aprovar"', (cargo) => {
    sessao.cargo = cargo;
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, {}, 'APROVAR');
    expect(contar(html, CAUCAO_CARGO_SEM_PERMISSAO)).toBe(1);
    expect(itemPreAutorizacao(html)).toContain(CAUCAO_CARGO_SEM_PERMISSAO);
    expect(html).not.toContain('type="radio"');
    expect(html).not.toContain('Simulação do cartão');
    expect(html).toContain('4/4 Confirmados');
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
    expect(html).not.toContain('Pronto para despacho de pátio');
    // A caixa da esquerda não muda com o cargo: a caução pode ser tentada.
    expect(html).toContain('Pré-autorização no cartão do cliente ao iniciar a locação.');
    expect(html).not.toContain('Pré-autorização indisponível');
  });

  it('o aviso do cargo segue o molde do texto de indisponível do item: fundo e texto de perigo, ícone block e sem role="alert"', () => {
    sessao.cargo = 'VENDEDOR';
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, {}, 'APROVAR');
    const posicao = html.indexOf(CAUCAO_CARGO_SEM_PERMISSAO);
    const abertura = html.slice(html.lastIndexOf('<p ', posicao), posicao);
    for (const classe of ['bg-status-perigo-fundo', 'p-4', 'text-corpo-denso', 'text-status-perigo']) expect(abertura).toContain(classe);
    expect(abertura).toContain('>block</span>');
    expect(abertura).not.toContain('role=');
  });

  it('página com VENDEDOR: subtítulo com "garantias", 0/4, os quatro interruptores ativos e o aviso do cargo uma vez', () => {
    sessao.cargo = 'VENDEDOR';
    const html = renderizarPagina({ data: comCaucao });
    expect(html).toContain('Conferência inicial de documentação, garantias e validação de reserva para entrega das chaves.');
    expect(html).toContain('0/4 Confirmados');
    expect(html.match(/role="switch"/g)).toHaveLength(4);
    expect(html).not.toContain('<fieldset disabled=""');
    expect(contar(html, CAUCAO_CARGO_SEM_PERMISSAO)).toBe(1);
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
  });

  it('caução indisponível com VENDEDOR: só o texto de indisponível (ele vence o do cargo)', () => {
    sessao.cargo = 'VENDEDOR';
    const html = renderizarComCaucao({ caucaoDisponivel: false, caucaoModoSimulacao: false }, TUDO_COM_CAUCAO, {}, 'APROVAR');
    expect(html).not.toContain(CAUCAO_CARGO_SEM_PERMISSAO);
    expect(contar(html, CAUCAO_INDISPONIVEL)).toBe(1);
    expect(contar(html, CAUCAO_INDISPONIVEL_CURTO)).toBe(1);
    expect(botaoComTexto(html, TEXTO_BOTAO)).toContain('disabled=""');
  });

  it.each(['ATENDENTE', 'ADMINISTRADOR'] as const)('cargo %s: o painel aparece, sem o aviso do cargo, e o botão liga com 4/4 e uma saída', (cargo) => {
    sessao.cargo = cargo;
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, {}, 'APROVAR');
    expect(html.match(/type="radio"/g)).toHaveLength(3);
    expect(html).not.toContain(CAUCAO_CARGO_SEM_PERMISSAO);
    expect(botaoComTexto(html, TEXTO_BOTAO)).not.toContain('disabled=""');
    expect(html).toContain('Pronto para despacho de pátio');
  });

  it.each([...CARGO_FUNCIONARIO, null])('sem caução, cargo %s: igual a hoje (3/3, botão ligado e pronto para despacho) e sem o aviso do cargo', (cargo) => {
    sessao.cargo = cargo;
    const html = renderizarConteudo({}, TUDO);
    expect(html).toContain('3/3 Confirmados');
    expect(botaoComTexto(html, TEXTO_BOTAO)).not.toContain('disabled=""');
    expect(html).toContain('Pronto para despacho de pátio');
    expect(html).not.toContain(CAUCAO_CARGO_SEM_PERMISSAO);
  });
});

describe('tela 16: o erro da abertura fica escrito na tela, sem aviso flutuante (ajuste A2)', () => {
  beforeEach(() => vi.clearAllMocks());
  const STATUS_DE_ERRO = [0, 400, 403, 404, 409, 429, 500, 502] as const;

  it.each(STATUS_DE_ERRO)('sem caução, status %s: o texto do servidor fica na linha com role="alert" embaixo da lista, uma vez', (status) => {
    const mensagem = `Texto do servidor para o status ${status}.`;
    const html = renderizarConteudo({}, TUDO, { isError: true, error: new ErroApi({ status, mensagem }) });
    expect(linhaDoErro(html)).toContain(mensagem);
    expect(contar(html, mensagem)).toBe(1);
    expect(html.indexOf('<div role="alert"')).toBeGreaterThan(html.indexOf('</ul>'));
  });

  it.each(STATUS_DE_ERRO)('com caução, status %s: o texto do servidor fica na linha com role="alert" embaixo da lista, uma vez', (status) => {
    const mensagem = `Texto do servidor para o status ${status}.`;
    const html = renderizarComCaucao({}, TUDO_COM_CAUCAO, { isError: true, error: new ErroApi({ status, mensagem }) }, 'APROVAR');
    expect(linhaDoErro(html)).toContain(mensagem);
    expect(contar(html, mensagem)).toBe(1);
    expect(html.indexOf('<div role="alert"')).toBeGreaterThan(html.indexOf('</ul>'));
  });
});

describe('tela 16: memória da escolha e da tentativa na página (sobrevive ao estado de erro)', () => {
  beforeEach(() => vi.clearAllMocks());
  const tentativa: TentativaCaucao = { escolha: 'APROVAR', token: 'FAKE-0123456789abcdef0123456789abcdef', status: 0 };

  it('ConteudoRetirada recebe a escolha e a tentativa por props: a escolha injetada aparece marcada', () => {
    const html = renderizarComCaucao({}, NADA, {}, 'ANALISE', false, tentativa);
    expect(radioDaSaida(html, 'ANALISE')).toContain('checked=""');
    expect(html.match(/checked=""/g)).toHaveLength(1);
  });

  it('a página passa ao conteúdo a memória da mesma reserva: a escolha lembrada aparece marcada', () => {
    const html = renderizarPagina({ data: comCaucao }, '8', {}, { idReserva: 8, escolha: 'RECUSAR', tentativaAnterior: tentativa });
    expect(radioDaSaida(html, 'RECUSAR')).toContain('checked=""');
    expect(html.match(/checked=""/g)).toHaveLength(1);
  });

  it('a memória de outra reserva não vale para a reserva da rota: nenhuma saída marcada', () => {
    const html = renderizarPagina({ data: comCaucao }, '8', {}, { idReserva: 7, escolha: 'RECUSAR', tentativaAnterior: tentativa });
    expect(html.match(/type="radio"/g)).toHaveLength(3);
    expect(html).not.toContain('checked=""');
  });

  it('com a consulta em erro, a página mostra o estado de erro mesmo com memória (a memória não muda o que aparece)', () => {
    const html = renderizarPagina(
      { isError: true, error: new ErroApi({ status: 0, mensagem: 'Não foi possível consultar.' }) }, '8', {},
      { idReserva: 8, escolha: 'APROVAR', tentativaAnterior: tentativa },
    );
    expect(html).toContain('Não foi possível consultar.');
    expect(html).toContain('Tentar novamente');
    expect(html).not.toContain('type="radio"');
  });
});
