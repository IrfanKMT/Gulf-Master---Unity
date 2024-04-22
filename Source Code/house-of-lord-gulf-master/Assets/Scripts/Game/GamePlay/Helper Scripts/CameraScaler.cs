using UnityEngine;

public class CameraScaler : MonoBehaviour
{
    public float offSet = 2;

    void Start()
    {
        Camera.main.orthographicSize = ((Grid.width / 2f) + offSet) / ((float)Screen.width / Screen.height);
    }

#if UNITY_EDITOR
    private void Update()
    {
        Camera.main.orthographicSize = ((Grid.width / 2f) + offSet) / ((float)Screen.width / Screen.height);

    }
#endif
}
