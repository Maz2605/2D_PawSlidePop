using System.Collections;
using _PawSlidePopGame._Scripts.Core.Audio;
using _PawSlidePopGame._Scripts.Core.Vibration;
using DG.Tweening;
using _PawSlidePopGame._Scripts.Feature.Match3.Data;
using _PawSlidePopGame._Scripts.Feature.Match3.Model.Entities;
using _PawSlidePopGame.Scripts.DesignPattern.ObjectPooling;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class Match3TileView : MonoBehaviour, IPoolable
    {
        [Header("Renderers")]
        [SerializeField] protected SpriteRenderer bodyRenderer;
        [SerializeField] protected SpriteRenderer shadowRenderer;

        [Header("Sprites")]
        [SerializeField] private bool useDefinitionIconAsFallback = true;

        [Header("Shadow")]
        [SerializeField] private Color previewShadowColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color activeShadowColor = new Color(1f, 1f, 1f, 0.9f);

        [Header("DOTween Idle")]
        [SerializeField] private bool enableIdlePulse;
        [SerializeField] private float pulseScale = 1.03f;
        [SerializeField] private float pulseDuration = 1.2f;
        [SerializeField] private float pulseLoopDelay = 1.8f;

        [Header("FX")]
        [SerializeField] private float clearDuration = 0.2f;
        [SerializeField] private float damageDuration = 0.18f;
        [SerializeField] private float landingDuration = 0.16f;
        [SerializeField] private float targetPulseDuration = 0.18f;
        [SerializeField] private float specialCreateDuration = 0.22f;
        [SerializeField] private float clearScale = 0.75f;
        [SerializeField] private float damagePunchScale = 0.1f;
        [SerializeField] private float landingPunchScale = 0.08f;

        [Header("Movement FX")]
        [SerializeField] private Ease slideEase = Ease.OutCubic;
        [SerializeField] private float slideOvershoot = 1.2f;
        [SerializeField] private float slideStretchAmount = 0.08f;
        [SerializeField] private float slideBounceDuration = 0.15f;

        private TileModel _tile;
        private BoardContentDefinitionSO _definition;
        private bool _isIdleEnabled;
        private Coroutine _blinkRoutine;
        private Tween _pulseTween;
        private Vector3 _initialScale;
        private Vector3 _initialShadowScale = Vector3.one;
        private Quaternion _initialLocalRotation;
        private Color _bodyBaseColor = Color.white;
        private Color _shadowBaseColor = Color.white;
        private TileShadowState _shadowState;
        private bool _isScaledUp;

        public int TileInstanceId => _tile != null ? _tile.InstanceId : 0;
        public TileModel Tile => _tile;
        protected BoardContentDefinitionSO Definition => _definition;
        protected SpriteRenderer BodyRenderer => bodyRenderer;
        protected SpriteRenderer ShadowRenderer => shadowRenderer;
        protected Vector3 InitialScale => _initialScale;
        protected Color BodyBaseColor => _bodyBaseColor;
        protected Color ShadowBaseColor => _shadowBaseColor;
        protected float DamageDuration => damageDuration;
        protected float TargetPulseDuration => targetPulseDuration;
        protected float SpecialCreateDuration => specialCreateDuration;
        protected float ClearDuration => clearDuration;
        protected float DamagePunchScale => damagePunchScale;
        protected float LandingDuration => landingDuration;
        protected float LandingPunchScale => landingPunchScale;
        protected float ClearScale => clearScale;
        protected TileShadowState ShadowState => _shadowState;
        protected virtual bool EnableIdleBlink => false;
        protected virtual Vector2 BlinkIntervalRange => new Vector2(2.5f, 5f);
        protected virtual float ClosedEyesDuration => 0.08f;
        protected virtual Sprite OpenSprite => null;
        protected virtual Sprite ClosedSprite => null;

        // Programmatic Sprites
        private static Sprite _cachedCircleSprite;
        private static Sprite _cachedRingSprite;
        private static Sprite _cachedSquareSprite;

        public static Sprite GetCircleSprite()
        {
            if (_cachedCircleSprite != null) return _cachedCircleSprite;
            int size = 32;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2.0f;
            float radius = size / 2.0f - 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            _cachedCircleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _cachedCircleSprite;
        }

        public static Sprite GetRingSprite()
        {
            if (_cachedRingSprite != null) return _cachedRingSprite;
            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2.0f;
            float outerRadius = size / 2.0f - 1.0f;
            float innerRadius = outerRadius * 0.65f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    if (dist <= outerRadius && dist >= innerRadius)
                    {
                        float alphaOuter = Mathf.Clamp01(outerRadius - dist);
                        float alphaInner = Mathf.Clamp01(dist - innerRadius);
                        float alpha = Mathf.Min(alphaOuter, alphaInner);
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                    else
                    {
                        tex.SetPixel(x, y, Color.clear);
                    }
                }
            }
            tex.Apply();
            _cachedRingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _cachedRingSprite;
        }

        public static Sprite GetSquareSprite()
        {
            if (_cachedSquareSprite != null) return _cachedSquareSprite;
            Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _cachedSquareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 100f);
            return _cachedSquareSprite;
        }

        public Color GetTileThemeColor()
        {
            int tileId = _definition != null ? _definition.TileId : 0;
            switch (tileId)
            {
                case 1: return new Color(1f, 0.6f, 0.7f, 1f);       // Cat - pink
                case 2: return new Color(0.95f, 0.75f, 0.3f, 1f);   // Dog - yellow/golden
                case 3: return new Color(0.95f, 0.5f, 0.2f, 1f);    // Fox - orange
                case 4: return new Color(0.65f, 0.45f, 0.3f, 1f);   // Bear - brown
                case 5: return new Color(0.9f, 0.9f, 0.95f, 1f);    // Rabbit - white-grey
                case 6: return new Color(0.85f, 0.85f, 0.85f, 1f);  // Panda - white
                case 7: return new Color(0.35f, 0.75f, 0.4f, 1f);   // Alligator - green
                case 8: return new Color(0.4f, 0.8f, 0.45f, 1f);    // Frog - green
                case 9: return new Color(0.7f, 0.7f, 0.75f, 1f);    // Mouse - grey
                case 10: return new Color(0.2f, 0.7f, 0.95f, 1f);   // Bird - blue
                case 11: return new Color(0.9f, 0.9f, 0.85f, 1f);   // Cow - cream
                case 12: return new Color(0.8f, 0.55f, 0.35f, 1f);  // Dear - light brown
                case 13: return new Color(0.4f, 0.75f, 0.95f, 1f);  // Dolphin - blue
                case 14: return new Color(0.6f, 0.7f, 0.8f, 1f);    // Elephant - grey-blue
                case 15: return new Color(0.95f, 0.8f, 0.2f, 1f);   // Giraffe - yellow
                case 16: return new Color(0.5f, 0.8f, 0.95f, 1f);   // Penguin - ice blue
                case 17: return new Color(1f, 0.7f, 0.75f, 1f);     // Pig - pink
                case 18: return new Color(0.95f, 0.95f, 0.9f, 1f);  // Sheep - creamy white
                case 19: return new Color(0.7f, 0.75f, 0.8f, 1f);   // Koala - grey
                case 20: return new Color(0.55f, 0.4f, 0.65f, 1f);  // Owl - purple
                default: return new Color(1f, 0.85f, 0.4f, 1f);     // Sparkly gold
            }
        }

        protected void TryShakeBoard(float duration, float strength)
        {
            Match3BoardView board = GetComponentInParent<Match3BoardView>();
            if (board != null)
            {
                board.ShakeBoard(duration, strength);
            }
        }

        protected void SpawnDebrisParticles(int count, float duration, Color color, Sprite customSprite = null, Vector3? customWorldPos = null, float spreadMultiplier = 1f)
        {
            Vector3 spawnPos = customWorldPos.HasValue ? customWorldPos.Value : transform.position;
            Sprite spriteToUse = customSprite != null ? customSprite : (bodyRenderer != null ? bodyRenderer.sprite : null);
            if (spriteToUse == null) spriteToUse = GetCircleSprite();

            for (int i = 0; i < count; i++)
            {
                GameObject p = new GameObject("DebrisParticle");
                p.transform.position = spawnPos;
                SpriteRenderer sr = p.AddComponent<SpriteRenderer>();

                if (i % 2 == 0)
                {
                    sr.sprite = spriteToUse;
                    sr.color = Color.white;
                }
                else
                {
                    sr.sprite = GetCircleSprite();
                    sr.color = new Color(color.r * 1.1f, color.g * 1.1f, color.b * 1.1f, color.a * 0.9f);
                }
                sr.sortingOrder = 105;

                float targetShardScale = Random.Range(0.18f, 0.26f);
                p.transform.localScale = _initialScale * targetShardScale;

                float angle = Random.Range(0f, Mathf.PI * 2f);
                float dist = Random.Range(0.6f, 1.4f) * spreadMultiplier;
                Vector3 targetPos = spawnPos + new Vector3(Mathf.Cos(angle) * dist, Mathf.Sin(angle) * dist, 0);

                p.transform.DOMove(targetPos, duration).SetEase(Ease.OutQuad);
                p.transform.DORotate(new Vector3(0, 0, Random.Range(-270f, 270f)), duration);
                p.transform.DOScale(Vector3.zero, duration).SetEase(Ease.InQuad);
                sr.DOFade(0f, duration).SetEase(Ease.InQuad).OnComplete(() => Destroy(p));
            }
        }

        protected void SpawnShockwaveRing(float maxScale, float duration, Color color, Vector3? customWorldPos = null)
        {
            Vector3 spawnPos = customWorldPos.HasValue ? customWorldPos.Value : transform.position;
            GameObject ring = new GameObject("ShockwaveRing");
            ring.transform.position = spawnPos;
            ring.transform.localScale = Vector3.one * 0.1f;
            
            SpriteRenderer sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite = GetRingSprite();
            sr.color = color;
            sr.sortingOrder = 100;

            ring.transform.DOScale(_initialScale * maxScale, duration).SetEase(Ease.OutQuad);
            sr.DOFade(0f, duration).SetEase(Ease.OutQuad).OnComplete(() => Destroy(ring));
        }

        protected virtual void Awake()
        {
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
            }
            if (shadowRenderer == null)
            {
                Transform shadowChild = transform.Find("Shadow");
                if (shadowChild != null)
                {
                    shadowRenderer = shadowChild.GetComponent<SpriteRenderer>();
                }
            }

            CacheInitialTransformState();
            _initialScale = transform.localScale;
            if (bodyRenderer != null)
            {
                _bodyBaseColor = bodyRenderer.color;
            }

            if (shadowRenderer != null)
            {
                _shadowBaseColor = shadowRenderer.color;
                _initialShadowScale = shadowRenderer.transform.localScale;
            }
        }

        protected virtual void OnEnable()
        {
            if (_isIdleEnabled)
            {
                StartIdleLoop();
            }
        }

        protected virtual void OnDisable()
        {
            StopIdle();
            ResetVisualState();
        }

        protected virtual void OnDestroy()
        {
            StopIdle();
            KillAllTweens();
        }

        public void OnSpawn()
        {
            ResetVisualState();
        }

        public void OnDespawn()
        {
            StopIdle();
            KillAllTweens();
            ResetVisualState();
            _tile = null;
            _definition = null;
        }

        public virtual void Bind(TileModel tile, BoardContentDefinitionSO definition)
        {
            _tile = tile;
            _definition = definition;

            RestoreBodyColor();
            RestoreShadowColor();
            ApplyOpenSprite();
            SetShadowState(TileShadowState.Off);
        }

        public virtual IEnumerator PlayActivateAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, damageDuration),
                damagePunchScale * 1.25f,
                new Color(1f, 0.95f, 0.7f, 1f),
                TileShadowState.Active,
                false);
        }

        public virtual IEnumerator PlayTargetSelectionAsync()
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, targetPulseDuration),
                damagePunchScale * 1.5f,
                new Color(1f, 0.9f, 0.65f, 1f),
                TileShadowState.Active,
                true);
        }

        public virtual IEnumerator PlaySpecialCreateAsync(float speedMultiplier = 1f)
        {
            yield return PlayPulseTintAsync(
                Mathf.Max(0.02f, specialCreateDuration / speedMultiplier),
                damagePunchScale * 1.35f,
                new Color(1f, 0.98f, 0.8f, 1f),
                TileShadowState.Active,
                false);
        }

        public void SnapToLocalPosition(Vector3 localPosition)
        {
            KillMotionTweens();
            transform.localPosition = localPosition;
        }

        public virtual void SetIdleEnabled(bool isEnabled)
        {
            _isIdleEnabled = isEnabled;

            if (!_isIdleEnabled)
            {
                StopIdle();
                ApplyOpenSprite();
                return;
            }

            StartIdleLoop();
        }

        public virtual void SetShadowState(TileShadowState state)
        {
            _shadowState = state;
            if (shadowRenderer == null)
            {
                return;
            }

            SyncShadowSpriteIfNeeded();
            if (shadowRenderer.sprite == null)
            {
                return;
            }

            switch (state)
            {
                case TileShadowState.Preview:
                    shadowRenderer.enabled = true;
                    shadowRenderer.color = previewShadowColor;
                    break;
                case TileShadowState.Active:
                    shadowRenderer.enabled = true;
                    shadowRenderer.color = activeShadowColor;
                    break;
                default:
                    shadowRenderer.enabled = false;
                    shadowRenderer.color = _shadowBaseColor;
                    break;
            }
        }

        public virtual void SetSelectedScale(bool isSelected)
        {
            if (isSelected)
            {
                if (!_isScaledUp)
                {
                    _isScaledUp = true;
                    // Stop idle animations without clearing _isIdleEnabled flag
                    StopBlinkLoop();
                    KillPulseTween();

                    // Scale up tile slightly (1.12x of _initialScale)
                    transform.DOScale(_initialScale * 1.12f, 0.15f)
                        .SetEase(Ease.OutBack)
                        .SetLink(gameObject);

                    // Snappy excited punch rotation (head wobble)
                    transform.DOPunchRotation(new Vector3(0, 0, 15f), 0.25f, 6, 0.6f)
                        .SetLink(gameObject);

                    // Scale up shadow to the same level/factor as the tile (1.12x of its initial scale)
                    if (shadowRenderer != null)
                    {
                        shadowRenderer.transform.DOScale(_initialShadowScale * 1.12f, 0.15f)
                            .SetEase(Ease.OutBack)
                            .SetLink(shadowRenderer.gameObject);
                    }
                }
            }
            else
            {
                if (_isScaledUp)
                {
                    _isScaledUp = false;
                    // Scale tile back to normal
                    transform.DOScale(_initialScale, 0.15f)
                        .SetEase(Ease.OutQuad)
                        .SetLink(gameObject)
                        .OnComplete(() =>
                        {
                            // Resume idle if still enabled on this tile
                            if (_isIdleEnabled && !_isScaledUp)
                            {
                                StartIdleLoop();
                            }
                        });

                    // Scale shadow back to normal
                    if (shadowRenderer != null)
                    {
                        shadowRenderer.transform.DOScale(_initialShadowScale, 0.15f)
                            .SetEase(Ease.OutQuad)
                            .SetLink(shadowRenderer.gameObject);
                    }
                }
            }
        }

        public void ApplyDimmedState(bool isDimmed)
        {
            if (bodyRenderer == null)
            {
                return;
            }

            DOTween.Kill(bodyRenderer, false);
            if (isDimmed)
            {
                Color dimmedColor = new Color(_bodyBaseColor.r * 0.45f, _bodyBaseColor.g * 0.45f, _bodyBaseColor.b * 0.45f, _bodyBaseColor.a);
                bodyRenderer.DOColor(dimmedColor, 0.15f)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
            }
            else
            {
                bodyRenderer.DOColor(_bodyBaseColor, 0.15f)
                    .SetEase(Ease.OutQuad)
                    .SetLink(gameObject);
            }
        }

        public virtual IEnumerator PlayClearAsync(bool isExplosion = false)
        {
            return ExecuteBaseClearAsync(isExplosion);
        }

        protected IEnumerator ExecuteBaseClearAsync(bool isExplosion = false)
        {
            StopIdle();
            SetShadowState(TileShadowState.Off);
            KillMotionTweens();

            float duration = Mathf.Max(0.05f, clearDuration);

            if (isExplosion)
            {
                // Explosion reaction: blow away, fast spin, and burst with wider debris!
                SpawnDebrisParticles(10, duration * 2f, GetTileThemeColor(), null, null, 1.6f);

                Sequence sequence = DOTween.Sequence().SetLink(gameObject);
                
                // Snappy scale pop
                sequence.Append(transform.DOScale(_initialScale * 1.35f, duration * 0.25f).SetEase(Ease.OutBack));
                sequence.Append(transform.DOScale(Vector3.zero, duration * 0.75f).SetEase(Ease.InQuad));

                // Blow away diagonal movement
                Vector2 blastDirection = Random.insideUnitCircle.normalized;
                float blastDist = Random.Range(0.6f, 1.2f);
                Vector3 blastTarget = transform.localPosition + new Vector3(blastDirection.x * blastDist, blastDirection.y * blastDist, 0);
                sequence.Join(transform.DOLocalMove(blastTarget, duration).SetEase(Ease.OutQuad));

                // Rapid rotation spin
                float spinAngle = Random.Range(360f, 540f) * (Random.value > 0.5f ? 1f : -1f);
                sequence.Join(transform.DORotate(new Vector3(0, 0, spinAngle), duration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));

                // Flash white and fade
                if (bodyRenderer != null)
                {
                    sequence.Join(bodyRenderer.DOColor(Color.white, duration * 0.2f).SetEase(Ease.OutQuad));
                    sequence.Append(bodyRenderer.DOFade(0f, duration * 0.8f).SetEase(Ease.InQuad));
                }

                if (shadowRenderer != null)
                {
                    sequence.Join(shadowRenderer.DOFade(0f, duration * 0.6f));
                }

                yield return sequence.WaitForCompletion();
            }
            else
            {
                // Standard match clear
                SpawnDebrisParticles(5, duration * 1.6f, GetTileThemeColor());

                Sequence sequence = DOTween.Sequence().SetLink(gameObject);
                
                // Shrink from all sides directly (uniform scale down to zero)
                sequence.Append(transform.DOScale(_initialScale * 1.1f, duration * 0.2f).SetEase(Ease.OutQuad));
                sequence.Append(transform.DOScale(Vector3.zero, duration * 0.8f).SetEase(Ease.InBack));

                // Spin as it shrinks!
                sequence.Join(transform.DORotate(new Vector3(0, 0, Random.Range(-120f, 120f)), duration).SetEase(Ease.InQuad));

                if (bodyRenderer != null)
                {
                    sequence.Join(bodyRenderer.DOFade(0f, duration));
                }

                if (shadowRenderer != null)
                {
                    sequence.Join(shadowRenderer.DOFade(0f, duration * 0.8f));
                }

                yield return sequence.WaitForCompletion();
            }
        }

        public virtual IEnumerator PlayDamageAsync(int currentHp, int previousHp)
        {
            Color flashColor = previousHp > currentHp
                ? new Color(1f, 0.75f, 0.75f, 1f)
                : new Color(1f, 1f, 1f, 1f);

            // Squeeze Y, stretch X on damage
            transform.DOComplete();
            Sequence damageSeq = DOTween.Sequence().SetLink(gameObject);
            damageSeq.Append(transform.DOScale(new Vector3(_initialScale.x * 1.15f, _initialScale.y * 0.85f, _initialScale.z), damageDuration * 0.4f).SetEase(Ease.OutQuad));
            damageSeq.Append(transform.DOScale(_initialScale, damageDuration * 0.6f).SetEase(Ease.OutElastic));

            yield return PlayPulseTintAsync(
                Mathf.Max(0.05f, damageDuration),
                damagePunchScale,
                flashColor,
                TileShadowState.Active,
                true);
        }

        public virtual IEnumerator PlayMoveAsync(Vector3 targetLocalPosition, float duration)
        {
            StopIdle();
            SetShadowState(TileShadowState.Active);
            KillMotionTweens();

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlayTileSlide();
            }

            float safeDuration = Mathf.Max(0.05f, duration);

            // Lướt mượt với easing snappy - không scale để tránh tile tràn ra ngoài board
            yield return transform
                .DOLocalMove(targetLocalPosition, safeDuration)
                .SetEase(Ease.OutCubic)
                .SetLink(gameObject)
                .WaitForCompletion();
        }

        /// <summary>
        /// Ẩn tile ngay lập tức khi bắt đầu wrap-around (không tween ra ngoài board).
        /// </summary>
        public void HideForWrapExit()
        {
            StopIdle();
            KillMotionTweens();
            SetShadowState(TileShadowState.Off);

            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = false;
            }
        }

        /// <summary>
        /// Hiện tile ngay lập tức sau khi đã snap về vị trí đúng ở biên đối diện.
        /// </summary>
        public void ShowAfterWrapEntry()
        {
            RestoreBodyColor();
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = true;
            }
        }


        public virtual IEnumerator PlaySpawnFallAsync(Vector3 startLocalPosition, Vector3 targetLocalPosition, float duration)
        {
            StopIdle();
            KillMotionTweens();
            transform.localPosition = startLocalPosition;
            transform.localScale = _initialScale * 0.2f;
            SetShadowState(TileShadowState.Active);

            if (bodyRenderer != null)
            {
                bodyRenderer.color = new Color(_bodyBaseColor.r, _bodyBaseColor.g, _bodyBaseColor.b, 0f);
            }

            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.Join(transform.DOLocalMove(targetLocalPosition, Mathf.Max(0.05f, duration)).SetEase(Ease.OutQuad));
            sequence.Join(transform.DOScale(_initialScale, Mathf.Max(0.05f, duration)).SetEase(Ease.OutBack));

            if (bodyRenderer != null)
            {
                sequence.Join(bodyRenderer.DOFade(1f, Mathf.Max(0.05f, duration * 0.75f)));
            }

            yield return sequence.WaitForCompletion();
        }

        public virtual IEnumerator PlayLandAsync(float intensity = 1f, float speedMultiplier = 1f)
        {
            StopIdle();

            if (AudioController.Instance != null)
            {
                AudioController.Instance.PlayTileDrop();
            }
            if (VibrationManager.Instance != null)
            {
                VibrationManager.Instance.PlayLightImpact(requireEnabled: true);
            }

            float duration = Mathf.Max(0.05f, landingDuration / speedMultiplier);
            float clampedIntensity = Mathf.Clamp(intensity, 0.4f, 2.0f);

            // Sequential squash-and-stretch for organic cartoony jelly bounce
            Sequence landSeq = DOTween.Sequence().SetLink(gameObject);
            
            // 1. Quick squash: expand horizontally, shrink vertically
            Vector3 squashScale = new Vector3(_initialScale.x * (1f + landingPunchScale * 1.5f * clampedIntensity), _initialScale.y * (1f - landingPunchScale * 0.8f * clampedIntensity), _initialScale.z);
            landSeq.Append(transform.DOScale(squashScale, duration * 0.35f).SetEase(Ease.OutQuad));
            
            // 2. Rebound stretch: shrink horizontally, expand vertically
            Vector3 stretchScale = new Vector3(_initialScale.x * (1f - landingPunchScale * 0.8f * clampedIntensity), _initialScale.y * (1f + landingPunchScale * 1.2f * clampedIntensity), _initialScale.z);
            landSeq.Append(transform.DOScale(stretchScale, duration * 0.35f).SetEase(Ease.InOutSine));
            
            // 3. Settle back to normal
            landSeq.Append(transform.DOScale(_initialScale, duration * 0.3f).SetEase(Ease.OutQuad));

            yield return landSeq.WaitForCompletion();
            
            SetShadowState(_shadowState == TileShadowState.Preview ? TileShadowState.Preview : TileShadowState.Off);
        }

        protected IEnumerator PlayPulseTintAsync(
            float duration,
            float punchAmount,
            Color flashColor,
            TileShadowState shadowState = TileShadowState.Active,
            bool resetShadowOff = false,
            int vibrato = 4,
            float elasticity = 0.75f)
        {
            StopIdle();
            SetShadowState(shadowState);
            KillMotionTweens();

            Sequence sequence = DOTween.Sequence().SetLink(gameObject);
            sequence.Join(transform.DOPunchScale(Vector3.one * punchAmount, duration, vibrato, elasticity));

            if (bodyRenderer != null)
            {
                sequence.Join(bodyRenderer.DOColor(flashColor, duration * 0.5f).SetLoops(2, LoopType.Yoyo));
            }

            yield return sequence.WaitForCompletion();
            RestoreBodyColor();

            if (resetShadowOff)
            {
                SetShadowState(TileShadowState.Off);
            }
        }

        protected void ApplyBodyColor(Color color)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = color;
            }
        }

        protected void RestoreBodyColor()
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.color = _bodyBaseColor;
            }
        }

        protected void RestoreShadowColor()
        {
            if (shadowRenderer != null)
            {
                shadowRenderer.color = _shadowBaseColor;
            }
        }

        protected void StopIdleForFx()
        {
            StopIdle();
        }

        protected void KillMotionTweensForFx()
        {
            KillMotionTweens();
        }

        public void ForceEyesClosed(bool closed)
        {
            if (bodyRenderer == null)
            {
                return;
            }

            if (closed && ClosedSprite != null)
            {
                StopBlinkLoop();
                bodyRenderer.sprite = ClosedSprite;
                SyncShadowSpriteIfNeeded();
            }
            else
            {
                ApplyOpenSprite();
            }
        }

        protected void ApplyOpenSprite()
        {
            if (bodyRenderer == null)
            {
                return;
            }

            Sprite spriteToUse = OpenSprite;
            if (spriteToUse == null && useDefinitionIconAsFallback && _definition != null)
            {
                spriteToUse = _definition.Icon;
            }

            bodyRenderer.sprite = spriteToUse;
            SyncShadowSpriteIfNeeded();
        }

        public void ResetVisualState()
        {
            _isScaledUp = false;
            RestoreBodyColor();
            RestoreShadowColor();
            
            if (bodyRenderer != null)
            {
                bodyRenderer.enabled = true;
            }
            if (shadowRenderer != null)
            {
                shadowRenderer.enabled = true;
            }

            ApplyOpenSprite();
            transform.localRotation = _initialLocalRotation;
            transform.localScale = _initialScale;
            if (shadowRenderer != null)
            {
                shadowRenderer.transform.localScale = _initialShadowScale;
            }
            SetShadowState(TileShadowState.Off);
        }

        protected void StopIdle()
        {
            _isIdleEnabled = false;
            StopBlinkLoop();
            KillPulseTween();
        }

        protected void StartIdleLoop()
        {
            StopBlinkLoop();
            StartIdlePulse();

            if (!_isIdleEnabled || !EnableIdleBlink || bodyRenderer == null || ClosedSprite == null)
            {
                return;
            }

            _blinkRoutine = StartCoroutine(BlinkLoop());
        }

        protected void KillMotionTweens()
        {
            DOTween.Kill(transform, false);

            if (bodyRenderer != null)
            {
                DOTween.Kill(bodyRenderer, false);
            }

            if (shadowRenderer != null)
            {
                DOTween.Kill(shadowRenderer, false);
                DOTween.Kill(shadowRenderer.transform, false);
            }

            _pulseTween = null;
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            bool changed = false;
            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
                if (bodyRenderer != null)
                {
                    changed = true;
                }
            }
            if (shadowRenderer == null)
            {
                Transform shadowChild = transform.Find("Shadow");
                if (shadowChild != null)
                {
                    shadowRenderer = shadowChild.GetComponent<SpriteRenderer>();
                    if (shadowRenderer != null)
                    {
                        changed = true;
                    }
                }
            }

            if (changed && !UnityEditor.EditorApplication.isPlaying)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
                var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(prefabStage.scene);
                }
            }

            if (pulseScale < 1f)
            {
                pulseScale = 1f;
            }

            if (pulseDuration < 0.05f)
            {
                pulseDuration = 0.05f;
            }

            if (pulseLoopDelay < 0f)
            {
                pulseLoopDelay = 0f;
            }

            if (clearDuration < 0.05f)
            {
                clearDuration = 0.05f;
            }

            if (damageDuration < 0.05f)
            {
                damageDuration = 0.05f;
            }

            if (landingDuration < 0.05f)
            {
                landingDuration = 0.05f;
            }

            if (targetPulseDuration < 0.05f)
            {
                targetPulseDuration = 0.05f;
            }

            if (specialCreateDuration < 0.05f)
            {
                specialCreateDuration = 0.05f;
            }

            if (bodyRenderer == null)
            {
                bodyRenderer = GetComponent<SpriteRenderer>();
            }

            if (shadowRenderer != null)
            {
                shadowRenderer.enabled = false;
            }
        }
