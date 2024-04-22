using UnityEngine;

public class PerksReferenceManager : MonoBehaviour
{
    public static PerksReferenceManager manager;

    public Transform starMakerPerkBlueGemStartingPoint;

    private void Awake()
    {
        manager = this;
    }
}
