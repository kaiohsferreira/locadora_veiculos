import { useRef, useState, type PointerEvent as ReactPointerEvent } from 'react';
import { Botao } from '@/components/ui';
import { MENSAGENS_CONTRATO_LOCACAO as M } from '@/lib/mensagensContratoLocacao';
import { ALTURA_MAXIMA_ASSINATURA, LARGURA_MAXIMA_ASSINATURA, dimensoesAssinatura } from './regrasContrato';

/**
 * Iguala a resolução do canvas à caixa que a pessoa vê (até o teto de 600 por 200), para o traço e o PNG não ficarem
 * esticados. Só deve ser chamada com o quadro vazio, porque trocar a resolução apaga o desenho.
 */
function ajustarResolucaoAoQuadro(canvas: HTMLCanvasElement): void {
  const retangulo = canvas.getBoundingClientRect();
  if (retangulo.width <= 0 || retangulo.height <= 0) return;
  const largura = Math.min(LARGURA_MAXIMA_ASSINATURA, Math.round(retangulo.width));
  const altura = Math.min(ALTURA_MAXIMA_ASSINATURA, Math.round(retangulo.height));
  if (canvas.width !== largura) canvas.width = largura;
  if (canvas.height !== altura) canvas.height = altura;
}

interface PropsQuadroAssinatura {
  /** Recebe o PNG em base64 (sem o prefixo data:) ao terminar cada traço; null quando o quadro é limpo. */
  aoMudar: (assinaturaBase64: string | null) => void;
  desabilitado?: boolean;
}

interface PontoQuadro {
  x: number;
  y: number;
}

/**
 * Passa o desenho para um PNG dentro do teto (600 por 200 pixels, proporção mantida). O quadro já nasce com o tamanho
 * máximo, então a redução é só a garantia de que o PNG nunca passa do teto.
 */
function exportarAssinatura(canvas: HTMLCanvasElement): string {
  const { largura, altura } = dimensoesAssinatura(canvas.width, canvas.height);
  const reduzido = document.createElement('canvas');
  reduzido.width = largura;
  reduzido.height = altura;
  reduzido.getContext('2d')?.drawImage(canvas, 0, 0, largura, altura);
  const url = reduzido.toDataURL('image/png');
  return url.slice(url.indexOf(',') + 1);
}

/**
 * Quadro de assinatura desenhado com Pointer Events (mouse, caneta e toque). `touch-none` impede que a página role
 * enquanto a pessoa assina. A cor do traço é a do token de texto do quadro (text-navy-profundo), lida no navegador.
 */
export function QuadroAssinatura({ aoMudar, desabilitado = false }: PropsQuadroAssinatura) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const pontoAnterior = useRef<PontoQuadro | null>(null);
  const temTracoRef = useRef(false);
  const [temTraco, setTemTraco] = useState(false);

  function posicaoNoQuadro(evento: ReactPointerEvent<HTMLCanvasElement>): PontoQuadro {
    const canvas = evento.currentTarget;
    const retangulo = canvas.getBoundingClientRect();
    return {
      x: ((evento.clientX - retangulo.left) * canvas.width) / retangulo.width,
      y: ((evento.clientY - retangulo.top) * canvas.height) / retangulo.height,
    };
  }

  function aoIniciar(evento: ReactPointerEvent<HTMLCanvasElement>): void {
    if (desabilitado) return;
    if (!temTracoRef.current) ajustarResolucaoAoQuadro(evento.currentTarget);
    evento.currentTarget.setPointerCapture(evento.pointerId);
    pontoAnterior.current = posicaoNoQuadro(evento);
  }

  function aoMover(evento: ReactPointerEvent<HTMLCanvasElement>): void {
    const anterior = pontoAnterior.current;
    if (!anterior || desabilitado) return;
    const canvas = evento.currentTarget;
    const contexto = canvas.getContext('2d');
    const atual = posicaoNoQuadro(evento);
    if (contexto) {
      contexto.strokeStyle = getComputedStyle(canvas).color;
      contexto.lineWidth = 2.8;
      contexto.lineCap = 'round';
      contexto.lineJoin = 'round';
      contexto.beginPath();
      contexto.moveTo(anterior.x, anterior.y);
      contexto.lineTo(atual.x, atual.y);
      contexto.stroke();
    }
    temTracoRef.current = true;
    pontoAnterior.current = atual;
  }

  function aoSoltar(evento: ReactPointerEvent<HTMLCanvasElement>): void {
    if (pontoAnterior.current === null) return;
    pontoAnterior.current = null;
    if (evento.currentTarget.hasPointerCapture(evento.pointerId)) {
      evento.currentTarget.releasePointerCapture(evento.pointerId);
    }
    if (temTracoRef.current) {
      setTemTraco(true);
      aoMudar(exportarAssinatura(evento.currentTarget));
    }
  }

  function limpar(): void {
    const canvas = canvasRef.current;
    canvas?.getContext('2d')?.clearRect(0, 0, canvas.width, canvas.height);
    temTracoRef.current = false;
    setTemTraco(false);
    aoMudar(null);
  }

  return (
    <div className="flex flex-col gap-2">
      <canvas
        ref={canvasRef}
        width={LARGURA_MAXIMA_ASSINATURA}
        height={ALTURA_MAXIMA_ASSINATURA}
        aria-label={M.quadroNomeAcessivel}
        className="block h-28 w-full touch-none select-none rounded-lg border-b border-dashed border-contorno-suave bg-branco text-navy-profundo"
        onPointerDown={aoIniciar}
        onPointerMove={aoMover}
        onPointerUp={aoSoltar}
        onPointerCancel={aoSoltar}
      />
      {!temTraco && <p className="text-apoio text-texto-apoio">{M.quadroDica}</p>}
      <div className="flex items-center justify-between gap-2">
        <Botao variante="cinzaAzulado" icone="restart_alt" onClick={limpar} disabled={desabilitado}>
          {M.limparAssinatura}
        </Botao>
      </div>
    </div>
  );
}
