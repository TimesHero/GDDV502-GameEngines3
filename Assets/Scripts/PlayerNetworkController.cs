using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Collections;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PlayerNetworkController : NetworkBehaviour
{
    [Header("Properties")]
    [SerializeField] private float turnSpeed = 90f;
    [Space(10), Header("Component References")]
    [SerializeField] private Animator localAnimator;

    public NetworkVariable<FixedString32Bytes> nickname = new();
    // Everyone can read Score, but only the Server can change it.
    public NetworkVariable<int> Score = new(0);
    [SerializeField] private NetworkAnimator networkAnimator;

    private readonly int isMovingParam = Animator.StringToHash("IsMoving");
    private readonly int isRunningParam = Animator.StringToHash("IsRunning");
    private readonly int isCrouchingParam = Animator.StringToHash("IsCrouching");
    private readonly int emoteParam = Animator.StringToHash("Emote");

    private int lastSentInputX = 0;
    private int serverInputX = 0;

    private PlayerNetworkData lastSentData;
    private PlayerNetworkData LastSentData  // LastSentData = someNewData;
    {
        set
        {
            if (!lastSentData.Equals(value))
            {
                lastSentData = value;
                SubmitAnimatorUpdateRpc(lastSentData);
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            RequestPositionRpc();
            SubmitNameUpdateRpc(NicknamePanelManager.nickname);
        }
    }

#region Callbacks
    private void OnSceneLoadCompleted(ulong clientId, string sceneName, LoadSceneMode sceneMode)
    {
        if (sceneName.Equals("EndScene"))
        {
            gameObject.SetActive(false);
        }
    }
#endregion

    private Vector3 GetRandomPosition()
    {
        return new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
    }

    // NetworkTransform uses Server authority, so only the Server may move a player.
    public void ServerRespawn()
    {
        if (IsServer) transform.position = GetRandomPosition();
    }

    // The one place a score changes. A negative amount takes points away,
    // and Mathf.Max stops the score from going below zero.
    public void ServerAddScore(int amount)
    {
        if (!IsServer) return;

        Score.Value = Mathf.Max(0, Score.Value + amount);
        ScorePanelManager.Singleton.UpdateScoreEntryRpc(OwnerClientId, Score.Value);
        GameStateManager.Singleton.ReportScore(Score.Value);
    }

    // Used between rounds. It skips ReportScore(), because a reset is not a score.
    public void ServerResetScore()
    {
        if (!IsServer) return;

        Score.Value = 0;
        ScorePanelManager.Singleton.UpdateScoreEntryRpc(OwnerClientId, 0);
    }

    void Update()
    {
        if (IsOwner)
        {
            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                RequestPositionRpc();
            }

            CaptureInput();
        }

        if (IsServer && serverInputX != 0)
        {
            UpdateRotation();
        }
    }

    public void CaptureInput()
    {
        if (Keyboard.current == null)
            return;

        int inputX =
            (Keyboard.current.dKey.isPressed ? 1 : 0) -
            (Keyboard.current.aKey.isPressed ? 1 : 0);

        if (inputX != lastSentInputX)
        {
            lastSentInputX = inputX;
            SubmitInputRpc(inputX);
        }

        int inputZ = Keyboard.current.wKey.isPressed ? 1 : 0;
        bool isRunning = Keyboard.current.leftShiftKey.isPressed;
        bool isCrouching = Keyboard.current.cKey.isPressed;

        LastSentData = new PlayerNetworkData()
        {
            zDelta = inputZ,
            isRunning = isRunning,
            isCrouching = isCrouching
        };

        if (Keyboard.current.tKey.wasPressedThisFrame)
        {
            SubmitEmoteRpc();
        }
    }

    private void UpdateRotation()
    {
        float rotation = turnSpeed * serverInputX * Time.deltaTime;
        transform.Rotate(0, rotation, 0);
    }

    #region RPCs
    [Rpc(SendTo.Server,
        InvokePermission = RpcInvokePermission.Owner)]
    private void RequestPositionRpc()
    {
        transform.position = GetRandomPosition();
    }

    [Rpc(SendTo.Server,
        InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitInputRpc(int inputX)
    {
        serverInputX = Mathf.Clamp(inputX, -1, 1);
    }

    [Rpc(SendTo.Server,
        InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitEmoteRpc()
    {
        networkAnimator.SetTrigger(emoteParam);
    }

    [Rpc(SendTo.Server,
        InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitAnimatorUpdateRpc(PlayerNetworkData data)
    {
        localAnimator.SetBool(isMovingParam, data.zDelta != 0);
        localAnimator.SetBool(isRunningParam, data.isRunning);
        localAnimator.SetBool(isCrouchingParam, data.isCrouching);
    }

    [Rpc(SendTo.Server,
        InvokePermission = RpcInvokePermission.Owner)]
    private void SubmitNameUpdateRpc(FixedString32Bytes nickname)
    {
        this.nickname.Value = nickname;
    }

    #endregion
}