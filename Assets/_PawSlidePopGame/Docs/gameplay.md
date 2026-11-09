# Gameplay Match-3

BoardModel giữ grid/cell/tile state; Match3BoardPresenter nối input, luật và presentation; Match3BoardView quản lý visual và spawn/despawn. Match3GameManager/Match3LevelManager cấp dữ liệu màn và runtime context. Luật slide xoay hàng/cột nằm trong LineSlideMoveRule.

Flow phân giải: nhận move → kiểm điều kiện → match finder → xử lý match/booster/blocker và target → gravity → refill → lặp cascade tới board ổn định → cập nhật lượt/kết quả. GameFlowManager quản lý substate; GameAppFlowManager quản lý menu, session, tim và kết quả.

BoardResolutionService, BoardGravityService, BoardRefillService và BoardMatchFinder vẫn thuộc game. Event/payload liên quan score, moves, target, booster và HUD nằm trong Gameplay/Events. Consumer feedback/UI/analytics subscribe và cleanup theo lifetime của chúng.

Nguồn: [Match3](../Gameplay/Match3), [game flow](../Startup/GameFlow), [events](../Gameplay/Events), [input](../Gameplay/Input).

