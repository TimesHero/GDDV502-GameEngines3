using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

// Shows a player's nickname above their head and keeps it facing the camera.
// It sits on the Overhead child, not the player root. A NetworkBehaviour only has to be
// somewhere under a NetworkObject to get OnNetworkSpawn.
public class PlayerOverhead : NetworkBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject crown;

    private PlayerNetworkController controller;

    private void Awake() => controller = GetComponentInParent<PlayerNetworkController>();

    // Players who were already in the game set their name before we joined, so it arrives
    // as the starting value and OnValueChanged never fires for it. A new player sends their
    // name by RPC just after spawning, so it arrives as a change. Handling both covers everyone.
    public override void OnNetworkSpawn()
    {
        controller.nickname.OnValueChanged += HandleNicknameChanged;
        ShowName(controller.nickname.Value);
    }

    public override void OnNetworkDespawn()
    {
        controller.nickname.OnValueChanged -= HandleNicknameChanged;
    }

    private void HandleNicknameChanged(FixedString32Bytes previous, FixedString32Bytes current) =>
        ShowName(current);

    private void ShowName(FixedString32Bytes nickname) => nameText.text = nickname.ToString();

    // Every client already knows every score (Score is a NetworkVariable), so each one
    // works out the leader for itself and gets the same answer. Nothing new is sent.
    // The test is > rather than >=, so players tied for first all wear a crown.
    private bool IsLeader()
    {
        int myScore = controller.Score.Value;
        if (myScore == 0) return false;

        foreach (PlayerLobbyState player in PlayerLobbyState.All)
            if (player.Controller.Score.Value > myScore) return false;

        return true;
    }

    // Billboarding: copy the camera's rotation so the text always faces the screen.
    // LookAt would point the text's front at the camera and make it read backwards.
    // LateUpdate runs after the player has turned this frame, so the plate never lags.
    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam != null) transform.rotation = cam.transform.rotation;

        // SetActive with an unchanged value costs almost nothing, so calling it every frame is fine.
        crown.SetActive(IsLeader());
    }
}
