# API e PostgreSQL

## Responsabilidade

`backend/GameBackend.Api` contém a API de contas e persistência. Ela não é o
servidor em tempo real.

Estrutura principal:

```text
GameBackend.Api/
├── Authentication/
├── Configuration/
├── Controllers/
├── Data/
├── DTOs/
├── Entities/
├── Extensions/
├── Middleware/
├── Repositories/
└── Services/
```

## Endpoints

| Método | Rota | Autenticação |
|---|---|---|
| POST | `/api/auth/register` | pública |
| POST | `/api/auth/login` | pública |
| POST | `/api/auth/forgot-password` | pública, resposta genérica |
| POST | `/api/auth/reset-password` | pública, token de uso único |
| GET | `/api/characters` | JWT |
| GET | `/api/characters/{id}` | JWT |
| POST | `/api/characters` | JWT |
| POST | `/api/game-sessions` | JWT |
| POST | `/api/internal/game-sessions/validate` | API Key interna |
| PUT | `/api/internal/characters/{id}/state` | API Key interna |

O usuário só consulta personagens da própria conta. O endpoint interno nunca deve
ser exposto sem a chave e proteção de rede.

## Entidades

### User

- `Id`, `Username`, `Email`, `PasswordHash`;
- `IsBlocked`;
- datas de criação e atualização.

### Character

- dono, nome, nível e experiência;
- vida atual e máxima;
- mapa e posição persistida;
- datas de criação e atualização.

### GameSession

- usuário e personagem;
- hash do token;
- criação, expiração e consumo;
- servidor que consumiu.

O token puro só é retornado uma vez e nunca é salvo.

## Configuração

As configurações suportam o padrão de variáveis ASP.NET:

```text
ConnectionStrings__DefaultConnection
Jwt__Issuer
Jwt__Audience
Jwt__Secret
Jwt__ExpirationMinutes
GameSession__ExpirationSeconds
GameSession__MaxPendingSessionsPerUser
GameServer__InternalApiKey
GameServer__DefaultHost
GameServer__DefaultPort
Database__ApplyMigrationsOnStartup
Swagger__Enabled
```

## Rate limiting atual

- login: 10 solicitações por minuto por IP;
- criação de sessão: 10 por minuto por usuário/IP;
- endpoints internos: 240 por minuto por IP.

## Migrations

Restaurar ferramenta:

```bash
cd backend
dotnet tool restore
```

Criar:

```bash
dotnet tool run dotnet-ef migrations add NomeDaMigration \
  --project GameBackend.Api/GameBackend.Api.csproj \
  --startup-project GameBackend.Api/GameBackend.Api.csproj \
  --output-dir Data/Migrations
```

Listar:

```bash
dotnet tool run dotnet-ef migrations list \
  --project GameBackend.Api/GameBackend.Api.csproj \
  --startup-project GameBackend.Api/GameBackend.Api.csproj
```

O container aplica migrations pendentes no startup. Revise a migration gerada antes
de versioná-la, principalmente constraints e migração de dados existentes.

## Inspecionar PostgreSQL

```bash
cd backend
docker compose exec postgres \
  psql -U game_backend -d game_backend
```

Comandos úteis no `psql`:

```sql
\dt
SELECT * FROM "__EFMigrationsHistory";
SELECT "Id", "Username", "Email" FROM users;
SELECT "Id", "Name", "CurrentHealth", "MapId" FROM characters;
```

Não publique a porta `5432` em produção sem necessidade.

## Testes

```bash
dotnet test backend/GameBackend.sln
```

Os testes de sessão cobrem expiração, uso único, concorrência, propriedade do
personagem, chave interna e hashing.
