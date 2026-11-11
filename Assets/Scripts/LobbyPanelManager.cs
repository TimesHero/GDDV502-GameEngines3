using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
public class LobbyPanelManager : MonoBehaviour
{
    [SerializeField] private ConnectionManagerUI connectionUI;

    [Header("Lobby")]
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private Transform playerList;
    [SerializeField] private GameObject lobbyEntryPrefab;
    [SerializeField] private TMP_Text lobbyStatusText;
    [SerializeField] private Button readyButton;
    [SerializeField] private TMP_Text readyButtonLabel;
    [SerializeField] private Button leaveButton;

    [Header("In Game")]
    [SerializeField] private TMP_Text roundTimerText;

    private PlayerLobbyState GetLocalLobbyState()
    {
        NetworkObject player = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
        return player != null ? player.GetComponent<PlayerLobbyState>() : null;
    }

    private void Start()
    {
        readyButton.onClick.AddListener(HandleReadyClicked);
        leaveButton.onClick.AddListener(HandleLeaveClicked);
        PlayerLobbyState.OnLobbyChanged += RefreshPlayerList;
    }

    private void OnDestroy() => PlayerLobbyState.OnLobbyChanged -= RefreshPlayerList;

    private void HandleReadyClicked()
    {
        PlayerLobbyState local = GetLocalLobbyState();
        if (local != null) local.ToggleReady();
    }

    private void Update()
    {
        GameStateManager game = GameStateManager.Singleton;
        bool connected = NetworkManager.Singleton.IsListening
                     && game != null && game.IsSpawned;
        GameState state = connected ? game.State.Value : GameState.Lobby;

        lobbyPanel.SetActive(connected &&
            (state == GameState.Lobby || state == GameState.Countdown));
        roundTimerText.gameObject.SetActive(connected && state == GameState.Playing);

        if (!connected) return;

        readyButton.gameObject.SetActive(GetLocalLobbyState() != null);

        switch (state)
        {
            case GameState.Lobby:
                lobbyStatusText.text = BuildLobbyStatus(game);
                break;
            case GameState.Countdown:
                lobbyStatusText.text = $"Starting in {Mathf.CeilToInt(game.SecondsRemaining)}";
                break;
            case GameState.Playing:
                int seconds = Mathf.CeilToInt(game.SecondsRemaining);
                roundTimerText.text = $"{seconds / 60}:{seconds % 60:00}";
                break;
        }
    }

    private string BuildLobbyStatus(GameStateManager game)
    {
        int total = PlayerLobbyState.All.Count;
        if (total < game.MinimumPlayers)
            return $"Waiting for players ({total}/{game.MinimumPlayers})";

        int ready = 0;
        foreach (PlayerLobbyState player in PlayerLobbyState.All)
            if (player.IsReady.Value) ready++;

        return $"Waiting for everyone to ready up ({ready}/{total})";
    }

    private void HandleLeaveClicked()
    {
        //if the host leaves, every client is disconnected
        NetworkManager.Singleton.Shutdown();
        connectionUI.ShowMenu("You left the game.");
    }

    private void RefreshPlayerList()
    {
        foreach (Transform oldRow in playerList)
            Destroy(oldRow.gameObject);
        
        foreach (PlayerLobbyState player in PlayerLobbyState.All)
        {
            string name = player.Controller.nickname.Value.ToString();
            if (player.IsOwner) name += " (you)";

            GameObject row = Instantiate(lobbyEntryPrefab, playerList);
            row.GetComponent<LobbyEntryController>().SetEntry(name, player.IsReady.Value);
        }

        PlayerLobbyState local = GetLocalLobbyState();
        if (local != null)
            readyButtonLabel.text = local.IsReady.Value ? "Not Ready" : "Ready";
    }
}
