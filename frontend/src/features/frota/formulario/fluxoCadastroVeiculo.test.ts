import { describe, expect, it } from 'vitest';
import {
  avisoRascunhoVisivel,
  caminhoEdicaoAposLote,
  destinoCancelamentoVeiculo,
  filaSemPendencias,
  identificarRotaVeiculo,
  origemDaTelaVeiculo,
  podeEnviarCadastro,
} from './fluxoCadastroVeiculo';

describe('identificarRotaVeiculo', () => {
  it('sem parâmetro é cadastro', () => {
    expect(identificarRotaVeiculo(undefined)).toEqual({ tipo: 'cadastro' });
  });

  it('número inteiro positivo é edição', () => {
    expect(identificarRotaVeiculo('12')).toEqual({ tipo: 'edicao', idVeiculo: 12 });
  });

  it('o que não é inteiro positivo é inválido e nunca vira consulta', () => {
    for (const parametro of ['', '0', '-1', '1.5', 'abc', '12a', ' 7', '99999999999999999999']) {
      expect(identificarRotaVeiculo(parametro)).toEqual({ tipo: 'invalido' });
    }
  });
});

describe('podeEnviarCadastro', () => {
  it('libera o envio quando nada está em curso e o veículo ainda não foi criado', () => {
    expect(podeEnviarCadastro({ enviando: false, criado: false })).toBe(true);
  });

  it('bloqueia enquanto um envio está em curso (clique duplo não grava duas vezes)', () => {
    expect(podeEnviarCadastro({ enviando: true, criado: false })).toBe(false);
  });

  it('bloqueia depois de criado (nova tentativa não duplica o veículo)', () => {
    expect(podeEnviarCadastro({ enviando: false, criado: true })).toBe(false);
  });
});

describe('filaSemPendencias', () => {
  it('fila vazia ou só com fotos gravadas não tem pendência', () => {
    expect(filaSemPendencias([])).toBe(true);
    expect(filaSemPendencias(['concluida', 'concluida'])).toBe(true);
  });

  it('foto aguardando, enviando ou com erro é pendência', () => {
    expect(filaSemPendencias(['aguardando'])).toBe(false);
    expect(filaSemPendencias(['enviando', 'concluida'])).toBe(false);
    expect(filaSemPendencias(['concluida', 'erro'])).toBe(false);
  });

  it('com as fotos com erro removidas e as reenviadas com sucesso, a fila fica sem pendência', () => {
    // Antes: duas com erro e uma gravada. Depois de remover as duas, sobra só a gravada.
    expect(filaSemPendencias(['erro', 'erro', 'concluida'])).toBe(false);
    expect(filaSemPendencias(['concluida'])).toBe(true);
  });
});

describe('caminhoEdicaoAposLote', () => {
  it('sem veículo criado, não troca a URL', () => {
    expect(caminhoEdicaoAposLote(null, false)).toBeNull();
  });

  it('com foto com falha, fica na tela para o "Tentar de novo"', () => {
    expect(caminhoEdicaoAposLote(7, true)).toBeNull();
  });

  it('com veículo criado e nada pendente, vai para a edição desse veículo', () => {
    expect(caminhoEdicaoAposLote(7, false)).toBe('/back-office/frota/veiculos/7/editar');
  });
});

describe('avisoRascunhoVisivel', () => {
  it('no cadastro vazio com rascunho recuperado, mostra o aviso', () => {
    expect(avisoRascunhoVisivel({ edicao: false, rascunhoAberto: true, criado: false })).toBe(true);
  });

  it('depois que o veículo é criado, o aviso some (e não pode limpar o formulário do veículo criado)', () => {
    expect(avisoRascunhoVisivel({ edicao: false, rascunhoAberto: true, criado: true })).toBe(false);
  });

  it('na edição nunca aparece, e sem rascunho também não', () => {
    expect(avisoRascunhoVisivel({ edicao: true, rascunhoAberto: true, criado: false })).toBe(false);
    expect(avisoRascunhoVisivel({ edicao: false, rascunhoAberto: false, criado: false })).toBe(false);
  });
});

describe('origemDaTelaVeiculo', () => {
  it('aberta direto (primeira entrada, sem estado): não veio do sistema', () => {
    expect(origemDaTelaVeiculo({ key: 'default', state: null })).toBe(false);
  });

  it('chegou por navegação interna (entrada com chave própria): veio do sistema', () => {
    expect(origemDaTelaVeiculo({ key: 'k3f9a', state: null })).toBe(true);
  });

  it('a troca de cadastro para edição leva a origem no estado, mesmo com substituição de entrada', () => {
    expect(origemDaTelaVeiculo({ key: 'novaChave', state: { veioDoSistema: false } })).toBe(false);
    expect(origemDaTelaVeiculo({ key: 'default', state: { veioDoSistema: true } })).toBe(true);
  });

  it('estado com valor que não é booleano é ignorado', () => {
    expect(origemDaTelaVeiculo({ key: 'default', state: { veioDoSistema: 'sim' } })).toBe(false);
  });
});

describe('destinoCancelamentoVeiculo', () => {
  it('veio do sistema: volta para a tela anterior', () => {
    expect(destinoCancelamentoVeiculo(true)).toBe('voltar');
  });

  it('aberta direto: vai para a frota', () => {
    expect(destinoCancelamentoVeiculo(false)).toBe('frota');
  });
});
