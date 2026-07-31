# Requisitos e ferramentas

## Requisitos mínimos

- Linux x86_64 para reproduzir a exportação atual do servidor;
- Godot `4.7.1` com suporte Mono/.NET;
- .NET SDK `10`;
- Docker Engine com Docker Compose;
- Git;
- templates de exportação Godot `4.7.1 Mono`.

Verifique:

```bash
godot --version
dotnet --version
dotnet --list-sdks
docker --version
docker compose version
```

O projeto Godot usa:

```xml
<TargetFramework>net10.0</TargetFramework>
```

A API também usa `net10.0`.

## Godot Mono

Use a distribuição Mono da mesma versão configurada pelo projeto. Uma instalação
Godot sem C# não compila os scripts.

No editor:

```text
Editor → Manage Export Templates
```

Instale exatamente os templates correspondentes à versão do editor.

## Docker

O usuário local precisa conseguir executar:

```bash
docker ps
```

Se for necessário usar `sudo docker`, ajuste os comandos dos guias ou configure o
grupo Docker conforme a política da máquina.

## Ferramentas opcionais

- Rider para editar C# e criar configurações de execução;
- `curl` e `jq` para testes HTTP;
- `psql` para inspecionar o banco;
- `gcloud` para uma VPS no Google Cloud;
- `scp`, `ssh`, `tar` e `systemd` para publicação do servidor.

## Clonar e restaurar

```bash
git clone URL_DO_REPOSITORIO
cd dbjao

dotnet restore dbjao.csproj
dotnet restore backend/GameBackend.sln
dotnet tool restore --tool-manifest backend/.config/dotnet-tools.json
```

Não remova estas proteções:

- `backend/.gdignore`;
- `<Compile Remove="backend/**/*.cs" />` em `dbjao.csproj`.

Elas impedem que o projeto Godot tente compilar a API ASP.NET e os arquivos de
`backend/obj`, situação que gera erros de assemblies duplicados.
