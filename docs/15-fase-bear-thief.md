# Fase Bear Thief

Bear Thief é o primeiro vertical slice de uma fase multiplayer. O conteúdo é
compartilhado no mundo do servidor e usa quatro `MapId`s:

```text
bear_thief_01 -> bear_thief_02 -> bear_thief_03 -> bear_thief_boss
```

`scenes/stages/BearThiefStage.tscn` instancia `CleanPath.tscn` nas quatro áreas.
Cada área possui `LeftEntrySpawnPoint` e `RightEntrySpawnPoint`; as três áreas
normais também possuem cinco pontos explícitos para Wolves. `StageRoute` define a
sequência e o lado de entrada, enquanto o `Player` detecta o limite do mapa no
movimento autoritativo e aplica a transição.

## Inimigos e spawn

`EnemyRegistry` centraliza os valores provisórios:

| Inimigo | CombatPower | Attack | Defense | MaxHealth | XP |
|---|---:|---:|---:|---:|---:|
| Wolf | 1.500 | 1.200 | 900 | 1.500 | 25 |
| Bear Thief | 5.000 | 5.000 | 4.500 | 25.000 | 500 |

Cada `WolfSpawner` mantém no máximo cinco Wolves. A primeira população aparece
quando há jogador na área; depois de uma morte, cada vaga é reposta separadamente
após três segundos. O timer de reposição não avança enquanto a área está vazia.

`BossSpawner` mantém no máximo um Bear Thief. O primeiro spawn fica disponível ao
entrar na arena. A morte inicia um cooldown em memória de 300 segundos, que avança
mesmo sem jogadores; terminado o prazo, o boss reaparece quando houver alguém na
arena. Reiniciar o servidor reinicia esse estado.

Wolves e boss são adicionados a `StageNPCs`, replicado por `StageNpcSpawner`.
Clientes recebem posição, movimento, animação, área e vida, mas não executam spawn,
IA, dano ou recompensa.

## Recompensa e conclusão

Wolves concedem o XP configurado ao jogador do golpe final. O Bear Thief registra
todo peer que causou dano válido durante sua vida. Na morte, recebe XP integral e
conclusão quem participou e ainda está em `bear_thief_boss`.

`Player.CompletedStageIds` mantém o estado replicado em runtime. A persistência usa
a relação genérica `character_completed_stages`, cuja chave composta
`(CharacterId, StageId)` torna a conclusão idempotente. O servidor envia a lista no
mesmo snapshot autoritativo do personagem e solicita o salvamento imediatamente
quando uma nova conclusão ocorre. Repetir o boss continua concedendo XP, mas não
adiciona outra conclusão.

## Extensão

Uma fase futura pode reutilizar `StageArea`, `StageRoute`, os controladores de vagas,
`EnemyDefinition` e o fluxo de conclusão. Seus dados próprios devem definir áreas,
spawn points, inimigos, boss e recompensas sem condicionais na lógica de combate.
