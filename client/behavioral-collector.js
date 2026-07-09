/**
 * Behavioral Data Collector
 * Collects mouse, keyboard, scroll, touch, and focus events.
 * Call BehavioralCollector.start() on page load.
 * Call BehavioralCollector.collect() before form submission.
 */

const BehavioralCollector = (() => {
  const state = {
    mouseEvents: [],
    keyEvents: [],
    scrollEvents: [],
    focusBlurCount: 0,
    touchEventCount: 0,
    startTime: Date.now(),
    lastScrollY: 0,
    lastScrollTime: Date.now(),
  };

  let mouseThrottle = null;

  function onMouseMove(e) {
    if (mouseThrottle) return;
    mouseThrottle = setTimeout(() => { mouseThrottle = null; }, 75);
    state.mouseEvents.push({ x: e.clientX, y: e.clientY, timestamp: Date.now() });
    // Keep last 500 points to limit payload size
    if (state.mouseEvents.length > 500) state.mouseEvents.shift();
  }

  function onKeyEvent(e) {
    state.keyEvents.push({ timestamp: Date.now(), keyType: e.type });
    if (state.keyEvents.length > 200) state.keyEvents.shift();
  }

  function onScroll() {
    const now = Date.now();
    const dy = Math.abs(window.scrollY - state.lastScrollY);
    const dt = (now - state.lastScrollTime) || 1;
    state.scrollEvents.push({ scrollY: window.scrollY, timestamp: now, velocity: dy / dt });
    state.lastScrollY = window.scrollY;
    state.lastScrollTime = now;
    if (state.scrollEvents.length > 100) state.scrollEvents.shift();
  }

  function onFocusBlur() { state.focusBlurCount++; }
  function onTouch() { state.touchEventCount++; }

  function start() {
    document.addEventListener('mousemove', onMouseMove, { passive: true });
    document.addEventListener('keydown', onKeyEvent, { passive: true });
    document.addEventListener('keyup', onKeyEvent, { passive: true });
    window.addEventListener('scroll', onScroll, { passive: true });
    window.addEventListener('focus', onFocusBlur);
    window.addEventListener('blur', onFocusBlur);
    document.addEventListener('touchstart', onTouch, { passive: true });
  }

  function stop() {
    document.removeEventListener('mousemove', onMouseMove);
    document.removeEventListener('keydown', onKeyEvent);
    document.removeEventListener('keyup', onKeyEvent);
    window.removeEventListener('scroll', onScroll);
    window.removeEventListener('focus', onFocusBlur);
    window.removeEventListener('blur', onFocusBlur);
    document.removeEventListener('touchstart', onTouch);
  }

  function collect() {
    return {
      mouseEvents: [...state.mouseEvents],
      keyEvents: [...state.keyEvents],
      scrollEvents: [...state.scrollEvents],
      timeOnPage: Date.now() - state.startTime,
      focusBlurCount: state.focusBlurCount,
      touchEventCount: state.touchEventCount,
    };
  }

  function reset() {
    state.mouseEvents = [];
    state.keyEvents = [];
    state.scrollEvents = [];
    state.focusBlurCount = 0;
    state.touchEventCount = 0;
    state.startTime = Date.now();
  }

  return { start, stop, collect, reset };
})();

if (typeof module !== 'undefined') module.exports = BehavioralCollector;
