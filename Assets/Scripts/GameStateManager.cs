using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum GameState : byte { Lobby, Countdown, Playing, Ended }

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Singleton { get; private set; }

    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private float roundSeconds = 90f;
    [SerializeField] private int minimumPlayers = 2;

    public int MinimumPlayers => minimumPlayers;

    public NetworkVariable<GameState> State = new(GameState.Lobby);
    public NetworkVariable<double> PhaseEndTime = new(0);

    public float SecondsRemaining =>
        Mathf.Max(0f,  (float)(PhaseEndTime.Value - NetworkManager.ServerTime.Time));

    private void Awake() => Singleton = this;

    public override void OnDestroy()
    {
        if (Singleton == this) Singleton = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        //in scene objects will keep old values if you host a second time
        if (IsServer) State.Value = GameState.Lobby;

        State.OnValueChanged += LogStateChange;
    }

    public override void OnNetworkDespawn()
    {
        State.OnValueChanged -= LogStateChange;
    }

    private void LogStateChange(GameState previous, GameState current) =>
        Debug.Log($"[GameState] {previous} -> {current}");
    
    private void BeginPhase(GameState next, float seconds)
    {
        //setting end time first so no clients see new state on an old timer
        PhaseEndTime.Value = NetworkManager.ServerTime.Time + seconds;
        State.Value = next;
    }

    private void StartRound()
    {
        BeginPhase(GameState.Playing, roundSeconds);
    }
    private bool EveryoneReady()
    {
        if (PlayerLobbyState.All.Count < minimumPlayers) return false;

        foreach(PlayerLobbyState player in PlayerLobbyState.All)
            if (!player.IsReady.Value) return false;

        return true;
    }
    private void EndRound()
    {
        if (State.Value == GameState.Ended) return; //ends once only
        State.Value = GameState.Ended;
        NetworkManager.SceneManager.LoadScene("EndScene", LoadSceneMode.Single);
    }

    private void Update()
    {
        if (!IsServer) return;

        switch (State.Value)
        {
            case GameState.Lobby:
                if (EveryoneReady())                                    
                    BeginPhase(GameState.Countdown, countdownSeconds);  
                break;                                                  

            case GameState.Countdown:
                if (!EveryoneReady()) State.Value = GameState.Lobby;    
                else if (SecondsRemaining <= 0f) StartRound();          
                break;                                                      
            
            case GameState.Playing:
                if(SecondsRemaining <= 0f) EndRound();
            break;
        }
    }
}
