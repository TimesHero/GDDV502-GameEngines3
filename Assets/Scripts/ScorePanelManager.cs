using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Netcode;

using UnityEngine;
using UnityEngine.SceneManagement;

public class ScorePanelManager : NetworkBehaviour
{
    public static ScorePanelManager Singleton;

    [SerializeField] private GameObject scoreEntryPrefab;
    [SerializeField] private Transform scorePanel;
    [SerializeField] private uint winningScore = 1;

    private readonly Dictionary<ulong, GameObject> scoreEntries = new();

    #region MonoBehavior
    private void Awake()
    {
        if (Singleton == null)
        {
            Singleton = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnConnectionEvent += HandleConnectionEvents;
        }
    }

    public override void OnNetworkDespawn()
    {
        if(IsServer)
            NetworkManager.Singleton.OnConnectionEvent -= HandleConnectionEvents;
    }

    private void OnDisable()
    {
        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnConnectionEvent -= HandleConnectionEvents;
        }
    }
    #endregion

    #region Callbacks
    private void HandleConnectionEvents(NetworkManager manager, ConnectionEventData eventData)
    {
        switch (eventData.EventType)
        {
            case ConnectionEvent.ClientConnected:
                OnClientConnected(eventData.ClientId);
                break;
            case ConnectionEvent.ClientDisconnected:
                OnClientDisconnected(eventData.ClientId);
                break;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        IReadOnlyDictionary<ulong, NetworkClient> connectedClients = NetworkManager.Singleton.ConnectedClients;

        SendNewClientExistingNames(clientId, connectedClients);
        SendExistingClientsNewName(clientId, connectedClients);
    }

    private void SendNewClientExistingNames(ulong clientId, IReadOnlyDictionary<ulong, NetworkClient> connectedClients)
    {
        ulong[] clientIds = new ulong[connectedClients.Count];
        FixedString32Bytes[] clientNames = new FixedString32Bytes[connectedClients.Count];
        int[] clientScores = new int[connectedClients.Count];

        int i = 0;

        foreach (KeyValuePair<ulong, NetworkClient> client in connectedClients)
        {
            PlayerNetworkController player = client.Value.PlayerObject.GetComponent<PlayerNetworkController>();

            clientIds[i] = client.Key;              // Get existing clients' ids.
            clientNames[i] = player.nickname.Value; // Get existing clients' nicknames.
            clientScores[i] = player.Score;         // Get existing clients' scores.
            i++;
        }

        NewScoreEntryRpc(
            clientIds,
            clientNames,
            clientScores,
            RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    private void SendExistingClientsNewName(ulong clientId, IReadOnlyDictionary<ulong, NetworkClient> connectedClients)
    {
        ulong[] newClientIdArray = new ulong[] { clientId }; // Get new client id.

        NetworkObject newPlayerObject = connectedClients[clientId].PlayerObject;
        FixedString32Bytes[] newPlayerNameArray = new FixedString32Bytes[] {        // Get new client nickname.
            newPlayerObject.GetComponent<PlayerNetworkController>().nickname.Value 
        };
        int[] newClientScoreArray = new int[1] { 0 }; // Set new client score as 0.

        ulong[] existingClientsIds = connectedClients
            .Where(entry => entry.Key != clientId)
            .Select(entry => entry.Key).ToArray();

        NewScoreEntryRpc(
            newClientIdArray,
            newPlayerNameArray,
            newClientScoreArray,
            RpcTarget.Group(existingClientsIds, RpcTargetUse.Temp)
        );
    }

    private void OnClientDisconnected(ulong clientId)
    {
        RemoveScoreEntryRpc(clientId);
    }
    #endregion
    public void ClearEntries()
    {
        foreach (GameObject entry in scoreEntries.Values)
            Destroy(entry);

        scoreEntries.Clear();
        scorePanel.gameObject.SetActive(false);
    }
    #region RPCs
    [Rpc(SendTo.SpecifiedInParams,
        InvokePermission = RpcInvokePermission.Server)]
    private void NewScoreEntryRpc(ulong[] clientIds, FixedString32Bytes[] clientNames, int[] clientScores, RpcParams rpcParams)
    {
        if (clientIds == null || clientNames == null || clientIds.Length != clientNames.Length)
        {
            Debug.LogError("Client IDs and names must have matching lengths.");
            return;
        }

        if (!scorePanel.gameObject.activeSelf) { scorePanel.gameObject.SetActive(true); }

        for (int i = 0; i < clientIds.Length; i++)
        {
            GameObject scoreEntry = Instantiate(scoreEntryPrefab, scorePanel);

            ScoreEntryController sec = scoreEntry.GetComponent<ScoreEntryController>();
            sec.SetEntry(clientNames[i].ToString(), clientScores[i]);

            scoreEntries.Add(clientIds[i], scoreEntry);
        }
    }

    [Rpc(SendTo.ClientsAndHost,
        InvokePermission = RpcInvokePermission.Server)]
    private void RemoveScoreEntryRpc(ulong client)
    {
        if (scoreEntries.TryGetValue(client, out GameObject scoreEntry))
        {
            Destroy(scoreEntry);
            scoreEntries.Remove(client);

            scorePanel.gameObject.SetActive(scoreEntries.Count != 0);
        }
    }

    [Rpc(SendTo.ClientsAndHost,
        InvokePermission = RpcInvokePermission.Server)]
    public void UpdateScoreEntryRpc(ulong client, int newScore)
    {
        if (scoreEntries.TryGetValue(client, out GameObject entry))
        {
            ScoreEntryController sec = entry.GetComponent<ScoreEntryController>();
            sec.UpdateScore(newScore);

            if (newScore == winningScore)
                NetworkManager.Singleton.SceneManager.LoadScene("EndScene", LoadSceneMode.Single);
        }
    }
    #endregion
}