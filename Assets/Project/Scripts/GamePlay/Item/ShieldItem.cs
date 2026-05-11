using UnityEngine;

// Reflects next attack
public class ShieldItem : ItemBase
{
    public override void Apply(GameObject tank)
    {
        tank.GetComponent<TankStatus>()?.ActivateShield();
    }
}