using System;
using System.Collections;
using UnityEngine.Networking;
using _PawSlidePopGame._Scripts.Data.Events;
using _PawSlidePopGame._Scripts.Gameplay.Meta.EconomyManager;
using MazFramework.Foundation.Events;
using UnityEngine;
using UnityEngine.UI;

#if FIREBASE_AUTH_ENABLED
using _PawSlidePopGame._Scripts.Services.Auth;
#endif

namespace _PawSlidePopGame._Scripts.UI.Components
{
    /// <summary>
    /// Displays the TopBar avatar.
    /// Updates the avatar sprite; the owning screen handles button actions.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ProfileAvatarView : MonoBehaviour
    {
        [Header("--- Avatar Image ---")]
        [SerializeField] private Image imgAvatar;
        [SerializeField] private Sprite[] avatarSprites;

        private void Awake()
        {
            if (imgAvatar == null)
            {
                imgAvatar = GetComponentInChildren<Image>();
            }
        }

        private void OnEnable()
        {

            PlayerEconomyRepository.OnDataChanged += RefreshAvatar;
            RefreshAvatar();
        }

        private void OnDisable()
        {
            PlayerEconomyRepository.OnDataChanged -= RefreshAvatar;
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
                    Debug.LogWarning($"[ProfileAvatarView] Failed to download profile photo: {webRequest.error}");
                }
            }
            _downloadCoroutine = null;
        }
    }
}
