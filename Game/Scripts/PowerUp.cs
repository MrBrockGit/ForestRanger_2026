using Godot;
using System;

public partial class PowerUp : Area2D
{
	// *Счетчик ходов до появления предмета* -
	private int _timer = 4;
	
	// *Случайный тип усиления (1, 2 или 3)* -
	private int _type;
	
	// *Ссылка на анимации AnimatedSprite2D* -
	private AnimatedSprite2D _sprite;

	public override void _Ready()
	{
		// *Получаем узел анимаций при старте* -
		_sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
		
		// *Выбираем случайный тип от 1 до 3* -
		_type = (int)(GD.Randi() % 3) + 1; 
		
		// *Ищем игрока на сцене Level1, чтобы следить за его шагами* -
		Player p = GetTree().Root.GetNodeOrNull<Player>("Level1/Player"); 
		if (p == null)
		{
			// *Запасной путь поиска, если сцена запущена отдельно* -
			p = GetParent().GetNodeOrNull<Player>("Player");
		}

		// *Если нашли игрока, подписываемся на его шаги* -
		if (p != null) 
		{
			p.Step += OnPlayerStep;
		}
		
		// *Обновляем визуальное отображение на старте* -
		UpdateVisuals();
	}

	private void OnPlayerStep()
	{
		// *Каждый шаг игрока уменьшаем таймер "созревания"* -
		if (_timer > 0)
		{
			_timer--;
			UpdateVisuals();
		}
	}

	private void UpdateVisuals()
	{
		// *Переключаем анимации подготовки в зависимости от таймера* -
		if (_timer == 3) _sprite.Play("warn3");
		else if (_timer == 2) _sprite.Play("warn2");
		else if (_timer == 1) _sprite.Play("warn1");
		else if (_timer == 0)
		{
			// *Когда таймер равен 0, запускаем анимацию конкретного выпавшего предмета (item1, item2 или item3)* -
			_sprite.Play("item" + _type);
		}
	}

	// *Этот метод вызывается автоматически сигналом body_entered* -
	public void _on_body_entered(Node2D body)
	{
		// *Проверяем, что предмет полностью появился и на него наступил именно Игрок* -
		if (_timer == 0 && body is Player p)
		{
			// *Передаем игроку его новые суперсилы* -
			p.ApplyPowerUp(_type);
			
			// *Удаляем объект усиления с карты* -
			QueueFree();
		}
	}
}
