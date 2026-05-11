using UnityEngine;

// 1 damage to all players
public class AirStrikeItem : ItemBase
{
    public override void Apply(GameObject tank)
    {
        TankHealth[] allTanks = FindObjectsByType<TankHealth>(FindObjectsSortMode.None);

        foreach (TankHealth target in allTanks)
        {
            if (target.gameObject == tank) continue;
            if (target.IsDead) continue;
            target.TakeDamage(1);
            // TODO: add visual effect
        }
    }
}