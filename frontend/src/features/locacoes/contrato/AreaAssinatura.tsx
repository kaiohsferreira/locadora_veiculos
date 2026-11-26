import { useMemo, useRef, useState, type FormEvent } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm, useWatch } from 'react-hook-form';
import { BotaoLink, Botao, Campo, Icone, useAviso } from '@/components/ui';
import { aplicarErrosDeCampo, obterErroDeCampo, obterMensagemErro } from '@/lib/erros';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import { CAMINHOS } from '@/routes/caminhos';
import type { ContratoLocacao } from '@/types/contratoLocacao';
import { useAssinarContratoLocacao } from './useContratoLocacao';
import { QuadroAssinatura } from './QuadroAssinatura';
import { criarEsquemaAssinatura, type ValoresAssinatura } from './esquemaAssinatura';
import {
  aceiteCombustivelAssinado,
  exigeAceiteCombustivel,
  imagemAssinaturaValida,
  problemaAssinaturaPng,
} from './regrasContrato';

interface PropsAreaAssinatura {
  contrato: ContratoLocacao;
  idLocacao: number;
}

/**
 * Área de assinatura (desenho 08, coluna da direita). Antes de assinar, mostra o formulário com os aceites, o nome e o
 * quadro; depois de assinar, mostra a cópia guardada e não deixa mudar nada.
 */
export function AreaAssinatura({ contrato, idLocacao }: PropsAreaAssinatura) {
  if (contrato.situacao === 'ASSINADO' && contrato.assinatura !== null) {
    return <AssinaturaRealizada contrato={contrato} />;
  }
  return <FormularioAssinatura contrato={contrato} idLocacao={idLocacao} />;
}

