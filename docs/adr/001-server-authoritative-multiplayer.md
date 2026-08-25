# ADR 001 — Multiplayer autoritativo no servidor Godot

Status: aceita e já implementada.

## Decisão

O servidor Godot dedicado, identificado pelo peer `1`, mantém o estado oficial da
simulação multiplayer. Clientes enviam intenções de movimento, ataque e autenticação;
o servidor valida o remetente e executa movimento, colisões, combate, vida, morte,
respawn, IA de NPCs e captura do estado que será persistido.

A API ASP.NET Core permanece fora do loop por frame. Ela autentica contas, emite e
consome sessões temporárias e persiste estado recebido pelo servidor dedicado através
de endpoints internos autenticados.

## Contexto

O jogo é online e estados competitivos ou persistentes não podem depender de valores
declarados pelo cliente. Cliente e servidor compartilham o mesmo projeto Godot; o modo
dedicado é ativado por `--server` ou pela feature `dedicated_server`.

## Motivo

Centralizar a simulação sensível reduz divergência entre peers e impede que um cliente
conceda a si mesmo posição, dano, vida, identidade ou resultado de combate. Manter a
API fora da simulação em tempo real separa persistência de física e networking ENet.

## Consequências

- RPCs aceitos de qualquer peer precisam derivar e validar o remetente no servidor.
- Estado frequente é replicado por `MultiplayerSynchronizer`; spawn de jogadores usa
  `MultiplayerSpawner` somente após autenticação.
- NPCs e componentes de vida mantêm autoridade do servidor e clientes apenas exibem o
  estado replicado, com interpolação visual local.
- Features futuras de inventário, drops e progressão persistente devem ser decididas
  no servidor antes de serem salvas.
- Testes multiplayer precisam cobrir rejeição de intenção inválida, late join,
  desconexão e consistência entre pelo menos dois clientes.
