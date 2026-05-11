using UnityEngine;
using Unity.Netcode;

// Base class for all items
public abstract class ItemBase : NetworkBehaviour
{
    public abstract void Apply(GameObject tank);
}