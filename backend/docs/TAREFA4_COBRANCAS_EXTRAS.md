# Semana 2 — Eduardo Freire — P5-BE-02 / RF-23

## Escopo

Cadastro de parâmetros comerciais pela própria locadora, por categoria de veículo,
e apuração automática de quilometragem excedente, combustível, atraso e avarias
novas. Não há tarifas comerciais predefinidas. A liderança esclareceu que a
locadora deve definir os parâmetros; Eduardo autorizou o cadastro e a migration.

O fluxo preserva as tarefas 1 a 3. Não encerra a locação, não altera o estado do
veículo e não movimenta caução, multas de trânsito ou limpeza.

## Cadastro

Rota administrativa: `/back-office/configuracoes/cobrancas-extras`.

Somente o administrador da locadora mantém as configurações. A empresa é obtida
da sessão autenticada e não é aceita no corpo da requisição. Uma configuração
ativa por empresa e categoria é validada no serviço sob bloqueio da empresa.
Alterações exigem a versão consultada, evitando sobrescrita silenciosa de edições
concorrentes. A exclusão é lógica.

Todos os parâmetros são explícitos: categoria; franquia de km total ou por diária;
valor por km excedente; preço de um tanque completo; tolerância em minutos;
unidade, fracionamento e tarifa de atraso; base de avarias estimadas na vistoria.

## Devolução e apuração

Após a comparação de saída da tarefa 3, a tela consulta a apuração existente.
Quando ela existe, mostra o histórico congelado. Caso contrário, a simulação busca
a configuração ativa da locadora e da categoria do veículo. O funcionário não
informa preços, leituras de instrumentos ou totais nesse fluxo.

A confirmação envia somente o identificador e a versão da configuração utilizada.
Se ela mudou desde a simulação, uma primeira apuração é recusada e deve ser
simulada novamente. O lançamento grava uma apuração, sua memória de cálculo e as
cobranças positivas numa mesma transação. O bloqueio da locação serializa pedidos
concorrentes.

Repetições recuperam a apuração anterior, sem recalcular ou duplicar lançamentos.
Uma apuração sem incidências também é registrada, com lista de cobranças vazia.
Alterações posteriores na política ou nas avarias originais não mudam os
parâmetros e valores congelados no histórico.

## Fórmulas

- Diárias previstas: teto da duração entre retirada e devolução prevista dividida
  por 24 horas. A duração contratada deve ser positiva. O atraso não aumenta o
  pacote de quilômetros.
- Pacote total: franquia informada, ou franquia por diária multiplicada pelas
  diárias previstas. A multiplicação usa inteiro de 64 bits.
- Quilometragem: `max(kmSaida - kmEntrada - pacoteTotal, 0) * valorKmExcedente`.
- Combustível: `max(nivelEntrada - nivelSaida, 0) * valorTanqueCompleto`.
  Os níveis são frações de 0 a 1; a locadora cadastra o preço do tanque por
  categoria. Não se presume capacidade em litros ou preço por litro.
- Atraso: horário persistido da saída menos previsão e tolerância, limitado a
  zero. Dividir por hora ou duração de 24 horas; manter a fração ou arredondar
  para a próxima unidade iniciada, conforme o cadastro; multiplicar pela tarifa.
  A tolerância é descontada antes da contagem.
- Avarias: soma somente dos valores estimados dos snapshots novos da saída.
  Essa base é selecionada explicitamente pela locadora no cadastro.

Valores em `decimal`, arredondados por categoria para duas casas com
`MidpointRounding.AwayFromZero`. O total soma as categorias arredondadas.
Cada categoria cabe em `NUMERIC(10,2)`; o total usa `NUMERIC(12,2)`.

## Bloqueios

Sem política cadastrada, sem duas vistorias consistentes, com combustível ausente,
comparação legada indeterminada ou estimativa desconhecida de nova avaria, a
apuração é recusada. Dados desconhecidos não são convertidos em zero.

Cobranças preexistentes dos quatro tipos RF-23, sem vínculo com uma apuração,
devem ser resolvidas antes de usar o novo fluxo, para evitar duplicidade.
Multas de trânsito e limpeza permanecem independentes.

## API

- `GET/POST /api/configuracoes-cobrancas-extras`
- `GET/PUT/DELETE /api/configuracoes-cobrancas-extras/{id}`
- `POST /api/locacoes/{idLocacao}/cobrancas-extras/simulacao` — sem preços no corpo.
- `GET /api/locacoes/{idLocacao}/cobrancas-extras/apuracao`
- `POST /api/locacoes/{idLocacao}/cobrancas-extras/apuracao` —
  `{ idConfiguracao, versaoEsperada }`; 201 na primeira gravação, 200 na repetição.

Cadastro restrito a administrador; devolução restrita a funcionário autenticado e
à locadora da sessão. Erros seguem ProblemDetails.

## Banco

Migration gerada: `20261119023757_CadastrarRegrasEApurarCobrancasExtras`.
Cria `configuracao_cobranca_extra`, `apuracao_cobranca_extra` e adiciona o vínculo
opcional `cobranca_extra.id_apuracao`. Não há índices únicos nem backfill.
As 32 migrations anteriores foram verificadas por hash e preservadas.

O responsável deve aplicar a migration da tarefa 3 e esta migration, na ordem,
no banco de destino. O agente não executa `database update`. Depois disso, o
administrador deve cadastrar os parâmetros reais da locadora.

## Verificação

Os testes cobrem cálculo, cadastro, permissões, isolamento de empresa, versão
concorrente, apuração com três e quatro cobranças, repetição, histórico congelado
e apuração sem incidências. O navegador verifica os fluxos com API simulada.
Os comandos e resultados da entrega são registrados no relatório de validação.
Testes com fakes e API simulada não equivalem a integração com PostgreSQL real.
