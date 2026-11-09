# Level Editor

LevelEditor là công cụ riêng cho schema Match-3, không thuộc Base/Editor. Scene setup/session/UI giữ state chỉnh sửa; palette và placement service thay đổi document; validator kiểm dữ liệu trước export. Generation service/rules tạo màn từ cấu hình game.

JSON export dùng LevelJsonExportService và LevelPathUtility; đường dẫn Assets đã cập nhật theo Content, đường Resources tương đối vẫn giữ nguyên. Format level, target, tile/blocker và validation được định nghĩa trong module LevelEditor.

Luồng chỉnh màn: mở EditorScene → chọn level → sửa mask/blocker/target → validate → export JSON → load lại trong gameplay.

Nguồn: [LevelEditor](../LevelEditor), [persistence](../LevelEditor/Persistence).
