using System.Collections.Generic;
using System.Text.Json;
using Godot;
using RedFiendsAndRice.Data;
using FileAccess = Godot.FileAccess;

namespace RedFiendsAndRice.System;

// PORT NOTE: Unity used `Application.streamingAssetsPath` + `System.IO.File`.
// Godot bundles non-script assets inside the PCK at runtime, so plain `File.Open`
// won't reach them. Use `Godot.FileAccess` with a `res://` path instead — that
// path resolves to the exported PCK in builds and to the project folder in-editor.
//
// PORT NOTE: Unity's `JsonUtility` is gone. `System.Text.Json` is the closest 1:1
// for POCO struct deserialization in a Godot/.NET project. It defaults to camelCase
// matching being case-insensitive when configured, so the field names below
// (PascalCase on the C# side) will pair with camelCase JSON if you keep the
// Unity-style enemydata.json. Tune `JsonSerializerOptions` if your JSON uses a
// different casing convention.
public static class FileUtilities
{
  private const string EnemyDataPath = "res://data/enemydata.json";

  public static List<EnemyData> LoadEnemyDataFile()
  {
	var list = new List<EnemyData>();

	if (!FileAccess.FileExists(EnemyDataPath))
	{
	  GD.PushError($"JSON file not found: {EnemyDataPath}");
	  return list;
	}

	using var file = FileAccess.Open(EnemyDataPath, FileAccess.ModeFlags.Read);
	string jsonContent = file.GetAsText();
	var options = new JsonSerializerOptions
	{
	  PropertyNameCaseInsensitive = true,
	  IncludeFields = true,
	};
	var loader = JsonSerializer.Deserialize<EnemyDataLoader>(jsonContent, options);
	if (loader.data != null && loader.data.Length > 0)
	{
	  list.AddRange(loader.data);
	}
	return list;
  }
}
