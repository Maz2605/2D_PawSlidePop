# Audio, Haptics và Settings

Đặt các prefab Audio/Prefabs/AudioManager, Haptics/Prefabs/HapticManager, Settings/Prefabs/SettingsManager trên root riêng. Bootstrap Audio → Haptics → Settings. Base audio prefab giữ mixer và config rỗng; prefab audio thuộc game có thể gán asset config kế thừa AudioClipConfigSO. Không gán clip game vào prefab base.

Audio API: PlayMusic(clip, loop, fadeDuration, volumeScale, pitch, delay); StopMusic, PauseMusic, ResumeMusic; PlaySfx(clip, volumeScale, pitch, delay, cooldown, cooldownKey), PlayRandomSfx, StopAllSfx. `pitch` là hệ số tuyệt đối, mặc định 1; không phải độ biến thiên. Delay được tính trước khi phát. Pool có giới hạn, source được thu hồi sau khi phát kết thúc. Mixer cần MasterVolume/MusicVolume/SFXVolume và routing mặc định.

HapticManager dùng Nice Vibrations. Initialize, SetEnabled, Stop và PlaySelection/LightImpact/MediumImpact/HeavyImpact/Success/Warning/Failure… là API chung. Game sở hữu mapping hành động. Editor có thể initialized nhưng không phát native haptics. Disable, mất focus hoặc pause dừng rung; không phát lại hiệu ứng đã ngắt khi quay về.

SettingsManager giữ MusicEnabled, SfxEnabled, HapticsEnabled, MasterVolume, MusicVolume, SfxVolume dưới các key PlayerPrefs `Settings.*`. Dùng SetMusicEnabled, SetSfxEnabled, SetHapticsEnabled, SetMasterVolume, SetMusicVolume, SetSfxVolume; Save để flush. ApplySettings áp dụng lại giá trị đã lưu trong manager; ResetToDefaults lưu defaults. Volume được clamp; giá trị không hữu hạn bị từ chối. Setters trước khi initialized không thay settings.

Preview UI không gọi setters Settings nếu cần hỗ trợ Cancel: preview trực tiếp vào Audio/Haptics, Cancel gọi ApplySettings, Save mới gọi setters và Save. Base không biết file settings.json cũ; migration thuộc game.

Nguồn: [AudioManager](../Audio/Script/AudioManager.cs), [HapticManager](../Haptics/Script/HapticManager.cs), [SettingsManager](../Settings/Script/SettingsManager.cs).

## Gọi trực tiếp và config

AudioManager.Instance là đầu phát; property Config trả trực tiếp asset AudioClipConfigSO đã gán trên prefab. Type này cho phép game kế thừa, nhưng Base không tham chiếu type hoặc asset game. Không có GetConfig, registry SoundId hay SoundCue. Caller truyền AudioClip, volumeScale, pitch tuyệt đối và delay khi cần.

PlaySfx và PlayRandomSfx thêm hai parameter cuối tùy chọn: cooldown mặc định 0 (không hạn chế), cooldownKey mặc định null. Khi cooldown dương hữu hạn, request dùng Time.unscaledTime để giới hạn phát. Key không rỗng dùng chung giữa mọi caller/clip có cùng key; không có key thì giới hạn theo clip. Các nhóm không chia sẻ key không chặn nhau. Timestamp thuộc manager runtime, xóa khi khởi tạo lần đầu hoặc destroy; Initialize lặp khi ready không xóa giới hạn đang hoạt động. StopAllSfx không reset cooldown.

Request thiếu clip, service chưa ready, SFX tắt, mất focus/pause hoặc pool đầy không được queue và không chiếm cooldown. Request được nhận ghi timestamp tại lúc yêu cầu, kể cả có delay. Cooldown không trì hoãn phát tới lượt sau. Chuỗi key nên là tập hữu hạn ổn định theo nhóm hành vi, không tạo key mới cho từng instance.

UIButtonVisual dùng clickClip override; khi trống, lấy Config.buttonClick. playAudio=false tắt cả override và fallback. Configure cho nút tạo runtime với clip null tắt audio để giữ các hiệu ứng thuần hình ảnh; cấu hình trong Inspector không có override vẫn có thể dùng fallback.

## Singleton và lifetime

AudioManager kế thừa Singleton<AudioManager>, HapticManager kế thừa Singleton<HapticManager> từ Foundation. Hai service không còn static Instance hoặc Awake đăng ký riêng. Caller vẫn dùng AudioManager.Instance/HapticManager.Instance; duplicate handling, DontDestroyOnLoadEnabled (mặc định true), quit guard và giải phóng instance dùng chung Base. Initialize/IAppSystem vẫn là bước riêng do bootstrap thực hiện, Instance không tự initialize.

Startup tạo service từ prefab trước khi consumer gọi Instance để có đủ config/mixer/source. Audio/Haptics được đánh dấu [SingletonPrefab]: Instance thiếu owner log lỗi và trả null, không tự tạo object rỗng. ExistingInstance chỉ đọc owner hiện có, không tìm/tạo object. Cleanup, Settings Apply/preview và component feedback tùy chọn dùng ExistingInstance để không sinh service mới khi UI đóng hoặc owner đã bị hủy.

OnDestroy của service thu hồi audio/haptic resource khi còn là owner, rồi gọi base.OnDestroy; duplicate không dừng service chính. Kiểm ownership dùng ExistingInstance kể cả khi quit guard đã bật. Singleton và EventManager tự reset các closed generic của mình qua hook non-generic riêng trong mỗi file khi bắt đầu Play session mới, không cần registry chung, hook riêng ở Audio/Haptics hoặc editor helper game khi Domain Reload tắt.

## Tích hợp với game

GamePopupFeedback có playOpenHaptics độc lập playOpenSound; prefab được serialize cờ rung từ hành vi cũ. Base HapticManager không thêm throttle mặc định vì chưa có bằng chứng device. GameAppFlowManager chọn nhạc theo app state; WinPopup/LosePopup chọn nhạc kết quả trực tiếp; result clip trống sẽ StopMusic(0.25f), không thay contract PlayMusic(null) của Base.

SettingsSession của game được tạo riêng mỗi lần mở popup và gọi trực tiếp Settings/Audio/Haptics hiện hữu. Preview không ghi preferences; Cancel khôi phục cả enabled flag lẫn volume đã lưu; MasterVolume ngoài phạm vi popup và giữ nguyên. Save thất bại không đóng popup hoặc đánh dấu thành công.