function FormularioAssinatura({ contrato, idLocacao }: PropsAreaAssinatura) {
  const aviso = useAviso();
  const envio = useAssinarContratoLocacao(idLocacao);
  const exige = exigeAceiteCombustivel(contrato.documento);
  const esquema = useMemo(() => criarEsquemaAssinatura(exige), [exige]);
  const [assinatura, setAssinatura] = useState<string | null>(null);
  const [erroAssinatura, setErroAssinatura] = useState<string | null>(null);
  // Trava síncrona: dois cliques (ou Enter e clique) antes da primeira renderização não disparam dois POSTs.
  const enviandoRef = useRef(false);

  const { locatario } = contrato.documento;
  const {
    register,
    handleSubmit,
    setError,
    control,
    formState: { errors },
  } = useForm<ValoresAssinatura, unknown, ValoresAssinatura>({
    resolver: zodResolver(esquema),
    defaultValues: { nomeAssinante: locatario.nome, aceiteCondicoesGerais: false, aceitePoliticaCombustivel: false },
  });
  const nomeDigitado = useWatch({ control, name: 'nomeAssinante' });
  const mensagemAceite = errors.aceiteCondicoesGerais?.message ?? errors.aceitePoliticaCombustivel?.message;

  const aoEnviar = handleSubmit(async (valores) => {
    const problema = problemaAssinaturaPng(assinatura);
    setErroAssinatura(problema);
    if (problema !== null || assinatura === null) return;
    try {
      await envio.mutateAsync({
        nomeAssinante: valores.nomeAssinante,
        aceiteCondicoesGerais: valores.aceiteCondicoesGerais,
        aceitePoliticaCombustivel: exige ? valores.aceitePoliticaCombustivel : null,
        assinaturaPngBase64: assinatura,
      });
      aviso.sucesso(M.assinadoComSucesso);
    } catch (erro: unknown) {
      const aplicouNome = aplicarErrosDeCampo(erro, ['nomeAssinante'] as const, (campo, mensagem) =>
        setError(campo, { message: mensagem }),
      );
      // O servidor também recusa a assinatura (PNG, tamanho) e o aceite de combustível: cada um vai ao seu lugar.
      const erroPng = obterErroDeCampo(erro, 'assinaturaPngBase64');
      if (erroPng !== undefined) setErroAssinatura(erroPng);
      // Aceites recusados pelo servidor (400) vão para o próprio aceite, com a mensagem do servidor.
      const erroAceiteGerais = obterErroDeCampo(erro, 'aceiteCondicoesGerais');
      if (erroAceiteGerais !== undefined) setError('aceiteCondicoesGerais', { message: erroAceiteGerais });
      const erroAceite = obterErroDeCampo(erro, 'aceitePoliticaCombustivel');
      if (erroAceite !== undefined) setError('aceitePoliticaCombustivel', { message: erroAceite });
      const aplicouAceite = erroAceiteGerais !== undefined || erroAceite !== undefined;
      // Erro sem campo (409 de já assinado ou de RP-06, 403, 404, 400 do corpo): mostra o texto do servidor no aviso.
      if (!aplicouNome && erroPng === undefined && !aplicouAceite) aviso.erro(obterMensagemErro(erro));
    }
  });

  function aoSubmeter(evento: FormEvent<HTMLFormElement>): void {
    evento.preventDefault();
    if (enviandoRef.current) return;
    enviandoRef.current = true;
    // O quadro é conferido a cada envio, mesmo quando os campos já estão corretos.
    setErroAssinatura(problemaAssinaturaPng(assinatura));
    void aoEnviar(evento).finally(() => {
      enviandoRef.current = false;
    });
  }

  return (
    <form noValidate onSubmit={aoSubmeter} className="flex flex-col gap-4 rounded-xl bg-branco p-6 shadow-md">
      <section className="flex flex-col gap-2">
        <h2 className="flex items-center gap-2 text-titulo-md font-bold text-texto-desenho">
          <Icone nome="gavel" tamanho={20} className="text-frota-oficina" />
          {M.termosTitulo}
        </h2>
        <label className="flex cursor-pointer select-none items-start gap-2 rounded-lg bg-steel-fundo p-2 transition-colors hover:bg-steel-suave">
          <input
            type="checkbox"
            aria-invalid={errors.aceiteCondicoesGerais !== undefined}
            className="mt-0.5 h-5 w-5 shrink-0 accent-navy-profundo"
            {...register('aceiteCondicoesGerais')}
          />
          <span className="flex flex-col">
            <span className="text-corpo-denso font-semibold text-texto-desenho">{M.aceiteCondicoesTitulo}</span>
            <span className="text-apoio text-texto-apoio">{M.aceiteCondicoesTexto}</span>
          </span>
        </label>
        {exige && (
          <label className="flex cursor-pointer select-none items-start gap-2 rounded-lg bg-steel-fundo p-2 transition-colors hover:bg-steel-suave">
            <input
              type="checkbox"
              aria-invalid={errors.aceitePoliticaCombustivel !== undefined}
              className="mt-0.5 h-5 w-5 shrink-0 accent-navy-profundo"
              {...register('aceitePoliticaCombustivel')}
            />
            <span className="flex flex-col">
              <span className="text-corpo-denso font-semibold text-texto-desenho">{M.aceiteCombustivelTitulo}</span>
              <span className="text-apoio text-texto-apoio">{M.aceiteCombustivelTexto}</span>
            </span>
          </label>
        )}
        {mensagemAceite !== undefined && (
          <p role="alert" className="text-apoio font-semibold text-status-perigo">
            {mensagemAceite}
          </p>
        )}
      </section>

      <section className="flex flex-col gap-3">
        <h2 className="flex items-center gap-2 text-titulo-md font-bold text-texto-desenho">
          <Icone nome="signature" tamanho={20} className="text-frota-oficina" />
          {M.assinaturaTitulo}
        </h2>
        <Campo
          rotulo={M.nomeAssinanteRotulo}
          obrigatorio
          erro={errors.nomeAssinante?.message}
          autoComplete="off"
          {...register('nomeAssinante')}
        />
        <div className="flex flex-col gap-2 rounded-xl bg-steel-fundo p-3">
          <span className="text-micro uppercase tracking-wider text-contorno">
            {M.assinaturaRotuloAntes}
          </span>
          <QuadroAssinatura
            aoMudar={(base64) => {
              setAssinatura(base64);
              setErroAssinatura(null);
            }}
            desabilitado={envio.isPending}
          />
          <p className="text-apoio text-texto-apoio">
            {`${nomeDigitado} • ${M.assinanteCpf(locatario.cpfMascarado)}`}
          </p>
        </div>
        {erroAssinatura !== null && (
          <p role="alert" className="text-apoio font-semibold text-status-perigo">
            {erroAssinatura}
          </p>
        )}
      </section>

      <Botao
        type="submit"
        variante="laranjaAcao"
        larguraTotal
        icone="lock_person"
        carregando={envio.isPending}
      >
        {envio.isPending ? M.assinando : M.assinarContrato}
      </Botao>
    </form>
  );
}

