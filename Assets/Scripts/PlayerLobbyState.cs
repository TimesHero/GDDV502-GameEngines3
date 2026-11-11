using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerNetworkController))]
public class PlayerLobbyState : NetworkBehaviour
{
    public static readonly List<PlayerLobbyState> All = new();
    public NetworkVariable<bool> IsReady = new(false);
    public PlayerNetworkController Controller { get; private set; }

    public void Awake() => Controller = GetComponent<PlayerNetworkController>();

    public override void OnNetworkSpawn()
    {
        All.Add(this);
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
    } 

    private void Update()
    {
        if (IsOwner && Keyboard.current.yKey.wasPressedThisFrame)
            ToggleReady();
    }

    public void ToggleReady()
    {
        if (!IsOwner) return;
        SetReadyRpc(!IsReady.Value);
    }

    [Rpc(SendTo.Server,
        InvokePermission = RpcInvokePermission.Owner)]
    private void SetReadyRpc(bool ready)
    {
        //Server should have the final say. No late ready's after the round has begun.
        GameState state = GameStateManager.Singleton.State.Value;
        if (state == GameState.Lobby || state == GameState.Countdown)
            IsReady.Value = ready;
    }
}
