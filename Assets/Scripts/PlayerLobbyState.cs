using System.Collections.Generic;
using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerNetworkController))]
public class PlayerLobbyState : NetworkBehaviour
{
    public static readonly List<PlayerLobbyState> All = new();
    public static event Action OnLobbyChanged;
    public NetworkVariable<bool> IsReady = new(false);
    public PlayerNetworkController Controller { get; private set; }

    public void Awake() => Controller = GetComponent<PlayerNetworkController>();

    public override void OnNetworkSpawn()
    {
        All.Add(this);
        IsReady.OnValueChanged += HandleReadyChanged;
        Controller.nickname.OnValueChanged += HandleNicknameChanged;
        OnLobbyChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        All.Remove(this);
        IsReady.OnValueChanged -= HandleReadyChanged;
        Controller.nickname.OnValueChanged -= HandleNicknameChanged;
        OnLobbyChanged?.Invoke();
    }

    private void HandleReadyChanged(bool previous, bool current) =>
        OnLobbyChanged?.Invoke();

    private void HandleNicknameChanged(FixedString32Bytes previous,FixedString32Bytes current) =>
        OnLobbyChanged?.Invoke();

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
