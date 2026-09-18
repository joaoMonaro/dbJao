# Sidra: conjunto visual integrado

Sidra agora usa o porte normal, com células de 192×192 px, fundo transparente,
alpha binário e paleta única de 32 cores. A referência frontal ocupa 64×128 px.
Todos os quadros apoiam os pés na linha `y = 159`.

| Animação | Asset usado na cena | Quadros | Reprodução |
|---|---|---:|---|
| Parado | [idle_front.png](../../../assets/frame/sidra/idle/idle_front.png) | 1 | Estática |
| Movimento | [walk.png](../../../assets/sheets/sidra/walk/walk.png) | 4 | 8 FPS, loop |
| Gesto de ataque | [attack.png](../../../assets/sheets/sidra/attack/attack.png) | 6 | 10 FPS, sem loop |

- [Prévia animada, ao lado do Goku](preview.gif).
- [Todos os quadros](overview.png).
- [Medidas de cada quadro e paleta](manifest.json).

A cena usa animações de caminhada e repouso conforme `AiState`. Se o estado
`IsAttacking` for ativado, mostra o gesto de ataque. A IA atual de Sidra só
patrulha; não inicia ataques. O gesto não cria dano ou efeitos. As poses
laterais olham para a direita e são espelhadas para a esquerda. Movimento
vertical mantém a direção visual horizontal anterior.

## Produção

A ferramenta integrada de geração de imagens usou o [sprite anterior](../../../assets/frame/sidra/front_01.png)
como referência de identidade e o conjunto visual do Goku como referência de
acabamento e escala. O original foi preservado. Prompts e imagens geradas:

- [Pose frontal](idle-prompt.txt) e [imagem gerada](idle-generated.png).
- [Movimento](walk-prompt.txt) e [imagem gerada](walk-generated.png).
- [Gesto de ataque](attack-prompt.txt) e [imagem gerada](attack-generated.png).

O [script de acabamento](finalize.py) aplica nearest, limpa pixels semitransparentes
e fragmentos, fixa a paleta a partir do idle e posiciona quadris e pés dentro
da grade. Ele foi executado sob a autorização anterior para finalizar imagens
com script. Para reproduzir, com Pillow instalado:

```bash
python3 output/imagegen/sidra-complete/finalize.py
```

A pasta contém `.gdignore`, para que imagens de produção e prévias não sejam
importadas pelo jogo.

## Integração

[Sidra.tscn](../../../scenes/Sidra.tscn) usa `AnimatedSprite2D` e recorta células
inteiras de 192×192. [Sidra.cs](../../../scripts/Sidra.cs) troca as animações
conforme o estado replicado. O limite do passeio autoritativo mantém as
dimensões visuais da arte anterior, apesar do padding transparente novo.
Colisão, dano recebido, respawn e sincronização não mudaram.

Com Godot 4.7.2 Mono, executar `./scripts/validate.sh` e
`godot --headless --path . --script output/imagegen/sidra-complete/check_sidra.gd`.
O segundo comando instancia a cena real e confere a troca de animações.
