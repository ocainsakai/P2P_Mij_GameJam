# Beach Flow — 24h Jam Puzzle

Game puzzle nối màu chủ đề bãi biển mùa hè. Kéo từ một chấm màu tới chấm cùng màu, đi qua các ô kề nhau và không chồng lên Flow khác. Nối đủ tất cả cặp để thắng.

## Nội dung hoàn chỉnh

- 10 level có nghiệm, tăng từ bàn 4x4/2 màu đến 7x7/5 màu.
- Mouse và touch input.
- Undo, reset, pause, home, next level.
- Level select, khóa/mở level, best moves và continue.
- Menu/HUD/popup responsive cho WebGL.
- Background bãi biển riêng tại `Assets/Resources/BeachFlow/beach_background.png`.
- Save bằng `PlayerPrefs`, phù hợp game jam.

## Scene

- `Init`: chứa `Jam24_Managers`, tự chuyển sang Home.
- `Home`: background, level select 1–10, Continue và Reset Save.
- `Gameplay`: Flow board, HUD, pause/win popup và navigation.

Build Settings đã xếp theo đúng thứ tự `Init -> Home -> Gameplay`.

## Công cụ trong Unity

- `Jam24 > Build Complete Beach Flow Game` hoặc `F7`: dựng lại toàn bộ scene UI.
- `Jam24 > Play Gameplay Smoke Test` hoặc `F8`: mở Gameplay và Play.
- `Jam24 > Validate Complete Game` hoặc `F5`: kiểm tra asset, scene và nghiệm không chồng đường của cả 10 level.

## Điều khiển

- Kéo chuột/ngón tay giữa hai chấm cùng màu.
- Kéo ngược lại để backtrack trong đường hiện tại.
- `Z`: undo, `R`: reset, `Esc`: pause.
