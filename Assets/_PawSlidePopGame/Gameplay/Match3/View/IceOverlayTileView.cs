using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class IceOverlayTileView : OverlayTileView
    {
        [Header("Ice FX")]
        [SerializeField] private Color iceDamageColor = new Color(0.72f, 0.9f, 1f, 1f);
        [SerializeField] private float iceDamagePunchMultiplier = 1.25f;
        [SerializeField] private Color iceClearColor = new Color(0.9f, 0.98f, 1f, 1f);
        [SerializeField] private float iceClearPunchMultiplier = 0.9f;

        public override IEnumerator PlayDamageAsync(int currentHp, int previousHp)
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * iceDamagePunchMultiplier,
                iceDamageColor,
                TileShadowState.Active,
                true);
        }

        public override IEnumerator PlayClearAsync(bool isExplosion = false)
        {
            // Spawn icy blue debris particles
            SpawnDebrisParticles(6, ClearDuration * 1.5f, iceDamageColor);

            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, ClearDuration * 0.85f),
                DamagePunchScale * iceClearPunchMultiplier,
                iceClearColor,
                TileShadowState.Active,
                false);

            yield return base.PlayClearAsync(isExplosion);
        }
    }
}
