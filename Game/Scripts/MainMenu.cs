using Godot;
using System;

public partial class MainMenu : Control
{

	[Export] public Control ConfirmMenu;

	// При нажатии кнопки "Играть"
	public void _on_button_pressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/LevelSelect.tscn");
	}

	// Вызывается при нажатии кнопки "Выйти" в меню
	public void OnExitButtonPressed()
	{
		if (ConfirmMenu != null)
		{
			ConfirmMenu.Visible = true; 
		}
	}

	// Если игрок нажал "ДА" — закрываем игру
	public void OnConfirmYesPressed()
	{
		GetTree().Quit();
	}

	// Если игрок нажал "НЕТ" - прячем меню подтверждения
	public void OnConfirmNoPressed()
	{
		if (ConfirmMenu != null)
		{
			ConfirmMenu.Visible = false;
		}
	}
}
