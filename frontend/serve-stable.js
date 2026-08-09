#!/usr/bin/env node
/**
 * Stable static frontend server (no watch / HMR).
 * Serves dist/stratix-web/browser and proxies /api → localhost:8080.
 */
const http = require('http');
const fs = require('fs');
const path = require('path');
const { URL } = require('url');

const PORT = Number(process.env.PORT || 4200);
const ROOT = path.join(__dirname, 'dist', 'stratix-web', 'browser');
const API = 'http://127.0.0.1:8080';

const TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.map': 'application/json',
  '.txt': 'text/plain; charset=utf-8',
};

const ASSET_EXT = new Set(Object.keys(TYPES).filter((e) => e !== '.html'));

function sendFile(res, filePath) {
  const ext = path.extname(filePath);
  res.writeHead(200, {
    'Content-Type': TYPES[ext] || 'application/octet-stream',
    'Cache-Control': 'no-store',
  });
  fs.createReadStream(filePath).pipe(res);
}

function proxyApi(req, res) {
  const target = new URL(req.url, API);
  const headers = { ...req.headers, host: '127.0.0.1:8080' };
  const upstream = http.request(
    target,
    { method: req.method, headers },
    (up) => {
      res.writeHead(up.statusCode || 502, up.headers);
      up.pipe(res);
    },
  );
  upstream.on('error', () => {
    res.writeHead(502, { 'Content-Type': 'text/plain' });
    res.end('API unavailable');
  });
  req.pipe(upstream);
}

const server = http.createServer((req, res) => {
  const urlPath = decodeURIComponent((req.url || '/').split('?')[0]);
  if (urlPath === '/api' || urlPath.startsWith('/api/')) {
    proxyApi(req, res);
    return;
  }

  let filePath = path.join(ROOT, urlPath === '/' ? 'index.html' : urlPath);
  if (!filePath.startsWith(ROOT)) {
    res.writeHead(403).end();
    return;
  }

  const ext = path.extname(filePath).toLowerCase();

  fs.stat(filePath, (err, st) => {
    if (!err && st.isFile()) {
      sendFile(res, filePath);
      return;
    }

    // Never SPA-fallback asset requests — returning HTML as JS freezes the app.
    if (ASSET_EXT.has(ext)) {
      res.writeHead(404, { 'Content-Type': 'text/plain; charset=utf-8' });
      res.end('Not found');
      return;
    }

    sendFile(res, path.join(ROOT, 'index.html'));
  });
});

server.listen(PORT, '127.0.0.1', () => {
  console.log(`Stratix static UI: http://localhost:${PORT}/ (no live reload)`);
});
