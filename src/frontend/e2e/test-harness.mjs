// Serves the built SPA against a deployed API, standing in for the Static Web App.
// It does the two things the SWA does for us in Azure: answer /.auth/me, and attach the
// x-ms-client-principal header that the linked backend injects on every proxied request.
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { extname, join, normalize, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const PORT = Number(process.env.HARNESS_PORT || 3000);
const API_URL = (process.env.TEST_API_URL || '').replace(/\/+$/, '');
const DIST = resolve(fileURLToPath(new URL('../dist', import.meta.url)));

if (!API_URL) {
  console.error('TEST_API_URL is required, e.g. https://api-xyz.azurewebsites.net');
  process.exit(1);
}

const principal = {
  identityProvider: 'aad',
  userId: process.env.E2E_USER_ID || 'e2e-test-user',
  userDetails: process.env.E2E_USER_DETAILS || 'e2e@lab.local',
  userRoles: ['anonymous', 'authenticated']
};
const principalHeader = Buffer.from(JSON.stringify(principal)).toString('base64');

const MIME = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
  '.png': 'image/png',
  '.woff2': 'font/woff2'
};

const readBody = req =>
  new Promise((res, rej) => {
    const chunks = [];
    req.on('data', c => chunks.push(c));
    req.on('end', () => res(chunks.length ? Buffer.concat(chunks) : undefined));
    req.on('error', rej);
  });

const sendIndex = async res => {
  res.writeHead(200, { 'content-type': MIME['.html'] });
  res.end(await readFile(join(DIST, 'index.html')));
};

createServer(async (req, res) => {
  try {
    const { pathname } = new URL(req.url, `http://127.0.0.1:${PORT}`);

    if (pathname === '/.auth/me') {
      res.writeHead(200, { 'content-type': 'application/json' });
      return res.end(JSON.stringify({ clientPrincipal: principal }));
    }

    if (pathname.startsWith('/api/')) {
      const upstream = await fetch(`${API_URL}${req.url}`, {
        method: req.method,
        headers: {
          'content-type': req.headers['content-type'] ?? 'application/json',
          'x-ms-client-principal': principalHeader
        },
        body: req.method === 'GET' || req.method === 'HEAD' ? undefined : await readBody(req)
      });
      const body = Buffer.from(await upstream.arrayBuffer());
      res.writeHead(upstream.status, {
        'content-type': upstream.headers.get('content-type') ?? 'application/json'
      });
      return res.end(body);
    }

    // Anything resolving outside dist/ falls through to the SPA entry point.
    const candidate = normalize(join(DIST, pathname));
    if (!candidate.startsWith(DIST + '/') && candidate !== DIST) return sendIndex(res);

    const content = await readFile(candidate);
    res.writeHead(200, { 'content-type': MIME[extname(candidate)] ?? 'application/octet-stream' });
    res.end(content);
  } catch (err) {
    if (err?.code === 'ENOENT' || err?.code === 'EISDIR') return sendIndex(res);
    console.error(err);
    res.writeHead(502, { 'content-type': 'text/plain' });
    res.end('harness error');
  }
}).listen(PORT, '127.0.0.1', () => {
  console.log(`harness http://127.0.0.1:${PORT} -> ${API_URL}`);
});
