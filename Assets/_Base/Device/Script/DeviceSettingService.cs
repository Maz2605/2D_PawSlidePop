using _PawSlidePopGame._Scripts.Core.Boostrap;
using DG.Tweening;
using UnityEngine;

namespace _PawSlidePopGame._Scripts.Core.System
{
    public class DeviceSettingService : MonoBehaviour, IAppService
    {
        [Header("Performance Settings")]
        [SerializeField] private int targetFPS = 60;
        [SerializeField] private bool disableVSync = true;

        [Header("Screen Settings")]
        [SerializeField] private bool keepScreenAwake = true;

        private void Awake()
        {
            DOTween.SetTweensCapacity(2000, 500);
            Debug.Log("[DeviceSettingService] Awake: DOTween Capacity set to 2000/500");
        }

        public void Init()
        {
            if (disableVSync)
            {
                QualitySettings.vSyncCount = 0;
            }
            Application.targetFrameRate = targetFPS;

            if (keepScreenAwake)
            {
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
            
            InitDOTween();
            SetTargetFPS(targetFPS);

            Debug.Log($"[DeviceSettingService] Initialized: Target FPS = {targetFPS}, VSync = {!disableVSync}");
        }
        
        public void SetTargetFPS(int fps)
        {
            Application.targetFrameRate = fps;
            Debug.Log($"[DeviceSettingService] Changed FPS to {fps}");
        }
        
        private void InitDOTween()
        {
            DOTween.SetTweensCapacity(2000, 500);
            DOTween.logBehaviour = LogBehaviour.ErrorsOnly; 
            DOTween.defaultAutoKill = true; 
            DOTween.defaultRecyclable = false; 
            
            Debug.Log($"[DeviceSettingService] DOTween Initialized. Capacity: 2000/500");
        }
    }
}