/**
 * Web Worker: Proof of Work solver
 * Runs SHA-256 mining off the main thread so UI stays responsive.
 * Usage: new Worker('pow-worker.js')
 * Messages in:  { challenge, difficulty }
 * Messages out: { nonce, hash, elapsed }
 */

self.onmessage = async function (e) {
  const { challenge, difficulty } = e.data;
  const prefix = '0'.repeat(difficulty);
  const start = performance.now();
  let nonce = 0;

  while (true) {
    const nonceStr = nonce.toString();
    const input = challenge + nonceStr;
    const hash = await sha256Hex(input);
    if (hash.startsWith(prefix)) {
      const elapsed = Math.round(performance.now() - start);
      self.postMessage({ nonce: nonceStr, hash, elapsed });
      return;
    }
    nonce++;
    // Yield every 5000 iterations to avoid watchdog
    if (nonce % 5000 === 0) await new Promise(r => setTimeout(r, 0));
  }
};

async function sha256Hex(message) {
  const msgBuffer = new TextEncoder().encode(message);
  const hashBuffer = await crypto.subtle.digest('SHA-256', msgBuffer);
  return Array.from(new Uint8Array(hashBuffer))
    .map(b => b.toString(16).padStart(2, '0'))
    .join('');
}
