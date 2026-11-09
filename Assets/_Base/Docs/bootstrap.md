# Bootstrap và lifetime

`IAppSystem` gồm `bool IsInitialized { get; }` và `bool Initialize()`. AppBootstrap nhận các MonoBehaviour có sẵn trong scene, gọi theo thứ tự, kiểm tra toàn bộ danh sách trước khi khởi tạo. Không tự spawn hoặc sort dependency.

```csharp
bootstrap.Configure(audio, haptics, settings, ui);
// Gọi trong Start hoặc sau Awake của scene, không trong Awake.
if (!bootstrap.InitializeSystems()) Debug.LogError(bootstrap.LastError);
```

Configure chỉ được gọi trước khi bắt đầu/Ready. Có thể setup Systems bằng Inspector thay cho Configure. Sau khi tất cả hệ thống thành công, IsReady=true và OnReady phát một lần. OnFailed/LastError báo cấu hình thiếu, entry trùng/disabled hoặc lỗi initialize. Retry giữ các system đã initialized; không rollback side effect. IsReady là trạng thái startup, không phải health check liên tục.

Audio và Haptics đứng trước Settings. Audio cần qua Awake để áp dụng AudioMixer. Overlay do UIManager tạo khi gọi; Device trước các consumer tween.

Audio/Haptics/UIManager có [SingletonPrefab], phải instantiate prefab trước truy cập Instance; thiếu service trả null và log lỗi, không tạo object thiếu config. Foundation reset static state ở đầu Play session trước scene Awake; game không cần reflection reset. Reset không initialize service và không thay thế Configure/InitializeSystems.

Audio/Haptics/Settings/UIManager dùng Base Singleton và mặc định tự giữ root qua scene. UIManager cần là root GameObject; game startup tạo prefab, initialize và bind scene camera, không gọi DontDestroyOnLoad trùng. UI root trùng bị disable/destroy cả hierarchy để không sót Canvas/camera. Base bootstrap vẫn sống theo scene. Không thêm manager persistent lần thứ hai trong scene sau.

SDK bất đồng bộ không implement Initialize thành 'ready' chỉ vì đã gửi request. Game điều phối readiness của Integrations riêng; gameplay local không bắt buộc đợi SDK.

Nguồn: [AppBootstrap](../Bootstrap/Script/AppBootstrap.cs), [IAppSystem](../Bootstrap/Script/IAppSystem.cs).
