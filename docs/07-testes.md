# Testes

## Validação rápida antes de commit

Na raiz:

```bash
dotnet build dbjao.csproj
dotnet test backend/GameBackend.sln
git diff --check
```

Resultado esperado:

```text
Build succeeded.
0 Warning(s)
0 Error(s)
Passed: 10
```

O número de testes pode crescer; não fixe automações futuras em exatamente 10.

## Smoke test da API

```bash
cd backend
docker compose up -d --build
docker compose ps
docker compose logs --tail=100 api
curl -sS -o /dev/null -w '%{http_code}\n' \
  http://127.0.0.1:5000/swagger/index.html
```

Esperado: `200`.

## Smoke test headless

Com API ativa:

```bash
cd ..
export GAME_API_URL=http://127.0.0.1:5000
export GAME_SERVER_API_KEY='chave-local'
export GAME_SERVER_ID=local-server-01

godot --headless --path . -- --server
```

Confirme:

- dois NPCs inicializados;
- backend configurado;
- servidor na porta 7000;
- nenhuma janela;
- nenhuma exceção;
- nenhum Player sem peer autenticado.

## Teste funcional com dois clientes

1. Suba API, banco e servidor.
2. Cadastre duas contas.
3. Crie um personagem em cada conta.
4. Abra dois clientes.
5. Faça login e selecione personagens diferentes.
6. Confirme peerIds diferentes.
7. Confirme dois Players nos dois clientes.
8. Movimente A e observe em B.
9. Movimente B e observe em A.
10. Ataque NPCs e confirme vida idêntica.
11. Mate um NPC e confirme respawn sincronizado.
12. Feche A e confirme remoção em B.
13. Entre novamente com A e confirme posição/vida persistidas.

## Casos de autenticação

### Sem token

Conecte um peer sem enviar token. Após 10 segundos:

- deve ser desconectado;
- nenhum Player deve aparecer.

### Token inválido ou expirado

- deve ser rejeitado;
- servidor não cria Player;
- logs não mostram o token.

### Reutilização

Tente validar a mesma sessão duas vezes:

- primeira: sucesso;
- segunda: `SESSION_ALREADY_CONSUMED`.

### Personagem duplicado

Tente entrar com o mesmo personagem em outro cliente:

```text
CHARACTER_ALREADY_ONLINE
```

A sessão antiga permanece conectada.

### API indisponível

Pare a API antes de autenticar:

```bash
cd backend
docker compose stop api
```

Esperado:

- `AUTH_SERVICE_UNAVAILABLE`;
- nenhum Player;
- servidor e NPCs continuam executando.

## Movimento, combate e respawn

Valide:

- cliente não movimenta jogador remoto;
- servidor limita direção e velocidade;
- ataque fora do alcance não causa dano;
- cooldown rejeita spam;
- morto não anda nem ataca;
- respawn aplica snap, sem atravessar o mapa;
- interpolação não altera colisão oficial.

## Entrada tardia e desconexão

- conecte cliente depois que NPCs já se moveram;
- confirme posição atual, sem partir de `(0,0)`;
- conecte durante morte de NPC;
- confirme estado morto atual;
- desconecte durante validação;
- desconecte jogador morto antes do respawn;
- confirme ausência de referências e exceções.

## Teste de internet

Em VPS:

- teste UDP `7000`;
- use o IP/DNS público retornado pela API;
- acompanhe perda de pacotes e latência;
- teste encerramento do servidor;
- teste API indisponível;
- confirme persistência após desconexão real.

Não considere `telnet` ou teste TCP como validação da porta ENet: o jogo usa UDP.
