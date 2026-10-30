using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NicknamePanelManager : MonoBehaviour
{
    public static string nickname {get; private set; } = string.Empty;

    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private GameObject connectionPanel;

    void Start()
    {
        submitButton.onClick.AddListener(ValidateNickname);
    }

    private void ValidateNickname()
    {
        string input = inputField.text;

        bool isValid = !string.IsNullOrEmpty(input) && input.Length >= 2;

        if(isValid)
        {
            nickname = input;
            gameObject.SetActive(false);
            connectionPanel.SetActive(true);
        }
    }
}
