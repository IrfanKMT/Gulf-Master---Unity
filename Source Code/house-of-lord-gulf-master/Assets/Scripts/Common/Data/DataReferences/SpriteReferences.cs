using UnityEngine;

public class SpriteReferences : MonoBehaviour
{
    public static SpriteReferences references;

    public Sprite errorSprite;
    public Sprite defaultAvatarSprite;
    public Sprite battlepass_lockedIcon;
    public Sprite battlepass_unlockedIcon;

    private void Awake()
    {
        references = this;
    }
}
