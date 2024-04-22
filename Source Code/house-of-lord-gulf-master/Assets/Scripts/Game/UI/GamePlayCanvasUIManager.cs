using Unity.Services.RemoteConfig;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayCanvasUIManager : MonoBehaviour
{
    [Header("Options Panel")]
    [SerializeField] GameObject optionsPanel;

    bool isBGMusicMuted = false;

    private void Start()
    {
        GamePlayManager.manager.OnGameStarted += InitializeUI;
    }

    #region Initialize

    private void InitializeUI()
    {
        optionsPanel.SetActive(false);
    }

    #endregion

    #region Option Menu Button Clicks

    public void OnClick_OpenOptionsButton()
    {
        if (optionsPanel.activeInHierarchy)
        {
            WaitAndClose(GameplayUIManager.manager.emojiChatCanvas);
            UIAnimationManager.manager.PopDownPanel(optionsPanel);
        }
        else
        {
            GameplayUIManager.manager.emojiChatCanvas.SetActive(true);
            UIAnimationManager.manager.PopUpPanel(optionsPanel);
        }
    }

    private async void WaitAndClose(GameObject Go)
    {
        await Utitlits._Waiter(500);
        Go.SetActive(false);
    }

    public void OnClick_MuteBackgroundMusicButton()
    {
        SoundManager.manager.MuteBackgroundMusic(!isBGMusicMuted);
        isBGMusicMuted = !isBGMusicMuted;
    }

    public void OnClick_ChangeBGMusicVolume_Mid()
    {
        SoundManager.manager.ChangeBackgroundMusicVolume(SoundManager.manager.defaultVolume);
    }


    public void OnClick_ChangeBGMusicVolume_Low()
    {
        SoundManager.manager.ChangeBackgroundMusicVolume(SoundManager.manager.defaultVolume / 2f);
    }

    public void OnClick_LeaveGameButton()
    {
        Debug.Log("Leave GaMe");
        GamePlayManager.manager.LocalPlayer.LeaveGame();
    }

    #endregion
}
