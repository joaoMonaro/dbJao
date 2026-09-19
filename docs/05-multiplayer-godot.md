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

## Mapas e viagem

`KameHouse.tscn` mantém o `NetworkManager`, os jogadores e os NPCs durante a viagem.
`CleanPath.tscn` é instanciada como cenário em outra região do mesmo mundo. Os limites,
IDs e pontos de chegada estão em `scripts/WorldMaps.cs`.

O HUD envia somente o ID do destino. O servidor valida o peer autenticado, o estado
do jogador e o destino permitido; então define `MapId`, posição e ponto de respawn.
`MultiplayerSynchronizer` replica `MapId` e posição. A câmera local usa os limites
do mapa atual. Na reconexão, posição e mapa salvos são validados antes do spawn.

## NPCs

`Sidra`, `Pilaf` e os três Pilafs de Clean Path ficam diretamente em
`KameHouse/NPCs`.

- IA e aleatoriedade rodam somente no servidor;
- movimento e colisão são oficiais no servidor;
- posição, velocidade, direção e estado são sincronizados;
- conexão de jogador não recria NPC;
- morte e respawn mantêm o mesmo node.

## Progressão

`Player.AddXp(long amount)` é a entrada para futuras recompensas. Ela executa
somente no servidor dedicado e recebe a quantidade final de XP; o cliente não
possui RPC para conceder XP. A origem da recompensa fica fora da progressão.

`CharacterProgression` aplica os níveis e resets; `ExponentialXpCurve` calcula o
custo do próximo nível. `XpCurveSettings.Default` centraliza `BaseXp = 100`,
`GrowthRate = 1.01` e `LevelsPerReset = 200`. Uma mudança em `LevelsPerReset`
também exige ajustar a validação e a constraint do banco em nova migration.

`TotalXp` é histórico e não é consumido. O XP dentro do nível é derivado de
`TotalXp`; `Level` e `Reset` são reconstruídos desse total no login. O servidor
replica os três campos pelo `MultiplayerSynchronizer` e os salva junto com o
estado do personagem na desconexão. O Player expõe os sinais `XpGained`,
`LevelUp` e `ResetCompleted` para reações à progressão.

### Poder de Luta

`BaseBattlePower` é o Poder de Luta permanente e persistido. A configuração fica em
`BattlePowerSettings.Default`, com `InitialBattlePower = 10` e
`BattlePowerPerLevel = 100`. `BattlePowerProgression` concentra os cálculos com
aritmética verificada contra overflow.

`Player.AddXp` conta os Levels realmente concluídos em `ProgressionResult.Steps` e
aplica um incremento para cada item. Assim, XP insuficiente não altera o poder, uma
concessão que atravessa vários Levels aplica todos os incrementos e a passagem de
Level 199 para o próximo Reset aplica somente o incremento daquele Level. Reset não
possui multiplicador ou bônus próprio.

O servidor calcula o novo valor antes de modificar XP, Level, Reset ou poder; se o
cálculo exceder `long`, a operação inteira é rejeitada. `BaseBattlePower` segue o
mesmo snapshot de persistência do personagem e é replicado pelo
`MultiplayerSynchronizer`. O sinal `BattlePowerChanged` atualiza o HUD do jogador
local. Sincronização, reconexão e eventos de apresentação não concedem poder.

### Personagem jogável ativo e atributos derivados

`scripts/Characters/CharacterRegistry.cs` é a fonte central das definições jogáveis.
Cada `CharacterDefinition` contém ID, nome, multiplicadores de Attack, Defense e
KiAttack e o caminho da cena do jogador. O primeiro registro é `goku`, com os três
multiplicadores em `1.00` e a cena `res://scenes/Player.tscn`.
Na inicialização, o `NetworkManager` registra no `MultiplayerSpawner` todas as cenas
expostas pelo registry; o caminho não fica repetido na lógica de spawn.

O registro persistido mantém somente `ActiveCharacterId`; os multiplicadores e os
atributos calculados não são persistidos. Após autenticar, o servidor resolve esse ID
no registry e instancia a cena configurada. Um ID inexistente gera erro no log e é
normalizado para `goku`; o snapshot salvo na desconexão persiste o fallback. O cliente
não possui RPC para escolher ou alterar o personagem ativo.

`CombatStatsCalculator` recebe o `BaseBattlePower`, que nesta etapa também representa
o Poder de Luta efetivo, e a definição ativa. Ele calcula separadamente:

```text
Attack   = floor(BaseBattlePower * AttackMultiplier)
Defense  = floor(BaseBattlePower * DefenseMultiplier)
KiAttack = floor(BaseBattlePower * KiAttackMultiplier)
```

