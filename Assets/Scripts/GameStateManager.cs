using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState : byte { Lobby, Countdown, Playing, Ended }

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Singleton { get; private set; }

    [SerializeField] private float countdownSeconds = 3f;
    [SerializeField] private float roundSeconds = 90f;
    [SerializeField] private int minimumPlayers = 2;
    [SerializeField] private int winningScore = 10;

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
        // Put every player on a new random spot so nobody gets a head start.
        foreach (PlayerLobbyState player in PlayerLobbyState.All)
            player.Controller.ServerRespawn();

        BeginPhase(GameState.Playing, roundSeconds);
    }
    private bool EveryoneReady()
    {
        if (PlayerLobbyState.All.Count < minimumPlayers) return false;

        foreach(PlayerLobbyState player in PlayerLobbyState.All)
            if (!player.IsReady.Value) return false;

        return true;
    }

    // Called by the Server each time a player's score changes.
    // >= instead of == so a 2-point patty that jumps from 9 to 11 still wins.
    public void ReportScore(int score)
    {
        if (IsServer && score >= winningScore) EndRound();
    }

    // Called by the host's Play Again button. Resets the match and returns everyone to the lobby.
    public void PlayAgain()
    {
        if (!IsServer || State.Value != GameState.Ended) return;

        // Clear the leftover patties. Despawn() on the Server removes them on every machine.
        // This happens here instead of in EndRound(), because the winning patty despawns
        // itself right after it ends the round, and despawning it twice is an error.
        foreach (CollectableNetworkController patty in
                 FindObjectsByType<CollectableNetworkController>(FindObjectsSortMode.None))
            if (patty.IsSpawned) patty.NetworkObject.Despawn();

        foreach (PlayerLobbyState player in PlayerLobbyState.All)
        {
            player.IsReady.Value = false;
            player.Controller.ServerResetScore();
        }

        State.Value = GameState.Lobby;
    }

    private void EndRound()
    {
        if (State.Value == GameState.Ended) return; //ends once only
        State.Value = GameState.Ended;
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