function AssinaturaRealizada({ contrato }: { contrato: ContratoLocacao }) {
  const assinatura = contrato.assinatura;
  if (assinatura === null) return null;
  const { locatario } = contrato.documento;
  return (
    <section className="flex flex-col gap-4 rounded-xl bg-branco p-6 shadow-md">
      <h2 className="flex items-center gap-2 text-titulo-md font-bold text-texto-desenho">
        <Icone nome="signature" tamanho={20} className="text-frota-oficina" />
        {M.assinaturaTitulo}
      </h2>
      <div className="flex flex-col gap-2 rounded-xl bg-steel-fundo p-3">
        <span className="text-micro uppercase tracking-wider text-contorno">{M.assinaturaRotuloDepois}</span>
        {imagemAssinaturaValida(assinatura.assinaturaPngBase64) ? (
          <img
            src={`data:image/png;base64,${assinatura.assinaturaPngBase64}`}
            alt={M.quadroNomeAcessivel}
            className="block h-28 w-full rounded-lg bg-branco object-contain"
          />
        ) : (
          <p role="alert" className="flex h-28 items-center justify-center text-apoio font-semibold text-status-perigo">
            {M.assinaturaIndisponivel}
          </p>
        )}
        {/* Carimbo logo abaixo da imagem (anexo tela 08): fica aqui, não no bloco do registro. */}
        <p className="text-micro text-contorno">
          {M.carimboAssinatura(assinatura.assinadoEmStr, assinatura.nomeFuncionarioColetor)}
        </p>
        <p className="text-apoio text-texto-apoio">{`${assinatura.nomeAssinante} • ${M.assinanteCpf(locatario.cpfMascarado)}`}</p>
      </div>
      <div className="flex flex-col gap-1 border-t border-steel-alto pt-3">
        <span className="flex items-center gap-1 text-rotulo text-texto-desenho">
          <Icone nome="fingerprint" tamanho={14} className="text-orange-acao" />
          {M.registroTitulo}
        </span>
        <span className="text-micro text-contorno">
          {M.registroLinha(assinatura.assinadoEmStr, assinatura.nomeAssinante, assinatura.nomeFuncionarioColetor)}
        </span>
        {contrato.hashSha256 !== null && (
          <span className="break-all text-dado-tabular text-texto-apoio">{`${M.hashRotulo}: ${contrato.hashSha256}`}</span>
        )}
      </div>
      <ul className="flex flex-col gap-1">
        <li className="flex items-center gap-2 text-apoio text-texto-apoio">
          <Icone nome="check_circle" tamanho={16} className="text-navy" />
          {`${M.aceiteCondicoesTitulo}: ${M.aceito}`}
        </li>
        {aceiteCombustivelAssinado(assinatura, contrato.documento) && (
          <li className="flex items-center gap-2 text-apoio text-texto-apoio">
            <Icone nome="check_circle" tamanho={16} className="text-navy" />
            {`${M.aceiteCombustivelTitulo}: ${M.aceito}`}
          </li>
        )}
      </ul>
      <BotaoLink to={CAMINHOS.locacoes} variante="contorno" icone="arrow_back">
        {M.voltarLocacoes}
      </BotaoLink>
    </section>
  );
}
