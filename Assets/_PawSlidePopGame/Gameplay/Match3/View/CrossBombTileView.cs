using System.Collections;
using DG.Tweening;
using UnityEngine;
using _PawSlidePopGame._Scripts.Core.Vibration;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class CrossBombTileView : SpecialTileView
    {
        [Header("Cross Bomb FX")]
        [SerializeField] private Color crossActivateColor = new Color(1f, 0.9f, 0.45f, 1f);
        [SerializeField] private float crossActivatePunchMultiplier = 1.85f;

        public override IEnumerator PlayActivateAsync()
        {
            // Trigger haptic vibration feedback on activation
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayMediumImpact(true);
            }

            // Do NOT shake the board on activation
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * crossActivatePunchMultiplier,
                crossActivateColor,
                TileShadowState.Active,
                false);
        }

        public override IEnumerator PlayClearAsync(bool isExplosion = false)
        {
            // Trigger haptic vibration feedback on explosion
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayMediumImpact(true);
            }

            // Spawn high-tech energy sweep laser effects
            SpawnCrossBeams();
            SpawnDebrisParticles(12, 0.4f, crossActivateColor, null, null, 1.2f);
            
            // Bypass SpecialTileView's PlayClearAsync to avoid camera shaking and shockwave ring,
            // going straight to Match3TileView's base clear animation.
            yield return ExecuteBaseClearAsync(isExplosion);
        }

        private void SpawnCrossBeams()
        {
            Color beamColor = new Color(crossActivateColor.r, crossActivateColor.g, crossActivateColor.b, 0.95f);
            float travelDistance = 9f; // Distance to cover the screen/board
            float travelDuration = 0.32f; // Fast, snappy sweep duration

            Vector2[] directions = { Vector2.left, Vector2.right, Vector2.up, Vector2.down };
            foreach (Vector2 dir in directions)
            {
                // 1. Create a double-layered sweep head (SweepNode)
                GameObject nodeObj = new GameObject("SweepNode_Root");
                nodeObj.transform.position = transform.position;

                // Inner bright white core
                GameObject nodeCore = new GameObject("Core");
                nodeCore.transform.SetParent(nodeObj.transform);
                nodeCore.transform.localPosition = Vector3.zero;
                nodeCore.transform.localScale = Vector3.one * 0.28f;
                SpriteRenderer coreSr = nodeCore.AddComponent<SpriteRenderer>();
                coreSr.sprite = GetCircleSprite();
                coreSr.color = Color.white;
                coreSr.sortingLayerID = BodyRenderer.sortingLayerID;
                coreSr.sortingLayerName = BodyRenderer.sortingLayerName;
                coreSr.sortingOrder = 505;

                // Outer energy glow
                GameObject nodeGlow = new GameObject("Glow");
                nodeGlow.transform.SetParent(nodeObj.transform);
                nodeGlow.transform.localPosition = Vector3.zero;
                nodeGlow.transform.localScale = Vector3.one * 0.6f;
                SpriteRenderer glowSr = nodeGlow.AddComponent<SpriteRenderer>();
                glowSr.sprite = GetCircleSprite();
                glowSr.color = new Color(beamColor.r, beamColor.g, beamColor.b, 0.65f);
                glowSr.sortingLayerID = BodyRenderer.sortingLayerID;
                glowSr.sortingLayerName = BodyRenderer.sortingLayerName;
                glowSr.sortingOrder = 504;

                // 2. Create a flat, high-tech double-layered laser beam trail
                GameObject trailObj = new GameObject("SweepTrail_Root");
                trailObj.transform.position = transform.position;

                bool isHorizontal = dir.x != 0f;

                // Outer Glow Trail
                GameObject trailGlow = new GameObject("GlowTrail");
                trailGlow.transform.SetParent(trailObj.transform);
                trailGlow.transform.localPosition = Vector3.zero;
                SpriteRenderer trailGlowSr = trailGlow.AddComponent<SpriteRenderer>();
                trailGlowSr.sprite = GetSquareSprite();
                trailGlowSr.color = new Color(beamColor.r, beamColor.g, beamColor.b, 0.45f);
                trailGlowSr.sortingLayerID = BodyRenderer.sortingLayerID;
                trailGlowSr.sortingLayerName = BodyRenderer.sortingLayerName;
                trailGlowSr.sortingOrder = 502;

                // Inner Core Trail (Very thin white core line)
                GameObject trailCore = new GameObject("CoreTrail");
                trailCore.transform.SetParent(trailObj.transform);
                trailCore.transform.localPosition = Vector3.zero;
                SpriteRenderer trailCoreSr = trailCore.AddComponent<SpriteRenderer>();
                trailCoreSr.sprite = GetSquareSprite();
                trailCoreSr.color = new Color(1f, 1f, 1f, 0.9f);
                trailCoreSr.sortingLayerID = BodyRenderer.sortingLayerID;
                trailCoreSr.sortingLayerName = BodyRenderer.sortingLayerName;
                trailCoreSr.sortingOrder = 503;

                // Initial scale configurations (extremely thin thickness)
                if (isHorizontal)
                {
                    trailGlow.transform.localScale = new Vector3(0.01f, 0.32f, 1f);
                    trailCore.transform.localScale = new Vector3(0.01f, 0.08f, 1f);
                }
                else
                {
                    trailGlow.transform.localScale = new Vector3(0.32f, 0.01f, 1f);
                    trailCore.transform.localScale = new Vector3(0.08f, 0.01f, 1f);
                }

                // 3. Animate SweepNode and Beam Trails using DOTween
                Vector3 targetPos = transform.position + (Vector3)(dir * travelDistance);

                // Sweep the head node outwards
                nodeObj.transform.DOMove(targetPos, travelDuration).SetEase(Ease.OutQuad).OnComplete(() => Destroy(nodeObj));
                nodeCore.transform.DOScale(Vector3.zero, travelDuration).SetEase(Ease.InQuad);
                nodeGlow.transform.DOScale(Vector3.zero, travelDuration).SetEase(Ease.InQuad);

                // Stretch the laser trail behind the moving node
                Vector3 targetCenterPos = transform.position + (Vector3)(dir * travelDistance * 0.5f);
                trailObj.transform.DOMove(targetCenterPos, travelDuration).SetEase(Ease.OutQuad);

                if (isHorizontal)
                {
                    trailGlow.transform.DOScale(new Vector3(travelDistance, 0.32f, 1f), travelDuration).SetEase(Ease.OutQuad);
                    trailCore.transform.DOScale(new Vector3(travelDistance, 0.08f, 1f), travelDuration).SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            // Shrink the width (Y-axis) to zero super fast for a sleek energy dissipation effect
                            trailCore.transform.DOScaleY(0f, 0.08f).SetEase(Ease.InQuad);
                            trailCoreSr.DOFade(0f, 0.08f).SetEase(Ease.InQuad);

                            trailGlow.transform.DOScaleY(0f, 0.16f).SetEase(Ease.InQuad);
                            trailGlowSr.DOFade(0f, 0.16f).SetEase(Ease.InQuad).OnComplete(() => Destroy(trailObj));
                        });
                }
                else
                {
                    trailGlow.transform.DOScale(new Vector3(0.32f, travelDistance, 1f), travelDuration).SetEase(Ease.OutQuad);
                    trailCore.transform.DOScale(new Vector3(0.08f, travelDistance, 1f), travelDuration).SetEase(Ease.OutQuad)
                        .OnComplete(() =>
                        {
                            trailCore.transform.DOScaleX(0f, 0.08f).SetEase(Ease.InQuad);
                            trailCoreSr.DOFade(0f, 0.08f).SetEase(Ease.InQuad);

                            trailGlow.transform.DOScaleX(0f, 0.16f).SetEase(Ease.InQuad);
                            trailGlowSr.DOFade(0f, 0.16f).SetEase(Ease.InQuad).OnComplete(() => Destroy(trailObj));
                        });
                }

                // 4. Spawn side energy discharges/sparks along the scan path
                StartCoroutine(SpawnSweepSparks(nodeObj, dir, beamColor, travelDuration));
            }
        }

        private IEnumerator SpawnSweepSparks(GameObject node, Vector2 dir, Color color, float duration)
        {
            float elapsed = 0f;
            float interval = 0.035f; // Frequent discharges
            while (elapsed < duration && node != null)
            {
                GameObject spark = new GameObject("SweepSpark");
                spark.transform.position = node.transform.position;

                bool isHorizontal = dir.x != 0f;
                if (isHorizontal)
                {
                    spark.transform.localScale = new Vector3(Random.Range(0.4f, 0.7f), Random.Range(0.04f, 0.08f), 1f);
                }
                else
                {
                    spark.transform.localScale = new Vector3(Random.Range(0.04f, 0.08f), Random.Range(0.4f, 0.7f), 1f);
                }

                SpriteRenderer sr = spark.AddComponent<SpriteRenderer>();
                sr.sprite = GetSquareSprite(); // Clean flat energy sparks
                sr.color = Color.Lerp(Color.white, color, Random.Range(0.3f, 0.9f));
                sr.sortingLayerID = BodyRenderer.sortingLayerID;
                sr.sortingLayerName = BodyRenderer.sortingLayerName;
                sr.sortingOrder = 501;

                // Move perpendicular to direction to add volume to the sweep
                Vector2 perpDir = new Vector2(-dir.y, dir.x);
                float lateralOffset = Random.Range(-0.45f, 0.45f);
                Vector3 targetPos = spark.transform.position + (Vector3)(perpDir * lateralOffset) - (Vector3)(dir * Random.Range(0.15f, 0.35f));

                float lifeTime = Random.Range(0.12f, 0.18f);
                spark.transform.DOMove(targetPos, lifeTime).SetEase(Ease.OutQuad);
                
                if (isHorizontal)
                {
                    spark.transform.DOScaleY(0f, lifeTime).SetEase(Ease.InQuad);
                }
                else
                {
                    spark.transform.DOScaleX(0f, lifeTime).SetEase(Ease.InQuad);
                }
                sr.DOFade(0f, lifeTime).SetEase(Ease.InQuad).OnComplete(() => Destroy(spark));

                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }
        }
    }
}
