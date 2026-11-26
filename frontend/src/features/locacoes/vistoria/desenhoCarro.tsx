import type { AnguloAvaria } from '@/types/avarias';

/**
 * Wireframes dos quatro lados do desenho 19 (SVG copiado do desenho). Decorativos: quem fala com o leitor de tela é o
 * quadrante. Os traços usam o token `stroke-contorno` no lugar do hex do desenho. Sem cor literal.
 */

const CLASSES_TRACO = 'text-contorno stroke-current fill-none stroke-[1.5]';

function FrenteSvg() {
  return (
    <svg className={`${CLASSES_TRACO} w-48 h-36`} viewBox="0 0 240 160" aria-hidden="true" focusable="false">
      <path className="fill-steel-alto/40" d="M55,60 L70,25 L170,25 L185,60 Z" />
      <path className="stroke-contorno" d="M120,25 L120,60" strokeDasharray="2 2" />
      <path d="M45,60 L195,60 L190,95 L50,95 Z" />
      <rect className="fill-steel-suave" height="18" rx="2" width="80" x="80" y="70" />
      <line strokeWidth="2.5" x1="88" x2="88" y1="72" y2="86" />
      <line strokeWidth="2.5" x1="98" x2="98" y1="72" y2="86" />
      <line strokeWidth="2.5" x1="108" x2="108" y1="72" y2="86" />
      <line strokeWidth="2.5" x1="120" x2="120" y1="72" y2="86" />
      <line strokeWidth="2.5" x1="132" x2="132" y1="72" y2="86" />
      <line strokeWidth="2.5" x1="142" x2="142" y1="72" y2="86" />
      <line strokeWidth="2.5" x1="152" x2="152" y1="72" y2="86" />
      <circle className="fill-steel-alto/60" cx="62" cy="78" r="9" />
      <circle className="fill-steel-alto/60" cx="178" cy="78" r="9" />
      <path d="M38,95 L202,95 L196,135 L44,135 Z" />
      <rect height="16" rx="2" strokeDasharray="4 2" width="90" x="75" y="110" />
      <rect className="fill-steel-suave" height="40" rx="3" width="14" x="25" y="95" />
      <rect className="fill-steel-suave" height="40" rx="3" width="14" x="201" y="95" />
      <path d="M48,55 L32,50 L34,65 L48,62 Z" />
      <path d="M192,55 L208,50 L206,65 L192,62 Z" />
    </svg>
  );
}

function TraseiraSvg() {
  return (
    <svg className={`${CLASSES_TRACO} w-48 h-36`} viewBox="0 0 240 160" aria-hidden="true" focusable="false">
      <path className="fill-steel-alto/40" d="M60,60 L75,25 L165,25 L180,60 Z" />
      <path d="M120,25 L120,38" strokeWidth="2" />
      <path d="M46,60 L194,60 L188,110 L52,110 Z" />
      <rect className="fill-steel-alto/60" height="20" rx="2" width="16" x="52" y="65" />
      <line strokeWidth="1.5" x1="53" x2="67" y1="66" y2="84" />
      <line strokeWidth="1.5" x1="67" x2="53" y1="66" y2="84" />
      <rect className="fill-steel-alto/60" height="20" rx="2" width="16" x="172" y="65" />
      <line strokeWidth="1.5" x1="173" x2="187" y1="66" y2="84" />
      <line strokeWidth="1.5" x1="187" x2="173" y1="66" y2="84" />
      <rect className="fill-steel-suave" height="15" rx="1.5" width="40" x="100" y="80" />
      <path d="M40,110 L200,110 L192,135 L48,135 Z" />
      <circle cx="180" cy="130" r="3.5" />
      <rect className="fill-steel-suave" height="40" rx="3" width="14" x="26" y="95" />
      <rect className="fill-steel-suave" height="40" rx="3" width="14" x="200" y="95" />
    </svg>
  );
}

/** Perfil do carro. `espelhado` vira o lado direito (o desenho usa o mesmo traço com escala -1 no eixo X). */
function LateralSvg({ espelhado }: { espelhado: boolean }) {
  return (
    <svg
      className={`${CLASSES_TRACO} w-60 h-32 ${espelhado ? 'scale-x-[-1]' : ''}`}
      viewBox="0 0 280 140"
      aria-hidden="true"
      focusable="false"
    >
      <path d="M22,90 L26,75 L60,65 L80,38 L190,38 L220,68 L255,75 L260,95 L250,98 L220,98 L200,98 L80,98 L60,98 Z" />
      <path className="fill-steel-alto/30" d="M85,42 L135,42 L135,66 L70,66 Z" />
      <path className="fill-steel-alto/30" d="M142,42 L185,42 L210,66 L142,66 Z" />
      <line x1="138" x2="138" y1="42" y2="98" />
      <line x1="72" x2="72" y1="66" y2="98" />
      <line x1="205" x2="205" y1="66" y2="98" />
      <circle cx="62" cy="98" r="18" strokeWidth="2" />
      <circle className="fill-steel-suave" cx="62" cy="98" r="9" />
      <circle cx="218" cy="98" r="18" strokeWidth="2" />
      <circle className="fill-steel-suave" cx="218" cy="98" r="9" />
      <line strokeWidth="2.5" x1="120" x2="130" y1="72" y2="72" />
      <line strokeWidth="2.5" x1="185" x2="195" y1="72" y2="72" />
    </svg>
  );
}

/** Desenho do lado indicado: frente, traseira, lateral esquerda ou lateral direita (espelhada). */
export function DesenhoAngulo({ angulo }: { angulo: AnguloAvaria }) {
  switch (angulo) {
    case 'FRENTE':
      return <FrenteSvg />;
    case 'TRASEIRA':
      return <TraseiraSvg />;
    case 'LATERAL_ESQUERDA':
      return <LateralSvg espelhado={false} />;
    case 'LATERAL_DIREITA':
      return <LateralSvg espelhado />;
  }
}
