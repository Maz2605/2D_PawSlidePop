# UI và overlay

UIManager cần screenRoot/popupRoot là hai Canvas riêng dưới manager, uiCamera là URP Overlay camera thuộc root và layer UI. TopRoot là Canvas thứ ba khi có overlay. Inspector giữ reference rõ ràng; Initialize gán layer, worldCamera và planeDistance=100 cho các root. Prefab Base không có overlay vẫn initialize được. UIManager kế thừa Singleton<UIManager> của Foundation, có [SingletonPrefab], mặc định giữ root qua scene bằng Base Singleton; không tạo EventSystem. Prefab cần nằm ở scene root để Base áp dụng DontDestroyOnLoad. Initialize vẫn phải được startup gọi sau khi instantiate prefab có đủ config.

Game gọi BindSceneCamera(Camera baseCamera) với đúng URP Base camera sau initialize/scene load. Bind idempotent, chỉ gỡ entry do manager thêm khi unbind, đổi camera hoặc destroy; entry authored có sẵn được giữ. Truyền null để unbind. Không tìm UICamera theo tên hoặc tự nối vào mọi Base camera. Duplicate UI root được disable trước destroy, UI con được disable khi quit.

ShowScreen<T>(setup, onOpened) và ShowPopup<T>(setup, onOpened) tạo/cache instance theo type. Prefab cần root component tương ứng, tên bằng typeof(T).Name và duy nhất trong `Resources/UI/Screens` hoặc `Resources/UI/Popups`, kể cả thư mục con. Không trộn prefab không phải screen/popup với type được yêu cầu.

CloseTopPopup, ClosePopup<T>, IsPopupVisible<T>, ClearAllPopups, HideCurrentScreen cung cấp điều khiển flow. Popup Hidden cập nhật stack cả khi popup tự đóng. Manager gỡ subscription khi destroy.

BaseScreen/BasePopup sở hữu transition tween, IsVisible và IsTransitioning. Override CreateShowTween/CreateHideTween và trả tween chưa complete; base đặt callback hoàn tất. Side effect ở cuối custom Sequence dùng AppendCallback, tránh ghi đè OnComplete của base. Custom Sequence giữ thời gian linear để không làm biến dạng ease của animation con; fade song song nội dung nên Insert ở thời điểm 0. Field ease của base dùng prefix transition để không trùng field serialize của lớp con. Khi override OnDisable/OnDestroy phải gọi base. Hook OnBeforeShow/OnShown/OnBeforeHide/OnHidden dành cho game; popup phát Showing/Hiding để component feedback tùy chọn subscribe; Base không chọn âm thanh nghiệp vụ. Callback đóng có thể mở lại cùng instance; base kiểm version để không disable instance vừa mở.

Initialize cũng đăng ký các screen/popup đã được đặt sẵn dưới roots và đưa về trạng thái hidden. Game không cần reflection để đăng ký cache; lần Show tiếp theo dùng lại đúng instance đã đặt sẵn.

Overlay kế thừa BaseOverlay, tách khỏi BasePopup và popup stack. UIManager chỉ giữ topRoot; prefab được tìm lazy trong Resources/UI/Overlays (kể cả thư mục con), tên duy nhất bằng typeof(T).Name và có T trên root. Không kéo LoadingScreen/ToastNotification prefab vào Inspector. Game giữ prefab trong Content/Shared/Resources/UI/Overlays; script/tween/lifetime dùng chung nằm ở Base.

ShowOverlay<T>(float duration = -1f, Action<T> setup = null, Action onOpened = null), với T : BaseOverlay, là API riêng. LoadingScreen được dành cho BeginLoading; gọi ShowOverlay/CloseOverlay trực tiếp với LoadingScreen báo lỗi và không điều khiển view loading. Duration là thời gian giữ sau animation mở; giá trị âm giữ đến khi gọi CloseOverlay<T>(), 0 bắt đầu đóng sau khi mở, số dương tự đóng theo unscaled time. NaN/Infinity bị từ chối. Gọi lại cùng type đang sống dùng lại instance, cập nhật setup và hủy timer/transition cũ để bắt đầu lượt hiển thị mới. Đóng xong animation: BaseOverlay phát Closed, UIManager xóa active entry, rồi instance bị Destroy; lần show sau tạo instance mới. Prefab registry vẫn được cache, không unload asset theo từng lần đóng.

