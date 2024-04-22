
using System;
using System.Collections;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using VivoxUnity;
//using Photon.Pun;
using System.ComponentModel;

public class VivoxManager : MonoBehaviour
{
#if ENABLE_VIVOX
    public static VivoxManager manager;
    public ILoginSession LoginSession;
    public IChannelSession channelSession;

    internal bool isLoggedIn = false;
    public event Action OnVivoxClientLoggedIn;

    #region Unity Functions

    private void Awake()
    {
        manager = this;
    }

    async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();

        VivoxService.Instance.Initialize(new() { DefaultCodecsMask = MediaCodecType.Opus8 });
        VivoxService.Instance.Client.AudioOutputDevices.BeginRefresh(new AsyncCallback((IAsyncResult result) => VivoxService.Instance.Client.AudioOutputDevices.VolumeAdjustment = 0));

        AuthenticationManager.manager.OnPlayerLoggedIn += () => Login(PlayerData.PlayfabID);
        AuthenticationManager.manager.OnPlayerLoggedOut += LogOut;
        Debug.Log("Ran this");
        //GamePlayManager.manager.OnGameStarted += EventCallback_JoinChannel;
        //NetworkManager.manager.OnPlayerLeaveRoom += LeaveChannel;
    }

    #endregion

    #region Initialization

    public void Login(string displayName)
    {
        Debug.Log("Login");
        var account = new Account(displayName);

        LoginSession = VivoxService.Instance.Client.GetLoginSession(account);
        LoginSession.PropertyChanged += LoginSession_PropertyChanged;

        LoginSession.BeginLogin(LoginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, ar =>
        {
            try
            {
                LoginSession.EndLogin(ar);
            }
            catch (Exception e)
            {
                Debug.LogError("Error While Logging Into Vivox: " + e.Message.ToString() + "\nStack Trace : " + e.StackTrace.ToString());
                return;
            }
        });
    }

    //Will be called when login session state changes
    private void LoginSession_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        var loginSession = (ILoginSession)sender;
        if (e.PropertyName == "State")
        {
            if (loginSession.State == LoginState.LoggedIn)
            {
                Debug.Log("Vivox Client Logged in");
                isLoggedIn = true;
                OnVivoxClientLoggedIn?.Invoke();
            }
        }
    }

    #endregion

    #region Joining Channel

    public void EventCallback_JoinChannel()
    {
        Debug.Log("EventCallback_JoinChannel");
        StartCoroutine(JoinPhotonRoomChannelAfterLoggingIn());
    }

    private void JoinChannel(string channelName, ChannelType channelType, bool connectAudio, bool connectText, bool transmissionSwitch = true, Channel3DProperties properties = null) //use channelType = ChannelType.NonPositional for non-positional channels which means no 3d audio
    {
        if (LoginSession.State == LoginState.LoggedIn)
        {
            Channel channel = new(channelName, channelType, properties);
            channelSession = LoginSession.GetChannelSession(channel);
            channelSession.PropertyChanged += SourceOnChannelPropertyChanged;
            print(channelSession.Participants.Count);

            channelSession.BeginConnect(connectAudio, connectText, transmissionSwitch, channelSession.GetConnectToken(), ar =>
            {
                try
                {
                    channelSession.EndConnect(ar);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Error Joining Vivox Channel: Could not connect to channel: {e.Message}");
                    return;
                }
            });
        }
        else
        {
            Debug.LogError("Error Joining Vivox Channel: Can't join a channel when not logged in.");
        }
    }

    void SourceOnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
    {
        var channelSession = (IChannelSession)sender;

        // This example only checks for AudioState changes.
        if (propertyChangedEventArgs.PropertyName == "AudioState")
        {
            switch (channelSession.AudioState)
            {
                case ConnectionState.Connecting:
                    Debug.Log("Audio connecting in " + channelSession.Key.Name);
                    break;

                case ConnectionState.Connected:
                    Debug.Log("Audio connected in " + channelSession.Key.Name);
                    break;

                case ConnectionState.Disconnecting:
                    Debug.Log("Audio disconnecting in " + channelSession.Key.Name);
                    break;

                case ConnectionState.Disconnected:
                    Debug.Log("Audio disconnected in " + channelSession.Key.Name);
                    break;
            }
        }
    }


    #endregion

    #region Communication Controls

    public void MuteSelf(bool mute)
    {
        if (VivoxService.Instance != null)
        {
            VivoxService.Instance.Client.AudioInputDevices.Muted = mute;
        }
    }

    public void SetSpeaker(bool setMute)
    {
        if (channelSession != null)
            foreach (IParticipant participant in channelSession.Participants)
                if (participant.InAudio)
                    participant.LocalMute = setMute;
    }

    public void LeaveChannel()
    {
        if (CommunicationManager.manager.isVoiceModeOn && channelSession != null)
            if (channelSession.ChannelState == ConnectionState.Connected || channelSession.ChannelState == ConnectionState.Connecting)
                channelSession.Disconnect();
    }

    #endregion

    #region Logout

    private void LogOut()
    {
        LoginSession.Logout();
    }

    #endregion

    #region Helper Functions

    IEnumerator JoinPhotonRoomChannelAfterLoggingIn()
    {
        yield return new WaitWhile(() => LoginSession == null);
        yield return new WaitWhile(() => LoginSession.State != LoginState.LoggedIn);

        if (CommunicationManager.manager.isVoiceModeOn)
        {
            Debug.Log("Joining Voice Channel : " + MatchMakingManager.manager.curruntmatchid);
            JoinChannel(MatchMakingManager.manager.curruntmatchid, ChannelType.NonPositional, true, false);
        }
        else
        {
            Debug.Log("Failed");
        }
    }

    #endregion


#endif
}