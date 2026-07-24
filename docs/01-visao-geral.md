# Visão geral e arquitetura

## Componentes

### Cliente Godot

Responsável por:

- interface de login e seleção de personagem;
- chamadas HTTPS para a API;
- conexão ENet com o servidor dedicado;
- captura de input local;
- exibição de jogadores, NPCs, vida, combate e interpolação.

O cliente não decide vida, posição oficial, dano, identidade ou alvo atingido.

### Servidor Godot dedicado

Executa o mesmo projeto Godot em modo `--headless --server` e é responsável por:

- aceitar conexões ENet na UDP `7000`;
- autenticar tokens temporários consultando a API;
- criar e remover jogadores;
- processar movimento e colisões;
- executar IA dos NPCs;
- validar combate, dano, morte e respawn;
- sincronizar o estado oficial;
- salvar o estado do personagem na desconexão.

### API ASP.NET Core

Responsável somente por:

- cadastro e login;
- JWT;
- personagens;
- sessões temporárias de jogo;
- persistência;
- comunicação autenticada com o servidor dedicado.

A API nunca deve receber ou processar movimento por frame, física, IA ou combate.

### PostgreSQL

Armazena usuários, personagens e sessões de entrada no jogo. O acesso padrão ocorre
somente pela rede interna do Docker.

## Código compartilhado entre cliente e servidor

Cliente e servidor Godot compartilham:

- `project.godot`;
- cenas em `scenes/`;
- scripts em `scripts/`;
- sprites, animações, colisões e demais recursos.

`NetworkManager.RunningAsServer` diferencia os modos. Ele considera:

```text
--server
OS.HasFeature("dedicated_server")
```

Elementos visuais e UI só são criados no cliente. Física, autoridade e colisões
continuam presentes no headless.

## Cena principal

```text
KameHouse
├── Map
├── Players
├── MultiplayerSpawner
└── NPCs
    ├── Sidra
    └── Pilaf
```

`Players` recebe instâncias dinâmicas de `Player.tscn`. Os NPCs são fixos na cena e
possuem caminhos determinísticos.

## Fluxo de entrada

```text
Login na API
  → listar personagens
  → criar sessão temporária
  → conectar via ENet
  → enviar somente o token temporário
  → servidor valida na API
  → servidor associa peer e personagem
  → servidor cria o Player
```

Nenhum Player é criado apenas porque o peer conectou.

## Tecnologias atuais

| Camada | Tecnologia |
|---|---|
| Jogo | Godot 4.7.1 Mono, C# |
| Runtime do jogo | .NET 10 |
| API | ASP.NET Core 10 |
| ORM | Entity Framework Core 10 |
| Banco | PostgreSQL 17 |
| Rede em tempo real | ENet UDP |
| Autenticação web | JWT + token temporário |
| Ambiente local | Docker Compose |
