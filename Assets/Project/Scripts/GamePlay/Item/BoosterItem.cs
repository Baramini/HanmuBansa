using UnityEngine;

// Booster: increases move speed by 25% for 15 seconds.
public class BoosterItem : ItemBase
{
    [SerializeField] private float speedMultiplier = 1.5f;
    [SerializeField] private float duration = 15f;

    public override void Apply(GameObject tank)
    {
        tank.GetComponent<TankStatus>()?.ActivateBooster(speedMultiplier, duration);
    }
}