# SceneManagement, Persistence và Device

## SceneManagement

SceneLoaderManager.LoadScene(sceneName,onSceneLoaded) nhận tên scene trong build. Yêu cầu mới khi IsLoading=true bị bỏ qua. Scene không hợp lệ được báo lỗi, không acquire hoặc release loading của tác vụ khác. Gán LoadingOverlay bằng ILoadingOverlay, hiện được UIManager implement với BeginLoading(Action onCovered = null) trả LoadingHandle.

Loader giữ handle riêng của lượt load và Dispose trong finally/OnDisable/OnDestroy; cleanup idempotent, không tra property LoadingOverlay lúc release. Đổi provider giữa tác vụ không chuyển ownership sang provider mới. Nếu provider disable loader ngay trong BeginLoading, handle vừa trả về được Dispose trước khi routine tiếp tục. Giữ timeout chờ cover, thời gian tối thiểu và cơ chế scene load async hiện có. Callback cover chỉ báo đã phủ màn hình; thiếu visual vẫn có fallback để không kẹt chờ. Base không chứa tên scene cố định, gameplay/SDK vẫn thuộc game.

## Persistence

SaveSystem cung cấp TrySave<T>(gameId, data) để báo kết quả ghi local; API Save<T>/Load<T>(gameId), SaveRawJson/LoadRawJson, SaveToPath/LoadFromPath và DeleteFile. Đường mặc định vẫn persistentDataPath/{gameId}.json. Save(gameId) dùng Base64 để tương thích dữ liệu cũ; đây không phải bảo mật/encryption. SaveToPath mặc định JSON thường.

Ghi file tạm rồi replace, giữ `.bak` của bản JSON hợp lệ trước đó; nền tảng không hỗ trợ replace dùng copy backup trước khi thay file. Đọc thử primary rồi backup; phục hồi đọc không ghi đè file lỗi. Sau phục hồi, lần ghi tiếp theo không lấy primary hỏng đè backup tốt. Không đọc `.tmp` chưa commit. DeleteFile xóa primary/tmp/bak.

OnBeforeLocalSaveWrite phát trước serialize; OnSaveCompleted trả JSON gốc sau local write; SaveRawJson phát OnSaveSynced. Subscriber phải tự gỡ đăng ký. Save schema, validate và migration thuộc game. API void log lỗi; không xem việc hàm trả về là cam kết cloud sync hoặc transaction kinh tế thành công.

## Device

DeviceSettingService implement IAppSystem. Inspector cấu hình targetFPS, disableVSync, keepScreenAwake, tweenerCapacity, sequenceCapacity. Initialize có tính lặp an toàn; SetTargetFPS cho đổi runtime. Đây là cấu hình giới hạn/capacity, không phải bằng chứng tối ưu hiệu năng.

Nguồn: [SceneLoaderManager](../SceneManagement/Script/SceneLoaderManager.cs), [ILoadingOverlay](../SceneManagement/Script/ILoadingOverlay.cs), [SaveSystem](../Persistence/Script/SaveSystem.cs), [DeviceSettingService](../Device/Script/DeviceSettingService.cs).
