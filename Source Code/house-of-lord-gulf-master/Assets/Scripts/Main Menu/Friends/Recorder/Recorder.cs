using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Networking;
using GG.Infrastructure.Utils.Swipe;

public class Recorder : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] GameObject recordGO;
    [SerializeField] int maxRecordTime = 300;

    public static Recorder recorder;
    AudioClip recording;
    AudioSource audioSource;
    private float startRecordingTime;
    SwipeListener swipeListener;
    bool pointerDown = false;
    bool deleteRecording = false;
    Vector2 mousePosStart;
    Vector2 mousePosEnd;

    private void Awake()
    {
        recorder = this;
    }

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        swipeListener = GetComponent<SwipeListener>();
    }

    private void OnEnable()
    {
        if (swipeListener == null)
        {
            swipeListener = GetComponent<SwipeListener>();
            swipeListener.OnSwipe.AddListener(OnSwipe);
        }
        else
            swipeListener.OnSwipe.AddListener(OnSwipe);
    }

    private void OnDisable()
    {
        swipeListener.OnSwipe.RemoveListener(OnSwipe);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!IAPManager.manager.IsSubscriptionActive) return;
        mousePosEnd = Input.mousePosition;
        pointerDown = false;

        ChatUIManager.manager.TypingIndicator_OnMessageTFValueChanged("");
        if (Vector2.Distance(mousePosEnd, mousePosStart) < 200) deleteRecording = false;

        if (!deleteRecording)
        {
            RecordAndSaveAudio();
        }
        else
        {
            deleteRecording = false;
        }
        recordGO.SetActive(false);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!IAPManager.manager.IsSubscriptionActive) return;

        recordGO.SetActive(true);
        ChatUIManager.manager.TypingIndicator_OnMessageTFValueChanged("VC");
        pointerDown = true;
        mousePosStart = Input.mousePosition;
        int minFreq;
        int maxFreq;
        int freq = 44100;
        Microphone.GetDeviceCaps("", out minFreq, out maxFreq);
        if (maxFreq < 44100)
            freq = maxFreq;

        recording = Microphone.Start("", false, maxRecordTime, 44100);
        startRecordingTime = Time.time;
    }

    void OnSwipe(string swipe)
    {
        if (!pointerDown) return;

        if (swipe.Equals("Left")) 
        { 
            deleteRecording = true;
        }
    }

    IEnumerator GetAudioClip(string filepath)
    {
        using (UnityWebRequest www = UnityWebRequest.Get("file://" + filepath))
        {
            yield return www.SendWebRequest();
            byte[] data = www.downloadHandler.data;
            SendVoiceData(data, Path.GetFileName(filepath));
        }
    }

    async void SendVoiceData(byte[] data, string fileName)
    {
        await ChatManager.manager.SendVoiceMessage(data, fileName);
    }

    void RecordAndSaveAudio()
    {
        Microphone.End("");

        AudioClip recordingNew = AudioClip.Create(recording.name, (int)((Time.time - startRecordingTime) * recording.frequency), recording.channels, recording.frequency, false);
        float[] data = new float[(int)((Time.time - startRecordingTime) * recording.frequency)];
        recording.GetData(data, 0);
        recordingNew.SetData(data, 0);
        recording = recordingNew;

        audioSource.clip = recording;
        string path = Path.Join(Application.temporaryCachePath, "VoiceMsg" + UnityEngine.Random.Range(0, 9999999).ToString() + UnityEngine.Random.Range(0, 9999999).ToString() + ".wav");
        SavWav.Save(path, recordingNew);

        StartCoroutine(GetAudioClip(path));
    }

}
