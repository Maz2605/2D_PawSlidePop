using System;
using UnityEngine;
using UnityEngine.UI;

namespace _PawSlidePopGame._Scripts.UI.Components
{
    [RequireComponent(typeof(Button))]
    public class InventoryAddButton : MonoBehaviour
    {
        private Button _button;
        private Action _onClickAction;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        public void Init(Action onClickAction)
        {
            _onClickAction = onClickAction;
            _button.onClick.RemoveListener(HandleClicked);
            _button.onClick.AddListener(HandleClicked);
        }

        private void HandleClicked()
        {
            _onClickAction?.Invoke();
        }
        private void OnDisable()
        {
            if (_button != null) _button.onClick.RemoveListener(HandleClicked);
        }
        private void OnEnable()
        {
            if (_button == null) _button = GetComponent<Button>();
            if (_button != null) { _button.onClick.RemoveListener(HandleClicked); _button.onClick.AddListener(HandleClicked); }
        }
    }
}
