# Solução de problemas

## `Porta 7000 ocupada`

```bash
sudo ss -lunp 'sport = :7000'
ps -ef | rg '[s]erver.x86_64|[g]odot'
```

Pare o processo anterior de forma normal. Não inicie dois servidores na mesma porta.

## API não inicia

Veja logs:

```bash
cd backend
docker compose logs --tail=200 api
```

Problemas comuns:

- `GAME_SERVER_API_KEY` ausente;
- `JWT_SECRET` curto;
- PostgreSQL ainda não saudável;
- porta host `5000` ocupada;
- migration incompatível com dados existentes.

## Compose pede `GAME_SERVER_API_KEY`

Crie `backend/.env`:

```bash
cp backend/.env.example backend/.env
```

Defina uma chave com pelo menos 32 caracteres.

## Cliente não conecta na VPS

Confirme:

- API retorna IP/DNS público, não `127.0.0.1`;
- servidor escuta UDP 7000;
- firewall da VM permite UDP;
- firewall do provedor permite UDP;
- cliente e servidor usam builds compatíveis.

Na VPS:

```bash
sudo ss -lunp 'sport = :7000'
sudo journalctl -u dbjao-server -f
```

## `Permission denied (publickey)` no SCP

O servidor SSH exige uma chave:

```bash
scp -i ~/.ssh/chave_privada arquivo usuario@ip:/tmp/
```

No Google Cloud, prefira:

```bash
gcloud compute scp arquivo INSTANCIA:/tmp/ --zone=ZONA
```

Confirme também o nome correto do usuário da VM.

## `scp: stat local ... No such file`

O caminho é relativo ao diretório atual. Liste antes:

```bash
pwd
rg --files build/server
```

Use o caminho real, por exemplo:

```bash
scp build/dbjao-server-linux-x86_64.tar.gz usuario@ip:/tmp/
```

## Segmentation fault no servidor exportado

Verifique se foi enviada a pasta completa:

```text
server.x86_64
server.pck
data_dbjao_linuxbsd_x86_64/
```

Também confira:

- mesma arquitetura Linux x86_64;
- templates Mono da mesma versão;
- permissões de execução;
- binário, PCK e DLLs gerados juntos;
- bibliotecas não corrompidas durante transferência.

Execute:

```bash
./server.x86_64 --headless --verbose --server
```

Se o log para ao carregar um `.cs`, a primeira suspeita é publicação .NET
incompleta ou incompatível.

## Erros ASP.NET aparecem no build Godot

Confirme:

```xml
<Compile Remove="backend/**/*.cs" />
```

e a existência de:

```text
backend/.gdignore
```

Sem isso, o SDK Godot pode incluir os fontes e `obj` da API no assembly do jogo.

## `SESSION_ALREADY_CONSUMED`

O token já foi usado. Solicite uma nova sessão pela API.

## Sessão pendente impede nova tentativa

Uma sessão válida para o mesmo personagem permanece pendente por até 60 segundos.
Espere expirar ou use outro personagem. Não altere o banco manualmente em produção.

## `CHARACTER_ALREADY_ONLINE`

O personagem já está associado a outro peer. Encerre corretamente a conexão antiga.
Na primeira versão, a nova conexão não derruba a anterior.

## `AUTH_SERVICE_UNAVAILABLE`

O servidor não conseguiu consultar a API:

- confirme `GAME_API_URL`;
- teste a URL a partir da máquina do servidor;
- confira DNS/TLS;
- confira timeout;
- veja logs da API;
- confirme que proxy/firewall aceita a conexão.

O comportamento correto é não criar Player.

## GCloud: `insufficient authentication scopes`

Execute a criação da regra no Cloud Shell, selecione o projeto:

```bash
gcloud config set project ID_DO_PROJETO
gcloud config get-value project
```

Depois repita o comando de firewall.

## GCloud: conflito entre `--action` e `--allow`

Use somente:

```text
--allow=udp:7000
```

Não combine com `--action=ALLOW`.
