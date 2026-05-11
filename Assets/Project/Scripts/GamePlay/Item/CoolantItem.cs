using UnityEngine;

// resets overheat 0
public class CoolantItem : ItemBase
{
    public override void Apply(GameObject tank)
    {
        tank.GetComponent<TankStatus>()?.ResetHeat();
    }
}