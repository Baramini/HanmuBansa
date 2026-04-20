using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

// Sets Cinemachine camera target to the local player's tank after spawn.
public class CameraController : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;

    public override void OnNetworkSpawn()
    {
        // -- Only set camera target for the local owner --
        if (!IsOwner) return;

        if (cinemachineCamera == null)
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();

        if (cinemachineCamera != null)
            cinemachineCamera.Follow = transform;
    }
}