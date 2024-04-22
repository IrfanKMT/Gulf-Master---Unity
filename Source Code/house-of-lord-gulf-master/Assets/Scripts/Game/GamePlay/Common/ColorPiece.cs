using System.Collections.Generic;
using UnityEngine;
//using Photon.Pun;

public class ColorPiece : MonoBehaviour
{
    //HOLDS SPRITES FOR DIFFRENT TYPES OF CANDY'S
    public SpriteContainerForCandy SpriteContainer;
    [Space]
    public SpriteRenderer candySprite;
    //[Space]
    //private Dictionary<ColorType, Sprite> colorSpritesDict;
    [Space]
    public GamePiece piece;
    private ColorType color;
    private MatchType mt;

    public ColorType Color
    {
        get { return color; }
        set { SetColor(value); }
    }

    //public int NumColors
    //{
    //    get { return colorSprites.Count; }
    //}

    //private void Awake()
    //{
    //    //piece = GetComponent<GamePiece>();
    //    colorSpritesDict = new();

    //    foreach (var item in colorSprites)
    //        if (!colorSpritesDict.ContainsKey(item.color))
    //            colorSpritesDict.Add(item.color, item.sprite);
    //}

    public void SetColor(ColorType newColor)
    {
        switch (GamePlayManager.manager.matchtype)
        {
            case MatchType.TwoPlayer:

                if ((GamePlayManager.manager.LocalPlayer != null && GamePlayManager.manager.LocalPlayer.isPlayer1) && piece.Type == PieceType.NORMAL)
                {
                    if (newColor == ColorType.Blue)
                        newColor = ColorType.Red;
                    else if (newColor == ColorType.Red)
                        newColor = ColorType.Blue;
                }
                else if (GamePlayManager.manager.LocalPlayer == null && piece.Type == PieceType.NORMAL)
                {
                    if (newColor == ColorType.Blue)
                        newColor = ColorType.Red;
                    else if (newColor == ColorType.Red)
                        newColor = ColorType.Blue;
                }
                break;

            case MatchType.FourPlayer:
#if !UNITY_SERVER
                if (GameMode.gameMode.GetMyTeam(PlayerData.PlayfabID) == TeamType.TeamA && piece.Type == PieceType.NORMAL)
                {
                    Debug.Log("Team 1" + GameMode.gameMode.GetMyTeam(PlayerData.PlayfabID) + " Id" + PlayerData.PlayfabID);
                    if (newColor == ColorType.Blue)
                        newColor = ColorType.Red;
                    else if (newColor == ColorType.Red)
                        newColor = ColorType.Blue;
                }
                else if (GameMode.gameMode.GetMyTeam(PlayerData.PlayfabID) == TeamType.TeamB && piece.Type == PieceType.NORMAL)
                {
                    Debug.Log("Team 2" + GameMode.gameMode.GetMyTeam(PlayerData.PlayfabID) + " Id" + PlayerData.PlayfabID);

                    if (newColor == ColorType.Blue)
                        newColor = ColorType.Red;
                    else if (newColor == ColorType.Red)
                        newColor = ColorType.Blue;
                }
#endif

#if UNITY_SERVER
                if (GameMode.gameMode.GetMyTeam(GamePlayManager.manager.currentTurnPlayfabID) == TeamType.TeamA && piece.Type == PieceType.NORMAL)
                {
                    //Debug.Log("TEAM A " + GamePlayManager.manager.currentTurnPlayfabID);

                    if (newColor == ColorType.Blue)
                        newColor = ColorType.Red;
                    else if (newColor == ColorType.Red)
                        newColor = ColorType.Blue;
                }
                else if (GameMode.gameMode.GetMyTeam(GamePlayManager.manager.currentTurnPlayfabID) == TeamType.TeamB && piece.Type == PieceType.NORMAL)
                {
                    //Debug.Log("TEAM B " + GamePlayManager.manager.currentTurnPlayfabID);

                    if (newColor == ColorType.Blue)
                        newColor = ColorType.Red;
                    else if (newColor == ColorType.Red)
                        newColor = ColorType.Blue;
                }

#endif
                break;
        }

        SetColorWithoutChangingColorForDifferentPlayers(newColor);
    }

    public void SetColorWithoutChangingColorForDifferentPlayers(ColorType newColor)
    {
        color = newColor;
        candySprite.sprite = GetColorSprite(newColor).sprite;
    }

    ColorSprite GetColorSprite(ColorType type)
    {
        return SpriteContainer.colorSprites.Find(asd => asd.color == type);
    }

    public void SetSpriteColor(Color color)
    {
        candySprite.color = color;
    }

    public void SetSpriteMaterial(Material mat)
    {
        candySprite.material = mat;
    }
}

public enum ColorType : int
{
    Red,
    Blue,
    Green,
    Yellow,
    Purple,
    Count
}


[System.Serializable]
public struct ColorSprite
{
    public ColorType color;
    public Sprite sprite;
}