Os resultados usam `long`, são saturados em `long.MaxValue` quando necessário e são
recalculados após mudança de Poder de Luta ou personagem ativo. `ActiveCharacterId`
é replicado pelo `MultiplayerSynchronizer`; cada cliente resolve a mesma definição e
exibe o nome e os stats no HUD. O BattlePower permanece no Player e não é copiado
para a definição.

Ataques físicos usam `PhysicalDamageCalculator`, que recebe o Attack efetivo do
atacante, a Defense efetiva do alvo e o multiplicador do golpe:

```text
Damage = floor(Attack * (Attack / (Attack + Defense)) * AttackMultiplier)
```

Depois que alcance, direção, cooldown e estado confirmam um acerto, o dano mínimo é
1. O cálculo usa `decimal`, valida stats e multiplicador, evita soma inteira com
overflow e satura em `long.MaxValue` se o resultado exceder o tipo. `DamageInfo`
transporta o resultado como `long`; `HealthComponent` mantém HP como `int` e elimina
o alvo com segurança quando o dano é maior ou igual à vida restante.

O jogador obtém Attack e Defense de `CurrentCombatStats`. O RPC `RequestAttack` não
recebe dano, Attack ou Defense: ele continua sendo somente a intenção de atacar, e o
servidor calcula o valor após validar o remetente e o alvo. O ataque básico usa
multiplicador `1.0`.

NPCs possuem Attack e Defense configuráveis diretamente em `NpcBase`, sem usar
BattlePower ou `CharacterDefinition`. O Sidra começa com Defense 10. O Pilaf começa
com Attack 17 e Defense 10; seu contato também usa `PhysicalDamageCalculator` contra
a Defense derivada do jogador, com multiplicador `1.0`. `KiAttack` permanece fora do
combate físico.

O Sidra é a primeira fonte de XP integrada. Sua propriedade exportada `XpReward`
vale 25. Quando `HealthComponent` aceita o golpe fatal, `NpcBase` encaminha o
`DamageInfo` para `Sidra.OnKilled`. O Sidra valida que a origem é um jogador,
resolve `AttackerPeerId` no container autoritativo `Players` e chama
`Player.AddXp`. Dano posterior à morte é rejeitado por `HealthComponent`, portanto
a mesma morte não gera uma segunda recompensa.

### Comandos de debug de XP

Builds debug exibem um botão `XP` recolhido no canto superior direito do HUD. O botão
ou a tecla `/` abre a entrada para `/addxp <quantidade>`, `/addxpnext` e `/xpinfo`;
`Esc` recolhe o painel. O cliente envia somente o texto do comando por RPC confiável
no seu próprio `Player`. O servidor confirma o remetente com
`GetRemoteSenderId()`, `OwnerPeerId`, o node autenticado e `CharacterId`.

O servidor aceita os comandos apenas quando roda em build debug e a variável
`DEBUG_COMMANDS_ENABLED=true` está definida. `scripts/dev-server.sh` ativa essa
variável por padrão; ela permanece desabilitada quando ausente. `/addxp` e
`/addxpnext` chamam `Player.AddXp`, preservando a mesma progressão usada pelo Sidra.
`/xpinfo` apenas consulta o snapshot reconstruído de `TotalXp` e exibe também o
`BaseBattlePower` atual.

Instruções de uso, respostas, validações e solução de problemas estão no guia
[Comandos de debug de XP](13-comandos-debug-xp.md).

## Combate e vida

- cliente solicita ataque;
- servidor valida peer, estado, cooldown, direção e alcance;
- servidor identifica NPCs atingidos;
- servidor resolve Attack e Defense e calcula o dano físico;
- dano e vida só mudam no servidor;
- morte bloqueia movimento, IA e ataque;
- respawn é controlado pelo servidor.

`CombatStatsCalculator` calcula os stats do jogador, `PhysicalDamageCalculator`
calcula somente o dano, e `HealthComponent` aplica esse dano. Morte e recompensa
continuam fora dos calculadores. `HealthComponent` é reutilizado por jogadores e NPCs.

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

## Guardrails automatizáveis no futuro

O código já possui verificações runtime de `RunningAsServer`, `Multiplayer.IsServer()`,
autoridade do peer `1` e remetente de RPC. Ainda não existe análise arquitetural
automatizada. Bons primeiros guardrails seriam:

- localizar RPCs `AnyPeer` sem validação de `GetRemoteSenderId()`;
- testar que movimento e ataque enviados por um peer não controlam outro Player;
- testar que dano, morte e respawn são rejeitados fora do servidor;
- impedir que o cliente chame endpoints internos de persistência;
- testar que Player só nasce após consumo válido e único da sessão.

Esses guardrails devem começar como testes focados; não crie um analisador estático
próprio antes de existir um caso que justifique sua manutenção.
