# Guia para continuar o desenvolvimento

## Estrutura do repositório

```text
backend/                   API e banco
scenes/                    cenas Godot
scripts/                   scripts Godot C#
  Api/                     cliente HTTP
  Combat/                  vida e dano
  Network/                 ENet, spawn e interpolação
  ServerAuth/              autenticação servidor → API
  ui/                      HUD
project.godot              projeto e Input Map
export_presets.cfg         presets de exportação
dbjao.csproj               assembly Godot
docs/                      documentação
```

## Fluxo recomendado de trabalho

1. atualize a branch;
2. suba API e banco;
3. faça uma mudança pequena;
4. compile o projeto afetado;
5. execute testes automatizados;
6. teste servidor headless;
7. teste dois clientes quando alterar rede;
8. revise logs e `git diff`;
9. atualize a documentação se mudar comandos, portas ou arquitetura.

```bash
./scripts/validate.sh
git status --short
git diff
```

Use `./scripts/build.sh` ou `./scripts/test.sh` durante iterações rápidas, mas execute
o ponto de entrada completo antes de concluir. O procedimento operacional está em
`AGENTS.md`; tarefas de feature e bug também possuem skills em `.agents/skills/`.

## Adicionar recursos Godot

- importe sprites, áudio e fontes pelo editor;
- use caminhos `res://`, nunca caminhos absolutos da máquina;
- mantenha arquivos relacionados próximos à cena;
- preserve os `.import` gerenciados pelo Godot;
- não torne sprite, áudio ou câmera obrigatórios no headless;
- colisões, shapes e navegação necessários à simulação devem existir no servidor.

Ao reorganizar node:

- revise `NodePath` exportados;
- revise paths do `MultiplayerSynchronizer`;
- revise buscas como `VisualRoot/AnimatedSprite2D`;
- teste entrada tardia e exportação.

## Adicionar propriedade sincronizada

1. determine quem possui autoridade;
2. use tipo simples serializável;
3. configure no `SceneReplicationConfig`;
4. escolha frequência e confiabilidade;
5. valide late join;
6. não sincronize algo derivável localmente;
7. não entregue autoridade física ao cliente.

Para movimento visual, prefira sincronizar estado oficial e derivar animação.

## Adicionar RPC

No servidor:

```csharp
int senderId = Multiplayer.GetRemoteSenderId();
```

Valide:

- execução realmente no servidor;
- peer conectado e autenticado;
- jogador associado ao sender;
- estado permite a ação;
- valores finitos e limitados;
- cooldown/rate limit;
- alvo ainda existe.

Nunca aceite como verdade:

```text
peerId
CharacterId
dano
vida
posição oficial
resultado do ataque
```

## Adicionar endpoint na API

Fluxo preferido:

```text
DTO → Controller → Service → Repository → DbContext
```

Depois:

1. registre dependências;
2. aplique autorização;
3. valide propriedade do recurso;
4. adicione rate limit quando necessário;
5. crie migration se o schema mudou;
6. escreva testes;
7. atualize Swagger e docs.

Não coloque lógica em tempo real na API.

## Alterar banco

- altere entidade e configuração EF;
- gere migration;
- leia `Up` e `Down`;
- considere dados existentes;
- teste em banco limpo e atualizado;
- não edite a migration já aplicada em produção.

## Próximas etapas coerentes

- salvamento periódico controlado;
- refresh token e logout de sessões web;
- reconexão segura;
- TLS e segredo gerenciado;
- testes de integração ENet automatizados;
- métricas e health checks;
- latência/perda de pacotes em VPS;
- persistência transacional de progressão e inventário.

Qualquer inventário, experiência ou drop deve ser decidido pelo servidor autoritativo
e persistido por eventos controlados, nunca por requisição direta do cliente dizendo
o resultado.
