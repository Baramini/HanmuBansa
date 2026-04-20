using UnityEngine;

// Coolant: instantly resets overheat to 0.
public class CoolantItem : ItemBase
{
    public override void Apply(GameObject tank)
    {
        Debug.Log("냉각수!");
        tank.GetComponent<TankStatus>()?.ResetHeat();
    }
}