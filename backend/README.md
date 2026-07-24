# Game Backend

API REST separada do servidor dedicado Godot. Ela cuida somente de contas,
autenticação, personagens e persistência.

Stack atual: ASP.NET Core 10, Entity Framework Core 10 e PostgreSQL.
O PostgreSQL fica acessível apenas pela rede interna do Docker; externamente, somente a API publica a porta `8080`.

## Executar com Docker

```bash
cd backend
cp .env.example .env
docker compose up -d --build
```

Antes de usar fora do ambiente local, altere `POSTGRES_PASSWORD` e
`JWT_SECRET` no arquivo `.env`.

Swagger: <http://localhost:8080/swagger>

Para encerrar sem apagar o banco:

```bash
docker compose down
```

Para também apagar o volume e todos os dados locais:

```bash
docker compose down -v
```

## Fluxo pelo Swagger

1. Use `POST /api/auth/register`.
2. Use `POST /api/auth/login`.
3. Copie o campo `accessToken`.
4. Clique em **Authorize** e informe somente o token.
5. Use os endpoints de `/api/characters`.

## Migrations

Restaure a ferramenta local e crie uma nova migration:

```bash
cd backend
dotnet tool restore
dotnet tool run dotnet-ef migrations add NomeDaMigration \
  --project GameBackend.Api/GameBackend.Api.csproj \
  --startup-project GameBackend.Api/GameBackend.Api.csproj \
  --output-dir Data/Migrations
```

O container da API aplica migrations pendentes automaticamente na inicialização.

## Limites atuais

- Até cinco personagens por usuário.
- Nomes de usuário, e-mails e nomes de personagem são comparados sem diferenciar
  maiúsculas e minúsculas.
- `IGameSessionService` é apenas o ponto de extensão para a futura comunicação
  segura com o servidor Godot.
- Não há lógica de movimento, combate, NPC ou ENet nesta API.
