/**
 * Advanced CAPTCHA SDK
 * Drop-in integration for the self-hosted CAPTCHA API.
 *
 * Usage:
 *   const captcha = new AdvancedCaptcha({ apiBase: 'https://your-api.com' });
 *   await captcha.init();   // starts behavioral collector + fingerprinting
 *   const token = await captcha.solve();  // runs PoW, behavioral, interactive if needed
 *
 * Include behavioral-collector.js, fingerprint.js, and pow-worker.js in the same dir.
 */

class AdvancedCaptcha {
  constructor(options = {}) {
    this.apiBase = options.apiBase ?? '';
    this.workerPath = options.workerPath ?? '/client/pow-worker.js';
    this.onInteractiveChallenge = options.onInteractiveChallenge ?? null;
    this._token = null;
    this._sessionId = options.sessionId ?? this._generateId();
  }

  async init() {
    // Start behavioral collection immediately
    if (typeof BehavioralCollector !== 'undefined') BehavioralCollector.start();
    // Pre-collect fingerprint
    if (typeof collectFingerprint !== 'undefined')
      this._fingerprint = await collectFingerprint();
  }

  async solve() {
    // Step 1: Get PoW challenge
    const fpHash = this._fingerprint?.hash ?? null;
    const challengeRes = await this._post('/api/captcha/challenge', {
      sessionId: this._sessionId,
      userAgent: navigator.userAgent,
      fingerprint: fpHash,
    });

    if (!challengeRes.challengeId) throw new Error('Failed to get challenge');

    // Step 2: Solve PoW in Web Worker
    const nonce = await this._solvePoW(challengeRes.publicData, challengeRes.difficulty);

    // Step 3: Collect behavioral data
    const behavioralData = typeof BehavioralCollector !== 'undefined'
      ? BehavioralCollector.collect() : null;

    // Step 4: Submit solution
    const verifyRes = await this._post('/api/captcha/verify', {
      challengeId: challengeRes.challengeId,
      solution: nonce,
      behavioralData,
      fingerprint: fpHash,
    });

    if (verifyRes.success && verifyRes.token) {
      this._token = verifyRes.token;
      return verifyRes.token;
    }

    // Step 5: Interactive challenge if required
    if (verifyRes.requiresInteractiveChallenge) {
      const token = await this._handleInteractive(verifyRes.challengeType);
      this._token = token;
      return token;
    }

    throw new Error(verifyRes.message ?? 'CAPTCHA verification failed');
  }

  async _handleInteractive(challengeType) {
    // Get interactive challenge data
    const data = await this._post('/api/captcha/interactive/get', {
      challengeId: this._sessionId,
      challengeType,
    });

    // Let integrator render the challenge and collect the answer
    if (typeof this.onInteractiveChallenge === 'function') {
      const answer = await this.onInteractiveChallenge(data);
      const result = await this._post('/api/captcha/interactive/verify', {
        challengeId: data.challengeId,
        answer: String(answer),
      });
      if (result.success && result.token) return result.token;
      throw new Error('Interactive challenge failed');
    }

    throw new Error('Interactive challenge required but no handler provided. ' +
      'Set options.onInteractiveChallenge = async (challengeData) => { ... return answer; }');
  }

  _solvePoW(challenge, difficulty) {
    return new Promise((resolve, reject) => {
      const worker = new Worker(this.workerPath);
      worker.postMessage({ challenge, difficulty });
      worker.onmessage = (e) => { worker.terminate(); resolve(e.data.nonce); };
      worker.onerror = (e) => { worker.terminate(); reject(e); };
    });
  }

  async _post(path, body) {
    const res = await fetch(this.apiBase + path, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });
    if (!res.ok) throw new Error(`HTTP ${res.status}`);
    return res.json();
  }

  getToken() { return this._token; }

  _generateId() {
    return ([1e7] + -1e3 + -4e3 + -8e3 + -1e11).replace(/[018]/g, c =>
      (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16));
  }
}

if (typeof module !== 'undefined') module.exports = AdvancedCaptcha;
