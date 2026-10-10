using UnityEngine;

/// <summary>Consumable cheese that restores 10 player health when used.</summary>
[CreateAssetMenu(fileName = "Cheese", menuName = "Inventory/Items/Consumable/Cheese")]
public class CheeseItem : ConsumableItem
{
    [Header("Cheese Healing")]
    [SerializeField, Min(0f)] private float healthRestored = 10f;

    protected override void OnEnable()
    {
        base.OnEnable();
        itemType = ItemType.Consumable;
        stackable = true;
        maxStackSize = 10;
        if (string.IsNullOrWhiteSpace(itemName)) itemName = "Cheese";
        if (string.IsNullOrWhiteSpace(description)) description = "Restores 10 health when eaten.";
    }

    public override void Use()
    {
        PlayerHealth health = Object.FindFirstObjectByType<PlayerHealth>();
        if (health == null)
        {
            Debug.LogError("[Cheese] Could not find PlayerHealth. Cheese was not consumed successfully.");
            return;
        }

        if (health.IsDead())
        {
            Debug.LogWarning("[Cheese] Cannot eat cheese while dead.");
            return;
        }

        float before = health.GetCurrentHealth();
        health.Heal(healthRestored);
        Debug.Log($"[Cheese] Eaten. Health: {before} -> {health.GetCurrentHealth()}.");
    }
}
