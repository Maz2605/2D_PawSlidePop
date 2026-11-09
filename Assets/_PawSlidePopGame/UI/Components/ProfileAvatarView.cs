using System;
using System.Collections;
using UnityEngine.Networking;
using _PawSlidePopGame._Scripts.Data.Audio;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.UI.Manager;
using _PawSlidePopGame._Scripts.UI.Popups;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using DG.Tweening;
using _PawSlidePopGame.Scripts.DesignPattern.ObserverPattern;
using UnityEngine;
using UnityEngine.UI;

#if FIREBASE_AUTH_ENABLED
using _PawSlidePopGame._Scripts.Services.Auth;
#endif

namespace _PawSlidePopGame._Scripts.UI.Components
{
    /// <summary>
    /// Attached to the TopBar Avatar button to open ProfilePopup.
    /// Manages punch-scale animation and updates the avatar sprite dynamically.
    /// </summary>
    [RequireComponent(typeof(Button))]
    [DisallowMultipleComponent]
    public sealed class ProfileButtonController : MonoBehaviour
    {
        [Header("--- Avatar Image ---")]
        [SerializeField] private Image imgAvatar;
        [SerializeField] private Sprite[] avatarSprites;

        [Header("--- Animation ---")]
        [SerializeField] private float punchScale = 0.12f;
        [SerializeField] private float punchDuration = 0.18f;
        [SerializeField] private int punchVibrato = 6;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (imgAvatar == null)
            {
                imgAvatar = GetComponentInChildren<Image>();
            }
        }

        private void OnEnable()
        {
            _button.onClick.RemoveListener(HandleClicked);
            _button.onClick.AddListener(HandleClicked);

            PlayerEconomyRepository.OnDataChanged += RefreshAvatar;
            RefreshAvatar();
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(HandleClicked);
            PlayerEconomyRepository.OnDataChanged -= RefreshAvatar;
        }

        private void HandleClicked()
        {
            if (!TryGetComponent<UIButtonSound>(out var _))
            {
                EventManager<FeedbackEvent>.Post(FeedbackEvent.UiButtonTap);
            }

            if (UIManager.Instance == null)
            {
                Debug.LogWarning("[ProfileButtonController] UIManager.Instance is null.", this);
                return;
            }

            transform.DOKill();
            transform.localScale = Vector3.one;
            transform
                .DOPunchScale(Vector3.one * -punchScale, punchDuration, punchVibrato)
                .SetUpdate(true)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(OpenProfilePopup);
        }

        private void OpenProfilePopup()
        {
            UIManager.Instance.ShowPopup<ProfilePopup>();
        }

        private Coroutine _downloadCoroutine;

        public void RefreshAvatar()
        {
            if (imgAvatar == null) return;
            if (PlayerEconomyRepository.Instance == null || PlayerEconomyRepository.Instance.Data == null) return;

            bool hasCustomPhoto = false;
            var economyData = PlayerEconomyRepository.Instance.Data;

#if FIREBASE_AUTH_ENABLED
            if (economyData.useSocialAvatar && FirebaseAuthService.Instance != null && FirebaseAuthService.Instance.IsLoggedIn)
            {
                var user = FirebaseAuthService.Instance.CurrentUser;
                if (user != null && user.PhotoUrl != null && !user.IsAnonymous)
                {
                    string photoUrl = user.PhotoUrl.ToString();
                    if (!string.IsNullOrEmpty(photoUrl))
                    {
                        hasCustomPhoto = true;
                        if (_downloadCoroutine != null)
                        {
                            StopCoroutine(_downloadCoroutine);
                        }
                        _downloadCoroutine = StartCoroutine(DownloadAvatarCoroutine(photoUrl));
                    }
                }
            }
#endif

            if (!hasCustomPhoto)
            {
                if (_downloadCoroutine != null)
                {
                    StopCoroutine(_downloadCoroutine);
                    _downloadCoroutine = null;
                }

                if (avatarSprites == null || avatarSprites.Length == 0) return;
                int index = economyData.avatarIndex;
                if (index >= 0 && index < avatarSprites.Length)
                {
                    imgAvatar.sprite = avatarSprites[index];
                }
            }
        }

        private IEnumerator DownloadAvatarCoroutine(string url)
        {
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url))
            {
                yield return webRequest.SendWebRequest();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                    if (texture != null && imgAvatar != null)
                    {
                        Sprite customSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        imgAvatar.sprite = customSprite;
                    }
                }
                else
                {
                    Debug.LogWarning($"[ProfileButtonController] Failed to download profile photo: {webRequest.error}");
                }
            }
            _downloadCoroutine = null;
        }
    }
}
