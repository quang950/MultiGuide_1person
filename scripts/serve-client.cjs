// Local preview: node scripts/serve-client.cjs (Ctrl+C to stop).
const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');

const root = path.resolve(__dirname, '../client');
const types = {
  '.html': 'text/html; charset=utf-8', '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8', '.json': 'application/json',
  '.svg': 'image/svg+xml', '.jpg': 'image/jpeg', '.png': 'image/png',
  '.mp3': 'audio/mpeg'
};

http.createServer((req, res) => {
  let file;
  try {
    const pathname = decodeURIComponent(new URL(req.url, 'http://localhost').pathname);
    file = path.resolve(root, '.' + pathname.replace(/\/$/, '/index.html'));
  } catch {
    res.writeHead(400).end(); return;
  }
  if (!file.startsWith(root + path.sep)) { res.writeHead(403).end(); return; }
  fs.stat(file, (error, stat) => {
    if (error || !stat.isFile()) { res.writeHead(404).end(); return; }
    const headers = {'Content-Type': types[path.extname(file)] || 'application/octet-stream',
      'Accept-Ranges': 'bytes', 'Cache-Control': 'no-store'};
    let start = 0;
    let end = stat.size - 1;
    if (req.headers.range) {
      const match = /^bytes=(\d+)-(\d*)$/.exec(req.headers.range);
      if (!match || (start = Number(match[1])) >= stat.size ||
          (end = match[2] ? Math.min(Number(match[2]), end) : end) < start) {
        res.writeHead(416, {'Content-Range': `bytes */${stat.size}`}).end(); return;
      }
      headers['Content-Range'] = `bytes ${start}-${end}/${stat.size}`;
    }
    headers['Content-Length'] = stat.size ? end - start + 1 : 0;
    res.writeHead(req.headers.range ? 206 : 200, headers);
    if (req.method === 'HEAD' || !stat.size) { res.end(); return; }
    const stream = fs.createReadStream(file, {start, end});
    stream.on('error', () => res.destroy());
    res.on('close', () => stream.destroy());
    stream.pipe(res);
  });
}).listen(8080, '127.0.0.1', () => console.log('MultiGuide client: http://127.0.0.1:8080'));
