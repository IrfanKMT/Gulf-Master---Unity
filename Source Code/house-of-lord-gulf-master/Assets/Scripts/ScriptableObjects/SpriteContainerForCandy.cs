using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpriteContainerForCandy", menuName = "Game/SpriteContainerForCandy")]
public class SpriteContainerForCandy : ScriptableObject
{
    public List<ColorSprite> colorSprites;
}
