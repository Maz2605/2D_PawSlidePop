using System.Collections;
using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class SpecialTileView : Match3TileView
    {
        [Header("Special FX")]
        [SerializeField] private Color specialActivateColor = new Color(1f, 0.88f, 0.55f, 1f);
        [SerializeField] private Color specialTargetColor = new Color(1f, 0.94f, 0.72f, 1f);
        [SerializeField] private Color specialCreateColor = new Color(1f, 0.98f, 0.84f, 1f);
        [SerializeField] private float specialActivatePunchMultiplier = 1.45f;
        [SerializeField] private float specialTargetPunchMultiplier = 1.65f;
        [SerializeField] private float specialCreatePunchMultiplier = 1.55f;

        public override IEnumerator PlayActivateAsync()
        {
            TryShakeBoard(0.25f, 0.14f);
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * specialActivatePunchMultiplier,
                specialActivateColor,
                TileShadowState.Active,
                false);
        }

        public override IEnumerator PlayTargetSelectionAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, TargetPulseDuration),
                DamagePunchScale * specialTargetPunchMultiplier,
                specialTargetColor,
                TileShadowState.Active,
                true);
        }

        public override IEnumerator PlaySpecialCreateAsync(float speedMultiplier = 1f)
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.02f, SpecialCreateDuration / speedMultiplier),
                DamagePunchScale * specialCreatePunchMultiplier,
                specialCreateColor,
                TileShadowState.Active,
                false);
        }

        public override IEnumerator PlayClearAsync(bool isExplosion = false)
        {
            TryShakeBoard(0.28f, 0.16f);
            SpawnShockwaveRing(1.8f, 0.25f, specialActivateColor);
            SpawnDebrisParticles(8, 0.35f, specialActivateColor);
            yield return base.PlayClearAsync(isExplosion);
        }
    }
}

