# Subir o ambiente local

## 1. Configurar segredos locais

```bash
cd backend
cp .env.example .env
```

Edite `backend/.env`:

```dotenv
POSTGRES_PASSWORD=uma-senha-local-forte
JWT_SECRET=uma-chave-jwt-com-pelo-menos-32-bytes
GAME_SERVER_API_KEY=uma-chave-interna-com-pelo-menos-32-bytes
GAME_SERVER_HOST=127.0.0.1
GAME_SERVER_PORT=7000
```

`backend/.env` é ignorado pelo Git. Não versione o arquivo.

## 2. Iniciar API e PostgreSQL

```bash
cd backend
docker compose up -d --build
docker compose ps
docker compose logs -f api
```

Resultado esperado:

```text
Migrations aplicadas com sucesso
Now listening on: http://[::]:8080
```

Dentro do container a API usa `8080`; no host ela é publicada em `5000`.

Teste:

```bash
curl -I http://127.0.0.1:5000/swagger/index.html
```

## 3. Criar uma conta

Acesse:

```text
http://127.0.0.1:5000/swagger
```

Use:

1. `POST /api/auth/register`;
2. `POST /api/auth/login`;
3. copie `accessToken`;
4. clique em **Authorize** e informe somente o token;
5. crie um personagem em `POST /api/characters`.

O cliente Godot também oferece login, listagem e criação de personagem, mas não
possui tela de cadastro nesta versão.

## 4. Iniciar servidor dedicado

Em outro terminal, na raiz:

```bash
export GAME_API_URL=http://127.0.0.1:5000
export GAME_SERVER_API_KEY='mesma-chave-de-backend/.env'
export GAME_SERVER_ID=local-server-01
export GAME_AUTH_TIMEOUT_SECONDS=10
export GAME_API_TIMEOUT_SECONDS=5

godot --headless --path . -- --server
```

Resultado esperado:

```text
[AUTH] Backend configurado em http://127.0.0.1:5000
[SERVER] Servidor iniciado na porta 7000
```

## 5. Iniciar clientes

Em outros terminais:

```bash
export GAME_API_URL=http://127.0.0.1:5000
godot --path .
```

Abra duas instâncias e use contas ou personagens diferentes. A API retorna o host e
a porta do servidor; o cliente não pede o IP diretamente.

## Encerrar

Preservando o banco:

```bash
cd backend
docker compose down
```

Apagando também todos os dados do volume:

```bash
docker compose down -v
```

O segundo comando é destrutivo.

## Portas ocupadas

```bash
ss -ltnp | rg ':5000'
sudo ss -lunp 'sport = :7000'
```

Pare o processo anterior antes de iniciar uma nova instância.
