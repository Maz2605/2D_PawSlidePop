# Kiến trúc và Foundation

Chiều phụ thuộc: Game → Integrations/Base; Integrations không cần game; Base không tham chiếu Game hoặc SDK. Namespace chung là `MazFramework`. Runtime chưa có asmdef tổng; Editor tools nằm trong thư mục Editor.

| Module | Namespace | Trách nhiệm |
|---|---|---|
| Foundation | MazFramework.Foundation.* | Singleton, Events, Pooling, Factory |
| Bootstrap | MazFramework.Bootstrap | IAppSystem và chuỗi startup đồng bộ |
| Audio/Haptics/Settings | MazFramework.Audio / Haptics / Settings | Phát feedback và lưu preferences |
| UI | MazFramework.UI | Transition, cache, popup stack, loading, toast |
| SceneManagement | MazFramework.SceneManagement | Load scene, ILoadingOverlay |
| Persistence | MazFramework.Persistence | Local JSON và backup |
| Device | MazFramework.Device | FPS, VSync, screen awake, tween capacity |

`EventManager<T>` dùng enum trực tiếp làm key (kể cả nền long). Mỗi ID có listener chỉ nhận một signature: Add/Remove/Post sai kiểu log expected/received, không đổi listener hoặc dispatch. Callback null bị bỏ qua; đăng ký trùng giữ multicast, một Remove gỡ một đăng ký. Post dùng snapshot delegate theo thứ tự; exception subscriber truyền về caller. Khi listener cuối bị gỡ có thể dùng signature khác. Publisher/subscriber phải RemoveListener cùng callback khi hết lifetime; base không chứa enum gameplay. Factory chỉ là contract, cách sinh tile thuộc game.

PoolingManager sở hữu pool theo prefab và mọi instance được cấp, kể cả pool đã retired. Spawn đặt parent/transform trước activate; hỗ trợ một IPoolable trên root. Despawn phát OnDespawn một lần rồi disable/cache. Release lặp bị cảnh báo; exception lifecycle được log nhưng cleanup vẫn chạy, Spawn lỗi trả null. Prewarm chỉ tạo inactive, không phát callback lifecycle, đảm bảo count instance rảnh (giới hạn maxSize) và không tạo dư khi gọi lặp. defaultCapacity là capacity collection; maxSize giới hạn cache rảnh, không giới hạn số object active.

ClearPools hủy cache rảnh và retire pool; active vẫn sống nhưng bị hủy khi trả về. Spawn sau clear tạo pool mới. Destroy manager thu hồi cả active của pool retired, phát OnDespawn một lần. Cache bị hủy ngoài manager được bỏ qua khi spawn/prewarm. PooledItem.Pool là API legacy obsolete, không được reassign ownership của item đã bind. Consumer nên trả object trước unload scene; teardown dùng ExistingInstance để không tạo manager mới.

Với item legacy do IObjectPool bên ngoài quản lý (không bind vào PoolingManager), Release tiếp tục forward tới pool đó qua mọi lượt reuse; pool ngoài sở hữu kiểm tra double-return. Item managed có số phiên rental để callback cũ không trả/hủy object vừa được callback lồng nhau spawn lại.

Singleton mặc định tự tạo trong Play Mode, không tạo ngoài Play hoặc khi quit. Service cần Inspector reference dùng [SingletonPrefab]: AudioManager/HapticManager phải được startup tạo từ prefab; Instance thiếu owner log lỗi và trả null. Policy dùng attribute để đọc được trước khi có object. ExistingInstance chỉ đọc owner đã đăng ký, không tìm scene/tạo object. Lookup chưa có owner nhưng thấy nhiều candidate sẽ log lỗi và trả null. Chỉ root được DontDestroyOnLoad, không tự reparent hierarchy. Readiness vẫn do IAppSystem.Initialize, không do truy cập singleton.

Singleton và EventManager độc lập, mỗi module có hook non-generic SubsystemRegistration ngay trong file của mình (SingletonRuntimeState và EventManagerRuntimeState), không dùng registry reset chung. Mỗi closed generic đăng ký reset một lần với module tương ứng; đầu Play session EventManager xóa listener, Singleton xóa reference owner, quit flag và phục hồi DontDestroyOnLoadEnabled=true. Không clear event khi đổi scene, không phụ thuộc editor helper game. Hỗ trợ Domain Reload bật/tắt với Scene Reload bật; chưa hỗ trợ tắt cả hai. API dùng trên Unity main thread.

Nguồn: [Foundation](../Foundation/Script), [UIManager](../UI/Script/Base/UIManager.cs). Các tiện ích Foundation không đồng nghĩa đã benchmark hoặc phù hợp mọi cách dùng.
