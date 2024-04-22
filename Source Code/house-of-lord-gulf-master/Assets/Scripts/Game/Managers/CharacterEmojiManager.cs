using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Collections;

public class CharacterEmojiManager : NetworkBehaviour
{
    [SerializeField] Button characterEmojiButton;
    [SerializeField] GameObject characterEmojiPanel;

    [System.Serializable]
    struct CharacterEmojiAnim
    {
        public GameObject characterEmojiPrefab;
        public AudioClip[] audioClips;
    }
    [SerializeField] CharacterEmojiAnim[] characterEmojiAnimations;

    [Header("Holders")]
    [SerializeField] Transform characterEmojiHolder;

    bool isAnimRunning = false;

    #region Button Clicks

    public void OnClick_OpenEmojiPanel()
    {
        if (!characterEmojiPanel.activeInHierarchy)
        {
            GameplayUIManager.manager.emojiChatCanvas.SetActive(true);
            UIAnimationManager.manager.PopUpPanel(characterEmojiPanel);
        }
        else
        {
            WaitAndClose(GameplayUIManager.manager.emojiChatCanvas);
            UIAnimationManager.manager.PopDownPanel(characterEmojiPanel);
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

        StartCoroutine(PlayEmojiAnimation(index));
        Cmd_SendCharacterEmoji(index);
    }

    #endregion

    #region Playing Animation

    IEnumerator PlayEmojiAnimation(int index)
    {
        if (isAnimRunning) yield break;
        isAnimRunning = true;

        UIAnimationManager.manager.PopDownPanel(characterEmojiPanel);
        CharacterEmojiAnim emojiAnim = characterEmojiAnimations[index];

        AudioClip randAudioClip = null;
        if (emojiAnim.audioClips.Length > 0)
            randAudioClip = emojiAnim.audioClips[Random.Range(0, emojiAnim.audioClips.Length)];

        SoundManager.manager.PlaySoundSeperately(randAudioClip);

        GameObject emojiGO = Instantiate(emojiAnim.characterEmojiPrefab, characterEmojiHolder);
        yield return new WaitForSeconds(randAudioClip.length);
        Destroy(emojiGO);

        isAnimRunning = false;
    }

    #endregion

    #region RPCs
    [Command]
    private void Cmd_SendCharacterEmoji(int index)
    {
        RPC_SendCharacterEmoji(index);
    }

    [ClientRpc]
    private void RPC_SendCharacterEmoji(int index) => StartCoroutine(PlayEmojiAnimation(index));

    #endregion
}
