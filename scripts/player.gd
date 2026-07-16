extends CharacterBody2D

enum PlayerState {
	IDLE,
	WALK,
	ATTACK,
}

const NORMAL_VISUAL_SCALE := Vector2.ONE
const ATTACK_VISUAL_SCALE := Vector2(0.5, 0.5)
const NORMAL_VISUAL_OFFSET := Vector2.ZERO
const ATTACK_VISUAL_OFFSET := Vector2(0.0, 9.0)

@export var move_speed: float = 200.0
@export var attack_damage: int = 20
@export var attack_active_frame: int = 3
@export var attack_offset_x: float = 46.0

@onready var animated_sprite: AnimatedSprite2D = $AnimatedSprite2D
@onready var attack_area: Area2D = $AttackArea
@onready var attack_shape: CollisionShape2D = $AttackArea/CollisionShape2D

var current_state: PlayerState = PlayerState.IDLE
var facing_direction: float = 1.0
var hit_targets: Array[Node] = []


func _ready() -> void:
	animated_sprite.animation_finished.connect(_on_animation_finished)
	animated_sprite.frame_changed.connect(_on_animated_sprite_frame_changed)
	attack_area.area_entered.connect(_on_attack_area_area_entered)
	_disable_attack_area()
	animated_sprite.play(&"idle")
	_clamp_to_viewport()


func _physics_process(_delta: float) -> void:
	if current_state == PlayerState.ATTACK:
		velocity = Vector2.ZERO
		move_and_slide()
		_clamp_to_viewport()
		return

	if Input.is_action_just_pressed("attack"):
		_start_attack()
		_clamp_to_viewport()
		return

	var direction := Input.get_vector("move_left", "move_right", "move_up", "move_down")

	if direction != Vector2.ZERO:
		direction = direction.normalized()

	velocity = direction * move_speed
	_update_movement_state(direction)
	move_and_slide()
	_clamp_to_viewport()


func _update_movement_state(direction: Vector2) -> void:
	if direction.x > 0.0:
		facing_direction = 1.0
		animated_sprite.flip_h = false
	elif direction.x < 0.0:
		facing_direction = -1.0
		animated_sprite.flip_h = true

	if direction != Vector2.ZERO:
		current_state = PlayerState.WALK
		if animated_sprite.animation != &"walk":
			animated_sprite.play(&"walk")
	else:
		current_state = PlayerState.IDLE
		if animated_sprite.animation != &"idle":
			animated_sprite.play(&"idle")


func _start_attack() -> void:
	if current_state == PlayerState.ATTACK:
		return

	current_state = PlayerState.ATTACK
	velocity = Vector2.ZERO
	hit_targets.clear()
	_disable_attack_area()
	_update_attack_area_position()
	animated_sprite.scale = ATTACK_VISUAL_SCALE
	animated_sprite.position = ATTACK_VISUAL_OFFSET
	animated_sprite.play(&"attack")


func _on_animated_sprite_frame_changed() -> void:
	if current_state == PlayerState.ATTACK and animated_sprite.animation == &"attack" and animated_sprite.frame == attack_active_frame:
		_enable_attack_area()
	else:
		_disable_attack_area()


func _enable_attack_area() -> void:
	_update_attack_area_position()
	attack_shape.set_deferred(&"disabled", false)
	attack_area.set_deferred(&"monitoring", true)


func _disable_attack_area() -> void:
	attack_shape.set_deferred(&"disabled", true)
	attack_area.set_deferred(&"monitoring", false)


func _update_attack_area_position() -> void:
	attack_area.position.x = attack_offset_x * facing_direction


func _on_attack_area_area_entered(area: Area2D) -> void:
	if current_state != PlayerState.ATTACK or animated_sprite.frame != attack_active_frame:
		return

	var target := area.get_parent()
	if target in hit_targets or not target.has_method(&"take_damage"):
		return

	hit_targets.append(target)
	target.take_damage(attack_damage)


func _on_animation_finished() -> void:
	if current_state != PlayerState.ATTACK or animated_sprite.animation != &"attack":
		return

	_disable_attack_area()
	hit_targets.clear()
	current_state = PlayerState.IDLE
	animated_sprite.scale = NORMAL_VISUAL_SCALE
	animated_sprite.position = NORMAL_VISUAL_OFFSET
	animated_sprite.play(&"idle")
	_clamp_to_viewport()


func _clamp_to_viewport() -> void:
	var viewport_rect: Rect2 = get_viewport_rect()
	var current_texture: Texture2D = animated_sprite.sprite_frames.get_frame_texture(animated_sprite.animation, animated_sprite.frame)
	var half_sprite_size: Vector2 = current_texture.get_size() * animated_sprite.scale.abs() / 2.0
	var visual_offset: Vector2 = animated_sprite.position
	var minimum_position: Vector2 = viewport_rect.position + half_sprite_size - visual_offset
	var maximum_position: Vector2 = viewport_rect.end - half_sprite_size - visual_offset

	global_position = global_position.clamp(minimum_position, maximum_position)
