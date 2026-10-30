using UnityEngine;
using TMPro;

public class ScoreEntryController : MonoBehaviour
{
    [SerializeField] private TMP_Text playerName;
    [SerializeField] private TMP_Text playerScore;

    public void SetEntry(string name, int score)
    {
        playerName.text = name;
        playerScore.text = score.ToString();
    }

    public void UpdateScore(int score)
    {
        playerScore.text = score.ToString();
    }
}
