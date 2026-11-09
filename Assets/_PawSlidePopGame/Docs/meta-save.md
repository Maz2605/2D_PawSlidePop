# Meta và dữ liệu lưu

Economy/HeartManager dùng PlayerEconomyRepository; BoosterInventory dùng định nghĩa booster riêng của game. LevelProgressRepository giữ progression; WheelStateRepository giữ trạng thái vòng quay. Shop gọi reward grant/handler để trừ coin và trao vật phẩm. Saga map cùng catalog/config thuộc game.

| Save key | Dữ liệu |
|---|---|
| player_economy | Coin, hearts/regeneration, inventory và trạng thái kinh tế hiện có |
| level_progress | Tiến trình level, kết quả và mở khóa |
| wheel_state | Trạng thái/cooldown vòng quay hiện có |

Không đổi tên key, field/schema hoặc quy tắc business trong đợt tách. SaveSystem chỉ chuyển sang [Persistence của base](../../_Base/Docs/services.md); repository vẫn sanitize/validate dữ liệu. Base64 cũ tiếp tục đọc được. Cloud Save là adapter của game, không nằm trong Base.

Settings là migration riêng từ settings.json sang Settings.* PlayerPrefs; không gộp vào save Economy/Level/Wheel. Giữ file cũ để kiểm tra/rollback; marker PawSlidePop.SettingsMigration.v1 tránh nhập lặp sau khi đã lưu.

Các repository đăng ký static save events; đây là ownership hiện có, chưa tổng quát hóa thành repository framework. Không dùng việc tách folder làm bằng chứng transaction shop/cloud merge đã được kiểm chứng.

Nguồn: [Economy](../Meta/Economy), [progression](../Meta/MapManager), [Wheel](../Meta/Wheel), [Cloud Save](../Startup/Integrations/Save/FirebaseSaveManager.cs).
