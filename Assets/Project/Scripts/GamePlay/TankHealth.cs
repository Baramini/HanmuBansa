using UnityEngine;
using System;

public class TankHealth : MonoBehaviour
{
    [SerializeField] private Renderer bodyRenderer;
    [SerializeField] private Material damagedMaterial;

    [SerializeField] private int maxHp = 2;

    public int CurrentHp { get; private set; }
    public bool IsDead { get; private set; }

    public event Action<int> OnDamaged;
    public event Action OnDead;

    private void Awake()
    {
        CurrentHp = maxHp;
    }

    public void TakeDamage(int amount = 1)
    {
        if (IsDead) return;

        CurrentHp = Mathf.Max(0, CurrentHp - amount);
        OnDamaged?.Invoke(CurrentHp);

        if (CurrentHp == 1 && bodyRenderer != null)
            bodyRenderer.material = damagedMaterial;

        if (CurrentHp <= 0)
            Die();
    }

    private void Die()
    {
        IsDead = true;
        OnDead?.Invoke();
    }
}