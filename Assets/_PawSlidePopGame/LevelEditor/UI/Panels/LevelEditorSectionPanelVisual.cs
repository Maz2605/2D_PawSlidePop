using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.Feature.LevelEditor.UI.Panels
{
    public sealed class LevelEditorSectionPanelVisual : MonoBehaviour
    {
        [SerializeField] private RectTransform sectionRoot;
        [SerializeField] private Button sectionToggleButton;
        [SerializeField] private TMP_Text sectionTitleText;
        [SerializeField] private TMP_Text subTitleText;
        [SerializeField] private TMP_Text shortcutKeyText;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private LevelEditorItemTileButton itemTemplate;
        [SerializeField] private RectTransform animatedBody;

        public RectTransform SectionRoot => sectionRoot;
        public Button SectionToggleButton => sectionToggleButton;
        public TMP_Text SectionTitleText => sectionTitleText;
        public TMP_Text SubTitleText => subTitleText;
        public TMP_Text ShortcutKeyText => shortcutKeyText;
        public ScrollRect ScrollRect => scrollRect;
        public RectTransform ContentRoot => contentRoot;
        public LevelEditorItemTileButton ItemTemplate => itemTemplate;
        public RectTransform AnimatedBody => animatedBody;
    }
}
