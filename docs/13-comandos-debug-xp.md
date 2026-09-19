# Comandos de debug de XP

Este guia descreve como testar `TotalXp`, `Level` e `Reset` sem editar código nem
matar NPCs repetidamente. Os comandos usam a progressão real do personagem e só
podem ser processados pelo servidor dedicado em ambiente de desenvolvimento.

## Disponibilidade

Os comandos exigem as duas condições abaixo no servidor:

```text
build debug do Godot
DEBUG_COMMANDS_ENABLED=true
```

O script local configura a variável automaticamente:

```bash
./scripts/dev-server.sh
```

Para iniciar o servidor manualmente:

```bash
export DEBUG_COMMANDS_ENABLED=true
godot --headless --path . -- --server
```

Também devem estar configuradas as variáveis de autenticação descritas em
[Subir o ambiente local](03-ambiente-local.md). Uma exportação release continua
bloqueando os comandos mesmo que receba `DEBUG_COMMANDS_ENABLED=true`.

## Abrir a entrada de comandos

1. Inicie API, banco e servidor dedicado.
2. Abra o cliente com `godot --path .`.
3. Faça login e entre com um personagem.
4. Pressione `/` para colocar o foco na entrada de debug do HUD.
5. Digite o comando e pressione `Enter`.

O painel só aparece em builds debug. Pressione `Esc` para devolver o foco ao jogo.
Enquanto a entrada está focada, teclas de texto não movimentam nem fazem o
personagem atacar.

O servidor aplica um intervalo mínimo de 250 milissegundos entre comandos do mesmo
jogador. Aguarde a resposta no painel antes de enviar o próximo comando.

## `/addxp <quantidade>`

Concede a quantidade informada ao personagem conectado:

```text
/addxp 25
/addxp 100
/addxp 10000
/addxp 1000000
```

O comando aceita valores entre `0` e `long.MaxValue`. A quantidade passa por
`Player.AddXp`, que processa XP excedente, múltiplos Levels, transições de Reset e
overflow. O comando não escreve diretamente em `TotalXp`, `Level` ou `Reset`.

Exemplo de resposta:

```text
[DEBUG XP] +10000 XP
Level: 0 -> 69
Reset: 0 -> 0
TotalXp: 25 -> 10025
```

Comportamentos de validação:

| Entrada | Resultado |
|---|---|
| `/addxp 0` | sucesso sem alteração de estado |
| `/addxp -1` | rejeitada por quantidade negativa |
| `/addxp texto` | rejeitada por valor não numérico |
| `/addxp` | rejeitada por quantidade ausente |
| valor acima de `long.MaxValue` | rejeitado sem overflow |

## `/addxpnext`

Concede exatamente o XP que falta para completar o Level atual:

```text
/addxpnext
```

O valor é calculado com o snapshot e a curva usados pela progressão:

```text
XP concedido = XP necessário para o próximo Level - XP atual no Level
```

A fórmula exponencial não é repetida no comando. A concessão também passa por
`Player.AddXp`. No Level 199, o resultado normal é o próximo Reset no Level 0.

Exemplo:

```text
XP atual: 121 / 199
/addxpnext
XP concedido: 78
```

## `/xpinfo`

Exibe o snapshot atual sem modificar o personagem:

```text
/xpinfo
```

Exemplo de resposta:

```text
[XP INFO]
TotalXp: 0
Reset: 0
Level: 0
GlobalLevel: 0
XP atual: 0 / 100
XP restante: 100
```

Os campos têm os seguintes significados:

| Campo | Descrição |
|---|---|
| `TotalXp` | todo o XP adquirido pelo personagem |
| `Reset` | quantidade de ciclos de 200 Levels concluídos |
| `Level` | posição atual entre 0 e 199 |
| `GlobalLevel` | `(Reset * 200) + Level` |
| `XP atual` | progresso dentro do Level atual |
| `XP restante` | quantidade necessária para completar o Level |

## Autoridade e sincronização

O cliente envia somente o texto digitado. O fluxo é:

```text
HUD
  → RPC confiável no Player local
  → servidor valida GetRemoteSenderId e OwnerPeerId
  → servidor confirma peer, node e CharacterId autenticados
  → DebugXpCommands interpreta a entrada
  → Player.AddXp processa a progressão
  → MultiplayerSynchronizer replica TotalXp, Level e Reset
  → HUD recebe o resultado e atualiza a progressão
```

Um cliente não consegue habilitar os comandos localmente. A decisão final usa a
configuração e o tipo de build do servidor. Não existe comando `/setlevel`,
`/setreset` ou `/setxp`.

O estado atualizado é salvo pelo mecanismo normal do personagem na desconexão.
Para validar persistência, execute um comando, desconecte o cliente, entre novamente
e confira `/xpinfo` e o HUD.

## Logs do servidor

Concessões aceitas geram logs somente no evento do comando:

```text
[DEBUG XP] Personagem <CharacterId> recebeu 10000 XP por comando.
[DEBUG XP] Reset 0 / Level 0 -> Reset 0 / Level 69; TotalXp 25 -> 10025.
```

`/xpinfo` registra o snapshot consultado. Não existem logs por frame.

## Validação automatizada

Execute o Harness:

```bash
./scripts/validate.sh
```

O teste `tests/godot/DebugXpCommandsIntegrationTest.tscn` cobre:

- `/addxp 100` usando a progressão real;
- quantidade ausente, negativa, inválida ou acima de `long`;
- bloqueio com comandos de debug desabilitados;
- XP exato de `/addxpnext`;
- `/addxpnext` no Level 199 concluindo Reset;
- `/xpinfo` sem alteração do estado.

## Solução rápida de problemas

### Painel não aparece

Confirme que o cliente está rodando como build debug. O painel não é exibido em uma
exportação release.

### Servidor responde que os comandos estão desabilitados

Inicie com `./scripts/dev-server.sh` ou defina `DEBUG_COMMANDS_ENABLED=true` antes de
iniciar o servidor. Reinicie o processo após alterar a variável.

### Alterei C# e o comportamento não mudou

O servidor em execução continua usando a DLL carregada. Pare o processo, compile e
inicie novamente:

```bash
./scripts/build.sh
./scripts/dev-server.sh
```

### O painel permanece em `Executando...`

Aguarde ao menos 250 milissegundos entre comandos. Se continuar, confira os logs do
servidor, a conexão ENet e se o personagem concluiu a autenticação.
