import { describe, expect, it } from 'vitest';
import { CARGO_FUNCIONARIO } from '@/types/enums';
import { ICONE_CARGO, RESPONSABILIDADES_CARGO } from './textosCargo';

describe('RESPONSABILIDADES_CARGO', () => {
  it('tem uma lista não vazia para cada um dos seis cargos', () => {
    for (const cargo of CARGO_FUNCIONARIO) {
      expect(RESPONSABILIDADES_CARGO[cargo].length, cargo).toBeGreaterThan(0);
    }
  });

  it('Atendente traz as três marcas do desenho 41', () => {
    expect(RESPONSABILIDADES_CARGO.ATENDENTE).toEqual([
      'Verificar cadastros de clientes',
      'Fazer check-in e check-out',
      'Registrar vistorias e emitir contratos',
    ]);
  });

  it('Administrador traz os dois textos passados pelo dono do produto', () => {
    expect(RESPONSABILIDADES_CARGO.ADMINISTRADOR).toEqual([
      'Gerencia a equipe e os dados da locadora',
      'Acessa todos os módulos do painel',
    ]);
  });

  it('Vendedor, Gestor de frota, Financeiro e Administrativo vêm da seção 2.2', () => {
    expect(RESPONSABILIDADES_CARGO.VENDEDOR).toEqual(['Conduzir a negociação e registrar a venda de veículos']);
    expect(RESPONSABILIDADES_CARGO.GESTOR_FROTA).toEqual([
      'Cadastrar veículos',
      'Abrir e encerrar manutenções',
      'Contratar apólices',
    ]);
    expect(RESPONSABILIDADES_CARGO.FINANCEIRO).toEqual(['Lançar e baixar cobranças', 'Acompanhar inadimplência']);
    expect(RESPONSABILIDADES_CARGO.ADMINISTRATIVO).toEqual([
      'Cuidar da documentação de transferência',
      'Acompanhar sinistros',
    ]);
  });

  it('nenhum texto por cargo fala em convite', () => {
    for (const cargo of CARGO_FUNCIONARIO) {
      for (const texto of RESPONSABILIDADES_CARGO[cargo]) {
        expect(texto.toLowerCase()).not.toContain('convite');
      }
    }
  });
});

describe('ICONE_CARGO', () => {
  it('tem um ícone para cada cargo, como no desenho', () => {
    for (const cargo of CARGO_FUNCIONARIO) {
      expect(ICONE_CARGO[cargo], cargo).toBeTruthy();
    }
    expect(ICONE_CARGO.ADMINISTRADOR).toBe('shield_person');
    expect(ICONE_CARGO.ATENDENTE).toBe('support_agent');
  });
});
