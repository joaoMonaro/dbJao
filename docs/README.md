# Documentação do dbjao

O projeto é formado por três processos independentes:

```text
Cliente Godot ──HTTPS──> API ASP.NET Core ──> PostgreSQL
      │
      └──────ENet/UDP──> Servidor Godot dedicado
```

O cliente e o servidor dedicado usam o mesmo projeto Godot e os mesmos arquivos.
O comportamento muda conforme os argumentos e as features da exportação. A API é
um projeto separado e não executa física, IA, movimento ou combate.

## Guias

1. [Visão geral e arquitetura](01-visao-geral.md)
2. [Requisitos e ferramentas](02-requisitos.md)
3. [Subir o ambiente local](03-ambiente-local.md)
4. [API, PostgreSQL e migrations](04-backend-api.md)
5. [Multiplayer e servidor autoritativo](05-multiplayer-godot.md)
6. [Autenticação e segurança](06-autenticacao-seguranca.md)
7. [Testes automatizados e manuais](07-testes.md)
8. [Exportar o servidor dedicado](08-exportacao-servidor.md)
9. [Publicar e operar em VPS](09-vps-producao.md)
10. [Continuar o desenvolvimento](10-guia-desenvolvimento.md)
11. [Configurar Rider e editor Godot](11-rider-godot.md)
12. [Solução de problemas](12-troubleshooting.md)

## Harness para agentes

- `AGENTS.md`: regras operacionais e critérios de conclusão;
- `.agents/skills/`: procedimentos reutilizáveis para features e correções;
- `scripts/validate.sh`: build, testes, smoke Godot quando disponível e diff check;
- [`adr/`](adr/): decisões arquiteturais confirmadas no código.

Agentes devem começar por `AGENTS.md` e consultar apenas os guias relevantes à tarefa.

## Início rápido

```bash
# Terminal 1: API + PostgreSQL
cd backend
cp .env.example .env
# Edite backend/.env antes de continuar.
docker compose up -d --build

# Terminal 2: servidor Godot
cd ..
export GAME_API_URL=http://127.0.0.1:5000
export GAME_SERVER_API_KEY='mesma-chave-do-backend/.env'
export GAME_SERVER_ID=local-server-01
godot --headless --path . -- --server

# Terminal 3: cliente
export GAME_API_URL=http://127.0.0.1:5000
godot --path .
```

Swagger:

```text
http://127.0.0.1:5000/swagger
```

Portas locais:

| Serviço | Protocolo | Porta |
|---|---:|---:|
| API | HTTP | 5000 |
| Servidor Godot | ENet/UDP | 7000 |
| PostgreSQL | TCP interno do Docker | 5432 |

O PostgreSQL não publica a porta no host pelo `docker-compose.yml` atual.
