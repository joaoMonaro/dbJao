# Exportar o servidor dedicado

## Pré-requisitos

- Godot Mono 4.7.2;
- templates de exportação Mono 4.7.2;
- build C# sem erros;
- preset `server` em `export_presets.cfg`.

O preset atual é Linux x86_64. O modo servidor é ativado em runtime por `--server`.
`OS.HasFeature("dedicated_server")` também está preparado, mas o preset atual contém:

```text
dedicated_server=false
```

Se essa opção for habilitada futuramente, teste novamente o conteúdo exportado.

## Compilar antes

```bash
dotnet build dbjao.csproj --configuration Release
```

## Exportar por linha de comando

Na raiz:

```bash
mkdir -p build/server

godot --headless --path . \
  --export-release server \
  build/server/server.x86_64
```

Resultado mínimo:

```text
build/server/
├── server.x86_64
├── server.pck
└── data_dbjao_linuxbsd_x86_64/
    ├── dbjao.dll
    ├── dbjao.deps.json
    ├── dbjao.runtimeconfig.json
    ├── GodotSharp.dll
    ├── System.*.dll
    ├── libcoreclr.so
    └── demais arquivos do runtime
```

## Regra importante para C#/.NET

Envie a pasta `build/server` inteira.

Não copie apenas:

```text
server.x86_64
server.pck
```

As DLLs e bibliotecas nativas vêm da publicação .NET feita pelo Godot. Sem
`data_dbjao_linuxbsd_x86_64`, o executável pode encerrar com segmentation fault ao
carregar scripts C#.

## Verificar artefatos

```bash
file build/server/server.x86_64
test -f build/server/server.pck
test -f build/server/data_dbjao_linuxbsd_x86_64/dbjao.dll
test -f build/server/data_dbjao_linuxbsd_x86_64/libcoreclr.so
```

Teste local exportado:

```bash
export GAME_API_URL=http://127.0.0.1:5000
export GAME_SERVER_API_KEY='chave-local'
export GAME_SERVER_ID=local-export-test

./build/server/server.x86_64 --headless --server
```

## Empacotar

```bash
tar -C build -czf build/dbjao-server-linux-x86_64.tar.gz server
sha256sum build/dbjao-server-linux-x86_64.tar.gz
```

O arquivo `.tar.gz` preserva a estrutura relativa. Guarde o checksum junto do
artefato para confirmar a transferência.

## Copiar com SCP

```bash
scp -i ~/.ssh/SUA_CHAVE \
  build/dbjao-server-linux-x86_64.tar.gz \
  usuario@IP_DA_VPS:/tmp/
```

Na VPS:

```bash
sudo mkdir -p /opt/dbjao
sudo chown "$USER":"$USER" /opt/dbjao
tar -xzf /tmp/dbjao-server-linux-x86_64.tar.gz -C /opt/dbjao
chmod +x /opt/dbjao/server/server.x86_64
```

Se o SSH aceitar somente chave pública, `scp` sem `-i` pode retornar:

```text
Permission denied (publickey)
```

Use a chave correta ou `gcloud compute scp` quando a VM for gerenciada pelo Google
Cloud.

## Compatibilidade

- exporte Linux x86_64 para VPS Linux x86_64;
- não misture executável de uma versão com PCK/DLLs de outra;
- reenvie a pasta completa a cada nova exportação;
- não reutilize templates de outra versão do Godot.
