# Startup và tích hợp SDK

LoadingScene giữ instance game AppBootstrap từ Resources/Core/AppBootstrap. Startup chạy migration settings, tạo service root từ prefab, chuẩn bị UI persistent rồi cấu hình Bootstrap base: Device → Audio → Haptics → Settings → UI. Khi base Ready, startup kiểm config trên PawAudioManager rồi initialize GameAppFlowManager; chuyển GameplayScene qua SceneLoaderManager.

Game giữ UIManager root qua scene; AppBootstrap gọi ui.BindSceneCamera(Camera.main) khi initialize và sceneLoaded, không quét toàn bộ Base camera. Audio/Haptics/Settings là root riêng, do base quản lý persistence. Không thêm manager cũ trong GameplayScene. AutoDevBootstrap chỉ phục vụ Editor: inject cùng prefab khi chạy trực tiếp GameplayScene, còn initialization chạy trong Start sau Awake để AudioMixer sẵn sàng. GameAppFlowManager xử lý cả scene đang active khi initialize, nên direct entry tự hiện StartScreen dù sceneLoaded đã phát trước đó. Startup có guard chống chạy lặp; UIManager tắt root trùng ngay trong Awake trước khi component con khởi tạo; Bootstrap dùng UIManager.Instance và UIManager của owner còn sống.

Firebase/Ads Init bắt đầu sau base; SDK giữ readiness riêng. Base Ready không có nghĩa SDK ready. Game vẫn chơi local khi không có SDK/network. Không có polling một frame rồi coi Firebase đã hoàn tất.

FirebaseRuntime trong `_Integrations/Firebase` sở hữu dependency check, cấu hình SDK, logging gateway và access tới Auth/Firestore. Adapter FirebaseService giữ tên event/parameter và subscription gameplay. FirebaseAuthService giữ flow tài khoản/UI; FirebaseSaveManager giữ danh sách repository, sync và reset/merge dữ liệu. Các compile define FIREBASE_AUTH_ENABLED, FIREBASE_FIRESTORE_ENABLED, GOOGLE_SIGNIN_ENABLED, FACEBOOK_SDK_ENABLED được giữ.

AdMobClient trong `_Integrations/AdMob` sở hữu SDK initialization, load/retry, ad handles, show/close và dispose. AdsService của game giữ platform ad unit ID, placement và chuyển kết quả thành AdsGameEvent. RewardedAdRewardService/catalog và UI xử lý phần thưởng thuộc game. RewardEarned chỉ phát một lần mỗi show; callback thành công phát khi SDK báo earned, như flow cũ. Không cấp reward chỉ vì đóng ad.

Nguồn: [game startup](../Startup/Bootstrap/AppBootstrap.cs), [SDK transport](../../_Integrations/README.md), [adapter](../Startup/Integrations).

Audio/Haptics không có controller chuyển tiếp. AudioConfig asset được gán trên PawAudioManager prefab; startup kiểm đúng PawSlidePopAudioConfig một lần, GameAppFlowManager phát nhạc theo app state bằng singleton Base. Không còn bước bật AudioController/VibrationController. Setup Audio And UI editor command chỉ gán config trên PawAudioManager, không mở/sửa scene hoặc tự thêm feedback controller.