#endif

        private void StopBlinkLoop()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }
        }

        private IEnumerator BlinkLoop()
        {
            while (_isIdleEnabled && bodyRenderer != null)
            {
                float waitTime = GetNextBlinkDelay();
                if (waitTime > 0f)
                {
                    yield return new WaitForSeconds(waitTime);
                }

                if (!_isIdleEnabled || bodyRenderer == null)
                {
                    yield break;
                }

                bodyRenderer.sprite = ClosedSprite;
                SyncShadowSpriteIfNeeded();
                yield return new WaitForSeconds(Mathf.Max(0.02f, ClosedEyesDuration));

                if (bodyRenderer == null)
                {
                    yield break;
                }

                ApplyOpenSprite();
            }
        }

        private float GetNextBlinkDelay()
        {
            float min = Mathf.Max(0.25f, BlinkIntervalRange.x);
            float max = Mathf.Max(min, BlinkIntervalRange.y);
            return Random.Range(min, max);
        }

        private void StartIdlePulse()
        {
            KillPulseTween();
            transform.localScale = _initialScale;

            if (!_isIdleEnabled || !enableIdlePulse)
            {
                return;
            }

            float clampedScale = Mathf.Max(1f, pulseScale);
            float clampedDuration = Mathf.Max(0.05f, pulseDuration);
            float clampedDelay = Mathf.Max(0f, pulseLoopDelay);

            _pulseTween = transform
                .DOScale(_initialScale * clampedScale, clampedDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(Random.Range(0f, clampedDelay))
                .SetLink(gameObject);
        }

        private void KillPulseTween()
        {
            if (_pulseTween != null && _pulseTween.IsActive())
            {
                _pulseTween.Kill();
            }

            _pulseTween = null;
            transform.localScale = _initialScale;
        }

        private void KillAllTweens()
        {
            KillMotionTweens();
            KillPulseTween();
        }

        private void SyncShadowSpriteIfNeeded()
        {
            if (shadowRenderer == null || shadowRenderer.sprite != null || bodyRenderer == null)
            {
                return;
            }

            shadowRenderer.sprite = bodyRenderer.sprite;
        }

        private void CacheInitialTransformState()
        {
            _initialScale = transform.localScale;
            _initialLocalRotation = transform.localRotation;
        }
    }
}

