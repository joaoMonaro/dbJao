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

@onready var animated_sprite: AnimatedSprite2D = $AnimatedSprite2D

var current_state: PlayerState = PlayerState.IDLE


func _ready() -> void:
	animated_sprite.animation_finished.connect(_on_animation_finished)
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
		animated_sprite.flip_h = false
	elif direction.x < 0.0:
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
	animated_sprite.scale = ATTACK_VISUAL_SCALE
	animated_sprite.position = ATTACK_VISUAL_OFFSET
	animated_sprite.play(&"attack")


func _on_animation_finished() -> void:
	if current_state != PlayerState.ATTACK or animated_sprite.animation != &"attack":
		return

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
