using MyToolz.InventorySystem.Models;
using MyToolz.UI.Labels;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MyToolz.Networking.Scoreboards
{
    public class TrainingGroundsLabel : Label
    {
        [SerializeField] private Image weaponIcon;
        [SerializeField] private TMP_Text totalAccuracy;
        [SerializeField] private TMP_Text headShotAccuracy;
        [SerializeField] private TMP_Text totalShotsDisplay;
        [SerializeField] private TMP_Text totalHitsDisplay;

        public void Construct(ItemSO weaponSO, WeaponStats weaponStats)
        {
            weaponIcon.sprite = weaponSO.ItemIcon;

            float totalShots = Mathf.Max(weaponStats.TotalShots, 1);
            float totalHits = Mathf.Max(weaponStats.Hits, 1);

            float totalAccuracyValue = (weaponStats.Hits / totalShots) * 100f;
            float headShotAccuracyValue = (weaponStats.HeadShots / totalShots) * 100f;
            totalShotsDisplay.text = $"{weaponStats.TotalShots}";
            totalHitsDisplay.text = $"{weaponStats.Hits}";
            totalAccuracy.text = $"{Mathf.RoundToInt(totalAccuracyValue)}%";
            headShotAccuracy.text = $"{Mathf.RoundToInt(headShotAccuracyValue)}%";
        }

    }
}
