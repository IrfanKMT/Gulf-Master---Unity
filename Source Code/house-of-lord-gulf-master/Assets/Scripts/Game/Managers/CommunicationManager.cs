using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CommunicationManager : MonoBehaviour
{
    public static CommunicationManager manager;

    [Header("Voice Control UI Images")]
    [SerializeField] Image micImage;
    [SerializeField] Image speakerImage;

    [Header("Voice Control Panel")]
    [SerializeField] Button muteToggleButton;
    [SerializeField] Button speakerToggleButton;
    [SerializeField] GameObject voiceControlPanel;

    [Header("Text Chat Panel")]
    [SerializeField] Button chatOpenButton;
    [SerializeField] TMP_Text messageText;
    [SerializeField] TMP_InputField messageTF;
    [SerializeField] GameObject chatPanel;

    bool isMuted = false;
    bool isSpeakerOn = true;
    bool isChatOpen = false;
    internal bool isVoiceModeOn = false;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

#if !UNITY_SERVER

    private void Start()
    {
        messageTF.onTouchScreenKeyboardStatusChanged.AddListener(SendTextMessage);
        chatOpenButton.onClick.RemoveAllListeners();
        chatOpenButton.onClick.AddListener(ToggleChatOpen);
        InitializeVoiceControlUI();
    }

#endif

    private void Update()
    {
#if UNITY_EDITOR

        //if (Input.GetKeyDown(KeyCode.Space) && PhotonNetwork.InRoom && PhotonNetwork.IsConnected)
        //    SendTextMessage(TouchScreenKeyboard.Status.Done);
#endif
    }


    #endregion

    #region Voice Mode

    private void InitializeVoiceControlUI()
    {
        isVoiceModeOn = LobbyUIManager.manager.isVoiceOn;
        voiceControlPanel.SetActive(isVoiceModeOn);
        muteToggleButton.onClick.RemoveAllListeners();
        speakerToggleButton.onClick.RemoveAllListeners();
        muteToggleButton.onClick.AddListener(ToggleMute);
        speakerToggleButton.onClick.AddListener(ToggleSpeaker);
        
        isMuted = false;
        isSpeakerOn = true;
        
        micImage.color = new(1, 1, 1, 1);
        speakerImage.color = new(1, 1, 1, 1);

#if ENABLE_VIVOX
        VivoxManager.manager.MuteSelf(isMuted);
        VivoxManager.manager.SetSpeaker(!isSpeakerOn);
#endif

    }

    private void ToggleMute()
    {
        isMuted = !isMuted;

#if ENABLE_VIVOX
        VivoxManager.manager.MuteSelf(isMuted);
#endif
        micImage.color = isMuted ? new(1, 1, 1, 0.5f) : new(1, 1, 1, 1);
    }

    private void ToggleSpeaker()
    {
        isSpeakerOn = !isSpeakerOn;
#if ENABLE_VIVOX
        VivoxManager.manager.SetSpeaker(!isSpeakerOn);
#endif
        speakerImage.color = isSpeakerOn ? new(1, 1, 1, 1) : new(1, 1, 1, 0.5f);
    }

    #endregion

    #region Text Chat

    private void ToggleChatOpen()
    {
        chatOpenButton.interactable = false;
        if (isChatOpen)
            UIAnimationManager.manager.PopDownPanel(chatPanel, () => chatOpenButton.interactable = true);
        else
            UIAnimationManager.manager.PopUpPanel(chatPanel, () => chatOpenButton.interactable = true);

        isChatOpen = !isChatOpen;
    }

    private void SendTextMessage(TouchScreenKeyboard.Status status)
    {
        if(status == TouchScreenKeyboard.Status.Done)
        {
            string msg = messageTF.text;
            string sender = PlayerData.Username[0..^5];
            //photonView.RPC("RPC_SendTextMessage", RpcTarget.Others, sender, msg);
            messageText.text += "\n" + sender + ": " + msg;
            messageTF.text = "";
        }
    }

    //[PunRPC]
    private void RPC_SendTextMessage(string sender, string msg)
    {
        messageText.text += "\n" + sender + ": " + msg;
    }

    #endregion
}
