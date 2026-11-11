using TMPro;
using UnityEngine;

public class LobbyEntryController : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text readyStatus;
    [SerializeField] private Color readyColour = new(0.30f, 0.69f, 0.31f); //green
    [SerializeField] private Color notReadyColour = new(0.90f, 0.22f, 0.21f); //red

    public void SetEntry(string name, bool ready)
    {
        playerName.text = name;
        readyStatus.text = ready ? "READY" : "Not Ready";
        readyStatus.color = ready ? readyColour : notReadyColour;
    }
}
