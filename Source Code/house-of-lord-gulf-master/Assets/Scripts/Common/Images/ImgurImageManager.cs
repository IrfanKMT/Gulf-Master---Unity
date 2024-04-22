using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageManager : MonoBehaviour
{
    private static readonly string clientId = "de9baae73cf89b7";
    private static readonly string uploadUrl = "https://api.imgur.com/3/image";

    public static async Task<string> UploadImage(string imagePath)
    {
        byte[] imageData = File.ReadAllBytes(imagePath);

        using (UnityWebRequest www = new UnityWebRequest(uploadUrl, UnityWebRequest.kHttpVerbPOST))
        {
            www.uploadHandler = new UploadHandlerRaw(imageData);
            www.downloadHandler = new DownloadHandlerBuffer();

            www.SetRequestHeader("Authorization", "Client-ID " + clientId);
            www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            var asyncOperation = www.SendWebRequest();

            while (!asyncOperation.isDone)
            {
                await Task.Delay(100);
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Image Upload Error :\n"+www.error);
                return null;
            }
            else
            {
                var response = JsonUtility.FromJson<ImgurResponse>(www.downloadHandler.text);
                var link = response.data.link;
                return link;
            }
        }
    }

    public static async Task<Texture2D> DownloadImage(string imageUrl)
    {

        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogError("Image URL is null.");
            return null;
        }
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);

        var asyncOperation = www.SendWebRequest();

        while (!asyncOperation.isDone)
        {
            await Task.Delay(100);
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image Download Error :\n" + www.error);
            return UITextureReferences.reference.error_icon;
        }
        else
        {
            Texture2D texture = ((DownloadHandlerTexture)www.downloadHandler).texture;
            return texture;
        }
    }

    public static async Task<Sprite> DownloadImage_Sprite(string imageUrl)
    {

        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogError("Image URL is null.");
            return null;
        }
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);

        var asyncOperation = www.SendWebRequest();

        while (!asyncOperation.isDone)
        {
            await Task.Delay(100);
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image Download Error :\n" + www.error);
            var tex = UITextureReferences.reference.error_icon;
            Rect rec = new Rect(0, 0, tex.width, tex.height);
            return Sprite.Create(tex, rec, new Vector2(0, 0), .01f);
        }
        else
        {
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Rect rec = new Rect(0, 0, tex.width, tex.height);
            return Sprite.Create(tex, rec, new Vector2(0, 0), .01f);
        }
    }

    public static async Task DownloadAndSetRemoteTextureToImage(string imageUrl, Image image)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogError("Image URL is null.");
            return;
        }

        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUrl);

        var asyncOperation = www.SendWebRequest();

        while (!asyncOperation.isDone)
        {
            await Task.Delay(100);
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image Download Error :\n" + www.error);
            Texture2D tex = UITextureReferences.reference.error_icon;
            Rect rec = new Rect(0, 0, tex.width, tex.height);
            image.sprite = Sprite.Create(tex, rec, new Vector2(0, 0), .01f);
        }
        else
        {
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Rect rec = new Rect(0, 0, tex.width, tex.height);
            if(image!=null)
                image.sprite = Sprite.Create(tex, rec, new Vector2(0, 0), .01f);
        }
    }

    public static async void GetAndSetLocalTextureToImage(string imageUrl, Image image)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogError("Image URL is null.");
            return;
        }

        UnityWebRequest www = UnityWebRequestTexture.GetTexture(Path.Join("file://",imageUrl));

        var asyncOperation = www.SendWebRequest();

        while (!asyncOperation.isDone)
        {
            await Task.Delay(100);
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image Download Error :\n" + www.error);
            Texture2D tex = UITextureReferences.reference.error_icon;
            Rect rec = new Rect(0, 0, tex.width, tex.height);
            image.sprite = Sprite.Create(tex, rec, new Vector2(0, 0), .01f);
        }
        else
        {
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Rect rec = new Rect(0, 0, tex.width, tex.height);
            image.sprite = Sprite.Create(tex, rec, new Vector2(0, 0), .01f);
        }
    }

    public static async Task<Sprite> GetLocalTextureAsSprite(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogError("Image URL is null.");
            Texture2D tex = UITextureReferences.reference.error_icon;
            Rect rec = new Rect(0, 0, tex.width, tex.height);
            return Sprite.Create(tex, rec, new Vector2(0, 0), .01f);
        }

        UnityWebRequest www = UnityWebRequestTexture.GetTexture(Path.Join("file://", imageUrl));

        var asyncOperation = www.SendWebRequest();

        while (!asyncOperation.isDone)
        {
            await Task.Delay(100);
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Image Download Error :\n" + www.error);
            Texture2D tex = UITextureReferences.reference.error_icon;
            Rect rec = new Rect(0, 0, tex.width, tex.height);
            return Sprite.Create(tex, rec, new Vector2(0, 0), .01f);
        }
        else
        {
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
            Rect rec = new Rect(0, 0, tex.width, tex.height);
            return Sprite.Create(tex, rec, new Vector2(0, 0), .01f);
        } 
    }
}

[System.Serializable]
public class ImgurResponse
{
    public Data data;
}

[System.Serializable]
public class Data
{
    public string link;
}
