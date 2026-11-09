# Mang base sang project khác

Base dành cho Unity 6000.3.11f1, gồm Foundation, Bootstrap, UI, Audio, Haptics, Settings, SceneManagement, Persistence và Device.

1. Chuẩn bị Unity 6000.3.11f1, URP 17.3.0, uGUI 2.0.0, Newtonsoft JSON 3.2.2; Input System 1.19.0 cho cấu hình input hiện tại/Nice Vibrations.
2. Chuẩn bị DOTween đang dùng trong project và Nice Vibrations từ bộ dependency đã có. Không copy vendor lần thứ hai; giữ license và metadata/plugin importer. Không cần demo, HapticSamples hoặc OlderVersions.
3. Copy toàn bộ Assets/_Base cùng `.meta`. Kiểm GUID trước import; nếu trùng do đã có cùng module, hợp nhất và remap reference, không giữ hai bản.
4. Đặt prefab Audio, Haptics, Settings và UI; cấu hình Bootstrap theo thứ tự trong [bootstrap](bootstrap.md). Tạo UI screen/popup cụ thể dưới Resources/UI/… của project đích.
5. Tạo EventSystem, nối URP camera stack và game-owned config. SceneManagement/Persistence/Device có thể thêm khi cần, không bắt buộc Firebase/AdMob hoặc PawSlidePop.
6. Compile project đích; kiểm startup, đổi scene, popup/loading, lưu dữ liệu và cấu hình âm thanh. Kiểm Safe Area, Back và haptics trên thiết bị đích.

Không thêm asmdef tổng khi chưa thiết lập dependency DOTween/vendor/predefined assemblies. Không copy các adapter PawSlidePop chỉ để dùng base. Docs trong thư mục này đi cùng base; docs game không phải dependency.
