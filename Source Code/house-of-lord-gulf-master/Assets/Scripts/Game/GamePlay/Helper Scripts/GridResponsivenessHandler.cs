using UnityEngine;

public class GridResponsivenessHandler : MonoBehaviour
{
    [SerializeField] RectTransform footer;
    [SerializeField] Vector3 offset;

    void Start()
    {
        transform.position = new Vector3(transform.position.x, Camera.main.ScreenToWorldPoint(footer.transform.position).y) + offset;       
    }

#if UNITY_EDITOR
    private void Update()
    {
        transform.position = new Vector3(transform.position.x, Camera.main.ScreenToWorldPoint(footer.transform.position).y) + offset;
    }
#endif
}
