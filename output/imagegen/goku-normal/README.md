# Goku: referência frontal no porte normal

Arte gerada com a ferramenta integrada de imagens, usando o idle existente
como referência de identidade e o personagem pequeno como referência de estilo.
O prompt completo está em [prompt.txt](prompt.txt) e o resultado bruto em
[generated.png](generated.png).

## Arquivo final

- [PNG do sprite](../../../assets/frame/goku/idle/idle_front_normal.png).
- Canvas: 192×192 px, RGBA, alpha binário (0 ou 255).
- Área visível: 64×128 px, incluindo cabelo e botas.
- Limites inclusivos: x = 64 a 127, y = 32 a 159.
- Âncora prática dos pés: (96, 159).
- Paleta final: 32 cores visíveis.
- [Comparação dos portes](comparison.png): escala comum ampliada 3 vezes
  com nearest, pés na mesma linha de chão.

Esta pose frontal passou a integrar o [conjunto visual completo](../goku-complete/README.md),
com movimento e ataque derivados dela. A cena usa a cópia em `idle_front.png`;
`idle_front_normal.png` permanece como referência de produção.

## Reprodução do acabamento

Na raiz do repositório, com Pillow instalado:

```bash
python3 output/imagegen/goku-normal/finalize.py
```

O script remove pixels com alpha menor que 128, converte os restantes para
opacos, recorta a silhueta e ajusta largura e altura ao alvo de 64×128 usando
nearest. A composição é ajustada nos dois eixos; não é uma redução proporcional
do arquivo bruto. Em seguida, reduz a paleta sem dithering e posiciona o sprite
no canvas final. A comparação é apenas uma imagem de revisão.

O arquivo `.gdignore` impede a importação dos materiais de geração e revisão
como recursos do jogo. O PNG final fica na pasta de assets.
