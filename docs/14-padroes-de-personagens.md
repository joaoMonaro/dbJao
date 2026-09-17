# Padrões visuais de personagens

Este documento define o padrão para produzir e revisar sprites de personagens
por porte: pequeno, normal e grande. As medidas são compartilhadas por todos os
personagens da mesma categoria; identidade, anatomia e paleta são próprias de
cada personagem.

As medidas abaixo são o padrão de produção. Assets existentes devem ser
conferidos antes de serem considerados compatíveis.

## Canvas e área visível

Canvas é a imagem completa, incluindo a região transparente. Área visível é o
retângulo que contém os pixels do personagem; efeitos devem ser medidos à parte.
Dois personagens podem compartilhar o mesmo canvas e ter alturas diferentes.

| Porte | Canvas por quadro | Área visível alvo na pose frontal em pé |
|---|---|---|
| Pequeno | 192×192 px | 36×68 px |
| Normal | 192×192 px | 64×128 px |
| Grande | 256×256 px | A definir conforme a silhueta; ainda sem medida comum estabelecida |

Pequenos e normais compartilham o canvas, com ocupações diferentes dentro dele.
O porte grande reserva mais espaço para silhuetas e movimentos maiores.
O canvas deve permanecer igual entre as animações de um mesmo personagem.
Não aumentar o personagem para preencher a imagem, nem reduzir cada pose
separadamente para fazê-la caber.

## Medidas por porte

As coordenadas deste documento começam em `(0, 0)`, no canto superior esquerdo,
e os limites dos retângulos são inclusivos.

| Propriedade | Pequeno | Normal |
|---|---|---|
| Canvas | 192×192 px | 192×192 px |
| Área visível de referência | 36×68 px | 64×128 px |
| Limites horizontais de referência | x = 78 a 113 | x = 64 a 127 |
| Limites verticais de referência | y = 92 a 159 | y = 32 a 159 |
| Centro horizontal geométrico | x = 95,5 | x = 95,5 |
| Âncora prática dos pés | (96, 159) | (96, 159) |
| Margem superior | 92 px | 32 px |
| Margem inferior | 32 px | 32 px |

A área visível inclui cabelo, calçados e acessórios vestidos. Medir seu
retângulo pelos pixels com alpha maior que zero, excluindo efeitos separados.
A altura normal equivale a aproximadamente 1,88 vez a pequena. Essa relação
define a escala artística entre os portes.

As dimensões são alvos para a pose frontal em pé, não limites para todas as
poses. A largura pode variar conforme a anatomia; para o porte normal, usar
60 a 72 px como faixa de ajuste da pose frontal. Registrar as medidas finais
na ficha do personagem antes de produzir animações. Golpes, poses laterais e
poses abertas podem ultrapassar a área de referência, respeitando o canvas
e preservando a escala corporal.

Para o porte grande, o canvas e a âncora já estão definidos neste documento.
A área visível comum permanece pendente de definição; registrar as medidas
de cada referência produzida até estabelecer esse padrão.

Reduzir uma imagem automaticamente com escala fracionária pode perder
detalhes. Revisar os pixels na resolução final ao adaptar assets existentes.

## Alinhamento e consistência entre quadros

- Para portes pequeno e normal, usar a âncora prática `(96, 159)` e a linha de apoio
  `y = 159`. Centralizar a pose de referência pelo corpo e pelo apoio dos pés.
- Manter a âncora estável ao exportar. Não recentralizar cada quadro pelo
  retângulo visível: um braço estendido não deve deslocar o corpo inteiro.
- Em movimentos no chão, o pé de apoio deve respeitar a linha de chão.
  Pés levantados e oscilações do corpo fazem parte da pose. No idle, limitar
  deslocamentos incidentais a 1 px por eixo.
- Saltos, voo e golpes aéreos podem afastar os pés do chão, com deslocamento
  intencional e retorno ao alinhamento de referência.
