# Testes

## Validação rápida antes de commit

Na raiz:

```bash
./scripts/validate.sh
```

O script executa:

```text
dotnet build dbjao.csproj
dotnet build backend/GameBackend.sln
dotnet test backend/GameBackend.sln
importação de assets e cena principal em Godot headless, quando disponível
teste headless da integração Sidra → killer → progressão, quando Godot está disponível
teste headless dos comandos de debug de XP, quando Godot está disponível
teste headless do HUD compacto e modal de perfil, quando Godot está disponível
git diff --check e git diff --cached --check
```

O build Godot possui atualmente dois warnings `CS0649` preexistentes em
`NetworkManager.Authentication.cs`. Não introduza warnings adicionais. A suíte possui
30 testes neste baseline, mas automações não devem fixar esse número.

Se Godot não for encontrado, o smoke aparece como `SKIP`. Instale Godot 4.7.2 Mono ou
defina `GODOT_BIN=/caminho/para/o/executavel` antes de validar alterações de cenas,
recursos ou networking. Um skip deve ser informado na entrega.

O executável deve ter a mesma versão do `Godot.NET.Sdk` em `dbjao.csproj`. Versões
diferentes também geram `SKIP`, pois o editor pode reescrever o `.csproj` durante a
importação; uma atualização de Godot deve ser uma mudança separada e intencional.

Antes de concluir, a revisão humana/agentic continua obrigatória:

```bash
git status --short
git diff
git diff --cached  # se houver mudanças staged
```

## Smoke test da API

```bash
cd backend
docker compose up -d --build
docker compose ps
docker compose logs --tail=100 api
curl -sS -o /dev/null -w '%{http_code}\n' \
  http://127.0.0.1:5000/swagger/index.html
```

Esperado: `200`.

## Smoke test headless

Com API ativa:

```bash
cd ..
export GAME_API_URL=http://127.0.0.1:5000
export GAME_SERVER_API_KEY='chave-local'
export GAME_SERVER_ID=local-server-01

godot --headless --path . -- --server
```

Confirme:

- cinco NPCs inicializados (dois em Kame House e três em Clean Path);
- backend configurado;
- servidor na porta 7000;
- nenhuma janela;
- nenhuma exceção;
- nenhum Player sem peer autenticado.

## Teste funcional com dois clientes

1. Suba API, banco e servidor.
2. Cadastre duas contas.
3. Crie um personagem em cada conta.
4. Abra dois clientes.
5. Faça login e selecione personagens diferentes.
6. Confirme peerIds diferentes.
7. Confirme dois Players nos dois clientes.
8. Movimente A e observe em B.
9. Movimente B e observe em A.
10. Ataque NPCs e confirme vida idêntica.
11. Mate um NPC e confirme respawn sincronizado.
12. Feche A e confirme remoção em B.
13. Entre novamente com A e confirme posição/vida persistidas.
14. Com A e B conectados, viaje para Clean Path pelo HUD e confirme que ambos veem os jogadores no mapa.
15. Retorne à Kame House e confirme que os dois clientes recebem as posições oficiais.
16. Desconecte em Clean Path e entre novamente para confirmar mapa e posição persistidos.

Para progressão, conceda XP somente por `Player.AddXp` no servidor de teste.
Confirme que dois clientes veem `TotalXp`, `Level`, `Reset` e `BaseBattlePower`
iguais, que uma concessão atravessa o nível 199 e que a reconexão mantém os quatro
valores sem conceder poder novamente.

Para personagem jogável, confirme que os dois clientes recebem `ActiveCharacterId`,
nome e stats iguais, e que o servidor instancia somente cenas registradas em
`CharacterRegistry`.

O teste `tests/godot/SidraXpIntegrationTest.tscn` valida automaticamente que:

- o ataque do jogador usa Attack derivado contra a Defense 10 do Sidra;
- o ataque de contato do Pilaf usa Attack 17 contra a Defense derivada do jogador;
- o RPC de ataque não aceita um dano final informado pelo cliente;
- dano não fatal não concede XP;
- o peer do golpe fatal recebe a recompensa de XP aplicada pelo Sidra;
- dano repetido durante a mesma morte não duplica a recompensa;
- a recompensa pode causar Level Up e Reset;
- XP excedente é preservado;
- Poder de Luta só aumenta pelos Levels realmente concluídos.

O teste `tests/godot/DebugXpCommandsIntegrationTest.tscn` valida `/addxp`, entradas
inválidas, bloqueio quando os comandos estão desabilitados, XP exato do
`/addxpnext`, Reset no Level 199, múltiplos Resets, Poder de Luta por Level,
proteção contra overflow e leitura sem mutação do `/xpinfo`.
Consulte [Comandos de debug de XP](13-comandos-debug-xp.md) para o procedimento
manual e os resultados esperados.

O teste `tests/godot/HudProfileIntegrationTest.tscn` valida o portrait configurável,
as barras permanentes de HP/Ki/XP, ausência de stats detalhados no HUD, abertura pelo
portrait, atualização do modal por signals, quatro multiplicadores, fechamento por X
e Esc e ausência de pausa na árvore.

Os testes `PlayableCharacterTests` cobrem o registry, a definição do Goku,
multiplicadores acima e abaixo de 100%, arredondamento para baixo, BattlePower
compartilhado e saturação numérica. O teste Godot dos comandos também verifica Goku
como personagem padrão, stats derivados após Level Up e preservação de
`ActiveCharacterId` na reconexão.

Os testes `PhysicalDamageCalculatorTests` cobrem Attack igual, maior e menor que a
Defense, multiplicador do golpe, arredondamento para baixo, dano mínimo, integração
com os stats do Goku, entradas inválidas e saturação numérica.

## Casos de autenticação

### Sem token

Conecte um peer sem enviar token. Após 10 segundos:

- deve ser desconectado;
- nenhum Player deve aparecer.

### Token inválido ou expirado

- deve ser rejeitado;
- servidor não cria Player;
- logs não mostram o token.

### Reutilização

Tente validar a mesma sessão duas vezes:

- primeira: sucesso;
- segunda: `SESSION_ALREADY_CONSUMED`.

### Personagem duplicado

Tente entrar com o mesmo personagem em outro cliente:

```text
CHARACTER_ALREADY_ONLINE
```

A sessão antiga permanece conectada.

### API indisponível

Pare a API antes de autenticar:

```bash
cd backend
docker compose stop api
```

Esperado:

- `AUTH_SERVICE_UNAVAILABLE`;
- nenhum Player;
- servidor e NPCs continuam executando.

## Movimento, combate e respawn

Valide:

- cliente não movimenta jogador remoto;
- servidor limita direção e velocidade;
- ataque fora do alcance não causa dano;
- ataque físico do jogador usa seu Attack derivado e a Defense configurada do NPC;
- contato do Pilaf usa o Attack do NPC contra a Defense derivada do jogador;
- cooldown rejeita spam;
- morto não anda nem ataca;
- respawn aplica snap, sem atravessar o mapa;
- interpolação não altera colisão oficial.

## Entrada tardia e desconexão

- conecte cliente depois que NPCs já se moveram;
- confirme posição atual, sem partir de `(0,0)`;
- conecte durante morte de NPC;
- confirme estado morto atual;
- desconecte durante validação;
- desconecte jogador morto antes do respawn;
- confirme ausência de referências e exceções.

## Teste de internet

Em VPS:

- teste UDP `7000`;
- use o IP/DNS público retornado pela API;
- acompanhe perda de pacotes e latência;
- teste encerramento do servidor;
- teste API indisponível;
- confirme persistência após desconexão real.

Não considere `telnet` ou teste TCP como validação da porta ENet: o jogo usa UDP.