BaseOverlay sở hữu transition/timer và chống callback cũ bằng version. Khi mở xong, timer được tạo trước OnShown/onOpened; mỗi callback được bắt exception riêng và log với owner. Callback đóng/disable/show lại view sẽ vô hiệu hóa continuation cũ; lỗi callback không làm mất timer; subclass override CreateShowTween/CreateHideTween, OnBeforeShow/OnShown/OnBeforeHide để giữ animation riêng. Trả tween chưa complete, không tự gắn OnComplete; sequence custom cần linear ease. Override OnDisable/OnDestroy phải gọi base. Overlay không vào popup stack; CloseTopPopup/ClearAllPopups không đóng overlay.

BeginLoading(Action onCovered = null) trả LoadingHandle (IDisposable), thay hoàn toàn cặp API loading cũ. Mỗi tác vụ giữ handle riêng, Dispose trong using/finally trên Unity main thread. Dispose lặp an toàn; request được quản lý theo ID. Chỉ request cuối được release mới đóng LoadingScreen và vẫn tôn trọng minShowTime. Request mới khi đang opening không chạy lại animation; khi covered được callback ngay; khi closing sẽ hủy closing và mở lại.

Callback cover của request còn sống được gọi tối đa một lần; request Dispose trước cover không nhận callback. Snapshot kiểm tra identity/notified/canceled trước từng callback, đánh dấu trước khi gọi và bắt exception riêng, nên callback có thể acquire/release request khác. UI thiếu root/chưa ready hoặc prefab không tạo được: log lỗi, callback fallback một lần và trả handle no-op. Disable/destroy UIManager hủy request, callback chờ và dọn view loading ngay; handle cũ không tác động lượt mới hoặc manager khác. SceneLoader nhận UIManager qua ILoadingOverlay.BeginLoading và giữ handle của provider đã cấp.

ShowToast(message,duration) giữ API convenience và defaultStayDuration từ prefab. Popup đăng nhập của game gắn loading với lượt mở view, không giữ request loading sau khi view bắt đầu đóng.

```csharp
using (var loading = UIManager.Instance.BeginLoading())
{
    // await công việc trên Unity main thread; finally/using giải phóng request của chính tác vụ.
}
```

Nguồn: [LoadingHandle](../SceneManagement/Script/LoadingHandle.cs), [BaseOverlay](../UI/Script/Base/BaseOverlay.cs), [BasePopup](../UI/Script/Base/BasePopup.cs), [BaseScreen](../UI/Script/Base/BaseScreen.cs), [UIManager](../UI/Script/Base/UIManager.cs).

## SafeArea

Dùng duy nhất `MazFramework.UI.SafeArea` trên container content stretch, offset bằng 0, dưới parent full screen không xoay. Background/lớp phủ che màn hình là sibling bên ngoài container; padding đặt ở content con. Không lồng hai SafeArea. Không gắn trực tiếp vào panel có offset/padding riêng.

Component chạy cả Editor, theo dõi `Screen.safeArea`, kích thước và orientation; refresh khi focus trở lại. `Refresh()` yêu cầu đọc lại, nhưng RectTransform chỉ ghi khi kết quả thay đổi. `TryCalculateAnchors(Rect, Vector2, out Vector2 min, out Vector2 max)` clamp anchor về 0..1 và trả false cho NaN/Infinity, kích thước không dương hoặc vùng không giao màn hình. Input lỗi giữ layout hợp lệ gần nhất. SafeAreaFitter cũ đã bỏ.

## UIButtonVisual và listener nghiệp vụ

