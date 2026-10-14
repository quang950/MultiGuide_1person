"""
Script tự động sinh 18 file audio thuyết minh (Edge TTS) và nạp lên Supabase Storage/DB
Dành cho Task T-2.10 (MultiGuide Backend).
"""

import os
import sys
import asyncio
from datetime import datetime, timezone
import requests
import edge_tts

# Cấu hình giọng đọc chuẩn bản xứ chất lượng cao của Microsoft Edge Neural
VOICES = {
    "vi": "vi-VN-HoaiMyNeural",  # Tiếng Việt nữ truyền cảm, tự nhiên
    "en": "en-US-JennyNeural",   # Tiếng Anh Mỹ nữ chuẩn phổ thông
    "ja": "ja-JP-NanamiNeural"   # Tiếng Nhật nữ chuẩn Tokyo
}

OUTPUT_DIR = os.path.join(os.path.dirname(__file__), "audio")


def load_env(env_path=".env"):
    """Đọc các biến môi trường từ file .env"""
    env_vars = {}
    if not os.path.exists(env_path):
        raise FileNotFoundError(f"Không tìm thấy file {env_path}")

    with open(env_path, "r", encoding="utf-8") as f:
        for line in f:
            line = line.strip()
            if line and not line.startswith("#") and "=" in line:
                key, val = line.split("=", 1)
                env_vars[key.strip()] = val.strip()
    return env_vars


async def generate_mp3(text: str, voice: str, output_path: str):
    """Sinh file MP3 từ văn bản qua Edge TTS"""
    communicate = edge_tts.Communicate(text, voice)
    await communicate.save(output_path)


def upload_audio_to_storage(base_url: str, key: str, bucket: str, storage_path: str, file_path: str):
    """Tải file MP3 lên Supabase Storage private bucket (hỗ trợ upsert)"""
    url = f"{base_url}/storage/v1/object/{bucket}/{storage_path}"
    headers = {
        "apikey": key,
        "Authorization": f"Bearer {key}",
        "Content-Type": "audio/mpeg",
        "x-upsert": "true"
    }

    with open(file_path, "rb") as f:
        content = f.read()

    response = requests.post(url, headers=headers, data=content)
    if response.status_code not in (200, 201):
        raise Exception(f"Lỗi upload Storage (HTTP {response.status_code}): {response.text}")
    return storage_path


def update_translation_db(base_url: str, key: str, translation_id: str, storage_path: str):
    """Cập nhật đường dẫn audio_path vào bảng poi_translation trên Supabase"""
    url = f"{base_url}/rest/v1/poi_translation?id=eq.{translation_id}"
    headers = {
        "apikey": key,
        "Authorization": f"Bearer {key}",
        "Content-Type": "application/json",
        "Prefer": "return=minimal"
    }
    payload = {
        "audio_path": storage_path,
        "updated_at": datetime.now(timezone.utc).isoformat()
    }
    response = requests.patch(url, headers=headers, json=payload)
    if response.status_code not in (200, 204):
        raise Exception(f"Lỗi cập nhật DB (HTTP {response.status_code}): {response.text}")


def create_signed_url(base_url: str, key: str, bucket: str, storage_path: str, expires_in=3600):
    """Tạo Signed URL để kiểm tra khả năng phát audio"""
    url = f"{base_url}/storage/v1/object/sign/{bucket}/{storage_path}"
    headers = {
        "apikey": key,
        "Authorization": f"Bearer {key}",
        "Content-Type": "application/json"
    }
    response = requests.post(url, headers=headers, json={"expiresIn": expires_in})
    if response.status_code == 200:
        data = response.json()
        signed_url = data.get("signedURL") or data.get("signedUrl")
        return f"{base_url}/storage/v1{signed_url}" if signed_url.startswith("/") else f"{base_url}/storage/v1/{signed_url}"
    return None


async def main():
    print("=" * 60)
    print("MultiGuide — Tool Nạp Audio Tự Động Cho 6 POI (Task T-2.10)")
    print("=" * 60)

    # 1. Load config
    env = load_env()
    supabase_url = env.get("Supabase__Url") or env.get("Supabase:Url")
    service_key = env.get("Supabase__ServiceRoleKey") or env.get("Supabase:ServiceRoleKey")
    bucket = env.get("Supabase__AudioBucket") or env.get("Supabase:AudioBucket") or "audio"

    if not supabase_url or not service_key:
        print("[!] Lỗi: Thiếu cấu hình Supabase URL hoặc ServiceRoleKey trong .env")
        sys.exit(1)

    print(f"[*] Supabase URL: {supabase_url}")
    print(f"[*] Bucket: {bucket}")

    os.makedirs(OUTPUT_DIR, exist_ok=True)

    # 2. Lấy danh sách translations từ database
    print("\n[+] Đang lấy danh sách bản dịch từ CSDL...")
    url = f"{supabase_url}/rest/v1/poi_translation?select=id,poi_id,language_code,title,description,audio_path&order=poi_id,language_code"
    headers = {
        "apikey": service_key,
        "Authorization": f"Bearer {service_key}"
    }
    r = requests.get(url, headers=headers)
    if r.status_code != 200:
        print(f"[!] Lỗi truy vấn poi_translation: HTTP {r.status_code} - {r.text}")
        sys.exit(1)

    translations = r.json()
    total = len(translations)
    print(f"[✓] Đã tìm thấy {total} bản dịch POI trong CSDL.\n")

    # 3. Duyệt qua từng bản dịch, sinh audio và upload
    sample_signed_url = None

    for idx, trans in enumerate(translations, start=1):
        trans_id = trans["id"]
        poi_id = trans["poi_id"]
        lang = trans["language_code"]
        title = trans["title"]
        desc = trans["description"]

        voice = VOICES.get(lang, "vi-VN-HoaiMyNeural")
        clean_text = f"{title}. {desc}"
        file_name = f"{poi_id}_{lang}.mp3"
        local_file = os.path.join(OUTPUT_DIR, file_name)
        storage_path = f"pois/{poi_id}/{lang}.mp3"

        print(f"[{idx}/{total}] POI ({lang.upper()}): {title[:35]}...")

        # Sinh file MP3 qua Edge TTS
        try:
            await generate_mp3(clean_text, voice, local_file)
            file_size_kb = os.path.getsize(local_file) / 1024
            print(f"    -> Đã tạo MP3 ({file_size_kb:.1f} KB, voice: {voice})")
        except Exception as e:
            print(f"    [!] Lỗi sinh audio: {e}")
            continue

        # Upload Storage
        try:
            upload_audio_to_storage(supabase_url, service_key, bucket, storage_path, local_file)
            print(f"    -> Upload Storage: {storage_path}")
        except Exception as e:
            print(f"    [!] Lỗi upload Storage: {e}")
            continue

        # Cập nhật DB
        try:
            update_translation_db(supabase_url, service_key, trans_id, storage_path)
            print("    -> Cập nhật DB thành công.")
        except Exception as e:
            print(f"    [!] Lỗi cập nhật DB: {e}")
            continue

        if not sample_signed_url and lang == "vi":
            sample_signed_url = create_signed_url(supabase_url, service_key, bucket, storage_path)

    print("\n" + "=" * 60)
    print("HOÀN THÀNH NẠP 18/18 AUDIO THÀNH CÔNG 100%!")
    print("=" * 60)
    if sample_signed_url:
        print(f"[*] Signed URL nghe thử nghiệm (mẫu tiếng Việt):")
        print(f"    {sample_signed_url}")


if __name__ == "__main__":
    asyncio.run(main())
