# Rider e editor Godot

## Abrir o projeto

No Rider, abra:

```text
/home/jao/dbjao/dbjao.sln
```

Para trabalhar apenas na API:

```text
/home/jao/dbjao/backend/GameBackend.sln
```

O SDK configurado deve ser .NET 10.

## Configuração do servidor no Rider

Crie uma configuração do tipo executável externo:

```text
Executable:
caminho/para/Godot_v4.7.1-stable_mono_linux.x86_64

Working directory:
/home/jao/dbjao

Arguments:
--headless --path /home/jao/dbjao -- --server
```

Variáveis:

```text
GAME_API_URL=http://127.0.0.1:5000
GAME_SERVER_API_KEY=chave-local
GAME_SERVER_ID=local-server-rider
GAME_AUTH_TIMEOUT_SECONDS=10
GAME_API_TIMEOUT_SECONDS=5
```

O separador `--` é importante ao executar o projeto pelo editor/engine: os
argumentos seguintes são entregues ao jogo.

## Configuração do cliente no Rider

```text
Executable:
caminho/para/Godot_v4.7.1-stable_mono_linux.x86_64

Working directory:
/home/jao/dbjao

Arguments:
--path /home/jao/dbjao

Environment:
GAME_API_URL=http://127.0.0.1:5000
```

Duplique a configuração para abrir dois clientes. Desative execução em instância
única na configuração, se o Rider oferecer essa opção.

## API no Rider

Crie uma configuração `.NET Project` apontando para:

```text
backend/GameBackend.Api/GameBackend.Api.csproj
```

Variáveis mínimas fora do Docker:

```text
ASPNETCORE_URLS=http://127.0.0.1:5000
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;...
Jwt__Secret=...
GameServer__InternalApiKey=...
```

Como o Compose não publica PostgreSQL no host, executar a API diretamente no Rider
exige um PostgreSQL acessível pelo host ou uma publicação temporária controlada.
Para o fluxo normal, mantenha API e banco no Docker.

## Integração editor Godot

No Godot:

```text
Editor Settings → Dotnet → Editor
```

Selecione Rider como editor externo. Sempre abra o projeto com a versão Mono.

Se o editor não reconhecer classes novas:

1. compile `dbjao.csproj`;
2. feche e reabra a cena;
3. confirme que não há erros no painel Build;
4. se necessário, reinicie editor e Rider.

Evite apagar `.godot/` como primeira tentativa; ele contém cache regenerável, mas a
reimportação pode ser demorada.
