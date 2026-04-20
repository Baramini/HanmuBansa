using UnityEngine;
using Unity.Netcode;
using System.Collections.Generic;

// Airstrike: deals 1 damage to all players at their current position.
// Cannot be blocked by shield.
public class AirStrikeItem : ItemBase
{
    public override void Apply(GameObject tank)
    {
        // -- Find all active tanks and deal damage --
        TankHealth[] allTanks = FindObjectsByType<TankHealth>(FindObjectsSortMode.None);

        foreach (TankHealth target in allTanks)
        {
            if (target.gameObject == tank) continue;
            if (target.IsDead) continue;
            target.TakeDamage(1);
            // -- TODO: show airstrike visual effect at target position --
        }
    }
}