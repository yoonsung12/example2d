using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
    DoubleJump,
    Dash,
    WallJump
}

public class PlayerAbilities : MonoBehaviour
{
    private PlatformerMovement     _movement;
    private HashSet<AbilityType>   _unlocked = new();

    private void Awake()
    {
        _movement = GetComponent<PlatformerMovement>();
    }

    private void Start()
    {
        // 저장된 능력 복원
        if (SaveManager.Instance == null) return;
        foreach (AbilityType ability in System.Enum.GetValues(typeof(AbilityType)))
        {
            if (SaveManager.Instance.IsAbilityUnlocked(ability.ToString()))
                Apply(ability);
        }
    }

    public void UnlockAbility(AbilityType ability)
    {
        if (_unlocked.Contains(ability)) return;
        Apply(ability);
        SaveManager.Instance?.AddUnlockedAbility(ability.ToString());
        Debug.Log($"[PlayerAbilities] Unlocked: {ability}");
    }

    private void Apply(AbilityType ability)
    {
        _unlocked.Add(ability);
        switch (ability)
        {
            case AbilityType.DoubleJump: _movement.CanDoubleJump = true; break;
            case AbilityType.Dash:       _movement.CanDash       = true; break;
            case AbilityType.WallJump:   _movement.CanWallJump   = true; break;
        }
    }

    public bool HasAbility(AbilityType ability) => _unlocked.Contains(ability);
}
