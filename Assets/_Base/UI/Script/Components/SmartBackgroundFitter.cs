using UnityEngine;

// Attribute này cho phép script chạy cả trong Edit Mode lẫn Play Mode
[ExecuteAlways] 
[RequireComponent(typeof(SpriteRenderer))]
public class SmartBackgroundFitter : MonoBehaviour
{
    [Header("Background Assets")]
    [SerializeField] private Sprite portraitSprite;
    [SerializeField] private Sprite landscapeSprite;

    private SpriteRenderer _spriteRenderer;
    private Camera _mainCamera;

    private int _lastScreenWidth = -1;
    private int _lastScreenHeight = -1;

    void Awake()
    {
        InitComponents();
    }

    // Tách riêng hàm Init để gọi lại an toàn trong Edit Mode
    private void InitComponents()
    {
        if (_spriteRenderer == null) _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_mainCamera == null) _mainCamera = Camera.main;
    }

    void Start()
    {
        UpdateBackgroundState();
    }

    void Update()
    {
        // Trong Play Mode: Chạy mỗi frame cực nhẹ nhàng nhờ so sánh int.
        // Trong Edit Mode: Unity sẽ tự động giảm tần suất gọi Update, 
        // nó chỉ chạy khi Scene View có sự thay đổi (tiết kiệm tài nguyên máy tính).
        if (Screen.width != _lastScreenWidth || Screen.height != _lastScreenHeight)
        {
            UpdateBackgroundState();
        }
    }

#if UNITY_EDITOR
    // Hàm này được Unity gọi mỗi khi có một giá trị nào đó bị thay đổi trên cửa sổ Inspector
    void OnValidate()
    {
        // Sử dụng delayCall để tránh lỗi Unity báo cảnh báo (warning) 
        // khi cố gắng thay đổi component trong lúc Unity đang vẽ Inspector
        UnityEditor.EditorApplication.delayCall += () => 
        {
            if (this == null) return; // Tránh lỗi khi object bị xóa
            InitComponents();
            UpdateBackgroundState();
        };
    }
#endif

    private void UpdateBackgroundState()
    {
        InitComponents(); // Safety check cho Edit Mode
        if (_mainCamera == null || _spriteRenderer == null) return;

        _lastScreenWidth = Screen.width;
        _lastScreenHeight = Screen.height;

        bool isLandscape = _lastScreenWidth > _lastScreenHeight;
        Sprite targetSprite = isLandscape ? landscapeSprite : portraitSprite;
        
        if (_spriteRenderer.sprite != targetSprite)
        {
            _spriteRenderer.sprite = targetSprite;
        }

        FitToScreen();
    }

    private void FitToScreen()
    {
        if (_spriteRenderer.sprite == null) return;

        // Reset scale về 1 để tính toán chuẩn xác
        transform.localScale = Vector3.one;

        float cameraHeight = _mainCamera.orthographicSize * 2f;
        float cameraWidth = cameraHeight * _mainCamera.aspect;

        float spriteWidth = _spriteRenderer.sprite.bounds.size.x;
        float spriteHeight = _spriteRenderer.sprite.bounds.size.y;

        float scaleX = cameraWidth / spriteWidth;
        float scaleY = cameraHeight / spriteHeight;

        // Cover mode
        float coverScale = Mathf.Max(scaleX, scaleY);
        transform.localScale = new Vector3(coverScale, coverScale, 1f);
    }
}