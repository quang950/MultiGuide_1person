// Generate bundled Mandarin narration without changing the production database.
const fs = require('node:fs/promises');
const path = require('node:path');
async function main() {
  const stories = JSON.parse(await fs.readFile(path.join(__dirname, 'chinese-stories.json'), 'utf8'));
  for (const story of stories) {
    const text = story.title + '。' + story.paragraph + (story.subtext || '');
    const chunks = text.match(/.{1,180}/gu);
    const parts = [];
    for (const chunk of chunks) {
      const url = new URL('https://translate.google.com/translate_tts');
      url.search = new URLSearchParams({ie:'UTF-8', client:'tw-ob', tl:'zh-CN', q:chunk});
      const response = await fetch(url, {signal: AbortSignal.timeout(30000)});
      if (!response.ok || !response.headers.get('content-type')?.includes('audio')) {
        throw new Error(`TTS failed: ${response.status}`);
      }
      parts.push(Buffer.from(await response.arrayBuffer()));
    }
    const audio = Buffer.concat(parts);
    if (audio.length < 1000) throw new Error('Audio is empty');
    const folder = path.join(__dirname, '../client/assets/audio/pois');
    await fs.writeFile(path.join(folder, story.id + '_zh.mp3'), audio);
    console.log(`Created Mandarin narration for stop ${story.order}: ${audio.length} bytes`);
  }
}
main().catch(error => { console.error(error); process.exitCode = 1; });
