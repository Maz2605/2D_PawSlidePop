using UnityEngine;

namespace _PawSlidePopGame._Scripts.Feature.Match3.View
{
    public class NormalTileView : Match3TileView
    {
        [Header("Normal Sprites")]
        [SerializeField] private Sprite openSprite;
        [SerializeField] private Sprite closedSprite;

        [Header("Idle Blink")]
        [SerializeField] private bool enableIdleBlink = true;
        [SerializeField] private Vector2 blinkIntervalRange = new Vector2(2.5f, 5f);
        [SerializeField] private float closedEyesDuration = 0.08f;

        protected override bool EnableIdleBlink => enableIdleBlink;
        protected override Vector2 BlinkIntervalRange => blinkIntervalRange;
        protected override float ClosedEyesDuration => closedEyesDuration;
        protected override Sprite OpenSprite => openSprite;
        protected override Sprite ClosedSprite => closedSprite;

#if UNITY_EDITOR
        protected override void OnValidate()
        {
            base.OnValidate();

            if (blinkIntervalRange.x < 0.25f)
            {
                blinkIntervalRange.x = 0.25f;
            }

            if (blinkIntervalRange.y < blinkIntervalRange.x)
            {
                blinkIntervalRange.y = blinkIntervalRange.x;
            }

            if (closedEyesDuration < 0.02f)
            {
                closedEyesDuration = 0.02f;
            }
        }
#endif
    }
}

