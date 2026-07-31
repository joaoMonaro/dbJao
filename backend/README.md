# Game Backend

API REST separada do servidor Godot. Usa ASP.NET Core 10, Entity Framework Core 10
e PostgreSQL 17.

Responsabilidades:

- cadastro e login;
- JWT;
- personagens;
- tokens temporários de entrada no jogo;
- validação interna do servidor dedicado;
- persistência de vida, mapa e posição.

Não contém movimento, física, combate, IA ou ENet.

## Executar

```bash
cp .env.example .env
# Edite todos os segredos do .env.
docker compose up -d --build
```

Swagger:

```text
http://127.0.0.1:5000/swagger
```

Testes:

```bash
dotnet test GameBackend.sln
```

## Documentação

- [API e banco](../docs/04-backend-api.md)
- [Ambiente local](../docs/03-ambiente-local.md)
- [Autenticação e segurança](../docs/06-autenticacao-seguranca.md)
- [Testes](../docs/07-testes.md)
