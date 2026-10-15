"""
Script kiểm thử tự động E2E mốc cuối ngày 3 trên Render:
Luồng: areas -> passes -> nearby -> pois/{id}
"""
import uuid
import sys
import json
import urllib.request
import urllib.error

sys.stdout.reconfigure(encoding='utf-8')

BASE_URL = "https://multiguide-1person.onrender.com"

def get_json(url, headers=None):
    req = urllib.request.Request(url, headers=headers or {})
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode("utf-8"))

def post_json(url, headers=None, data=None):
    body = json.dumps(data or {}).encode("utf-8") if data else None
    req = urllib.request.Request(url, data=body, headers=headers or {}, method="POST")
    with urllib.request.urlopen(req) as resp:
        return json.loads(resp.read().decode("utf-8"))

def main():
    print("=================================================================")
    print("🚀 BẮT ĐẦU KIỂM THỬ E2E TỰ ĐỘNG MỐC CUỐI NGÀY 3 TRÊN RENDER")
    print(f"🌐 Server: {BASE_URL}")
    print("=================================================================\n")

    # 1. areas: GET /api/v1/areas/{code}
    print("[1/4] 🏛️ Gọi GET /api/v1/areas/van-mieu (Quét QR khu)...")
    try:
        area = get_json(f"{BASE_URL}/api/v1/areas/van-mieu")
        langs = ", ".join(l["name"] for l in area.get("supportedLanguages", []))
        print(f"  ✅ Thành công: {area['name']} | Giá vé: {area['priceUsd']} USD | Ngôn ngữ: {langs}")
    except Exception as e:
        print(f"  ❌ Thất bại: {e}")
        sys.exit(1)

    # 2. passes: POST /api/v1/areas/{code}/passes
    print("\n[2/4] 🎟️ Gọi POST /api/v1/areas/van-mieu/passes (Mua vé tham quan)...")
    idem_key = str(uuid.uuid4())
    headers = {"Idempotency-Key": idem_key}
    try:
        pass_res = post_json(f"{BASE_URL}/api/v1/areas/van-mieu/passes", headers=headers)
        token = pass_res["accessToken"]
        print(f"  ✅ Thành công: Cấp vé đến {pass_res['expiresAt']}")
        print(f"  🔑 Access Token: {token[:16]}... (độ dài {len(token)})")
    except Exception as e:
        print(f"  ❌ Thất bại: {e}")
        sys.exit(1)

    # 3. nearby: GET /api/v1/pois/nearby
    print("\n[3/4] 📍 Gọi GET /api/v1/pois/nearby (Bật GPS tọa độ 21.0285, 105.8355)...")
    auth_headers = {"Authorization": f"Bearer {token}"}
    try:
        nearby_url = f"{BASE_URL}/api/v1/pois/nearby?lat=21.0285&lng=105.8355&radius=150&lang=vi"
        pois = get_json(nearby_url, headers=auth_headers)
        print(f"  ✅ Thành công: Tìm thấy {len(pois)} POI trong bán kính 150m:")
        for poi in pois:
            status = "🔊 ĐỦ GẦN ĐỂ PHÁT" if poi.get("withinTrigger") else "🚶 Cần lại gần hơn"
            print(f"     - [{poi['title']}] cách {poi['distanceM']:.1f}m -> {status}")
        first_poi = pois[0]
    except Exception as e:
        print(f"  ❌ Thất bại: {e}")
        sys.exit(1)

    # 4. pois/{id}: GET /api/v1/pois/{id}?lang=vi
    print(f"\n[4/4] 🎧 Gọi GET /api/v1/pois/{first_poi['id']}?lang=vi (Nghe thuyết minh [{first_poi['title']}])...")
    try:
        detail_url = f"{BASE_URL}/api/v1/pois/{first_poi['id']}?lang=vi"
        detail = get_json(detail_url, headers=auth_headers)
        print(f"  ✅ Thành công: Nhận nội dung POI:")
        print(f"     - Tiêu đề: {detail['title']}")
        print(f"     - Ngôn ngữ: {detail['language']} (Fallback: {detail.get('fallbackLanguage') or 'Không'})")
        print(f"     - Thời lượng: {detail['audioDurationSec']} giây")
        print(f"     - Audio Signed URL (Supabase Storage): {detail['audioUrl'][:60]}...")
    except Exception as e:
        print(f"  ❌ Thất bại: {e}")
        sys.exit(1)

    # 5. passes/current: GET /api/v1/passes/current (Bổ sung)
    print("\n[Bổ sung] ⏱️ Gọi GET /api/v1/passes/current (Kiểm tra hạn vé)...")
    try:
        curr = get_json(f"{BASE_URL}/api/v1/passes/current", headers=auth_headers)
        print(f"  ✅ Trạng thái vé: {curr['status']} | Còn lại: {curr['remainingSeconds']} giây")
    except Exception as e:
        print(f"  ⚠️ Thất bại: {e}")

    print("\n=================================================================")
    print("🎉 TẤT CẢ 4 BƯỚC MỐC CUỐI NGÀY 3 ĐÃ HOÀN TẤT THÀNH CÔNG 100%!")
    print("=================================================================")

if __name__ == "__main__":
    main()
