using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI for starting host/client
/// </summary>
public class NetworkUI : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button clientButton;
    [SerializeField] private GameObject menuPanel;
    
    private void Start()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(StartHost);
            
        if (clientButton != null)
            clientButton.onClick.AddListener(StartClient);
    }
    
    private void StartHost()
    {
        if (networkManager != null)
        {
            networkManager.StartHost();
            HideMenu();
        }
    }
    
    private void StartClient()
    {
        if (networkManager != null)
        {
            networkManager.StartClient();
            HideMenu();
        }
    }
    
    private void HideMenu()
    {
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }
}