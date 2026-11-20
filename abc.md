# Đường dẫn audio thuyết minh theo ngôn ngữ

File này ghi **đường dẫn MP3 được tạo và phát**, không phải danh sách file mã nguồn cần dịch. Đây là tài liệu tra cứu; các script và frontend hiện chưa đọc file Markdown này làm cấu hình.

## Audio đóng gói trong frontend

Thư mục: `client/assets/audio/pois/`. Sáu POI Văn Miếu dưới đây đều đã có đủ bốn file theo mẫu:

| Ngôn ngữ | Đường dẫn từ gốc project |
| --- | --- |
| Việt (`vi`) | `client/assets/audio/pois/{poi_id}.mp3` |
| Anh (`en`) | `client/assets/audio/pois/{poi_id}_en.mp3` |
| Trung (`zh`) | `client/assets/audio/pois/{poi_id}_zh.mp3` |
| Nhật (`ja`) | `client/assets/audio/pois/{poi_id}_ja.mp3` |

Thay `{poi_id}` bằng mã tương ứng:

| Điểm | Địa điểm | `poi_id` |
| --- | --- | --- |
| VM-01 | Văn Miếu Môn | `c1875fb2-84ad-4a5a-936a-173fdbf4f7f3` |
| VM-02 | Khuê Văn Các | `3ede2134-c305-4270-9413-39c9743f9cb8` |
| VM-03 | Giếng Thiên Quang và bia tiến sĩ | `70dabf88-5450-48e7-9603-b753f5350047` |
| VM-04 | Đại Thành Môn và sân Đại Bái | `cc63b968-096f-4b06-b4f8-19f8f829ef42` |
| VM-05 | Điện Đại Thành | `0c23b2e0-73d1-4f37-90a0-f95201abf43b` |
| VM-06 | Khu Thái Học | `cd3c98eb-79fe-4980-a870-6f690f9e40cd` |

Ví dụ audio tiếng Trung VM-02: `client/assets/audio/pois/3ede2134-c305-4270-9413-39c9743f9cb8_zh.mp3`.

`scripts/generate-chinese-audio.cjs` đọc `scripts/chinese-stories.json` và tạo sáu file `{poi_id}_zh.mp3` tại thư mục này. `client/js/audio.js` dùng audio local khi API không có nguồn phù hợp. Với POI khác, fallback hiện có thể dựa vào số thứ tự trạm; cần kiểm tra file thật trước khi dùng.

## Audio tạo và lưu trên Supabase

Hai script Python hiện dùng **hai quy ước khác nhau**:

| Script | File tạo trên máy | Đường dẫn đối tượng trong bucket Supabase |
| --- | --- | --- |
| `scripts/generate_audio.py` | `scripts/temp_{translation_id}.mp3` (file tạm, xóa sau khi chạy) | `pois/{translation_id}/{lang}.mp3` |
| `scripts/seed_audio.py` | `scripts/audio/{poi_id}_{lang}.mp3` | `pois/{poi_id}/{lang}.mp3` |

`{lang}` là `vi`, `en`, `zh` hoặc `ja`. `{translation_id}` là mã bản dịch POI, **khác** `{poi_id}` là mã địa điểm. Đường dẫn `pois/...` ở cột Supabase nằm trong bucket audio, không phải trong thư mục `client/`. Sau khi upload, đường dẫn được lưu ở trường `audio_path` của bản dịch; backend tạo URL tạm để frontend phát.

## Khi thêm audio ngôn ngữ hoặc địa điểm mới

1. Tạo hoặc xác định bản dịch POI và mã ngôn ngữ.
2. Dùng đúng quy ước đường dẫn của script đang chạy; không đổi lẫn `translation_id` và `poi_id`.
3. Nếu đóng gói file local, đặt tên theo mẫu `{poi_id}` và hậu tố ngôn ngữ ở bảng đầu, rồi xác nhận file tồn tại.
4. Bổ sung POI mới vào bảng tra cứu này sau khi đã xác nhận mã và tên địa điểm.
