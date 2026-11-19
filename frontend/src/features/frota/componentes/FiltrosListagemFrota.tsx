import { Campo, Icone, Select, type OpcaoSelect } from '@/components/ui';
import {
  MENSAGEM_FROTA_BUSCA_PLACEHOLDER,
  MENSAGEM_FROTA_BUSCA_ROTULO,
  MENSAGEM_FROTA_CATEGORIA_ROTULO,
  MENSAGEM_FROTA_CATEGORIA_TODAS,
} from '@/lib/mensagens';
import { TAMANHO_MAXIMO_BUSCA_FROTA } from '@/features/frota/listagem/parametrosListagemFrota';

export interface FiltrosListagemFrotaProps {
  /** Texto digitado na busca (o pai decide quando ele vai para a consulta). */
  busca: string;
  aoMudarBusca: (busca: string) => void;
  /** Categorias já montadas em opções (valor = id). Vazio enquanto carregam. */
  opcoesCategoria: readonly OpcaoSelect[];
  /** Id da categoria escolhida, ou vazio para todas. */
  idCategoria: number | '';
  aoMudarCategoria: (idCategoria: number | '') => void;
}

/**
 * Busca e categoria, em linha no desktop, com a folga de 8px acima do desenho (pt-2). As colunas de pátio e câmbio do
 * desenho não existem nesta tela: a busca ocupa 7 colunas e a categoria, 5.
 */
export function FiltrosListagemFrota({
  busca,
  aoMudarBusca,
  opcoesCategoria,
  idCategoria,
  aoMudarCategoria,
}: FiltrosListagemFrotaProps) {
  return (
    <div className="grid grid-cols-1 items-center gap-2 pt-2 md:grid-cols-12">
      <div className="md:col-span-7">
        <Campo
          variante="frota"
          type="search"
          rotulo={MENSAGEM_FROTA_BUSCA_ROTULO}
          ocultarRotulo
          prefixo={<Icone nome="search" tamanho={18} className="text-contorno" />}
          placeholder={MENSAGEM_FROTA_BUSCA_PLACEHOLDER}
          maxLength={TAMANHO_MAXIMO_BUSCA_FROTA}
          autoComplete="off"
          value={busca}
          onChange={(evento) => aoMudarBusca(evento.target.value)}
        />
      </div>
      <div className="md:col-span-5">
        <Select
          variante="frota"
          rotulo={MENSAGEM_FROTA_CATEGORIA_ROTULO}
          ocultarRotulo
          opcaoVazia={MENSAGEM_FROTA_CATEGORIA_TODAS}
          opcoes={opcoesCategoria}
          value={idCategoria === '' ? '' : String(idCategoria)}
          onChange={(evento) => {
            const valor = evento.target.value;
            aoMudarCategoria(valor === '' ? '' : Number(valor));
          }}
        />
      </div>
    </div>
  );
}
