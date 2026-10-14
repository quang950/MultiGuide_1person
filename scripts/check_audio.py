import requests
from seed_audio import load_env

env = load_env()
url = env.get("Supabase__Url")
key = env.get("Supabase__ServiceRoleKey")

headers = {
    "apikey": key,
    "Authorization": f"Bearer {key}"
}

r = requests.get(f"{url}/rest/v1/poi_translation?select=id,poi_id,language_code,title,audio_path&order=poi_id,language_code", headers=headers)
rows = r.json()

missing = []
print(f"Tổng số bản dịch: {len(rows)}")
for idx, row in enumerate(rows, 1):
    audio = row.get("audio_path")
    has_audio = "✓" if audio else "✗"
    if not audio:
        missing.append(row)
    print(f"[{idx:2d}] {row['language_code'].upper()} | {has_audio} | {audio} | {row['title'][:35]}")

print(f"\nSố bản dịch đã có audio: {len(rows) - len(missing)}/{len(rows)}")
if missing:
    print(f"Số bản dịch còn thiếu audio: {len(missing)}")
    for m in missing:
        print(f" - {m['id']} ({m['language_code']}): {m['title']}")
