extends CharacterBody2D

@export var move_speed: float = 40.0
@export var min_direction_time: float = 1.5
@export var max_direction_time: float = 4.0
@export var max_health: int = 100

@onready var sprite: Sprite2D = $Sprite2D
@onready var body_shape: CollisionShape2D = $CollisionShape2D
@onready var hurtbox: Area2D = $Hurtbox
@onready var hurtbox_shape: CollisionShape2D = $Hurtbox/CollisionShape2D
@onready var health_bar: ProgressBar = $HealthBar

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
var current_direction: Vector2 = Vector2.ZERO
var direction_timer: float = 0.0
var current_health: int
var is_dead: bool = false
var random := RandomNumberGenerator.new()


func _ready() -> void:
	current_health = maxi(max_health, 0)
	_update_health_bar()
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


func take_damage(amount: int) -> void:
	if is_dead or amount <= 0:
		return

	current_health = maxi(current_health - amount, 0)
	_update_health_bar()

	if current_health <= 0:
		_die()


func _update_health_bar() -> void:
	health_bar.max_value = max_health
	health_bar.value = current_health


func _die() -> void:
	if is_dead:
		return

	is_dead = true
	current_direction = Vector2.ZERO
	velocity = Vector2.ZERO
	set_physics_process(false)
	body_shape.set_deferred(&"disabled", true)
	hurtbox_shape.set_deferred(&"disabled", true)
	hurtbox.set_deferred(&"monitoring", false)
	hurtbox.set_deferred(&"monitorable", false)
	queue_free()


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


func _keep_inside_viewport() -> Vector2:
	var viewport_rect: Rect2 = get_viewport_rect()
	var half_sprite_size: Vector2 = sprite.get_rect().size * sprite.scale.abs() / 2.0
	var minimum_position: Vector2 = viewport_rect.position + half_sprite_size
	var maximum_position: Vector2 = viewport_rect.end - half_sprite_size
	var inward_direction := Vector2.ZERO

	if global_position.x < minimum_position.x:
		inward_direction.x = 1.0
	elif global_position.x > maximum_position.x:
		inward_direction.x = -1.0

	if global_position.y < minimum_position.y:
		inward_direction.y = 1.0
	elif global_position.y > maximum_position.y:
		inward_direction.y = -1.0

	global_position = global_position.clamp(minimum_position, maximum_position)
	return inward_direction


func _update_sprite_direction() -> void:
	if current_direction.x > 0.0:
		sprite.flip_h = false
	elif current_direction.x < 0.0:
		sprite.flip_h = true
