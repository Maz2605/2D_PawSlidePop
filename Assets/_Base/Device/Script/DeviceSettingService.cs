using MazFramework.Bootstrap;
using DG.Tweening;
using UnityEngine;
namespace MazFramework.Device
{
    public sealed class DeviceSettingService : MonoBehaviour,IAppSystem
    {
        [SerializeField] private int targetFPS=60;
        [SerializeField] private bool disableVSync=true;
        [SerializeField] private bool keepScreenAwake=true;
        [SerializeField] private int tweenerCapacity=2000;
        [SerializeField] private int sequenceCapacity=500;
        public bool IsInitialized { get; private set; }
        public bool Initialize()
        {
            if(IsInitialized) return true;
            if(disableVSync) QualitySettings.vSyncCount=0;
            SetTargetFPS(targetFPS);
            if(keepScreenAwake) Screen.sleepTimeout=SleepTimeout.NeverSleep;
            DOTween.SetTweensCapacity(Mathf.Max(1,tweenerCapacity),Mathf.Max(1,sequenceCapacity));
            DOTween.logBehaviour=LogBehaviour.ErrorsOnly;
            DOTween.defaultAutoKill=true;
            DOTween.defaultRecyclable=false;
            IsInitialized=true; return true;
        }
        public void SetTargetFPS(int fps) { targetFPS=Mathf.Max(1,fps); Application.targetFrameRate=targetFPS; }
    }
}
