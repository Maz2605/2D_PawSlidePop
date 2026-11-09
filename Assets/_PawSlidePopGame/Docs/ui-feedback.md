# UI và feedback

## Screen, popup và overlay

Screen/popup game kế thừa MazFramework.UI.BaseScreen/BasePopup; Map, Shop, Rewards và Wheel dùng BaseSubScreen. UIManager của Base sở hữu cache, popup stack, transition và khóa input. Xem [UI Base](../../_Base/Docs/ui.md) cho API và cấu hình root/camera.

View trả tween từ CreateShowTween/CreateHideTween để Base quản lý completion/cancel. Đặt side effect vào OnShown/OnHidden hoặc callback trong Sequence; override OnDisable/OnDestroy phải gọi base. Tween entry/pulse riêng được dừng khi owner disable.

Loading và Toast kế thừa BaseOverlay; prefab nằm tại Content/Shared/Resources/UI/Overlays. Mỗi tác vụ lấy LoadingHandle qua BeginLoading và Dispose trong finally hoặc khi owner đóng. Loading chỉ tắt sau khi request cuối kết thúc. Toast không khóa input.

ProfilePopup/EmailAuthPopup giữ AuthRequest theo lượt mở view. Bắt đầu đóng, disable hoặc destroy sẽ giải phóng handle và vô hiệu hóa callback cũ. Callback thành công/thất bại chỉ xử lý một lần; lỗi đồng bộ của SDK cũng phải giải phóng handle.

## Nút và layout

Screen/popup giữ reference Button, đăng ký và gỡ handler có tên theo lifetime. Nghiệp vụ chạy ngay trong click frame. UIButtonVisual sở hữu punch, click audio và rung Selection; GamePopupFeedback phát feedback lúc mở/đóng popup. Win/Lose tự phát feedback kết quả, không phát thêm generic popup feedback.

GameMenuScreen sở hữu nút Settings/Profile; ProfileAvatarView chỉ hiển thị avatar. NavigationBar và các view map/shop/reward/inventory báo sự kiện về owner.

SafeArea nằm trên container nội dung; background toàn màn hình đặt ngoài safe content. UIManager giữ root qua scene; startup tạo/initialize prefab và gọi BindSceneCamera với Base camera của scene.

## Audio và haptics

PawSlidePopAudioConfig kế thừa AudioClipConfigSO và được gán trên PawAudioManager. Caller đọc AudioManager.Config để lấy nhóm clip game; Base không tham chiếu config PawSlidePop. Chỉnh nhóm Music/Match3Sfx/UiSfx trên asset để thay mapping; OnValidate đồng bộ các field UI chung.

| Hành động | Owner | Haptic |
|---|---|---|
| Button / preview toggle | UIButtonVisual / Settings | Selection |
| Move / chọn booster | Match3BoardPresenter / BoosterController | MediumImpact |
| Move / booster bị từ chối | Owner của hành động | Warning |
| Thắng / thua | WinPopup / LosePopup | Success / Failure |
| Slide / drop | Match3TileView / Match3BoardView | Theo cấu hình hành động |
| Match / combo / target | GameFlowManager | Theo cấu hình hành động |
| Nhạc startup / menu / gameplay | GameAppFlowManager | — |

Owner gọi trực tiếp PlaySfx/PlayMusic và HapticManager tại thời điểm cần feedback. Slide/drop dùng cooldown 0.08s với key Paw.TileSlide/Paw.TileDrop; match dùng Paw.Match và matchSfxCooldown trong config. Khi thiếu nhạc kết quả, game fade-stop nhạc gameplay; SFX kết quả vẫn phát nếu được cấu hình.

## Settings

SettingsSession được tạo cho từng lần mở popup và gọi trực tiếp SettingsManager, AudioManager và HapticManager. Preview áp dụng tức thì nhưng không ghi preferences. Save lưu lựa chọn; Cancel/Close/disable phục hồi giá trị đã lưu. Preview và cleanup dùng ExistingInstance để không tạo service mới.

## Input và Back

UIManager khóa board khi loading, popup hoặc screen transition. Match3InputController giữ cờ UI riêng; modal xuất hiện giữa drag/long-press sẽ hủy gesture. Gỡ khóa UI không mở khóa gameplay đang resolving.

Chỉ popup trên cùng đã mở xong nhận input. Popup đang đóng vẫn nằm trong stack đến Hidden. Một Image blocker chặn các lớp dưới và chuyển sang Overlay Root khi loading. Toast không khóa. UIManager xóa selection không hợp lệ; không tự chọn control tiếp theo.

UIBackInput chuyển Back chưa được UI consume sang GameBackNavigation. Gameplay tương tác được thì yêu cầu Pause; Back ở Pause gọi ResumeGameplay, ở Settings đi qua Cancel, ở Win/Lose được consume. Menu/Start không tự thoát ứng dụng.

## Chốt kết quả và lưu progression

GameAppFlowManager giữ CurrentSessionId, LevelId và cờ đã ghi kết quả cho từng lượt. GameFlowManager phát ResultFinalized sau Sugar Crush; app flow kiểm session và ghi progression tối đa một lần. GameplayResultPopupPresenter chỉ trình bày, hủy continuation khi hide/disable hoặc đổi session.

Khi thoát/restart sau thắng, flow dừng outro, tính nhanh phần còn lại trên board/random hiện có, chốt score/stars và ghi local thành công trước khi chuyển lượt. Không chốt snapshot tại GameplayWon. Vòng xử lý có progress guard và budget.

SaveSystem.TrySave và LevelProgressRepository.TryRecordWinAndAdvance trả kết quả ghi local. Repository dùng bản sao detached và rollback memory khi ghi thất bại. Win popup có thể hiện dù save thất bại; navigation retry lưu mà không tính điểm lại. Lỗi cloud/observer sau local write không biến lần ghi đó thành thất bại.

Navigation đi qua EnterMainMenu/Restart/StartGameplaySession để bảo đảm finalize. Không unload scene trực tiếp giữa victory nếu cần giữ bảo đảm này.

Nguồn: [UI](../UI), [SettingsSession](../UI/Settings/SettingsSession.cs), [audio config](../Configs/Audio/PawSlidePopAudioConfig.cs), [app flow](../Startup/GameFlow/GameAppFlowManager.cs).
