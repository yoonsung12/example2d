using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SaveFileName = "save.json";
    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    [Serializable]
    public class SaveData
    {
        public float playerX;
        public float playerY;
        public float currentHealth;
        public List<string> unlockedAbilities = new();
        public List<string> visitedRooms = new();
    }

    public SaveData CurrentSave { get; private set; } = new();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Save(Vector2 playerPos, float health)
    {
        CurrentSave.playerX = playerPos.x;
        CurrentSave.playerY = playerPos.y;
        CurrentSave.currentHealth = health;
        File.WriteAllText(SavePath, JsonUtility.ToJson(CurrentSave, true));
        Debug.Log($"[SaveManager] Saved at {playerPos}");
    }

    public bool Load()
    {
        if (!File.Exists(SavePath)) return false;
        CurrentSave = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        return true;
    }

    public void AddUnlockedAbility(string abilityName)
    {
        if (!CurrentSave.unlockedAbilities.Contains(abilityName))
            CurrentSave.unlockedAbilities.Add(abilityName);
    }

    public bool IsAbilityUnlocked(string abilityName)
        => CurrentSave.unlockedAbilities.Contains(abilityName);

    public void AddVisitedRoom(string roomId)
    {
        if (!CurrentSave.visitedRooms.Contains(roomId))
            CurrentSave.visitedRooms.Add(roomId);
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        CurrentSave = new SaveData();
    }
}
