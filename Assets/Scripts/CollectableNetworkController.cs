using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(NetworkObject))]
public class CollectableNetworkController : NetworkBehaviour
{
    public int pointValue = 1;
    private void OnTriggerEnter(Collider other)
    {
        if(IsServer)
        {
            NetworkObject playerObject = other.GetComponent<NetworkObject>();
            AwardPointsToPlayer(playerObject.OwnerClientId);

            NetworkObject obj = GetComponent<NetworkObject>();
            obj.Despawn();
        }
    }
    
    private void AwardPointsToPlayer(ulong clientId)
    {
        NetworkObject player = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        PlayerNetworkController playerController = player.GetComponent<PlayerNetworkController>();

        playerController.Score += 1;
        ScorePanelManager.Singleton.UpdateScoreEntryRpc(clientId, playerController.Score);
    }
}
