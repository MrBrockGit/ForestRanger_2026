using Godot;
using System;
using System.Collections.Generic;

public partial class GameManager : Node
{
	// *Ссылка на сетку с полом* -
	[Export] public TileMapLayer FloorMap;
	
	// *Ссылки на интерфейс (перетащи их из CanvasLayer в Инспекторе)* -
	[Export] public Label PercentLabel;
	[Export] public ProgressBar PercentBar;
	[Export] public Control WinMenu;
	[Export] public ColorRect FadeOverlay;
	[Export] public Control ConfirmMenu;

	// *Ссылка на сцену усиления (чтобы её создавать)* -
	[Export] public PackedScene PowerUpScene;

	// *Переменные для подсчета победы и усилений* -
	private int _totalCellsToPaint = 0;
	private int _stepCounter = 0;

	// *Точные координаты твоих клеток в индексах Godot (счет с 0)* -
	private readonly Vector2I _floorCoords = new Vector2I(2, 1);  // Клетка пола (3,2 -> индекс 2,1)
	private readonly Vector2I _playerCoords = new Vector2I(1, 1); // Клетка игрока (2,2 -> индекс 1,1)
	private readonly Vector2I _enemyCoords = new Vector2I(1, 3);  // Клетка врага (2,4 -> индекс 1,3)
	private readonly Vector2I _goldCoords = new Vector2I(2, 3);   // Золотая клетка (3,4 -> индекс 2,3)
	
	

	public override void _Ready()
	{
		// *Ищем узел Floor в нашей сцене* -
		FloorMap = GetParent().GetNodeOrNull<TileMapLayer>("Floor");
		
		if (FloorMap != null)
		{
			// *Считаем только те клетки, которые изначально являются полом* -
			_totalCellsToPaint = 0;
			var usedCells = FloorMap.GetUsedCells();
			foreach (var cell in usedCells)
			{
				Vector2I coords = FloorMap.GetCellAtlasCoords(cell);
				if (coords == _floorCoords || coords == _playerCoords || coords == _enemyCoords || coords == _goldCoords)
				{
					_totalCellsToPaint++;
				}
			}
		}

		// *Прячем менюшки и прозрачный экран при старте* -
		if (WinMenu != null) WinMenu.Visible = false;
		if (ConfirmMenu != null) ConfirmMenu.Visible = false;
		if (FadeOverlay != null) FadeOverlay.Modulate = new Color(0, 0, 0, 0); // Прозрачный черный
	}

	// *Метод, который красят игрок и враги. Принимает координаты и ID цвета (0 - игрок, 1 - враг, 2 - золотой)* -
	public void PaintCell(Vector2 globalPos, int colorId)
	{
		if (FloorMap == null) return;

		// *Переводим мировые координаты (пиксели) в координаты сетки (тайлы)* -
		Vector2I cellCoords = FloorMap.LocalToMap(FloorMap.ToLocal(globalPos));
		
		// *Безопасно получаем текущий ID источника (атласа) из этой ячейки* -
		int sourceId = FloorMap.GetCellSourceId(cellCoords);
		if (sourceId == -1) sourceId = 0;
		
		// *Определяем координаты в атласе согласно твоей разметке* -
		Vector2I atlasCoord = _enemyCoords; // Враг по умолчанию (1,3)
		if (colorId == 0) atlasCoord = _playerCoords; // Игрок (1,1)
		if (colorId == 2) atlasCoord = _goldCoords;   // Золотая клетка (2,3)
		
		// *Проверяем, существует ли такой тайл в атласе перед установкой* -
		TileSet tileSet = FloorMap.TileSet;
		if (tileSet != null && tileSet.HasSource(sourceId))
		{
			TileSetSource source = tileSet.GetSource(sourceId);
			if (source is TileSetAtlasSource atlasSource && atlasSource.HasTile(atlasCoord))
			{
				Vector2I currentCoords = FloorMap.GetCellAtlasCoords(cellCoords);
				
				// *Красим только то, что является полом или уже покрашено (не трогаем стены)* -
				if (currentCoords == _floorCoords || currentCoords == _playerCoords || currentCoords == _enemyCoords || currentCoords == _goldCoords)
				{
					FloorMap.SetCell(cellCoords, sourceId, atlasCoord);
				}
			}
		}

		// *Проверяем проценты после каждого закрашивания* -
		CheckWinCondition();
	}

