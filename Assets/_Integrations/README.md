# SDK Integrations

Các gateway này không tham chiếu PawSlidePop. Base không phụ thuộc folder này.

- Firebase/FirebaseRuntime: Firebase dependency check, analytics/crashlytics, Auth/Firestore access theo compile define.
- AdMob/AdMobClient: SDK init, load/retry, ad lifetime và callbacks; caller cung cấp ad unit ID.

Cần Firebase SDK và GoogleMobileAds/ExternalDependencyManager hiện có khi dùng module tương ứng. Gateway không tự cung cấp credentials hoặc cấu hình ứng dụng. Game adapter sở hữu event name, auth UX, placement, reward và cloud save schema; xem [docs game](../_PawSlidePopGame/Docs/startup-integrations.md).

Chỉ copy Base sang game offline thì không cần folder Integrations hoặc SDK. Không có cam kết hot-remove Firebase/AdMob khỏi PawSlidePop mà không gỡ adapter/cấu hình tương ứng.
