using System.Collections;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class OverlayTileView : Match3TileView
    {
        [Header("Overlay FX")]
        [SerializeField] private Color overlayDamageColor = new Color(0.82f, 0.92f, 1f, 1f);
        [SerializeField] private float overlayDamagePunchMultiplier = 1.15f;

        public override IEnumerator PlayDamageAsync(int currentHp, int previousHp)
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * overlayDamagePunchMultiplier,
                overlayDamageColor,
                TileShadowState.Active,
                true);
        }
    }
}
