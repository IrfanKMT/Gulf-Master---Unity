using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StreamChat.Core.StatefulModels;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;
using RTLTMPro;
using StreamChat.Core.Requests;
using System;

public class MessageItem : MonoBehaviour
{
    [Header("Common")]
    [SerializeField] Image blueTick;
    [SerializeField] TMP_Text timeText;
    [SerializeField] Button deleteButton;
    [SerializeField] RectTransform footerLayoutComponent;

    const string containerTextAppender = "a\na\n";
    [Header("Text Message")]
    [SerializeField] RTLTextMeshPro messageContainerText;
    [SerializeField] RTLTextMeshPro messageText;

    [Header("Image Message")]
    [SerializeField] RectTransform imageHolder;
    [SerializeField] Image imageAttachment;
    [SerializeField] Vector2 maxSize;

    [Header("Voice Message")]
    [SerializeField] Button playAudioButton;      
    [SerializeField] Slider audioTimeSlider;
    [SerializeField] AudioSource audioSource;
    bool isAudioPlaying = false;
    bool msgSetuppedLocally = false;

    public bool m_mymsg;

    private bool m_check_now;

    private string m_channel_name;
    private string m_id;

    #region Unity Functions

    private void Awake()
    {
        if(blueTick!=null)
            blueTick.enabled = false;
    }

    private void Update()
    {
        UpdateAudioSlider();
    }

    #endregion

    #region Message Setup

    public async void SetupMessage(IStreamMessage msg)
    {
        if (!msgSetuppedLocally)
        {
            if (!string.IsNullOrEmpty(msg.Text))
            {
                messageContainerText.text = containerTextAppender + msg.Text;
                messageContainerText.UpdateText();
                messageContainerText.ForceMeshUpdate();
                messageText.text = msg.Text;
            }
            else if (!string.IsNullOrEmpty(msg.Attachments[0].ImageUrl))
            {
                var spr = await ImageManager.DownloadImage_Sprite(msg.Attachments[0].ImageUrl);
                if (imageAttachment != null)
                {
                    imageAttachment.sprite = spr;
                    imageAttachment.SetNativeSize();

                    float spriteWidth = imageAttachment.sprite.rect.width;
                    float spriteHeight = imageAttachment.sprite.rect.height;

                    float aspectRatio = spriteWidth / spriteHeight;

                    float newWidth = Mathf.Min(maxSize.x, spriteWidth);
                    float newHeight = Mathf.Min(maxSize.y, spriteHeight);

                    if (newWidth / aspectRatio > maxSize.y)
                    {
                        newWidth = maxSize.y * aspectRatio;
                        newHeight = maxSize.y;
                    }
                    else if (newHeight * aspectRatio > maxSize.x)
                    {
                        newHeight = maxSize.x / aspectRatio;
                        newWidth = maxSize.x;
                    }

                    imageAttachment.rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
                    imageHolder.sizeDelta = new Vector2(imageHolder.sizeDelta.x, newHeight);
                }
                ChatUIManager.manager.UpdateLayoutGroup();
            }
            else if (!string.IsNullOrEmpty(msg.Attachments[0].AssetUrl) && !string.IsNullOrEmpty(msg.Attachments[0].OgScrapeUrl))
            {
                DownloadVoiceNote(msg.Attachments[0].AssetUrl, Path.GetFileName(msg.Attachments[0].OgScrapeUrl));
            }
            timeText.text = msg.CreatedAt.LocalDateTime.ToString("h:mm tt");
            LayoutRebuilder.ForceRebuildLayoutImmediate(footerLayoutComponent);
        }
        SetButtonListeners(msg.Id);
    }

