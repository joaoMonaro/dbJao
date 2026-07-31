# Multiplayer Godot

## Inicialização

`NetworkManager` está no node raiz de `KameHouse.tscn`.

Servidor:

```bash
godot --headless --path . -- --server
```

Cliente:

```bash
godot --path .
```

No executável exportado, use:

```bash
./server.x86_64 --headless --server
```

## Rede

```text
ENet UDP: 7000
Peer do servidor: 1
Máximo inicial: 32 clientes
Heartbeat de input: 0,1 segundo
```

Constantes: `scripts/Network/NetworkConstants.cs`.

## Jogadores

- o servidor recebe conexão e cria uma entrada pendente;
- o Player não existe antes da autenticação;
- após validar, o servidor instancia `Player.tscn`;
- o nome do node continua baseado no `peerId`;
- identidade da conta e `CharacterId` são propriedades separadas;
- `MultiplayerSpawner` replica criação e remoção;
- o servidor remove e salva o Player na desconexão.

## Movimento

```text
cliente captura direção
  → RPC não confiável
  → servidor valida remetente
  → servidor calcula Velocity
  → MoveAndSlide
  → MultiplayerSynchronizer replica estado
```

O cliente nunca envia posição oficial.

Input Map atual:

- `move_up`;
- `move_down`;
- `move_left`;
- `move_right`;
- `attack`.

Movimento possui WASD e setas configurados em `project.godot`.

## NPCs

`Sidra` e `Pilaf` ficam diretamente em `KameHouse/NPCs`.

- IA e aleatoriedade rodam somente no servidor;
- movimento e colisão são oficiais no servidor;
- posição, velocidade, direção e estado são sincronizados;
- conexão de jogador não recria NPC;
- morte e respawn mantêm o mesmo node.

## Combate e vida

- cliente solicita ataque;
- servidor valida peer, estado, cooldown, direção e alcance;
- servidor identifica NPCs atingidos;
- dano e vida só mudam no servidor;
- morte bloqueia movimento, IA e ataque;
- respawn é controlado pelo servidor.

`HealthComponent` é reutilizado por jogadores e NPCs.

## Interpolação

O node físico permanece na posição oficial. `NetworkInterpolation2D` suaviza
`VisualRoot` apenas nos clientes.

```text
raiz física oficial
├── CollisionShape2D
├── NetworkInterpolation
└── VisualRoot interpolado
```

Morte, respawn, teleporte e correções grandes limpam a interpolação e aplicam snap.
O servidor headless não interpola.

## Regras para novas funcionalidades

- toda alteração persistente ou competitiva deve ser validada no servidor;
- não aceite `peerId`, dano, vida ou posição como verdade enviada pelo cliente;
- obtenha o remetente por `Multiplayer.GetRemoteSenderId()`;
- eventos pontuais importantes usam `Reliable`;
- estado frequente de movimento usa mecanismo não confiável;
- mantenha paths determinísticos;
- não execute física remota nos clientes.