Gắn `UIButtonVisual` trên cùng GameObject với `Button`. Inspector chọn effectTarget (mặc định Button), bật punch, strength, duration, vibrato, clickClip override, playAudio, volumeScale và Selection haptics. Khi không có override, lấy Config.buttonClick từ AudioManager. Không có enum âm thanh hay callback game trong Base. `Configure(...)` dùng cho nút tạo runtime.

Feedback dùng `Button.onClick`, nên click và Submit đi chung một đường. Không thêm IPointerClickHandler. Tween unscaled, mỗi click thu hồi tween click trước, lấy resting scale hiện tại và bắt đầu lại; disable/destroy gỡ đúng listener và khôi phục scale, không ép Vector3.one hoặc kill tween của owner khác. Với entry/pulse riêng, view phải quản lý tween đó; tránh hai owner đồng thời ghi scale của cùng target (chọn target con nếu cần).

Screen/Popup kéo reference Button, đăng ký handler có tên sau khi view active, ví dụ:

```csharp
[SerializeField] private Button closeButton;
protected override void OnBeforeShow()
{
    closeButton.onClick.RemoveListener(HandleClose);
    closeButton.onClick.AddListener(HandleClose);
}
private void HandleClose() => Hide();
protected override void OnDisable()
{
    closeButton.onClick.RemoveListener(HandleClose);
    base.OnDisable();
}
```

UIButtonVisual đăng ký ở OnEnable trước handler view. Với nút được bật muộn, owner bật GameObject rồi RemoveListener/AddListener handler nghiệp vụ để giữ thứ tự. Không RemoveAllListeners, không gắn action vào tween completion. Khi click đóng UI ngay, feedback đã phát và nghiệp vụ không phải chờ animation.

BaseScreen/BasePopup có `Hide()` và `Hide(Action onHidden)` để dùng method group/UnityEvent. BaseSubScreen cung cấp Init/Show/Hide cho tab do screen sở hữu; không bind Button và không vào stack top-level. Chính sách modal được mô tả bên dưới.

## Navigation input và Back

UIManager quản lý input trực tiếp trong UIManager.cs. Popup stack là nguồn thứ tự duy nhất; không sắp xếp lại popup từ cache. BaseScreen/BasePopup có IsPresented (bao gồm closing), PresentationStateChanged và SetNavigationInputAllowed; quyền hiệu lực là visible, mở xong và được navigation cho phép. Không tự đổi Button.interactable của nghiệp vụ. Không recompute bằng LINQ mỗi frame; cập nhật theo lifecycle event/API.

Chỉ popup trên cùng nhận input khi stable. Full-screen raycast barrier nằm dưới popup stable và phủ trên popup đang transition; popup bị disable giữa closing vẫn release IsPresented dù custom code đã kill tween. Chỉ dùng một Image trong suốt làm raycast blocker, chuyển giữa Popup Root và Top Root khi loading. UIManager chỉ xóa selection đã inactive hoặc không interactable; không lưu/khôi phục focus hoặc tự chọn nút đầu tiên. Keyboard Submit vẫn bị CanvasGroup chặn ở lớp dưới; muốn tiếp tục điều hướng bàn phím cần chọn lại control hợp lệ.

IsGameplayInputBlocked và GameplayInputBlockedChanged thông báo cho game khóa board độc lập với gameplay state. Loading giữ khóa đến hết closing; request handle cuối mới bắt đầu đóng. Toast không tham gia khóa. Barrier được tạo runtime, roots/camera prefab giữ wiring cũ.

TryHandleBack() consume khi loading, transition hoặc popup hiện diện. BasePopup.TryHandleBack() cho phép override nghiệp vụ; CloseOnBack=false chặn truyền request xuống dưới. UIBackInput có một InputAction Escape, bỏ dispatch trùng trong cùng frame; UnhandledBack dành cho game, Base không biết Pause/Settings/Win/Lose. Input System cần có trong project đích. Android system Back phải kiểm tra trên thiết bị, không suy từ Editor Escape.
