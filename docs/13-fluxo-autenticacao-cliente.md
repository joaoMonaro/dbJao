# Fluxo inicial de autenticação

O cliente inicia em `AuthRoot`, sem abrir conexão ENet. A ordem é:

```text
Login/cadastro/recuperação → seleção de personagem
→ POST /api/game-sessions → conexão ENet
```

## Configuração do cliente

| Variável | Padrão | Uso |
|---|---|---|
| `API_BASE_URL` | `http://127.0.0.1:5000` | URL REST centralizada |
| `REQUEST_TIMEOUT_SECONDS` | `10` | timeout entre 1 e 60 segundos |
| `ENVIRONMENT` | `Development` | ambiente do cliente |
| `CLIENT_VERSION` | `0.1.0` | versão exibida no login |

`GAME_API_URL` continua aceito como compatibilidade. Em produção use HTTPS.

## Recuperação local

A API salva a mensagem simulada em `/tmp/dbjao-password-reset`, sem registrar o
token no log. Para preencher o token automaticamente no cliente em
desenvolvimento, configure explicitamente:

```text
PasswordReset__ExposeTokenInDevelopment=true
```

Nunca habilite essa opção em produção. A expiração é configurada por
`PasswordReset__ExpirationMinutes` e o padrão é 30 minutos.

## Execução

```bash
docker compose -f backend/docker-compose.yml up -d
dotnet run --project backend/GameBackend.Api
godot
```

A migration `AddPasswordResetTokens` é aplicada na inicialização conforme a
configuração existente.

## Roteiro manual

1. Abra em 1280x720 e confirme foco no email, Tab e Enter.
2. Navegue Login → Criar conta → Voltar e Login → Recuperar → Voltar.
3. Valide nome curto, email inválido, senha curta, confirmação e termos.
4. Cadastre uma conta; repita email e nome para validar mensagens amigáveis.
5. Teste login correto, senha incorreta, campos vazios e senha limpa após falha.
6. Derrube a API e valide erro amigável e fim do loading.
7. Clique repetidamente durante cada operação e confirme um único envio.
8. Solicite recuperação para email existente e inexistente; a mensagem deve ser igual.
9. Redefina com token válido, expirado, inválido e já consumido.
10. Entre com a nova senha e confirme que o token não pode ser reutilizado.
11. Redimensione para 1920x1080 e para uma janela menor.
12. Confirme que logs não contêm senha, JWT nem token de recuperação.
13. Confirme que ENet só conecta após selecionar personagem e criar sessão.
14. Simule HTTP 429 e confirme mensagem amigável sem repetição automática.
