# Goku: conjunto visual integrado

O jogador usa a nova pose frontal e as novas sequências de movimento e ataque,
com canvas de 192×192 por quadro, alpha binário e paleta comum de 32 cores.

| Animação | Asset em uso | Quadros | Reprodução |
|---|---|---|---|
| Parado | [idle_front.png](../../../assets/frame/goku/idle/idle_front.png) | 1 | Estática |
| Movimento | [player_walk_sheet.png](../../../assets/sheets/goku/walk/player_walk_sheet.png) | 4 | 8 FPS, loop |
| Ataque | [attack.png](../../../assets/sheets/goku/attack/attack.png) | 6 | 10 FPS, sem loop |

- [Prévia animada com comparação de porte](preview.gif).
- [Todos os quadros](overview.png).
- [Medidas e paleta](manifest.json).

O idle tem área visível de 64×128. As demais poses variam sua área visível
conforme o movimento, com escala fixa por sheet. Todas apoiam os pés na linha
159. As sequências são voltadas para a direita; a cena espelha para a esquerda.
O movimento vertical reutiliza essa apresentação, conforme o comportamento atual.

## Produção

Foi usada a ferramenta integrada de geração de imagens, com a nova referência
frontal e o acabamento do personagem pequeno como entradas. Os prompts e
resultados brutos estão nesta pasta:

- [Movimento](walk-prompt.txt) e [imagem gerada](walk-generated.png).
- [Ataque](attack-prompt.txt) e [imagem gerada](attack-generated.png).
- [Produção da referência frontal](../goku-normal/README.md).

O acabamento por script foi autorizado: remove ruído de alpha, mantém o corpo
conectado, aplica nearest e a paleta da referência sem dithering. O alinhamento
horizontal usa os quadris, evitando recentralizar o personagem quando estende
um braço. O ajuste vertical corrige o arredondamento da sola após a redução.

Para reproduzir, com Pillow instalado, na raiz do repositório:

```bash
python3 output/imagegen/goku-complete/finalize.py
```

A pasta contém `.gdignore`: materiais de produção não são importados pelo jogo.

## Integração e validação

`scenes/Player.tscn` recorta células inteiras de 192×192. `scripts/Player.cs`
usa escala 1 e origem uniforme em todas as animações. Os limites anteriores
do viewport foram preservados explicitamente, para que o novo padding
transparente não altere a área de movimento. A duração do ataque continua
em 0,6 segundo, sem alterar dano, alcance, cooldown, colisões ou RPCs.

Com Godot 4.7.2 Mono disponível, executar na raiz:

```bash
GODOT_BIN=/caminho/para/godot ./scripts/validate.sh
/caminho/para/godot --headless --path . --script output/imagegen/goku-complete/check_player.gd
```

A verificação adicional instancia a cena real e confere os 11 quadros, os pés,
a reprodução, as transições, o espelhamento, a escala e os limites do viewport.
Ela não substitui uma sessão multiplayer com jogadores autenticados.
