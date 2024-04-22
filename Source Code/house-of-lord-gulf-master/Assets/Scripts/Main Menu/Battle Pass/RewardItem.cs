using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class RewardItem : MonoBehaviour
{
    [SerializeField] Image lockImage;
    [SerializeField] Button collectRewardBtn;
    [SerializeField] Image rewardImage;
    [SerializeField] Image rewardLogoImage;
    [SerializeField] TMP_Text levelTxt;
    [SerializeField] GameObject[] lockVfxs;
    [SerializeField] GameObject[] rewardVfxs;
    [SerializeField] Color collectedRewardColor = Color.gray;

    internal List<string> rewardIDs = new();
    internal int currentLevel;

    public void InitializeReward(Reward reward, bool canCollect, bool collected, bool showVFX)
    {
        string imageURL = reward.rewardImageURL;

        currentLevel = reward.level;
        rewardIDs = reward.rewardID;

        lockImage.sprite = canCollect ? SpriteReferences.references.battlepass_unlockedIcon : SpriteReferences.references.battlepass_lockedIcon;

        foreach (var item in rewardVfxs)
            item.SetActive(showVFX);

        foreach (var item in lockVfxs)
            item.SetActive(showVFX);

        if (collected)
        {
            rewardImage.color = collectedRewardColor;
            rewardLogoImage.color = collectedRewardColor;
        }

        if (levelTxt!=null)
            levelTxt.text = currentLevel.ToString();

        if (!string.IsNullOrEmpty(imageURL) && rewardImage != null)
            _ = ImageManager.DownloadAndSetRemoteTextureToImage(imageURL, rewardImage);

        collectRewardBtn.onClick.RemoveAllListeners();

        if (canCollect)
        {
            

            collectRewardBtn.interactable = !collected;

            if (!collected)
                collectRewardBtn.onClick.AddListener(OnClick_CollectBtn);
            else
            {
                collectRewardBtn.interactable = false;
                collectRewardBtn.onClick.RemoveAllListeners();
            }
            return;
        }

        collectRewardBtn.interactable = false;
    }

    private void OnClick_CollectBtn()
    {
        collectRewardBtn.interactable = false;
        InventoryManager.manager.GrantItems(rewardIDs.ToArray(), OnRewardCollectedResult);
    }

    private void OnRewardCollectedResult(bool success)
    {
        if (success)
        {
            BattlePassManager.manager.UpdateCollectedRewardsList(currentLevel);
            Debug.Log("Collected Reward Successfully");
        }
        else
        {
            collectRewardBtn.interactable = true;
            Debug.LogError("Error while collecting reward item.");
        }
    }
}
