# Sole Flow — Tám Tay Một Dép

Vertical slice 10 level dựa trên `Assets/Docs/Gameplay Proposal.docx.md`.

## Core loop

1. **Observe** — đọc đường preview, vị trí dép, hang và các cơ chế.
2. **Real-time Flow** — dép tự trôi sau chưa đầy một giây; click/tap hoặc phím `1–9` để xoay vòi, mở van, đặt đá, bật bong bóng và mở rong ngay khi nó đang di chuyển.
3. **React** — mỗi mechanism cho một cửa sổ ngắn để sửa hướng trước khi dép mắc kẹt. Người chơi không được kéo dép trực tiếp.
4. **Collect** — đưa dép qua chuỗi đẩy → nâng → đổi hướng → bật → vào hang, nhận 1–3 sao và đặt dép lên kệ của Octo.

## 10 level MVP

1. Một cú đẩy nhẹ — fixed current.
2. Quay đúng hướng — rotating jet.
3. Chặn dòng sai — rock diverter.
4. Đi lên bằng bong bóng — bubble column.
5. Cú bật của vỏ sò — jet + bounce shell.
6. Cánh cửa rong biển — seaweed gate + bubbles.
7. Ngã ba san hô — flow divider + pearl objective.
8. Đẩy, giữ, nâng — valve + seaweed + bubbles.
9. Dòng nước theo nhịp — release timing.
10. Hòn đá trên công tắc — rock-linked pressure switch + rotating jet.

## Hệ thống

- Real-time Flow/Win/Fail modes, interaction trong lúc dép di chuyển, undo và reset nhanh.
- Mouse, touch, keyboard và interaction rules theo proposal.
- Deterministic semi-physics để cùng một setup luôn cho kết quả ổn định.
- Ba sao, best actions, unlock progression và bộ sưu tập 10 chiếc dép.
- Procedural SFX cho valve, release, success và stuck.
- Loading async và intro cutscene 3 trang.
- WebGL-safe scene flow: `Init → Loading → Cutscene → Home → Gameplay`.

## Asset

- Background: `Assets/Resources/BeachFlow/beach_background.png`.
- Sprite sheet tự tạo: `Assets/Resources/SoleFlow/soleflow_sheet.png`.
- `SoleArt` loại chroma green lúc runtime và cache 8 sprite: Octo, slipper, nest, jet, bubbles, rock, shell, seaweed.

## Debug

- `F5`: validate proposal, 10 level, scene và asset.
- `F7`: dựng lại toàn bộ scene.
- `F8`: gameplay smoke test.
- `F9`: full intro smoke test.
- `1–9`: tương tác mechanism, `Z`: undo, `R`: reset, `Esc`: pause.