    public async void SetupLocalMessage(StreamSendMessageRequest msg)
    {
        if (!string.IsNullOrEmpty(msg.Text))
        {
            messageContainerText.text = containerTextAppender + msg.Text;
            messageContainerText.UpdateText();
            messageContainerText.ForceMeshUpdate();
            messageText.text = msg.Text;
        }
        else if (!string.IsNullOrEmpty(msg.Attachments[0].ThumbUrl))
        {
            var spr = await ImageManager.GetLocalTextureAsSprite(msg.Attachments[0].ThumbUrl);
            if (imageAttachment != null)
            {
                imageAttachment.sprite = spr;
                imageAttachment.SetNativeSize();

                float spriteWidth = imageAttachment.sprite.rect.width;
                float spriteHeight = imageAttachment.sprite.rect.height;

                float aspectRatio = spriteWidth / spriteHeight;

                float newWidth = Mathf.Min(maxSize.x, spriteWidth);
                float newHeight = Mathf.Min(maxSize.y, spriteHeight);

                if (newWidth / aspectRatio > maxSize.y)
                {
                    newWidth = maxSize.y * aspectRatio;
                    newHeight = maxSize.y;
                }
                else if (newHeight * aspectRatio > maxSize.x)
                {
                    newHeight = maxSize.x / aspectRatio;
                    newWidth = maxSize.x;
                }

                imageAttachment.rectTransform.sizeDelta = new Vector2(newWidth, newHeight);
                imageHolder.sizeDelta = new Vector2(imageHolder.sizeDelta.x, newHeight);
            }
            ChatUIManager.manager.UpdateLayoutGroup();
        }
        else if (!string.IsNullOrEmpty(msg.Attachments[0].OgScrapeUrl))
        {
            DownloadVoiceNote(msg.Attachments[0].AssetUrl, Path.GetFileName(msg.Attachments[0].OgScrapeUrl));
        }

        timeText.text = DateTime.Now.ToString("h:mm tt");
        LayoutRebuilder.ForceRebuildLayoutImmediate(footerLayoutComponent);
        msgSetuppedLocally = true;
    }

    public void MarkMessageAsRead(bool read)
    {
        if (blueTick == null) return;
        blueTick.enabled = true;
        if (read)
            blueTick.color = Color.blue;
        else
            blueTick.color = Color.white;
    }
    //---
    void LateUpdate()
    {
        if (m_check_now)
        {
            Debug.Log("Check");
            m_check_now = false;
            Check();
        }
    }

    void Check()
    {
        ReadReceipts.IsChatOpen(m_channel_name, m_id, newaction =>
        {
            if (newaction)
            {
                Debug.Log("True Now");
                m_check_now = false;
                ChatManager.manager.CreateOrJoinChannelWithFriend(m_id, false);
            }
            else
            {
                Debug.Log("False");
                m_check_now = true;
            }
        });
    }


    public async void CheckForRead(string m_channel, string m_user_id, IStreamMessage m_mymsg)
    {
        m_channel_name = m_channel;
        m_id = m_user_id;
      //  m_msg = m_mymsg;

        if (string.IsNullOrEmpty(m_channel_name) || string.IsNullOrEmpty(m_id))
        {
            Debug.Log("Retunred");
            return;
        }
        m_check_now = true;
    }

    private void OnDisable()
    {
        m_check_now = false;
    }
    //----
    private void SetButtonListeners(string msgID)
    {
        if (deleteButton != null)
        {
            deleteButton.interactable = true;
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(()=>ChatManager.manager.DeleteMessage(msgID));
        }
    }

    #endregion

    #region Image OnClick

    public void OnClickImage()
    {
        ChatUIManager.manager.ShowImageFullScreen(imageAttachment.sprite);
    }

    #endregion

    #region Audio Setup

    private void UpdateAudioSlider()
    {
        if (!isAudioPlaying) return;

        if (audioSource.time < audioTimeSlider.maxValue)
            audioTimeSlider.value = audioSource.time;
        else if (audioSource.time >= audioTimeSlider.maxValue)
            ToggleVoiceMessage();
    }

    private void ToggleVoiceMessage()
    {
        if (!isAudioPlaying)
        {
            audioSource.time = audioTimeSlider.value;
            audioSource.Play();
        }
        else
        {
            audioSource.Stop();
            audioTimeSlider.value = 0;
        }
        isAudioPlaying = !isAudioPlaying;
    }

    private async void DownloadVoiceNote(string voiceUrl, string fileName)
    {
        string filePath = Path.Join(Application.temporaryCachePath, fileName);
        if (File.Exists(filePath))
        {
            StartCoroutine(SetAudioClipData(filePath));
        }
        else
        {
            using (UnityWebRequest www = UnityWebRequest.Get(voiceUrl))
            {
                var asyncOperation = www.SendWebRequest();
                while (!asyncOperation.isDone)
                    await Task.Delay(100);

                byte[] data = www.downloadHandler.data;
                File.WriteAllBytes(filePath, data);
            }
            StartCoroutine(SetAudioClipData(filePath));
        }
    }


    private IEnumerator SetAudioClipData(string filePath)
    {
        using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.WAV);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError)
            Debug.Log(www.error);
        else
        {
            audioSource.clip = DownloadHandlerAudioClip.GetContent(www);
            audioTimeSlider.minValue = 0;
            audioTimeSlider.maxValue = audioSource.clip.length;
            playAudioButton.interactable = true;
            playAudioButton.onClick.AddListener(ToggleVoiceMessage);
        }
    }

    #endregion

}