- Preservar tamanho da cabeça, comprimento dos membros, volume do corpo,
  roupa e densidade dos pixels. Mudar a pose sem mudar a escala do personagem.
- Para canvas grande, usar `(128, 223)` como âncora de produção, preservando
  os 32 px abaixo da linha dos pés. A integração deve compensar a diferença
  de âncora ao colocar personagens de canvases diferentes no mesmo chão.

Essas âncoras são coordenadas de desenho no PNG. Não são automaticamente a
origem do nó Godot. Com sprite de 192×192 centralizado e sem offset, a linha
`y = 159` fica 63 unidades abaixo da origem. Conferir posição visual, colisão e
barra de vida ao integrar qualquer novo sprite.

## Estilo e transparência

- Produzir pixel art na resolução final, com contornos escuros consistentes,
  formas legíveis e sombras em blocos. Manter a mesma densidade de pixels
  e o mesmo acabamento entre personagens de todos os portes.
- Fixar uma paleta por personagem a partir da pose de referência. Manter as
  mesmas cores entre quadros e evitar novas cores sem necessidade.
- Exportar o corpo em PNG RGBA com fundo transparente e alpha binário
  (`0` ou `255`), sem suavização, blur ou halo nas bordas.
- Manter rosto, cabelo, roupa e acessórios reconhecíveis entre as poses.
  Derivar os quadros da referência aprovada e revisar lado a lado.
- Exportar o corpo inteiro, sem cortes, fragmentos acidentais, texto,
  interface, cenário, chão ou sombra incorporada.
- Produzir aura, ki e impacto como recursos separados quando necessários,
  para preservar a leitura do corpo e permitir posicionamento próprio.
  Efeitos podem usar transparência parcial quando o visual exigir.

## Animações e spritesheets

Cada célula deve ter o tamanho completo do canvas, inclusive as áreas
transparentes. Não recortar automaticamente os quadros pela silhueta.
Usar células contíguas, sem espaçamento, em ordem de reprodução da esquerda
para a direita. Para uma linha de `N` quadros pequenos ou normais, a imagem
mede `(N × 192) × 192 px`; quatro quadros resultam em 768×192 px.
Para o porte grande, a imagem mede `(N × 256) × 256 px`.

Usar a seguinte configuração como ponto de partida para novas animações:

| Animação | Quadros iniciais sugeridos | Reprodução |
|---|---|---|
| `idle` | 1 | Pose estática |
| `walk` | 4 | 8 FPS, em loop |
| `attack` | 4 | 10 FPS, sem loop |

Essas contagens são uma referência inicial, não uma obrigação para todos os
personagens. Ajustar a duração visual em conjunto com a ação implementada;
a animação não determina dano, alcance ou cooldown do servidor.

Usar escala visual `(1, 1)` e filtragem nearest no padrão de produção. A troca
de animação não deve exigir uma escala diferente para compensar assets de
resoluções incompatíveis. Frente, lado e costas precisam de referências
próprias quando forem necessárias. Espelhamento horizontal só resolve
esquerda/direita e exige revisão de detalhes assimétricos.

## Revisão antes de integrar

1. Conferir dimensões, transparência, grade e ausência de pixels cortados.
2. Medir a pose de referência e registrar porte, canvas, área visível, âncora
   e paleta na ficha de cada personagem.
3. Comparar personagens do mesmo porte e de portes diferentes em escala 1:1,
   com os pés na mesma linha de chão.
4. Reproduzir a sequência e conferir apoio dos pés, volume, rosto, roupa e
   continuidade do último para o primeiro quadro nas animações em loop.
5. No Godot, conferir recortes, nearest, escala, origem visual, colisões e HUD.
6. Ao alterar assets ou cenas, executar `./scripts/validate.sh` e a validação
   runtime aplicável conforme [o guia de testes](07-testes.md).

Este documento altera apenas a especificação. A adaptação das imagens, cenas
e scripts existentes deve ser validada quando for implementada.
