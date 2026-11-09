using System.Collections;
using DG.Tweening;
using UnityEngine;
using _PawSlidePopGame._Scripts.Core.Vibration;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class AreaBombTileView : SpecialTileView
    {
        [Header("Area Bomb FX")]
        [SerializeField] private Color areaActivateColor = new Color(1f, 0.86f, 0.62f, 1f);
        [SerializeField] private Color areaCreateColor = new Color(1f, 0.95f, 0.78f, 1f);
        [SerializeField] private float areaActivatePunchMultiplier = 1.7f;
        [SerializeField] private float areaCreatePunchMultiplier = 1.75f;

        [Header("Snappy Shake Config")]
        [SerializeField] private float clearShakeDuration = 0.22f;
        [SerializeField] private float clearShakeStrength = 0.14f;

        public override IEnumerator PlayActivateAsync()
        {
            // Trigger haptic vibration on activation
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayMediumImpact(true);
            }

            // Do NOT shake the board/camera on activation
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, DamageDuration),
                DamagePunchScale * areaActivatePunchMultiplier,
                areaActivateColor,
                TileShadowState.Active,
                false);
        }

        public override IEnumerator PlaySpecialCreateAsync(float speedMultiplier = 1f)
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.02f, SpecialCreateDuration / speedMultiplier),
                DamagePunchScale * areaCreatePunchMultiplier,
                areaCreateColor,
                TileShadowState.Active,
                false);
        }

        public override IEnumerator PlayClearAsync(bool isExplosion = false)
        {
            // Trigger haptic vibration on explosion
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayMediumImpact(true);
            }

            // Snappy camera shake
            TryShakeBoard(clearShakeDuration, clearShakeStrength);

            // Spawn radial blast explosion visuals
            SpawnAreaExplosion();
            SpawnDebrisParticles(12, 0.45f, areaActivateColor, null, null, 1.4f);

            // Bypass SpecialTileView's PlayClearAsync to avoid duplicated shake
            yield return ExecuteBaseClearAsync(isExplosion);
        }

        private void SpawnAreaExplosion()
        {
            // Dual expanding shockwave rings
            SpawnShockwaveRing(3.2f, 0.35f, new Color(areaActivateColor.r, areaActivateColor.g, areaActivateColor.b, 0.9f));
            
            DOVirtual.DelayedCall(0.06f, () => {
                SpawnShockwaveRing(4f, 0.32f, new Color(areaActivateColor.r * 1.1f, areaActivateColor.g * 0.9f, areaActivateColor.b * 0.7f, 0.7f));
            });

            // Premium radial energy burst rays (8 directions)
            int rayCount = 8;
            float travelDistance = 3.5f;
            float travelDuration = 0.25f;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = (360f / rayCount) * i + Random.Range(-5f, 5f);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

                GameObject ray = new GameObject("ExplosionRay");
                ray.transform.position = transform.position;
                ray.transform.rotation = Quaternion.Euler(0f, 0f, angle);

                SpriteRenderer sr = ray.AddComponent<SpriteRenderer>();
                sr.sprite = GetSquareSprite();
                sr.color = new Color(areaActivateColor.r, areaActivateColor.g * 0.95f, areaActivateColor.b * 0.8f, 0.85f);
                sr.sortingLayerID = BodyRenderer.sortingLayerID;
                sr.sortingLayerName = BodyRenderer.sortingLayerName;
                sr.sortingOrder = 502;

                ray.transform.localScale = new Vector3(0.01f, 0.18f, 1f);
                ray.transform.DOMove(transform.position + (Vector3)(dir * travelDistance * 0.5f), travelDuration).SetEase(Ease.OutQuad);
                
                Sequence seq = DOTween.Sequence().SetLink(ray);
                seq.Append(ray.transform.DOScale(new Vector3(travelDistance, 0.02f, 1f), travelDuration).SetEase(Ease.OutQuad));
                seq.Join(sr.DOFade(0f, travelDuration).SetEase(Ease.InQuad));
                seq.OnComplete(() => Destroy(ray));
            }
        }
    }
}
