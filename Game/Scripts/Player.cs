using Godot;
using System;

public partial class Player : CharacterBody2D
{
	// *Размер одной клетки в пикселях* -
	[Export] public int TileSize = 6;
	
	// *Флаг, не дающий зажать две кнопки одновременно* -
	private bool _isMoving = false;
	
	// *Счетчики ходов для усилений (1 и 2)* -
	private int _aoeTurns = 0;
	private int _goldenTurns = 0;
	
	// *Ссылки на узлы* -
	private RayCast2D _ray;
	private AnimatedSprite2D _sprite;
	private GameManager _gameManager;

	// *Сигналы для общения с врагами* -
	[Signal] public delegate void StepEventHandler();
	[Signal] public delegate void EndedEventHandler();

	public override void _Ready()
	{
		_ray = GetNode<RayCast2D>("RayCast2D");
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		_gameManager = GetParent().GetNodeOrNull<GameManager>("GameManager");
		
		Position = Position.Snapped(Vector2.One);
		_sprite.Play("default");
	}

	public override void _Process(double delta)
	{
		if (_isMoving || GetTree().Paused) return;

		Vector2 inputDir = Vector2.Zero;

		if (Input.IsActionPressed("ui_right")) {
			inputDir = Vector2.Right;
			_sprite.Play("right");
		}
		else if (Input.IsActionPressed("ui_left")) {
			inputDir = Vector2.Left;
			_sprite.Play("left");
		}
		else if (Input.IsActionPressed("ui_up")) {
			inputDir = Vector2.Up;
			_sprite.Play("up");
		}
		else if (Input.IsActionPressed("ui_down")) {
			inputDir = Vector2.Down;
			_sprite.Play("down");
		}

		if (inputDir != Vector2.Zero)
		{
			MovePlayer(inputDir);
		}
	}

	private void MovePlayer(Vector2 direction)
	{
		_ray.TargetPosition = direction * TileSize;
		_ray.ForceRaycastUpdate();

		if (_ray.IsColliding()) return;

		_isMoving = true;
		
		if (_gameManager != null) _gameManager.CountStepForPowerUp();
		
		EmitSignal(SignalName.Step); 

		Vector2 targetPos = Position + (direction * TileSize);
		
		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(this, "position", targetPos, 0.1f);
		
		tween.Finished += OnMoveFinished;
	}

	private void OnMoveFinished()
	{
		_isMoving = false;
		
		// *Цвет покраски: 2 - золотой, 0 - обычный розовый* -
		int currentColor = (_goldenTurns > 0) ? 2 : 0;
		if (_goldenTurns > 0) _goldenTurns--;

		// *Если активно 1 усиление (закраска области 3х3)* -
		if (_aoeTurns > 0)
		{
			for (int x = -1; x <= 1; x++)
			{
				for (int y = -1; y <= 1; y++)
				{
					Vector2 offset = new Vector2(x * TileSize, y * TileSize);
					if (_gameManager != null) _gameManager.PaintCell(GlobalPosition + offset, currentColor);
				}
			}
			_aoeTurns--;
		}
		else
		{
			if (_gameManager != null) _gameManager.PaintCell(GlobalPosition, currentColor);
		}
		
		EmitSignal(SignalName.Ended); 
	}

	public void ApplyPowerUp(int type)
	{
		if (type == 1) _aoeTurns = 5;       // Усиление 1: 3x3
		if (type == 2) _goldenTurns = 7;    // Усиление 2: Золотые клетки
		if (type == 3) 
		{
			// *Усиление 3: Заморозка всех врагов* -
			var enemies = GetTree().GetNodesInGroup("Enemies");
			foreach (Node enemy in enemies)
			{
				if (enemy is Enemy e) e.Freeze(5);
			}
		}
	}

	public void Die()
	{
		_isMoving = true;
		if (_gameManager != null) _gameManager.GameOverFade();
	}
}
