using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UITextureReferences : MonoBehaviour
{
    public static UITextureReferences reference;

    public Texture2D error_icon;

    private void Awake()
    {
        reference = this;
    }
}
