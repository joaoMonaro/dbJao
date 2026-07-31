# Publicar e operar em VPS

## Topologia inicial

Uma implantação simples pode usar:

```text
Internet
├── HTTPS 443 → API
└── UDP 7000 → servidor Godot

API → PostgreSQL privado
Servidor Godot → API interna/HTTPS
```

O banco não precisa ficar público.

## Configurar a API

Na configuração da API:

```text
GameServer__DefaultHost=IP_OU_DNS_PUBLICO_DO_SERVIDOR
GameServer__DefaultPort=7000
```

Esse host é enviado ao cliente. Não use `127.0.0.1` em produção, pois no cliente
isso apontaria para o próprio computador do jogador.

O servidor dedicado usa:

```text
GAME_API_URL=https://api.seujogo.com
GAME_SERVER_API_KEY=segredo-interno
GAME_SERVER_ID=vps-server-01
```

## Firewall Linux

```bash
sudo ufw allow 7000/udp
sudo ufw status
```

Se `Status: inactive`, o UFW não está filtrando; ainda pode existir firewall do
provedor.

Confirme o processo:

```bash
sudo ss -lunp 'sport = :7000'
```

## Firewall Google Cloud

No Cloud Shell:

```bash
gcloud config set project ID_DO_PROJETO

gcloud compute firewall-rules create allow-game-server-udp-7000 \
  --project=ID_DO_PROJETO \
  --network=default \
  --direction=INGRESS \
  --allow=udp:7000 \
  --source-ranges=0.0.0.0/0
```

Use `--allow` sem `--action`; os dois são mutuamente exclusivos.

Se a VM retornar `insufficient authentication scopes`, execute o comando no Cloud
Shell ou ajuste a conta/escopos da VM.

Em produção, considere `target-tags` para limitar a regra à VM do jogo.

## Arquivo de ambiente

Crie fora do repositório:

```bash
sudo install -m 600 /dev/null /etc/dbjao-server.env
sudo editor /etc/dbjao-server.env
```

Conteúdo:

```dotenv
GAME_API_URL=https://api.seujogo.com
GAME_SERVER_API_KEY=CHAVE_REAL
GAME_SERVER_ID=vps-server-01
GAME_AUTH_TIMEOUT_SECONDS=10
GAME_API_TIMEOUT_SECONDS=5
```

## Serviço systemd

Crie `/etc/systemd/system/dbjao-server.service`:

```ini
[Unit]
Description=dbjao Godot Dedicated Server
After=network-online.target
Wants=network-online.target

[Service]
Type=simple
User=game
Group=game
WorkingDirectory=/opt/dbjao/server
EnvironmentFile=/etc/dbjao-server.env
ExecStart=/opt/dbjao/server/server.x86_64 --headless --server
Restart=on-failure
RestartSec=3
NoNewPrivileges=true
PrivateTmp=true

[Install]
WantedBy=multi-user.target
```

Crie e ajuste o usuário/diretórios conforme sua distribuição. Depois:

```bash
sudo systemctl daemon-reload
sudo systemctl enable --now dbjao-server
sudo systemctl status dbjao-server
sudo journalctl -u dbjao-server -f
```

## Atualização segura

1. exporte e gere o `.tar.gz`;
2. valide checksum;
3. envie para `/tmp`;
4. extraia em diretório de versão;
5. pare o serviço;
6. troque o link/diretório ativo;
7. inicie e acompanhe logs;
8. mantenha a versão anterior para rollback.

Não sobrescreva arquivos enquanto o processo está executando.

## Checklist

- API usa HTTPS;
- API Key não aparece em repositório ou logs;
- PostgreSQL é privado;
- apenas UDP 7000 está aberto para o jogo;
- host público retornado pela API está correto;
- relógio da VPS está sincronizado;
- systemd reinicia falhas;
- logs e espaço em disco são monitorados;
- backups do PostgreSQL existem;
- autenticação inválida falha sem criar Player.
