using System.Collections;
using DG.Tweening;
using UnityEngine;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class SquareBombTileView : SpecialTileView
    {
        [Header("Square Bomb FX")]
        [SerializeField] private Color squareTargetColor = new Color(1f, 0.82f, 0.5f, 1f);
        [SerializeField] private float squareTargetPunchMultiplier = 1.9f;

        private bool _alreadyExplodedAtTarget;

        public override void Bind(TileModel tile, BoardContentDefinitionSO definition)
        {
            base.Bind(tile, definition);
            _alreadyExplodedAtTarget = false;
            if (BodyRenderer != null) BodyRenderer.enabled = true;
            if (ShadowRenderer != null) ShadowRenderer.enabled = true;
        }

        public override IEnumerator PlayActivateAsync()
        {
            // Do NOT shake the board during activation for SquareBomb
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * squareTargetPunchMultiplier,
                squareTargetColor,
                TileShadowState.Active,
                false);
        }

        public override IEnumerator PlayTargetSelectionAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, TargetPulseDuration),
                DamagePunchScale * squareTargetPunchMultiplier,
                squareTargetColor,
                TileShadowState.Active,
                true);
        }

        public override IEnumerator PlayClearAsync(bool isExplosion = false)
        {
            if (!_alreadyExplodedAtTarget)
            {
                TryShakeBoard(0.24f, 0.14f);
                SpawnSquareExplosionAt(transform.position);
                SpawnDebrisParticles(8, 0.35f, squareTargetColor);
            }

            _alreadyExplodedAtTarget = false; // Reset for pooling

            if (BodyRenderer != null) BodyRenderer.enabled = true;
            if (ShadowRenderer != null) ShadowRenderer.enabled = true;

            yield return base.PlayClearAsync(isExplosion);
        }

        public IEnumerator PlaySquareFlyAndExplodeAsync(Vector3 targetLocalPos)
        {
            StopIdle();
            KillMotionTweens();

            // Squeeze Y, stretch X for a quick launch wind-up
            yield return transform.DOScale(new Vector3(InitialScale.x * 1.25f, InitialScale.y * 0.75f, InitialScale.z), 0.12f).SetEase(Ease.OutQuad).WaitForCompletion();

            // Create clone
            GameObject clone = new GameObject("SquareBombClone");
            clone.transform.SetParent(transform.parent, false);
            clone.transform.localPosition = transform.localPosition;
            clone.transform.localScale = transform.localScale;

            SpriteRenderer cloneSr = clone.AddComponent<SpriteRenderer>();
            if (BodyRenderer != null)
            {
                cloneSr.sprite = BodyRenderer.sprite;
                cloneSr.color = BodyRenderer.color;
                cloneSr.sortingLayerID = BodyRenderer.sortingLayerID;
                cloneSr.sortingLayerName = BodyRenderer.sortingLayerName;
                cloneSr.sortingOrder = 500; // Render on top of everything
            }

            // Hide original bomb view
            if (BodyRenderer != null) BodyRenderer.enabled = false;
            if (ShadowRenderer != null) ShadowRenderer.enabled = false;

            // Calculate camera center in local space of the board
            Vector3 cameraCenterLocal;
            if (Camera.main != null)
            {
                Vector3 cameraCenterWorld = Camera.main.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, -Camera.main.transform.position.z + transform.position.z));
                cameraCenterLocal = transform.parent.InverseTransformPoint(cameraCenterWorld);
            }
            else
            {
                // Fallback to screen center approximation in local space
                Vector3 startLocalPos = transform.localPosition;
                float peakY = Mathf.Max(startLocalPos.y, targetLocalPos.y) + 3.0f;
                float midX = (startLocalPos.x + targetLocalPos.x) * 0.5f;
                cameraCenterLocal = new Vector3(midX, peakY, startLocalPos.z);
            }

            cameraCenterLocal.z = transform.localPosition.z;
            targetLocalPos.z = transform.localPosition.z;

            Sequence seq = DOTween.Sequence().SetLink(clone);
            
            // Phase 1: Fly and scale up to camera center (front of camera)
            seq.Append(clone.transform.DOLocalMove(cameraCenterLocal, 0.38f).SetEase(Ease.OutCubic));
            seq.Join(clone.transform.DOScale(InitialScale * 2.2f, 0.38f).SetEase(Ease.OutBack));
            seq.Join(clone.transform.DORotate(new Vector3(0, 0, 180f), 0.38f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
            
            // Phase 2: Stay in front of camera for a moment with a small pulse/spin
            seq.AppendInterval(0.25f);
            seq.Append(clone.transform.DOPunchScale(InitialScale * 0.2f, 0.25f, 5, 0.5f));
            seq.Join(clone.transform.DORotate(new Vector3(0, 0, 195f), 0.25f).SetEase(Ease.InOutQuad));
            
            // Phase 3: Dive down to target position
            seq.Append(clone.transform.DOLocalMove(targetLocalPos, 0.42f).SetEase(Ease.InBack));
            seq.Join(clone.transform.DOScale(InitialScale * 0.9f, 0.42f).SetEase(Ease.InQuad));
            seq.Join(clone.transform.DORotate(new Vector3(0, 0, -360f), 0.42f, RotateMode.FastBeyond360).SetEase(Ease.InQuad));

            // Start premium trail effects concurrently for the entire duration (1.3s total)
            float totalDuration = 1.3f;
            Coroutine trailGhost = StartCoroutine(SpawnAfterimages(clone, cloneSr.sprite, cloneSr.color, totalDuration));
            Coroutine trailSparks = StartCoroutine(SpawnFlightParticles(clone, totalDuration));

            yield return seq.WaitForCompletion();

            if (trailGhost != null) StopCoroutine(trailGhost);
            if (trailSparks != null) StopCoroutine(trailSparks);

            Vector3 targetWorldPos = clone.transform.position;
            
            // Shake camera/board ONLY on landing
            TryShakeBoard(0.24f, 0.14f);

            // Spawn explosions at target position
            SpawnShockwaveRing(1.8f, 0.25f, squareTargetColor, targetWorldPos);
            SpawnSquareExplosionAt(targetWorldPos);
            SpawnDebrisParticles(8, 0.35f, squareTargetColor, null, targetWorldPos);

            Destroy(clone);
            _alreadyExplodedAtTarget = true;
        }

        private IEnumerator SpawnAfterimages(GameObject clone, Sprite sprite, Color color, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && clone != null)
            {
                GameObject ghost = new GameObject("SquareBombGhost");
                ghost.transform.position = clone.transform.position;
                ghost.transform.rotation = clone.transform.rotation;
                ghost.transform.localScale = clone.transform.localScale * 0.9f;

                SpriteRenderer ghostSr = ghost.AddComponent<SpriteRenderer>();
                ghostSr.sprite = sprite;
                ghostSr.color = new Color(color.r, color.g, color.b, 0.4f);

                SpriteRenderer cloneSr = clone.GetComponent<SpriteRenderer>();
                if (cloneSr != null)
                {
                    ghostSr.sortingLayerID = cloneSr.sortingLayerID;
                    ghostSr.sortingLayerName = cloneSr.sortingLayerName;
                }
                ghostSr.sortingOrder = cloneSr != null ? cloneSr.sortingOrder - 1 : 499;

                ghostSr.DOFade(0f, 0.18f).SetEase(Ease.OutQuad);
                ghost.transform.DOScale(Vector3.zero, 0.18f).SetEase(Ease.InQuad).OnComplete(() => Destroy(ghost));

                yield return new WaitForSeconds(0.04f);
                elapsed += 0.04f;
            }
        }

        private IEnumerator SpawnFlightParticles(GameObject clone, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration && clone != null)
            {
                GameObject p = new GameObject("FlightParticle");
                p.transform.position = clone.transform.position + (Vector3)(Random.insideUnitCircle * 0.15f);
                p.transform.localScale = InitialScale * Random.Range(0.12f, 0.2f);

                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();
                sr.sprite = GetCircleSprite();
                sr.color = new Color(squareTargetColor.r * 1.2f, squareTargetColor.g * 1.2f, squareTargetColor.b, 0.8f);

                SpriteRenderer cloneSr = clone.GetComponent<SpriteRenderer>();
                if (cloneSr != null)
                {
                    sr.sortingLayerID = cloneSr.sortingLayerID;
                    sr.sortingLayerName = cloneSr.sortingLayerName;
                }
                sr.sortingOrder = cloneSr != null ? cloneSr.sortingOrder - 2 : 498;

                p.transform.DOMove(p.transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f), 0.25f).SetEase(Ease.OutQuad);
                p.transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InQuad);
                sr.DOFade(0f, 0.25f).SetEase(Ease.InQuad).OnComplete(() => Destroy(p));

                yield return new WaitForSeconds(0.06f);
                elapsed += 0.06f;
            }
        }

        private void SpawnSquareExplosionAt(Vector3 worldPos)
        {
            GameObject squareShock = new GameObject("SquareShockwave");
            squareShock.transform.position = worldPos;
            squareShock.transform.localScale = Vector3.one * 0.1f;

            SpriteRenderer sr = squareShock.AddComponent<SpriteRenderer>();
            sr.sprite = GetSquareSprite();
            sr.color = new Color(squareTargetColor.r, squareTargetColor.g, squareTargetColor.b, 0.8f);
            sr.sortingOrder = 99;

            squareShock.transform.DOScale(InitialScale * 2.2f, 0.3f).SetEase(Ease.OutQuad);
            sr.DOFade(0f, 0.3f).SetEase(Ease.InQuad).OnComplete(() => Destroy(squareShock));
        }
    }
}
