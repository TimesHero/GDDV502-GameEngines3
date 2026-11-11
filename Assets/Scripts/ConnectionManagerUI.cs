using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using Unity.Netcode.Transports.UTP;

using TMPro;

using System.Net;
using System.Net.Sockets;

public class ConnectionManagerUI : MonoBehaviour
{

    [SerializeField] private GameObject scorePanel;
    public Button serverButton;
    public Button hostButton;
    public Button clientButton;    
    
    [Header("Local Area Network")]
    [SerializeField] private TMP_InputField addressInput;
    [SerializeField] private TMP_Text connectionStatusText;
    [SerializeField] private TMP_Text hostAddressText;
    [SerializeField] private ushort port = 7777;
        private UnityTransport Transport =>
            NetworkManager.Singleton.GetComponent<UnityTransport>();

    void Start()
    {
        // Must be set before any button starts the network, because the host's own
        // player goes through approval too. NGO allows only one callback, hence = not +=.
        NetworkManager.Singleton.ConnectionApprovalCallback = ApproveConnection;

        serverButton.onClick.AddListener( () => { 
            ConfigureAsHost();
            NetworkManager.Singleton.StartServer(); 
            ShowScorePanel();
        });
        hostButton.onClick.AddListener(() => { 
            ConfigureAsHost();
            NetworkManager.Singleton.StartHost();
            ShowScorePanel();
        });
        clientButton.onClick.AddListener( () => { 
            if (!ConfigureAsClient()) return;
            NetworkManager.Singleton.StartClient();
            ShowScorePanel();
        });

        NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
    }

    private void ConfigureAsHost()
    {
        //Listens on every adapter (0.0.0.0)
        Transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        hostAddressText.text = $"Hosting on {GetLocalIPv4()} : {port}";
    }

    private bool ConfigureAsClient()
        {
            string address = addressInput.text.Trim();
            if (address.Length == 0) address = "127.0.0.1";

            if (!IPAddress.TryParse(address, out _))
            {
                connectionStatusText.text = $"\"{address}\" is not an IP address.";
                return false;
            }

            Transport.SetConnectionData(address, port);
            hostAddressText.text = string.Empty;
            return true;
        }
    
    private void ShowScorePanel()
    {
        connectionStatusText.text = string.Empty;
        scorePanel.SetActive(true);
        gameObject.SetActive(false);
    }

    public void ShowMenu(string message) 
    {
        if (ScorePanelManager.Singleton != null)
            ScorePanelManager.Singleton.ClearEntries();
        
        scorePanel.SetActive(false);
        hostAddressText.text = string.Empty;
        connectionStatusText.text = message;
        gameObject.SetActive(true);
    }

    private void HandleDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer || gameObject.activeSelf) return;

        string reason = NetworkManager.Singleton.DisconnectReason;
        ShowMenu(string.IsNullOrEmpty(reason) ? "Lost connection to the host." : reason);
    }

    // The Server runs this for every connection request and answers yes or no.
    // Joining during Countdown is allowed: the new player is not ready, so the
    // countdown drops back to Lobby.
    private void ApproveConnection(NetworkManager.ConnectionApprovalRequest request,
                                   NetworkManager.ConnectionApprovalResponse response)
    {
        GameStateManager game = GameStateManager.Singleton;
        bool matchRunning = game != null && game.IsSpawned &&
            (game.State.Value == GameState.Playing || game.State.Value == GameState.Ended);

        response.Approved = !matchRunning;
        response.CreatePlayerObject = true;
        response.Reason = matchRunning
            ? "A match is already in progress. Try again when it ends."
            : string.Empty;
    }
    
    public static string GetLocalIPv4()
    {
        try
        {
            using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Connect("8.8.8.8", 65530);
            return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
        }
        catch (SocketException)
        {
                return "127.0.0.1";
        }
    }    
}
