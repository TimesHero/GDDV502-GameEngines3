using UnityEngine;
using UnityEngine.SceneManagement;

using TMPro;
using Unity.Netcode;
using Unity.Collections;

using System.Collections.Generic;
using System.Linq;

public class VictoryPanelManager : NetworkBehaviour
{
    [SerializeField] private TMP_Text nicknameText;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadCompleted;
        }
    }

    private void OnSceneLoadCompleted(
        string sceneName,
        LoadSceneMode loadsceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
        {
            IReadOnlyDictionary<ulong, NetworkClient> clients = NetworkManager.Singleton.ConnectedClients;
            PlayerNetworkController[] players =
                clients.Select(entry => entry.Value.PlayerObject.GetComponent<PlayerNetworkController>()).ToArray();

            players = players.OrderByDescending(player => player.Score).ToArray();
            SendVictorNicknameRpc(players[0].nickname.Value);
        }

    [Rpc(SendTo.ClientsAndHost,
        InvokePermission = RpcInvokePermission.Server)]
    private void SendVictorNicknameRpc(FixedString32Bytes nickname)
    {
        nicknameText.text = nickname.ToString();
    }
}
