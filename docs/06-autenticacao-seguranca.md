# Autenticação e segurança

## Fluxo completo

```text
1. Cliente envia e-mail/senha à API.
2. API devolve JWT.
3. Cliente lista personagens do usuário.
4. Cliente solicita sessão para um CharacterId.
5. API confirma que o personagem pertence ao JWT.
6. API gera token aleatório de 256 bits e salva somente SHA-256.
7. Cliente recebe token, host e porta.
8. Cliente conecta via ENet e envia somente o token.
9. Servidor consulta a API usando X-Game-Server-Key.
10. API consome atomicamente o token.
11. Servidor recebe dados confiáveis e cria o Player.
```

## Por que há dois tokens

O JWT autentica o usuário na API. O token de sessão autentica uma única entrada no
servidor de jogo.

O token temporário:

- expira em 60 segundos por padrão;
- é de uso único;
- possui 32 bytes aleatórios;
- é armazenado somente como SHA-256;
- é vinculado ao usuário e personagem;
- registra o servidor que o consumiu.

O JWT não é enviado ao servidor ENet.

## Consumo atômico

A API consome apenas quando:

```text
TokenHash corresponde
IsConsumed == false
ExpiresAt > agora
```

A atualização condicional garante que dois servidores não consumam o mesmo token.

## Peer pendente

Ao conectar:

- nenhum Player é criado;
- o peer tem 10 segundos para enviar o token;
- somente uma validação pode ficar ativa;
- token vazio ou maior que 512 caracteres é rejeitado;
- falha ou timeout desconecta o peer.

Mapas mantidos pelo servidor:

```text
peerId → conexão pendente
peerId → personagem autenticado
characterId → peerId
```

O mesmo personagem não pode entrar duas vezes. A política atual rejeita a nova
conexão com `CHARACTER_ALREADY_ONLINE`.

## Códigos importantes

API:

```text
SESSION_NOT_FOUND
SESSION_EXPIRED
SESSION_ALREADY_CONSUMED
CHARACTER_NOT_FOUND
USER_NOT_FOUND
INVALID_SERVER_KEY
INVALID_REQUEST
```

Servidor:

```text
AUTH_TIMEOUT
AUTH_SERVICE_UNAVAILABLE
MULTIPLE_AUTH_ATTEMPTS
CHARACTER_ALREADY_ONLINE
PLAYER_SPAWN_FAILED
```

## Segredos

Nunca versione:

- senha do PostgreSQL;
- `Jwt__Secret`;
- `GameServer__InternalApiKey`;
- JWT;
- token temporário;
- arquivos `.env` reais.

Locais aceitos:

- variáveis de ambiente;
- Secret Manager;
- arquivo de ambiente fora do repositório;
- credenciais administradas pelo serviço de deploy.

Em produção:

- use HTTPS na API;
- limite o endpoint interno por firewall/rede privada;
- use chaves diferentes por ambiente;
- execute API e servidor com usuários sem privilégios;
- rotacione chaves em caso de exposição;
- não coloque segredo em argumentos de linha de comando.

## Logs

Os logs podem conter:

- IDs de sessão, usuário e personagem;
- peerId;
- código de erro;
- identificador do servidor.

Não podem conter:

- senha;
- JWT completo;
- token temporário;
- API Key.

## Persistência

Na desconexão, o servidor envia:

- vida atual;
- posição X/Y;
- mapa;
- usuário e personagem já autenticados.

O cliente não chama o endpoint interno. A API confirma que personagem e usuário
correspondem antes de atualizar.

Limitações atuais:

- não existe refresh token;
- não há reconexão automática;
- não há salvamento periódico;
- uma sessão pendente do mesmo personagem precisa expirar antes de outra ser criada.
