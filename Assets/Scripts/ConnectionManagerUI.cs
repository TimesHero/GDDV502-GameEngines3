using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class ConnectionManagerUI : MonoBehaviour
{

    [SerializeField] private GameObject scorePanel;
    
    public Button serverButton;
    public Button hostButton;
    public Button clientButton;
    void Start()
    {
        serverButton.onClick.AddListener( () => { 
            NetworkManager.Singleton.StartServer(); 
            ShowScorePanel();
        });
        hostButton.onClick.AddListener(() => { 
            NetworkManager.Singleton.StartHost();
            ShowScorePanel();
        });
        clientButton.onClick.AddListener( () => { 
            NetworkManager.Singleton.StartClient();
            ShowScorePanel();
        });
    }

    private void ShowScorePanel()
    {
        scorePanel.SetActive(!scorePanel.activeSelf);
        gameObject.SetActive(false);
    }

}
