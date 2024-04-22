using UnityEngine;
using AssetKits.ParticleImage;
using System.Collections;

public class Perk_StarMaker : Perk
{
    [SerializeField] string starMakerPrkID; 
    [SerializeField] ParticleImage candyMovementVFX;
    int numOfCoroutinesRunning = 0;

    public override void Boost()
    {
        PerksManager.manager.isPerkWorking = true;
         
        for (int i = 0; i < 5; i++)
        {
            int randX = GenerateRandom(0, Grid.width);
            int randY = GenerateRandom(0, Grid.height);
            GamePiece randPiece = Grid.grid.pieces[randX, randY];
            StartCoroutine(AnimateBlueGem(randPiece));
        }
        StartCoroutine(DestroyPerk());
    }

    IEnumerator AnimateBlueGem(GamePiece randPiece)
    {
        if (randPiece == null)
            yield break;
        numOfCoroutinesRunning++;
        ParticleImage particle = Instantiate(candyMovementVFX.gameObject, PerksManager.manager.player1Perk1ID.Equals(starMakerPrkID)? GameplayUIManager.manager.perk1Button.transform.position : GameplayUIManager.manager.perk2Button.transform.position, Quaternion.identity).GetComponent<ParticleImage>();
        particle.transform.SetParent(GameplayUIManager.manager.gameplayUI);
        particle.transform.localScale = new(1, 1, 1);

        GameObject targetGO = new("Target");
        var target = targetGO.AddComponent<RectTransform>();
        targetGO.transform.position = Grid.grid.GetWorldPosition(randPiece.X, randPiece.Y);
        targetGO.transform.SetParent(GameplayUIManager.manager.gameplayUI);

        particle.attractorTarget = target;

        yield return new WaitWhile(() => !particle.isStopped);

        Destroy(particle.gameObject);
        Destroy(target.gameObject);

        ColorType color;

        if(GameNetworkManager.manager.mode == Mirror.NetworkManagerMode.ServerOnly)
            //Because server's grid is same as player 1 grid
            color = isPlayer1Perk ? ColorType.Blue : ColorType.Red;
        else
            color = GamePlayManager.manager.Client_IsMyTurn() ? ColorType.Blue : ColorType.Red;

        randPiece.ColorComponent.SetColorWithoutChangingColorForDifferentPlayers(color);

        numOfCoroutinesRunning--;

    }

    IEnumerator DestroyPerk()
    {
        yield return new WaitWhile(() => numOfCoroutinesRunning > 0);
        Grid.grid.StartFillingGrid(true);
        PerksManager.manager.On_PerkUsed(isPlayer1Perk, isPerk1);
        Destroy(gameObject);
    }
}
