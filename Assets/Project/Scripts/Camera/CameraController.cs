using UnityEngine;
using Unity.Cinemachine;
using Unity.Netcode;

public class CameraController : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;

    public override void OnNetworkSpawn()
    {
        // Only local owner
        if (!IsOwner || !NetworkObject.IsPlayerObject) return;

        if (cinemachineCamera == null) cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        if (cinemachineCamera != null) cinemachineCamera.Follow = transform;
    }
}