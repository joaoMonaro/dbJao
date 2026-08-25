# dbjao

Jogo multiplayer 2D feito em Godot 4.7.2 com C#/.NET 10, servidor dedicado
autoritativo, API ASP.NET Core e PostgreSQL.

## Documentação

A documentação completa está em:

- [Índice da documentação](docs/README.md)
- [Subir o ambiente local](docs/03-ambiente-local.md)
- [Testar o projeto](docs/07-testes.md)
- [Exportar o servidor](docs/08-exportacao-servidor.md)
- [Publicar em VPS](docs/09-vps-producao.md)
- [Continuar o desenvolvimento](docs/10-guia-desenvolvimento.md)

Início rápido:

```bash
cd backend
cp .env.example .env
# Edite .env.
docker compose up -d --build
```

Depois, na raiz:

```bash
export GAME_API_URL=http://127.0.0.1:5000
export GAME_SERVER_API_KEY='mesma-chave-do-backend/.env'
export GAME_SERVER_ID=local-server-01
godot --headless --path . -- --server
```
