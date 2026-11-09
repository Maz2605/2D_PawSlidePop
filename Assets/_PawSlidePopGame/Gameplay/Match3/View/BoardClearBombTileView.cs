using System.Collections;
using DG.Tweening;
using UnityEngine;
using _PawSlidePopGame._Scripts.Core.Vibration;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class BoardClearBombTileView : SpecialTileView
    {
        [Header("Board Clear Bomb FX")]
        [SerializeField] private Color clearActivateColor = new Color(0.9f, 0.5f, 1f, 1f); // Vibrant purple/magenta candy glow
        [SerializeField] private float clearActivatePunchMultiplier = 1.9f;
        [SerializeField] private float clearShakeDuration = 0.32f;
        [SerializeField] private float clearShakeStrength = 0.22f;

        public override IEnumerator PlayActivateAsync()
        {
            // Trigger haptic vibration feedback on activation
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayMediumImpact(true);
            }

            // High energy gathering effect: Rapidly spin and pulse before imploding
            transform.DORotate(new Vector3(0f, 0f, 360f), DamageDuration, RotateMode.FastBeyond360).SetEase(Ease.InBack);
            
            // Do NOT shake the board on activation
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * clearActivatePunchMultiplier,
                clearActivateColor,
                TileShadowState.Active,
                false);
        }

        public override IEnumerator PlayClearAsync(bool isExplosion = false)
        {
            // Trigger strong haptic vibration on detonation
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayMediumImpact(true);
            }

            // Snappy, clean camera shake on board clear
            TryShakeBoard(clearShakeDuration, clearShakeStrength);

            // Supernova Implosion-Explosion visuals
            SpawnSupernovaFX();

            // Spawn massive multi-colored sparkly debris particles
            SpawnDebrisParticles(24, 0.65f, Color.white, null, null, 1.8f);

            // Bypass SpecialTileView's PlayClearAsync to avoid duplicated shake/shockwave
            yield return ExecuteBaseClearAsync(isExplosion);
        }

        private void SpawnSupernovaFX()
        {
            // 1. Giant shockwave ring expanding outward rapidly
            SpawnShockwaveRing(6.5f, 0.45f, clearActivateColor);

            // 2. Multi-directional lightning energy discharge arcs
            int arcCount = 14;
            float travelDistance = 10f;
            float travelDuration = 0.35f;

            for (int i = 0; i < arcCount; i++)
            {
                float angle = (360f / arcCount) * i + Random.Range(-12f, 12f);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                GameObject arc = new GameObject("LightningArc");
                arc.transform.position = transform.position;
                arc.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                SpriteRenderer sr = arc.AddComponent<SpriteRenderer>();
                sr.sprite = GetSquareSprite();
                // Random neon colors (rainbow candy feel)
                Color arcColor = Color.HSVToRGB(Random.value, 0.75f, 1f);
                sr.color = new Color(arcColor.r, arcColor.g, arcColor.b, 0.85f);
                sr.sortingLayerID = BodyRenderer.sortingLayerID;
                sr.sortingLayerName = BodyRenderer.sortingLayerName;
                sr.sortingOrder = 502;

                // Animate lightning stretch and fade
                arc.transform.localScale = new Vector3(0.01f, Random.Range(0.04f, 0.12f), 1f);
                arc.transform.DOMove(transform.position + (Vector3)(dir * travelDistance * 0.5f), travelDuration).SetEase(Ease.OutExpo);
                
                Sequence seq = DOTween.Sequence().SetLink(arc);
                seq.Append(arc.transform.DOScale(new Vector3(travelDistance, Random.Range(0.03f, 0.08f), 1f), travelDuration).SetEase(Ease.OutExpo));
                seq.Join(sr.DOFade(0f, travelDuration).SetEase(Ease.InQuad));
                seq.OnComplete(() => Destroy(arc));
            }
        }
    }
}
