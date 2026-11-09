using System;
using System.Collections.Generic;
using _PawSlidePopGame._Scripts.Core.System.DesignPattern.Singleton;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace _PawSlidePopGame.Input
{
    public class InputManager : Singleton<InputManager>
    {
        public event Action<Vector2> OnTouchMove;
        public event Action<Vector2> OnTouchEnd;
        public event Action<Vector2> OnTouchStart;

        private GameInput _inputActions;
        private Camera _mainCamera;
        private bool _isDragging;

        private List<RaycastResult> _raycastResults = new List<RaycastResult>();
        private PointerEventData _pointerEventData;

        private Camera MainCamera
        {
            get
            {
                if (_mainCamera == null) _mainCamera = Camera.main;
                return _mainCamera;
            }
        }

        protected override void Awake()
        {
            base.Awake(); 
            
            if (_inputActions == null)
            {
                _inputActions = new GameInput();
            }
        }

        private void OnEnable()
        {
            if (_inputActions == null) return;

            _inputActions.Enable();
            _inputActions.Touch.TouchContact.started += OnTouchPress;
            _inputActions.Touch.TouchContact.canceled += OnTouchCancel;
            _inputActions.Touch.TouchPosition.performed += OnTouchPosition;
        }

        private void OnDisable()
        {
            if (_inputActions == null) return;

            _inputActions.Touch.TouchContact.started -= OnTouchPress;
            _inputActions.Touch.TouchContact.canceled -= OnTouchCancel;
            _inputActions.Touch.TouchPosition.performed -= OnTouchPosition;
            _inputActions.Disable();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy(); 
            _inputActions?.Dispose();
        }

        private void OnTouchPress(InputAction.CallbackContext ctx)
        {
            if (IsPointerOverUI()) return;
            
            _isDragging = true;
            OnTouchStart?.Invoke(ReadTouchPosition());
        }

        private void OnTouchCancel(InputAction.CallbackContext ctx)
        {
            if (_isDragging)
            {
                _isDragging = false;
                OnTouchEnd?.Invoke(ReadTouchPosition());
            }
        }

        private void OnTouchPosition(InputAction.CallbackContext ctx)
        {
            if (_isDragging)
            {
                OnTouchMove?.Invoke(ctx.ReadValue<Vector2>());
            }
        }

        private Vector2 ReadTouchPosition()
        {
            return _inputActions != null ? _inputActions.Touch.TouchPosition.ReadValue<Vector2>() : Vector2.zero;
        }

        public Vector3 GetWorldPosition()
        {
            if (MainCamera == null) return Vector3.zero;

            Vector2 screenPos = ReadTouchPosition();
            Vector3 worldPos = MainCamera.ScreenToWorldPoint(screenPos);
            worldPos.z = 0;
            return worldPos;
        }

        /// <summary>
        /// Kiểm tra xem người dùng có đang chạm vào UI không.
        /// Được tối ưu để không tạo Garbage Collector mỗi frame.
        /// </summary>
        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;

            Vector2 touchPos = ReadTouchPosition();

            if (_pointerEventData == null)
            {
                _pointerEventData = new PointerEventData(EventSystem.current);
            }
            
            _pointerEventData.position = touchPos;
            _raycastResults.Clear();

            EventSystem.current.RaycastAll(_pointerEventData, _raycastResults);

            return _raycastResults.Count > 0;
        }
    }
}