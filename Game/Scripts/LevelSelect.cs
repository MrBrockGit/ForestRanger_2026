using Godot;
using System;

public partial class LevelSelect : Control
{
	// При нажатии кнопки "Уровень 1"
	public void _on_button_pressed()
	{
		// Загрузитть сцену 1 уровня
		GetTree().ChangeSceneToFile("res://Scenes/Level1.tscn");
	}
}
