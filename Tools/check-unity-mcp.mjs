import { spawn } from 'node:child_process';
import { resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

// Uji baca-saja untuk transport stdio MCP yang sama dengan konfigurasi project.
const project = resolve(fileURLToPath(new URL('..', import.meta.url)));
const cli = process.env.UNITY_CLI || '/Users/jgo/.unity/bin/unity';
const child = spawn(cli, ['mcp', '--project-path', project], { stdio: ['pipe', 'pipe', 'pipe'] });
const pending = new Map();
let sequence = 0, buffer = '';
child.stdout.setEncoding('utf8');
child.stdout.on('data', data => {
  buffer += data;
  let end;
  while ((end = buffer.indexOf('\n')) !== -1) {
    const line = buffer.slice(0, end); buffer = buffer.slice(end + 1);
    let response;
    try { response = JSON.parse(line); } catch { continue; }
    const request = pending.get(response.id);
    if (!request) continue;
    pending.delete(response.id);
    if (response.error) request.reject(new Error(JSON.stringify(response.error)));
    else request.resolve(response.result);
  }
});
child.stderr.resume(); // Logs can contain local server metadata; do not echo them.
child.on('error', error => { for (const p of pending.values()) p.reject(error); });
function request(method, params = {}) {
  return new Promise((resolve, reject) => {
    const id = ++sequence;
    pending.set(id, { resolve, reject });
    child.stdin.write(JSON.stringify({ jsonrpc: '2.0', id, method, params }) + '\n');
  });
}
const deadline = setTimeout(() => {
  for (const p of pending.values()) p.reject(new Error('MCP handshake timed out'));
}, 45000);
try {
  const initialized = await request('initialize', {
    protocolVersion: '2025-06-18', capabilities: {},
    clientInfo: { name: 'geprek-local-verification', version: '1.0.0' },
  });
  child.stdin.write(JSON.stringify({ jsonrpc: '2.0', method: 'notifications/initialized' }) + '\n');
  const catalog = await request('tools/list');
  if (!catalog.tools.some(tool => tool.name === 'editor_status')) throw new Error('editor_status is missing');
  const editor = await request('tools/call', { name: 'editor_status', arguments: {} });
  if (editor.isError) throw new Error('Editor status request failed');
  console.log(JSON.stringify({ server: initialized.serverInfo, protocol: initialized.protocolVersion,
    toolCount: catalog.tools.length, editor }, null, 2));
} catch (error) {
  console.error(error.message); process.exitCode = 1;
} finally {
  clearTimeout(deadline); child.stdin.end(); child.kill('SIGTERM');
}
