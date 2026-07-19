extends "res://scripts/npc_base.gd"

@export var target_group: StringName = &"player"
@export var contact_damage: int = 10
@export var contact_damage_interval: float = 3.0

@onready var damage_area: Area2D = $DamageArea
@onready var damage_timer: Timer = $DamageTimer

var target: Node2D
var contact_target: Node


func _ready() -> void:
	super._ready()
	damage_area.body_entered.connect(_on_damage_area_body_entered)
	damage_area.body_exited.connect(_on_damage_area_body_exited)
	damage_timer.timeout.connect(_on_damage_timer_timeout)
	damage_timer.wait_time = maxf(contact_damage_interval, 0.01)
	_find_target()


func _physics_process(_delta: float) -> void:
	if is_dead:
		return

	if not is_instance_valid(target):
		_find_target()

	if not is_instance_valid(target):
		velocity = Vector2.ZERO
		return

	current_direction = global_position.direction_to(target.global_position)
	velocity = current_direction * move_speed
	move_and_slide()
	_keep_inside_viewport()
	_update_sprite_direction()


func _find_target() -> void:
	target = get_tree().get_first_node_in_group(target_group) as Node2D


func _on_damage_area_body_entered(body: Node2D) -> void:
	if is_dead or not body.is_in_group(target_group):
		return

	contact_target = body
	_deal_contact_damage()
	damage_timer.start(maxf(contact_damage_interval, 0.01))


func _on_damage_area_body_exited(body: Node2D) -> void:
	if body != contact_target:
		return

	contact_target = null
	damage_timer.stop()


func _on_damage_timer_timeout() -> void:
	if is_dead or not is_instance_valid(contact_target):
		return

	_deal_contact_damage()


func _deal_contact_damage() -> void:
	if contact_damage <= 0 or not is_instance_valid(contact_target):
		return

	if contact_target.has_method(&"take_damage"):
		contact_target.take_damage(contact_damage)
