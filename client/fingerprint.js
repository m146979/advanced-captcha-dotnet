/**
 * Client-side fingerprinting and headless browser detection.
 * Returns a fingerprint object to be sent with /api/captcha/challenge.
 */

async function collectFingerprint() {
  const fp = {};

  // 1. Headless/automation detection
  fp.webdriver = !!navigator.webdriver;
  fp.hasChrome = typeof window.chrome !== 'undefined';
  fp.hasNotification = 'Notification' in window;
  fp.hasPlugins = navigator.plugins.length > 0;
  fp.pluginCount = navigator.plugins.length;
  fp.languages = navigator.languages?.join(',') ?? '';
  fp.platform = navigator.platform;
  fp.hardwareConcurrency = navigator.hardwareConcurrency;
  fp.deviceMemory = navigator.deviceMemory ?? -1;
  fp.touchPoints = navigator.maxTouchPoints;
  fp.screenRes = `${screen.width}x${screen.height}`;
  fp.colorDepth = screen.colorDepth;
  fp.timezone = Intl.DateTimeFormat().resolvedOptions().timeZone;
  fp.doNotTrack = navigator.doNotTrack;

  // 2. WebGL fingerprint
  try {
    const canvas = document.createElement('canvas');
    const gl = canvas.getContext('webgl') || canvas.getContext('experimental-webgl');
    if (gl) {
      fp.glVendor = gl.getParameter(gl.VENDOR);
      fp.glRenderer = gl.getParameter(gl.RENDERER);
      const dbgInfo = gl.getExtension('WEBGL_debug_renderer_info');
      if (dbgInfo) {
        fp.glUnmaskedVendor = gl.getParameter(dbgInfo.UNMASKED_VENDOR_WEBGL);
        fp.glUnmaskedRenderer = gl.getParameter(dbgInfo.UNMASKED_RENDERER_WEBGL);
      }
    }
  } catch (_) { fp.glError = true; }

  // 3. Canvas fingerprint hash
  try {
    const canvas = document.createElement('canvas');
    canvas.width = 200; canvas.height = 40;
    const ctx = canvas.getContext('2d');
    ctx.textBaseline = 'alphabetic';
    ctx.font = '14px Arial';
    ctx.fillStyle = '#f60';
    ctx.fillRect(125, 1, 62, 20);
    ctx.fillStyle = '#069';
    ctx.fillText('AdvCaptcha!', 2, 15);
    ctx.fillStyle = 'rgba(102,204,0,0.7)';
    ctx.fillText('AdvCaptcha!', 4, 17);
    fp.canvasHash = canvas.toDataURL().slice(-50);
  } catch (_) { fp.canvasError = true; }

  // 4. Font detection (sample)
  const testFonts = ['Arial', 'Times New Roman', 'Courier New', 'Georgia', 'Verdana', 'Comic Sans MS'];
  fp.detectedFonts = testFonts.filter(font => isFontAvailable(font)).join(',');
  fp.fontCount = fp.detectedFonts.split(',').filter(Boolean).length;

  // 5. Automation indicators
  fp.seleniumPresent = '__selenium_evaluate' in window || 'selenium' in window;
  fp.phantomPresent = 'callPhantom' in window || '_phantom' in window;
  fp.puppeteerPresent = navigator.userAgent.includes('HeadlessChrome');

  // Hash fingerprint to single token
  const fpString = JSON.stringify(fp);
  const encoded = new TextEncoder().encode(fpString);
  const hashBuf = await crypto.subtle.digest('SHA-256', encoded);
  const hashHex = Array.from(new Uint8Array(hashBuf))
    .map(b => b.toString(16).padStart(2, '0')).join('');

  return { raw: fp, hash: hashHex };
}

function isFontAvailable(font) {
  const base = 'monospace';
  const testStr = 'mmmmmmmmmmlli';
  const testSize = '72px';
  const canvas = document.createElement('canvas');
  const ctx = canvas.getContext('2d');
  ctx.font = `${testSize} ${base}`;
  const baseWidth = ctx.measureText(testStr).width;
  ctx.font = `${testSize} '${font}', ${base}`;
  return ctx.measureText(testStr).width !== baseWidth;
}

if (typeof module !== 'undefined') module.exports = { collectFingerprint };
