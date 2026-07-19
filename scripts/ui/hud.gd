extends CanvasLayer

@export var player_group: StringName = &"player"

@onready var health_bar: ProgressBar = $MarginContainer/PanelContainer/HBoxContainer/Bars/HealthBar
@onready var mana_bar: ProgressBar = $MarginContainer/PanelContainer/HBoxContainer/Bars/ManaBar
@onready var experience_bar: ProgressBar = $MarginContainer/PanelContainer/HBoxContainer/Bars/ExperienceBar
@onready var health_label: Label = $MarginContainer/PanelContainer/HBoxContainer/Bars/HealthBar/Label

var player: Node


func _ready() -> void:
	player = get_tree().get_first_node_in_group(player_group)
	if not is_instance_valid(player):
		return

	if player.has_signal(&"health_changed"):
		player.connect(&"health_changed", _on_health_changed)
	if player.has_signal(&"mana_changed"):
		player.connect(&"mana_changed", _on_mana_changed)
	if player.has_signal(&"experience_changed"):
		player.connect(&"experience_changed", _on_experience_changed)

	_on_health_changed(int(player.get(&"current_health")), int(player.get(&"max_health")))
	_on_mana_changed(int(player.get(&"current_mana")), int(player.get(&"max_mana")))
	_on_experience_changed(int(player.get(&"current_experience")), int(player.get(&"max_experience")))


func _on_health_changed(current: int, maximum: int) -> void:
	health_bar.max_value = maxi(maximum, 1)
	health_bar.value = current
	health_label.text = "%d/%d" % [current, maximum]


func _on_mana_changed(current: int, maximum: int) -> void:
	mana_bar.max_value = maxi(maximum, 1)
	mana_bar.value = current


func _on_experience_changed(current: int, maximum: int) -> void:
	experience_bar.max_value = maxi(maximum, 1)
	experience_bar.value = current
