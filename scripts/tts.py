import sys, argparse, asyncio, os, re

if hasattr(sys.stdout, 'reconfigure'):
    sys.stdout.reconfigure(encoding='utf-8', errors='replace')
if hasattr(sys.stderr, 'reconfigure'):
    sys.stderr.reconfigure(encoding='utf-8', errors='replace')

async def synthesize_chunk(text, voice, rate, outfile):
    import edge_tts
    comm = edge_tts.Communicate(
        text,
        voice,
        rate=rate,
        connect_timeout=10,
        receive_timeout=60
    ) if (rate and rate.strip() not in ('', '+0%', '0%')) else edge_tts.Communicate(
        text,
        voice,
        connect_timeout=10,
        receive_timeout=60
    )
    await comm.save(outfile)
    return os.path.exists(outfile) and os.path.getsize(outfile) > 100

def clean_text_for_tts(text):
    if not text:
        return ""
    # Chuyển đổi em-dash và en-dash thành dấu phẩy để giọng đọc ngắt nghỉ tự nhiên,
    # đồng thời loại trừ triệt để lỗi "No audio was received" của Edge TTS
    t = text.replace('\ufeff', '').replace('\u200b', '')
    t = t.replace('—', ', ').replace('–', ', ')
    t = t.replace('“', '').replace('”', '').replace('"', '')
    t = re.sub(r'[\r\n]+', '. ', t)
    t = re.sub(r'\s*([.,;?!])\s*', r'\1 ', t)
    t = re.sub(r'\s+', ' ', t).strip()
    return t

async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--text-file', required=True)
    parser.add_argument('--voice', default='vi-VN-HoaiMyNeural')
    parser.add_argument('--rate', default='-15%')
    parser.add_argument('--output', required=True)
    args, unknown = parser.parse_known_args()
    if unknown:
        for u in unknown:
            if re.match(r'^[+-]?\d+%$', u):
                args.rate = u
                break

    with open(args.text_file, 'r', encoding='utf-8-sig') as f:
        raw_text = f.read().strip()

    text = clean_text_for_tts(raw_text)
    if not text:
        sys.exit(1)

    # 1. Direct synthesis with up to 2 retries (Fast: ~1.5s)
    last_err = None
    for attempt in range(2):
        try:
            if await synthesize_chunk(text, args.voice, args.rate, args.output):
                print(f"SUCCESS: {os.path.getsize(args.output)} bytes")
                sys.exit(0)
        except Exception as e:
            last_err = e
            await asyncio.sleep(0.2)

    # 2. If direct synthesis fails, chunk by larger blocks (around 450 chars) to minimize connections
    print(f"Direct synthesis warning: {last_err}, falling back to fast chunking...", file=sys.stderr)
    try:
        sentences = [s.strip() for s in re.split(r'([.?!;\n]+)', text) if s.strip()]
        chunks = []
        cur = ""
        for s in sentences:
            if len(cur) + len(s) < 450:
                cur += (" " if cur else "") + s
            else:
                if cur: chunks.append(cur)
                cur = s
        if cur: chunks.append(cur)

        combined_bytes = bytearray()
        for idx, chunk in enumerate(chunks):
            tmp_part = f"{args.output}.part_{idx}.mp3"
            success = False
            for r in range(2):
                try:
                    if await synthesize_chunk(chunk, args.voice, args.rate, tmp_part):
                        with open(tmp_part, 'rb') as pf:
                            combined_bytes.extend(pf.read())
                        success = True
                        break
                except Exception:
                    await asyncio.sleep(0.2)
            if not success:
                raise RuntimeError(f"Failed to synthesize chunk: {chunk[:30]}")
            if os.path.exists(tmp_part):
                os.remove(tmp_part)

        with open(args.output, 'wb') as out_f:
            out_f.write(combined_bytes)

        if os.path.exists(args.output) and os.path.getsize(args.output) > 100:
            print(f"SUCCESS_CHUNKED: {os.path.getsize(args.output)} bytes")
            sys.exit(0)
    except Exception as e:
        print(f"FAIL: {e}", file=sys.stderr)
        sys.exit(2)

if __name__ == '__main__':
    asyncio.run(main())
