using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Mirror;

public class EmojiManager : NetworkBehaviour
{
    [SerializeField] Button emojiButton;
    [SerializeField] GameObject emojiPanel;

    [System.Serializable] struct EmojiAnim
    {
        public float animTime;
        public Animator animator;
        public string animationStateName;
        public AudioClip[] maleAudioClips;
        public AudioClip[] femaleAudioClips;
    }

    [SerializeField]  EmojiAnim[] localEmojiAnimations;

    //The data of animations is same as local animation data, so only cache opponent's anim animators, and get aniamtion data from localEmojiAnimations
    [SerializeField]  Animator[] opponentEmojiAnimations;

    [Header("Bubbles")]
    [SerializeField] GameObject localEmojiBubble;
    [SerializeField] GameObject opponentEmojiBubble;

    bool isAnimRunning = false;

    #region Button Clicks

    public void OnClick_OpenEmojiPanel()
    {
        if (!Shop_EmojiPack.manager.isEmojiPackBought) return;

        if (!emojiPanel.activeInHierarchy)
        {
            GameplayUIManager.manager.emojiChatCanvas.SetActive(true);
            UIAnimationManager.manager.PopUpPanel(emojiPanel);
        }
        else
        {
            WaitAndClose(GameplayUIManager.manager.emojiChatCanvas);
            UIAnimationManager.manager.PopDownPanel(emojiPanel);
        }
           
            
    }

    private async void WaitAndClose(GameObject Go)
    {
        await Utitlits._Waiter(500);
        Go.SetActive(false);
    }

    public void OnClick_PlayEmoji(int index)
    {
        if (isAnimRunning) return;

        StartCoroutine(PlayEmojiAnimation(index, true, Gender.Male == (Gender)PlayerData.Gender));
        Cmd_SendEmoji(index, Gender.Male == (Gender)PlayerData.Gender);
    }

    #endregion

    #region Playing Animation

    IEnumerator PlayEmojiAnimation(int index, bool isLocal, bool isMale)
    {
        isAnimRunning = true;

        if (isLocal)
        {
            localEmojiBubble.SetActive(true);
            emojiButton.interactable = false;
        }
        else
            opponentEmojiBubble.SetActive(true);

        UIAnimationManager.manager.PopDownPanel(emojiPanel);


        AudioClip[] audioClips = isMale ? localEmojiAnimations[index].maleAudioClips : localEmojiAnimations[index].femaleAudioClips;

        for (int i = 0; i < localEmojiAnimations.Length; i++)
        {
            if (isLocal)
                localEmojiAnimations[i].animator.gameObject.SetActive(false);
            else
                opponentEmojiAnimations[i].gameObject.SetActive(false);
        }

        if (isLocal)
            localEmojiAnimations[index].animator.gameObject.SetActive(true);
        else
            opponentEmojiAnimations[index].gameObject.SetActive(true);

        SoundManager.manager.PlaySoundSeperately(audioClips[Random.Range(0, audioClips.Length)]);

        yield return new WaitForSeconds(localEmojiAnimations[index].animTime);

        if (isLocal)
        {
            localEmojiBubble.SetActive(false);
            emojiButton.interactable = true;
        }
        else
            opponentEmojiBubble.SetActive(false);

        isAnimRunning = false;
    }

    #endregion

    #region RPCs

    [Command]
    private void Cmd_SendEmoji(int index, bool isMale)
    {
        RPC_SendEmoji(index, isMale);
    }

    [ClientRpc]
    private void RPC_SendEmoji(int index, bool isMale) => StartCoroutine(PlayEmojiAnimation(index, false, isMale));

    #endregion
}
