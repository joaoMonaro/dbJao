# Manual operacional para agentes

Este repositório usa um Harness de desenvolvimento: código escrito, isoladamente, não
significa tarefa concluída. Uma mudança só termina depois que as validações aplicáveis
passarem e o diff for revisado.

## Stack

- Godot `4.7.2` Mono com C# e .NET `10`;
- cliente e servidor dedicado no mesmo projeto Godot (`dbjao.csproj`);
- multiplayer ENet/UDP com servidor dedicado autoritativo;
- API ASP.NET Core `10` separada;
- Entity Framework Core `10` com PostgreSQL `17`;
- testes xUnit para o backend;
- Docker Compose para API e banco no ambiente local.

Não há camadas físicas `Client`, `Server` e `Shared` no projeto Godot. Cenas e scripts
são compartilhados e `NetworkManager.RunningAsServer` seleciona o modo por `--server`
ou pela feature `dedicated_server`. O diretório `backend/` é um projeto separado e é
excluído da compilação Godot.

## Fontes de verdade

Consulte somente os documentos relevantes à mudança:

- arquitetura e módulos: `docs/01-visao-geral.md`;
- ambiente e processos: `docs/03-ambiente-local.md`;
- API, banco e migrations: `docs/04-backend-api.md`;
- rede e autoridade: `docs/05-multiplayer-godot.md`;
- autenticação e segredos: `docs/06-autenticacao-seguranca.md`;
- testes: `docs/07-testes.md`;
- exportação dedicada: `docs/08-exportacao-servidor.md`;
- fluxo de desenvolvimento: `docs/10-guia-desenvolvimento.md`;
- decisão de autoridade: `docs/adr/001-server-authoritative-multiplayer.md`.

Se código e documentação divergirem, confirme o comportamento no código, não invente
uma terceira convenção e atualize a documentação no mesmo diff quando estiver no escopo.

## Princípios arquiteturais

### Autoridade do servidor

O cliente envia intenção; o servidor valida o remetente e calcula o estado oficial.
Na implementação atual, o servidor é autoridade sobre:

- identidade autenticada e criação/remoção de jogadores;
- posição, velocidade, colisões e validações de movimento;
- alvo, alcance, cooldown e dano de combate;
- vida, morte e respawn;
- IA e movimento de NPCs;
- estado do personagem enviado para persistência na desconexão.

RPCs `AnyPeer` devem obter o remetente com `Multiplayer.GetRemoteSenderId()` e confirmar
peer, jogador, estado e limites dos valores. Não aceite do cliente como verdade
`peerId`, `CharacterId`, posição oficial, dano, vida ou resultado de uma ação.

Inventário, drops e progressão de XP ainda não possuem um fluxo completo no código.
Quando forem implementados, decisões competitivas ou persistentes devem seguir a mesma
autoridade do servidor; não presuma modelos ou APIs antes da respectiva tarefa.

### Persistência e responsabilidades

O cliente usa JWT apenas com a API pública e envia um token temporário ao servidor
ENet. Somente o servidor dedicado usa a API interna, protegida por
`X-Game-Server-Key`, para validar sessões e salvar estado. A API persiste contas,
personagens e sessões; física, IA e combate não pertencem ao backend HTTP.

## Loop obrigatório

1. Entenda o resultado pedido e os critérios de aceitação.
2. Execute `git status --short` e preserve alterações preexistentes do usuário.
3. Pesquise implementações relacionadas antes de criar código novo.
4. Identifique projetos, cenas, scripts, API, banco e documentação afetados.
5. Leia apenas a documentação arquitetural relevante.
6. Planeje a menor alteração coerente; evite refatorações incidentais.
7. Implemente reutilizando padrões existentes e sem abstrações especulativas.
8. Compile e execute os testes dos projetos afetados.
9. Execute validações Godot e testes manuais de rede quando aplicáveis.
10. Revise `git status --short` e `git diff`; use também `git diff --cached` se houver
    mudanças staged.
11. Corrija problemas encontrados e repita as validações afetadas.
12. Informe comandos, resultados, skips justificados e riscos residuais.

O ponto de entrada padrão é:

```bash
./scripts/validate.sh
```

Scripts específicos:

```bash
./scripts/build.sh
./scripts/test.sh
./scripts/dev-server.sh
```

`validate.sh` compila os dois projetos, executa os testes do backend, importa assets e
tenta iniciar a cena principal em Godot headless, além de verificar whitespace no diff.
Se Godot não estiver disponível ou sua versão diferir do SDK declarado no `.csproj`,
importação e smoke são marcados como `SKIP`; isso deve constar na entrega e não
substitui validação runtime para mudanças em cenas, recursos ou networking. A exigência
de versão exata evita que o editor reescreva o projeto durante uma validação.

Mudanças multiplayer exigem, quando aplicável, servidor headless e ao menos dois
clientes conforme `docs/07-testes.md`. Mudanças de schema exigem revisão de `Up` e
`Down` da migration e teste com banco compatível.

## Regras de alteração e revisão

- Pesquise primeiro e evite duplicação.
- Preserve os limites entre jogo em tempo real e API de persistência.
- Faça alterações pequenas, focadas e compatíveis com a arquitetura atual.
- Não altere arquivos não relacionados nem sobrescreva trabalho preexistente.
- Não crie abstrações, sistemas ou dependências sem uma necessidade atual.
- Não versione `.env`, senhas, chaves, JWTs, tokens ou dados sensíveis em logs.
- Não introduza warnings; há dois `CS0649` preexistentes no build Godot que devem ser
  tratados separadamente, não usados para aceitar warnings novos.
- Não considere compilação isolada suficiente e não omita validações que falharam.
- Antes de concluir, procure no diff código duplicado, debug esquecido, secrets,
  alterações acidentais e mudanças arquiteturais não intencionais.

Use as skills `create-feature` e `fix-bug` em `.agents/skills/` quando correspondentes
à tarefa. Elas complementam este manual; não substituem os critérios específicos do
pedido atual.
