---
name: fix-bug
description: Diagnosticar e corrigir bugs neste projeto com reprodução, causa raiz, teste de regressão e validação proporcional ao sistema afetado.
---

# Corrigir bug

1. Leia `AGENTS.md`, registre o estado inicial com `git status --short` e reproduza o
   problema quando o ambiente permitir. Preserve logs e condições relevantes sem
   expor secrets.
2. Pesquise o fluxo completo e identifique a causa raiz. Em bugs multiplayer, separe
   intenção do cliente, validação/estado do servidor e apresentação replicada.
3. Verifique se o comportamento observado é uma violação de autoridade, ownership,
   path de cena, autenticação, persistência ou timing antes de alterar o sintoma.
4. Adicione um teste de regressão quando houver uma fronteira testável sem refatoração
   desproporcional. Se não houver, defina uma reprodução manual objetiva.
5. Faça a menor correção que trate a causa sem alterar comportamento não relacionado.
6. Execute `./scripts/validate.sh` e as validações específicas do fluxo reproduzido.
7. Revise regressões adjacentes, `git status --short`, `git diff` e staged diff quando
   existir. Remova debug temporário e repita testes após ajustes.

Não considere o bug corrigido apenas porque deixou de aparecer uma vez. Compare o caso
original, o teste de regressão e os principais caminhos vizinhos; relate validações não
executadas.
