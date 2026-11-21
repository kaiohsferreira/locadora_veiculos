import { describe, expect, it } from 'vitest';
import { formatarMoeda } from '@/lib/formatters';
import { escreverFiltroNoEndereco, lerFiltroDoEndereco, caminhoProposta } from './enderecoCatalogo';
import {
  CAMPOS_FILTRO_VAZIOS,
  camposParaFiltro,
  contarFiltrosAtivos,
  etiquetasFiltro,
  filtroParaCampos,
  filtrosValidos,
  mascararAno,
  mascararKm,
  removerFiltro,
  validarCamposFiltro,
  type CamposFiltro,
} from './filtrosCatalogo';

function campos(parcial: Partial<CamposFiltro>): CamposFiltro {
  return { ...CAMPOS_FILTRO_VAZIOS, ...parcial };
}

describe('validarCamposFiltro', () => {
  it('campos vazios não são erro (sem filtro)', () => {
    expect(filtrosValidos(validarCamposFiltro(CAMPOS_FILTRO_VAZIOS))).toBe(true);
  });

  it('preço mínimo maior que o máximo dá erro no campo "Até"', () => {
    const erros = validarCamposFiltro(campos({ precoMinimo: '90.000,00', precoMaximo: '50.000,00' }));
    expect(erros.precoMaximo).toBe('O preço mínimo não pode ser maior que o preço máximo.');
  });

  it('ano precisa de 4 dígitos e a faixa não pode ser invertida', () => {
    expect(validarCamposFiltro(campos({ anoMinimo: '202' })).anoMinimo).toBe('Informe o ano com 4 dígitos.');
    expect(validarCamposFiltro(campos({ anoMinimo: '2024', anoMaximo: '2020' })).anoMaximo).toBe(
      'O ano mínimo não pode ser maior que o ano máximo.',
    );
    expect(filtrosValidos(validarCamposFiltro(campos({ anoMinimo: '2020', anoMaximo: '2024' })))).toBe(true);
  });

  it('marca com mais de 40 caracteres dá erro', () => {
    expect(validarCamposFiltro(campos({ marca: 'x'.repeat(41) })).marca).toBeDefined();
    expect(validarCamposFiltro(campos({ marca: 'x'.repeat(40) })).marca).toBeUndefined();
  });

  it('as máscaras guardam só dígitos (ano 4, km 7)', () => {
    expect(mascararAno('20a245')).toBe('2024');
    expect(mascararKm('50.000 km')).toBe('50000');
  });
});

describe('camposParaFiltro e filtroParaCampos', () => {
  it('converte os campos no filtro da API, na primeira página, sem os vazios', () => {
    const filtro = camposParaFiltro(campos({ marca: '  Jeep ', precoMaximo: '100.000,00', anoMinimo: '2021', kmMaximo: '50000' }));
    expect(filtro).toEqual({ pagina: 1, tamanhoPagina: 20, marca: 'Jeep', precoMaximo: 100000, anoMinimo: 2021, kmMaximo: 50000 });
  });

  it('volta do filtro para os campos com a máscara de reais', () => {
    const textos = filtroParaCampos({ pagina: 3, tamanhoPagina: 20, precoMinimo: 45990.5, anoMaximo: 2023 });
    expect(textos.precoMinimo).toBe('45.990,50');
    expect(textos.anoMaximo).toBe('2023');
    expect(textos.marca).toBe('');
  });
});

describe('etiquetas e remoção', () => {
  const filtro = { pagina: 2, tamanhoPagina: 20, marca: 'Jeep', precoMaximo: 100000, kmMaximo: 50000 };

  it('cria uma etiqueta por filtro ativo, na ordem dos campos', () => {
    expect(etiquetasFiltro(filtro).map((etiqueta) => etiqueta.rotulo)).toEqual([
      'Marca: Jeep',
      `Até ${formatarMoeda(100000)}`,
      'Até 50.000 km',
    ]);
    expect(contarFiltrosAtivos(filtro)).toBe(3);
  });

  it('remover um filtro tira só ele e volta para a página 1', () => {
    expect(removerFiltro(filtro, 'marca')).toEqual({ pagina: 1, tamanhoPagina: 20, precoMaximo: 100000, kmMaximo: 50000 });
  });
});

describe('endereço do catálogo', () => {
  it('lê filtros e página válidos do endereço', () => {
    const filtro = lerFiltroDoEndereco(new URLSearchParams('marca=jeep&precoMinimo=45000.5&anoMaximo=2023&pagina=3'));
    expect(filtro).toEqual({ pagina: 3, tamanhoPagina: 20, marca: 'jeep', precoMinimo: 45000.5, anoMaximo: 2023 });
  });

  it('ignora valores inválidos sem quebrar e corrige faixas invertidas', () => {
    const filtro = lerFiltroDoEndereco(new URLSearchParams('pagina=0&precoMinimo=abc&kmMaximo=-5&anoMinimo=2024&anoMaximo=2020'));
    expect(filtro).toEqual({ pagina: 1, tamanhoPagina: 20, anoMinimo: 2024 });
  });

  it('escreve só os filtros ativos e a página quando não é a primeira', () => {
    expect(escreverFiltroNoEndereco({ pagina: 1, tamanhoPagina: 20, marca: 'Fiat' }).toString()).toBe('marca=Fiat');
    expect(escreverFiltroNoEndereco({ pagina: 2, tamanhoPagina: 20, kmMaximo: 50000 }).toString()).toBe('kmMaximo=50000&pagina=2');
  });

  it('ler o que foi escrito devolve o mesmo filtro', () => {
    const filtro = { pagina: 4, tamanhoPagina: 20, marca: 'VW', precoMinimo: 30000, precoMaximo: 80000, anoMinimo: 2020, kmMaximo: 60000 };
    expect(lerFiltroDoEndereco(escreverFiltroNoEndereco(filtro))).toEqual(filtro);
  });

  it('cada cartão leva à proposta do veículo', () => {
    expect(caminhoProposta(4)).toBe('/seminovos/4/proposta');
  });
});
