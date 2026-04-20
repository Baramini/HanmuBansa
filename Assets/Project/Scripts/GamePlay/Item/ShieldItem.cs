using UnityEngine;

// Shield: reflects the next incoming projectile once, then consumed.
public class ShieldItem : ItemBase
{
    public override void Apply(GameObject tank)
    {
        tank.GetComponent<TankStatus>()?.ActivateShield();
    }
}