	// *Возвращает цвет клетки. Золотую мы отдаем как "Вражескую (1)", чтобы он её не трогал* -
	public int GetCellColorId(Vector2 globalPos)
	{
		if (FloorMap == null) return -1;
		Vector2I mapPos = FloorMap.LocalToMap(FloorMap.ToLocal(globalPos));
		
		int sourceId = FloorMap.GetCellSourceId(mapPos);
		if (sourceId == -1) return -1;

		Vector2I atlasCoords = FloorMap.GetCellAtlasCoords(mapPos);
		
		// *Проверка наличия тайла в атласе* -
		TileSet tileSet = FloorMap.TileSet;
		if (tileSet != null && tileSet.HasSource(sourceId))
		{
			TileSetSource source = tileSet.GetSource(sourceId);
			if (source is TileSetAtlasSource atlasSource && !atlasSource.HasTile(atlasCoords))
			{
				return -1;
			}
		}
		
		if (atlasCoords == _enemyCoords) return 1; // Цвет врага
		if (atlasCoords == _goldCoords) return 1;  // Золотую клетку враг считает своей, чтобы обходить её
		if (atlasCoords == _playerCoords) return 0; // Цвет игрока
		if (atlasCoords == _floorCoords) return -2; // Чистый пол
		
		return -1; // Стены или пустота
	}

	private void CheckWinCondition()
	{
		int paintedCount = 0;
		var usedCells = FloorMap.GetUsedCells();
		TileSet tileSet = FloorMap.TileSet;

		foreach (var cell in usedCells)
		{
			int sourceId = FloorMap.GetCellSourceId(cell);
			if (sourceId == -1) continue;

			Vector2I coords = FloorMap.GetCellAtlasCoords(cell);
			
			if (tileSet != null && tileSet.HasSource(sourceId))
			{
				TileSetSource source = tileSet.GetSource(sourceId);
				if (source is TileSetAtlasSource atlasSource && !atlasSource.HasTile(coords))
				{
					continue;
				}
			}

			// *В зачет победы идут обычные закрашенные и золотые клетки* -
			if (coords == _playerCoords || coords == _goldCoords)
			{
				paintedCount++;
			}
		}

		// *Вычисляем процент и обновляем ProgressBar и Label* -
		if (_totalCellsToPaint > 0)
		{
			float percentage = (float)paintedCount / _totalCellsToPaint * 100f;
			
			if (PercentLabel != null) PercentLabel.Text = $"{(int)percentage}%";
			if (PercentBar != null) PercentBar.Value = percentage;

			// *Если закрашено больше 80% - ПОБЕДА* -
			if (percentage >= 80f)
			{
				ShowWinMenu();
			}
		}
	}

	private void ShowWinMenu()
	{
		if (WinMenu == null || WinMenu.Visible) return;
		
		WinMenu.Visible = true;
		Control panel = WinMenu.GetChildOrNull<Control>(0);
		if (panel != null)
		{
			panel.Scale = Vector2.Zero;
			Tween tween = CreateTween();
			tween.SetTrans(Tween.TransitionType.Back);
			tween.SetEase(Tween.EaseType.Out);
			tween.TweenProperty(panel, "scale", Vector2.One, 0.5f);
		}
	}

	public async void GameOverFade()
	{
		if (FadeOverlay == null) return;

		Tween tween = CreateTween();
		tween.TweenProperty(FadeOverlay, "modulate:a", 1.0f, 2.0f);
		
		await ToSignal(GetTree().CreateTimer(2.0f), "timeout");
		GetTree().ReloadCurrentScene();
	}

	public void CountStepForPowerUp()
	{
		_stepCounter++;
		if (_stepCounter >= 10 && PowerUpScene != null)
		{
			_stepCounter = 0;
			SpawnPowerUp();
		}
	}

	private void SpawnPowerUp()
	{
		var cells = FloorMap.GetUsedCells();
		List<Vector2I> validFloorCells = new List<Vector2I>();

		foreach (var cell in cells)
		{
			Vector2I coords = FloorMap.GetCellAtlasCoords(cell);
			if (coords == _floorCoords || coords == _playerCoords || coords == _enemyCoords || coords == _goldCoords)
			{
				validFloorCells.Add(cell);
			}
		}

		if (validFloorCells.Count == 0) return;

		Vector2I randomMapCell = validFloorCells[(int)(GD.Randi() % validFloorCells.Count)];
		Vector2 spawnGlobalPos = FloorMap.ToGlobal(FloorMap.MapToLocal(randomMapCell));

		Node2D powerUp = PowerUpScene.Instantiate<Node2D>();
		GetParent().AddChild(powerUp);
		powerUp.GlobalPosition = spawnGlobalPos;
	}

	public void OnHomeButtonPressed()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	public void OnExitButtonPressed()
	{
		if (ConfirmMenu != null)
		{
			GetTree().Paused = true;
			ConfirmMenu.Visible = true;
		}
	}

	public void OnConfirmYesPressed()
	{
		GetTree().Paused = false;
		GetTree().ChangeSceneToFile("res://Scenes/MainMenu.tscn");
	}

	public void OnConfirmNoPressed()
	{
		if (ConfirmMenu != null) ConfirmMenu.Visible = false;
		GetTree().Paused = false;
	}
}
