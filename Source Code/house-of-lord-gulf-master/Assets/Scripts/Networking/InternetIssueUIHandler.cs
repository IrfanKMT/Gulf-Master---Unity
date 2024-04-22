using TMPro;
using UnityEngine;
//using Photon.Pun;

public class InternetIssueUIHandler : MonoBehaviour
{
    public static InternetIssueUIHandler handler;

    public GameObject heirarchy;

    public GameObject waitingForDataGO;
    public GameObject internetErrorGO;
    public TMP_Text disconnectCauseTxt;
    public GameObject highPingGO;
    public GameObject resyncingGO;

    private void Awake()
    {
        handler = this;
    }

    //public void Start()
    //{
    //    //NetworkManager.manager.OnPlayerDisconnected += (cause) =>
    //    //{
    //    //    if(internetErrorGO!=null)
    //    //        internetErrorGO.SetActive(true);

    //    //    if(disconnectCauseTxt != null)
    //    //        disconnectCauseTxt.text = cause.ToString();
    //    //};

    //    //NetworkManager.manager.OnPlayerJoinedLobby += () =>internetErrorGO.SetActive(false);

    //    //NetworkManager.manager.OnPlayerJoinRoom += () => internetErrorGO.SetActive(false);

    //}

#if UNITY_EDITOR
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Mouse2))
        {
            heirarchy.SetActive(!heirarchy.activeInHierarchy);
        }
    }
#endif

    private void OnApplicationPause(bool pause)
    {
        //if (!pause && PhotonNetwork.InRoom && PhotonNetwork.IsConnected)
        //{
        //    internetErrorGO.SetActive(true);
        //    disconnectCauseTxt.text = "Re-Syncing...";
        //}
    }
}
