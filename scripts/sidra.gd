extends "res://scripts/npc_base.gd"

@export var min_direction_time: float = 1.5
@export var max_direction_time: float = 4.0

var possible_directions: Array[Vector2] = [
	Vector2.ZERO,
	Vector2.ZERO,
	Vector2.UP,
	Vector2.DOWN,
	Vector2.LEFT,
	Vector2.RIGHT,
	Vector2(-1.0, -1.0).normalized(),
	Vector2(1.0, -1.0).normalized(),
	Vector2(-1.0, 1.0).normalized(),
	Vector2(1.0, 1.0).normalized(),
]
var direction_timer: float = 0.0
var random := RandomNumberGenerator.new()


func _ready() -> void:
	super._ready()
	random.randomize()
	_choose_new_direction()


func _physics_process(delta: float) -> void:
	if is_dead:
		return

	direction_timer -= delta
	if direction_timer <= 0.0:
		_choose_new_direction()

	velocity = current_direction * move_speed
	move_and_slide()

	var avoidance_direction := _get_collision_avoidance_direction()
	avoidance_direction += _keep_inside_viewport()

	if avoidance_direction != Vector2.ZERO:
		_choose_new_direction(avoidance_direction.normalized())
	elif get_slide_collision_count() > 0:
		_choose_new_direction()

	_update_sprite_direction()


func _on_respawned() -> void:
	_choose_new_direction()
	_update_sprite_direction()


func _get_collision_avoidance_direction() -> Vector2:
	var avoidance_direction := Vector2.ZERO

	for collision_index in get_slide_collision_count():
		var collision := get_slide_collision(collision_index)
		avoidance_direction += collision.get_normal()

	return avoidance_direction.normalized() if avoidance_direction != Vector2.ZERO else Vector2.ZERO


func _choose_new_direction(inward_direction: Vector2 = Vector2.ZERO) -> void:
	var candidate := Vector2.ZERO
	var candidate_found := false

	for _attempt in possible_directions.size():
		candidate = possible_directions[random.randi_range(0, possible_directions.size() - 1)]
		if inward_direction == Vector2.ZERO or candidate == Vector2.ZERO or candidate.dot(inward_direction) >= 0.0:
			candidate_found = true
			break

	if not candidate_found:
		candidate = inward_direction.normalized()

	current_direction = candidate.normalized() if candidate != Vector2.ZERO else Vector2.ZERO

	var minimum_time := minf(min_direction_time, max_direction_time)
	var maximum_time := maxf(min_direction_time, max_direction_time)
	direction_timer = random.randf_range(maxf(0.1, minimum_time), maxf(0.1, maximum_time))
