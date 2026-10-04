using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;

public class ConnectionManagerUI : MonoBehaviour
{
    
    public Button serverButton;
    public Button hostButton;
    public Button clientButton;
    void Start()
    {
        serverButton.onClick.AddListener( () => { NetworkManager.Singleton.StartServer(); });
        hostButton.onClick.AddListener(() => { NetworkManager.Singleton.StartHost();} );
        clientButton.onClick.AddListener( () => { NetworkManager.Singleton.StartClient(); });
    }

}
