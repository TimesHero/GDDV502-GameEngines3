using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class CollectableNetworkController : NetworkBehaviour
{
    public int pointValue = 1;
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !IsSpawned) return;
        if (GameStateManager.Singleton.State.Value != GameState.Playing) return;

        if (!other.TryGetComponent(out NetworkObject playerObject)
            || !playerObject.IsPlayerObject) return;

        AwardPointsToPlayer(playerObject.OwnerClientId);
        NetworkObject.Despawn();
    }

    private void AwardPointsToPlayer(ulong clientId)
    {
        NetworkObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        PlayerNetworkController playerController = player.GetComponent<PlayerNetworkController>();

        playerController.ServerAddScore(pointValue);
    }
}
