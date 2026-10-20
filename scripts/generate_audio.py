"""
Script sinh file audio MP3 (Edge TTS Neural) cho một bản dịch POI và đẩy lên Supabase Storage/DB.
Usage:
    python scripts/generate_audio.py --translation-id <UUID> --text "Nội dung" --lang vi
"""

import os
import sys
import argparse
import asyncio
from datetime import datetime, timezone
import requests
import edge_tts

VOICES = {
    "vi": "vi-VN-HoaiMyNeural",  # Tiếng Việt nữ truyền cảm
    "en": "en-US-JennyNeural",   # Tiếng Anh Mỹ chuẩn
    "ja": "ja-JP-NanamiNeural",  # Tiếng Nhật chuẩn Tokyo
    "zh": "zh-CN-XiaoxiaoNeural" # Tiếng Trung phổ thông
}

def load_env(env_path=".env"):
    env_vars = {}
    if not os.path.exists(env_path):
        env_path = os.path.join(os.path.dirname(os.path.dirname(__file__)), ".env")
    
    if os.path.exists(env_path):
        with open(env_path, "r", encoding="utf-8") as f:
            for line in f:
                line = line.strip()
                if line and not line.startswith("#") and "=" in line:
                    key, val = line.split("=", 1)
                    env_vars[key.strip()] = val.strip()
    return env_vars

async def generate_and_upload(translation_id: str, text: str, lang: str):
    env = load_env()
    base_url = env.get("Supabase__Url") or os.environ.get("Supabase__Url")
    key = env.get("Supabase__ServiceRoleKey") or os.environ.get("Supabase__ServiceRoleKey")
    bucket = env.get("Supabase__AudioBucket", "audio")

    if not base_url or not key:
        print("[ERROR] Thiếu Supabase__Url hoặc Supabase__ServiceRoleKey")
        sys.exit(1)

    voice = VOICES.get(lang.lower(), VOICES["vi"])
    storage_path = f"pois/{translation_id}/{lang.lower()}.mp3"
    temp_file = os.path.join(os.path.dirname(__file__), f"temp_{translation_id}.mp3")

    try:
        # 1. Sinh file MP3 qua Edge TTS
        communicate = edge_tts.Communicate(text, voice)
        await communicate.save(temp_file)
        file_size = os.path.getsize(temp_file)

        # 2. Upload lên Supabase Storage
        upload_url = f"{base_url}/storage/v1/object/{bucket}/{storage_path}"
        headers = {
            "apikey": key,
            "Authorization": f"Bearer {key}",
            "Content-Type": "audio/mpeg",
            "x-upsert": "true"
        }
        with open(temp_file, "rb") as f:
            content = f.read()

        res_upload = requests.post(upload_url, headers=headers, data=content)
        if res_upload.status_code not in (200, 201):
            raise Exception(f"Lỗi upload Supabase Storage: {res_upload.status_code} - {res_upload.text}")

        # 3. Cập nhật vào DB
        patch_url = f"{base_url}/rest/v1/poi_translation?id=eq.{translation_id}"
        db_headers = {
            "apikey": key,
            "Authorization": f"Bearer {key}",
            "Content-Type": "application/json",
            "Prefer": "return=minimal"
        }
        patch_payload = {
            "audio_path": storage_path,
            "updated_at": datetime.now(timezone.utc).isoformat()
        }
        res_db = requests.patch(patch_url, headers=db_headers, json=patch_payload)
        if res_db.status_code not in (200, 204):
            raise Exception(f"Lỗi update DB: {res_db.status_code} - {res_db.text}")

        # 4. Sinh Signed URL 1h để preview
        sign_url = f"{base_url}/storage/v1/object/sign/{bucket}/{storage_path}"
        res_sign = requests.post(sign_url, headers=db_headers, json={"expiresIn": 3600})
        signed_audio_url = ""
        if res_sign.status_code == 200:
            data = res_sign.json()
            raw_url = data.get("signedURL") or data.get("signedUrl")
            signed_audio_url = f"{base_url}/storage/v1{raw_url}" if raw_url.startswith("/") else f"{base_url}/storage/v1/{raw_url}"

        print(f"[SUCCESS] Audio generated ({file_size} bytes). Storage: {storage_path}")
        print(f"[SIGNED_URL] {signed_audio_url}")
        return signed_audio_url

    finally:
        if os.path.exists(temp_file):
            try:
                os.remove(temp_file)
            except Exception:
                pass

def main():
    parser = argparse.ArgumentParser(description="MultiGuide - Sinh Audio TTS cho POI Translation")
    parser.add_argument("--translation-id", required=True, help="UUID của PoiTranslation")
    parser.add_argument("--text", required=True, help="Văn bản thuyết minh cần đọc")
    parser.add_argument("--lang", default="vi", help="Mã ngôn ngữ (vi, en, ja, zh)")

    args = parser.parse_args()
    asyncio.run(generate_and_upload(args.translation_id, args.text, args.lang))

if __name__ == "__main__":
    main()
