using UnityEngine;
using Unity.Netcode;

// Base class for all items.
// Inherit this and implement Apply() for each item type.
public abstract class ItemBase : NetworkBehaviour
{
    public abstract void Apply(GameObject tank);
}