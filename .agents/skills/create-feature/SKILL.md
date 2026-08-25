---
name: create-feature
description: Implementar uma feature neste projeto Godot multiplayer, preservando autoridade, limites entre jogo e backend e validações do Harness.
---

# Criar feature

Use este procedimento para adicionar comportamento ao jogo, networking ou backend. Não
o use para inventar subsistemas futuros fora do pedido.

1. Leia `AGENTS.md` e os critérios da tarefa.
2. Execute `git status --short` e pesquise com `rg` cenas, scripts, serviços, DTOs e
   testes relacionados. Prefira estender a implementação existente.
3. Classifique o impacto: apresentação cliente, simulação servidor, dados compartilhados
   no projeto Godot, API, persistência e/ou migration.
4. Para multiplayer, defina a intenção enviada pelo cliente, as validações no servidor
   e o estado replicado. Leia `docs/05-multiplayer-godot.md`.
5. Planeje a menor mudança completa, incluindo testes e documentação afetada.
6. Implemente sem mover responsabilidades entre Godot e API incidentalmente.
7. Execute `./scripts/validate.sh`. Acrescente testes manuais de servidor/dois clientes
   para rede e revisão de migration/banco para persistência quando aplicável.
8. Revise `git status --short`, `git diff` e, se necessário, `git diff --cached`.
9. Corrija regressões e repita as validações afetadas.

Só conclua quando todas as validações aplicáveis passarem. Relate explicitamente
qualquer smoke ou teste manual que não pôde ser executado e o risco que permaneceu.
