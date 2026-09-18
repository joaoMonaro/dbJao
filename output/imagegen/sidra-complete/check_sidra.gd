extends SceneTree

var failures: Array[String] = []

func expect(condition: bool, description: String) -> void:
	if not condition:
		failures.append(description)
		push_error(description)

func _initialize() -> void:
	call_deferred("run_checks")

func run_checks() -> void:
	var sidra = load("res://scenes/Sidra.tscn").instantiate()
	root.add_child(sidra)
	var sprite: AnimatedSprite2D = sidra.get_node("VisualRoot/AnimatedSprite2D")
	var frames: SpriteFrames = sprite.sprite_frames
	expect(sidra.get("NetworkSetupIsValid"), "Configuração do NPC")
	expect(sidra.get_node_or_null("VisualRoot/Sprite2D") == null, "Um só visual")
	expect(sprite.texture_filter == CanvasItem.TEXTURE_FILTER_NEAREST, "Filtro nearest")
	for animation in {"idle": 1, "walk": 4, "attack": 6}:
		var count: int = {"idle": 1, "walk": 4, "attack": 6}[animation]
		expect(frames.get_frame_count(animation) == count, animation + ": quadros")
		for index in count:
			var texture = frames.get_frame_texture(animation, index)
			expect(texture.get_size() == Vector2(192, 192), animation + ": célula")
			expect(texture.get_image().get_used_rect().end.y == 160,
			animation + ": pés na linha 159")
	expect(sprite.animation == &"idle", "Repouso inicial")
	expect(frames.get_animation_speed("walk") == 8, "Caminhada a 8 FPS")
	expect(frames.get_animation_speed("attack") == 10, "Gesto a 10 FPS")
	expect(not frames.get_animation_loop("attack"), "Gesto sem loop")
	sidra.set("AiState", 1)
	sidra.set("MovementDirection", Vector2.LEFT)
	await create_timer(0.1).timeout
	expect(sprite.animation == &"walk", "Estado de patrulha mostra caminhada")
	expect(sprite.flip_h, "Caminhada espelha para esquerda")
	await create_timer(0.2).timeout
	expect(sprite.frame > 0, "Caminhada avança quadros")
	sidra.set("IsAttacking", true)
	sidra.set("MovementDirection", Vector2.RIGHT)
	await create_timer(0.1).timeout
	expect(sprite.animation == &"attack", "Estado de ataque mostra gesto")
	expect(not sprite.flip_h, "Gesto orientado para direita")
	await create_timer(0.65).timeout
	expect(sprite.frame == 5 and not sprite.is_playing(),
		"Gesto percorre seis quadros e para")
	sidra.set("IsAttacking", false)
	await create_timer(0.1).timeout
	expect(sprite.animation == &"walk", "Gesto retorna ao movimento")
	sidra.set("AiState", 0)
	await create_timer(0.1).timeout
	expect(sprite.animation == &"idle", "Movimento retorna ao repouso")
	expect(sprite.scale == Vector2.ONE and sprite.position == Vector2.ZERO,
		"Escala e origem constantes")
	sidra.queue_free()
	await process_frame
	if failures.is_empty():
		print("[PASS] Sidra: 11 quadros, estados, playback, espelhamento e alinhamento.")
	quit(0 if failures.is_empty() else 1)
