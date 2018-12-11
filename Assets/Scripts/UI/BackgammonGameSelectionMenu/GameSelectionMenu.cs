using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameSelectionMenu : MonoBehaviour
{
    void Start()
    {
        var GameImageContainerTransform = transform.Find("GameImageScroller/Viewport/GameImageContainer");
        var GameImageTemplate = GameImageContainerTransform.Find("Template");

        foreach (var KV in TransientData.Instance.Games.OrderBy(kv => kv.Value.Sort))
        {
            var ImageTransform = Instantiate(GameImageTemplate);
            ImageTransform.SetParent(GameImageContainerTransform, false);
            ImageTransform.gameObject.SetActive(true);
            ImageTransform.Find("Image").GetComponent<RawImage>().texture = Resources.Load<Texture2D>($"Backgammon/Board/SelectionScreen/{KV.Value.BoardID}");
            ImageTransform.Find("Image/RewardText").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(KV.Value.Reward.ToString());
            ImageTransform.Find("Image/EntryText").GetComponent<TextMeshProUGUI>().text = PersianTextShaper.PersianTextShaper.ShapeText(KV.Value.EntranceFee.ToString());
            // ImageTransform.Find("Image/VideoAdButton").gameObject.SetActive(KV.Value.CanWatchVideoAd);
            ImageTransform.GetComponent<GameImage>().GameID = KV.Key;
        }
    }
}
