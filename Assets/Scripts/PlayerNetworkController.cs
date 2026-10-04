using Unity.Netcode;

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerNetworkController : NetworkBehaviour
{
    [Header("Properties")]
    [SerializeField] private float turnSpeed = 90f;
    [Space(10), Header("Component References")]
    [SerializeField] private Animator localAnimator;

    private readonly int isMovingParam = Animator.StringToHash("IsMoving");
    private readonly int isRunningParam = Animator.StringToHash("IsRunning");

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
        }
    }

    private Vector3 GetRandomPosition()
    {
        return new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
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

        LastSentData = new PlayerNetworkData()
        {
            zDelta = inputZ,
            isRunning = isRunning
        };
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
    private void SubmitAnimatorUpdateRpc(PlayerNetworkData data)
    {
        localAnimator.SetBool(isMovingParam, data.zDelta != 0);
        localAnimator.SetBool(isRunningParam, data.isRunning);
    }
    #endregion
}