extends SceneTree

var failures: Array[String] = []

func expect(condition: bool, description: String) -> void:
	if not condition:
		failures.append(description)
		push_error(description)

func _initialize() -> void:
	call_deferred("run_checks")

func spawn_player(position: Vector2, attacking: bool = false) -> CharacterBody2D:
	var player = load("res://scenes/Player.tscn").instantiate()
	player.set("OwnerPeerId", 2)
	player.set("IsAttacking", attacking)
	player.position = position
	root.add_child(player)
	player.set_physics_process(false)
	player.get_node("NetworkInterpolation").set_process(false)
	return player

func run_checks() -> void:
	var player = spawn_player(Vector2(400, 300))
	var sprite: AnimatedSprite2D = player.get_node("VisualRoot/AnimatedSprite2D")
	var frames = sprite.sprite_frames
	for animation in {"idle": 1, "walk": 4, "attack": 6}:
		var count: int = {"idle": 1, "walk": 4, "attack": 6}[animation]
		expect(frames.get_frame_count(animation) == count, animation + ": contagem")
		for index in count:
			var texture = frames.get_frame_texture(animation, index)
			expect(texture.get_size() == Vector2(192, 192), animation + ": célula")
			var bounds = texture.get_image().get_used_rect()
			expect(bounds.end.y == 160, animation + ": pés na linha 159")
	expect(sprite.texture_filter == CanvasItem.TEXTURE_FILTER_NEAREST, "Filtro nearest")
	expect(sprite.animation == &"idle", "Idle inicial")
	expect(frames.get_animation_speed("walk") == 8, "Movimento a 8 FPS")
	expect(frames.get_animation_speed("attack") == 10, "Ataque a 10 FPS")
	expect(not frames.get_animation_loop("attack"), "Ataque sem loop")
	expect(is_equal_approx(player.get("AttackActionDuration"), 0.6), "Duração do ataque")
	player.set("FacingDirection", Vector2.LEFT)
	expect(sprite.flip_h, "Espelhamento para esquerda")
	player.set("IsAttacking", true)
	expect(sprite.animation == &"attack", "Estado inicia ataque")
	expect(sprite.scale == Vector2.ONE and sprite.position == Vector2.ZERO,
		"Ataque mantém escala e origem")
	await create_timer(0.65).timeout
	expect(sprite.frame == 5 and not sprite.is_playing(), "Ataque percorre os seis quadros")
	player.velocity = Vector2(200, 0)
	player.set("IsAttacking", false)
	expect(sprite.animation == &"walk", "Ataque retorna ao movimento")
	await create_timer(0.2).timeout
	expect(sprite.frame > 0, "Movimento avança quadros")
	player.set("FacingDirection", Vector2.RIGHT)
	expect(not sprite.flip_h, "Espelhamento para direita")
	player.set("IsAttacking", true)
	player.velocity = Vector2.ZERO
	player.set("IsAttacking", false)
	expect(sprite.animation == &"idle", "Ataque retorna ao idle")
	expect(sprite.scale == Vector2.ONE and sprite.position == Vector2.ZERO,
		"Transições mantêm escala e origem")
	var low_idle = spawn_player(Vector2(-1000, -1000))
	expect(low_idle.position == Vector2(64, 69), "Limite mínimo original em idle")
	var low_attack = spawn_player(Vector2(-1000, -1000), true)
	expect(low_attack.position == Vector2(64, 62), "Limite mínimo original no ataque")
	var viewport_end = root.get_visible_rect().end
	var high_idle = spawn_player(Vector2(10000, 10000))
	expect(high_idle.position == viewport_end - Vector2(64, 69), "Limite máximo em idle")
	var high_attack = spawn_player(Vector2(10000, 10000), true)
	expect(high_attack.position == viewport_end - Vector2(64, 80), "Limite máximo no ataque")
	for node in [player, low_idle, low_attack, high_idle, high_attack]:
		node.queue_free()
	await process_frame
	if failures.is_empty():
		print("[PASS] Player: 11 quadros, alinhamento, playback, transições, espelhamento e limites.")
	quit(0 if failures.is_empty() else 1)
