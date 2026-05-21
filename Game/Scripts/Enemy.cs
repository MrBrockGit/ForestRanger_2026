using Godot;
using System;
using System.Collections.Generic;

public partial class Enemy : CharacterBody2D
{
	[Export] public int TileSize = 6;
	[Export] public int EnemyColorId = 1; // ID цвета врага

	private bool _isMoving = false;
	private Vector2 _nextDirection = Vector2.Zero;
	private int _freezeTurns = 0; // Шаги заморозки

	private RayCast2D _ray;
	private Sprite2D _dot;
	private AnimatedSprite2D _sprite;
	private Player _player;
	private GameManager _gameManager;

	public override void _Ready()
	{
		_ray = GetNode<RayCast2D>("RayCast2D");
		_dot = GetNode<Sprite2D>("Dot");
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		
		_player = GetParent().GetNodeOrNull<Player>("Player");
		_gameManager = GetParent().GetNodeOrNull<GameManager>("GameManager");

		if (_player != null)
		{
			_player.Step += OnPlayerStep;
			_player.Ended += Prognose;
		}

		Position = Position.Snapped(Vector2.One);
		_sprite.Play("default");
		Prognose();
	}

	public void Freeze(int turns)
	{
		_freezeTurns = turns;
		if (_dot != null) _dot.Visible = false;
	}

	private void Prognose()
	{
		if (_freezeTurns > 0) return;

		CheckCollisionWithPlayer();

		Vector2[] allDirs = { Vector2.Right, Vector2.Left, Vector2.Up, Vector2.Down };
		List<Vector2> bestDirs = new List<Vector2>();     
		List<Vector2> availableDirs = new List<Vector2>(); 

		foreach (Vector2 dir in allDirs)
		{
			_ray.TargetPosition = dir * TileSize;
			_ray.ForceRaycastUpdate();

			if (!_ray.IsColliding())
			{
				availableDirs.Add(dir);
				
				if (_gameManager != null)
				{
					Vector2 targetGlobalPos = GlobalPosition + (dir * TileSize);
					int currentColor = _gameManager.GetCellColorId(targetGlobalPos);

					// *Приоритет: чистые клетки (-2) или клетки игрока (0)* -
					if (currentColor != EnemyColorId)
					{
						bestDirs.Add(dir);
					}
				}
			}
		}

		if (bestDirs.Count > 0)
		{
			_nextDirection = bestDirs[(int)(GD.Randi() % bestDirs.Count)];
		}
		else if (availableDirs.Count > 0)
		{
			_nextDirection = availableDirs[(int)(GD.Randi() % availableDirs.Count)];
		}
		else
		{
			_nextDirection = Vector2.Zero;
		}

		if (_dot != null && _nextDirection != Vector2.Zero)
		{
			_dot.Visible = true;
			_dot.Position = _nextDirection * TileSize;
		}
	}

	private void OnPlayerStep()
	{
		if (_dot != null) _dot.Visible = false;
		
		if (_freezeTurns > 0)
		{
			_freezeTurns--;
			return;
		}

		if (_nextDirection != Vector2.Zero)
		{
			MoveEnemy(_nextDirection);
		}
	}

	private void MoveEnemy(Vector2 direction)
	{
		if (_isMoving) return;

		if (direction == Vector2.Right) _sprite.Play("right");
		else if (direction == Vector2.Left) _sprite.Play("left");
		else if (direction == Vector2.Up) _sprite.Play("up");
		else if (direction == Vector2.Down) _sprite.Play("down");

		_isMoving = true;
		Vector2 targetPos = Position + (direction * TileSize);
		
		Tween tween = CreateTween();
		tween.SetTrans(Tween.TransitionType.Sine);
		tween.SetEase(Tween.EaseType.InOut);
		tween.TweenProperty(this, "position", targetPos, 0.1f);
		
		tween.Finished += () => 
		{
			_isMoving = false;
			if (_gameManager != null)
			{
				_gameManager.PaintCell(GlobalPosition, EnemyColorId);
			}
		};
	}

	private void CheckCollisionWithPlayer()
	{
		if (_player != null && Position.DistanceTo(_player.Position) < 1.0f)
		{
			_player.Die();
		}
	}
}
