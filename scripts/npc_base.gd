class_name NpcBase
extends CharacterBody2D

@export var move_speed: float = 40.0
@export var max_health: int = 100
@export var respawn_delay: float = 5.0

@onready var sprite: Sprite2D = $Sprite2D
@onready var body_shape: CollisionShape2D = $CollisionShape2D
@onready var hurtbox: Area2D = $Hurtbox
@onready var hurtbox_shape: CollisionShape2D = $Hurtbox/CollisionShape2D
@onready var health_bar: ProgressBar = $HealthBar

var current_direction: Vector2 = Vector2.ZERO
var current_health: int
var is_dead: bool = false
var spawn_position: Vector2


func _ready() -> void:
	spawn_position = global_position
	current_health = maxi(max_health, 0)
	_update_health_bar()


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
	visible = false

	await get_tree().create_timer(maxf(respawn_delay, 0.0)).timeout
	if is_inside_tree():
		_respawn()


func _respawn() -> void:
	global_position = spawn_position
	current_health = maxi(max_health, 0)
	_update_health_bar()
	body_shape.set_deferred(&"disabled", false)
	hurtbox_shape.set_deferred(&"disabled", false)
	hurtbox.set_deferred(&"monitoring", true)
	hurtbox.set_deferred(&"monitorable", true)
	visible = true
	is_dead = false
	_on_respawned()
	set_physics_process(true)


func _on_respawned() -> void:
	pass


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
