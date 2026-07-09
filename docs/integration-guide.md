# Integration Guide

## Overview

This guide explains how to embed the Advanced CAPTCHA into any web application.

## 1. Include client scripts

```html
<script src="/client/behavioral-collector.js"></script>
<script src="/client/fingerprint.js"></script>
<script src="/client/captcha-sdk.js"></script>
```

## 2. Initialize on page load

```js
const captcha = new AdvancedCaptcha({
  apiBase: 'https://your-captcha-api.com',
  workerPath: '/client/pow-worker.js',
  // Optional: handle interactive challenges
  onInteractiveChallenge: async (data) => {
    // Render data.data (ImageChallenge / PhysicsPuzzleChallenge / etc.)
    // Return the user's answer as a string
    return await showChallengeUI(data);
  }
});

await captcha.init(); // Start behavioral tracking
```

## 3. Solve before form submit

```js
document.querySelector('#myForm').addEventListener('submit', async (e) => {
  e.preventDefault();
  try {
    const token = await captcha.solve();
    document.querySelector('#captchaToken').value = token;
    e.target.submit();
  } catch (err) {
    alert('CAPTCHA failed: ' + err.message);
  }
});
```

## 4. Validate token server-side

In your application's endpoint that processes the form, inject `ITokenService` and call:

```csharp
var (valid, payload) = await _tokenService.ValidateTokenAsync(tokenFromRequest);
if (!valid) return Results.Forbid();
// Optionally check payload.Ip matches request IP
```

## 5. Interactive challenge UI examples

### Image Text
```js
if (data.type === 'ImageText') {
  const img = document.createElement('img');
  img.src = data.data.imageUrl;
  document.body.appendChild(img);
  return prompt(data.data.instruction);
}
```

### Logical Reasoning
```js
if (data.type === 'LogicalReasoning') {
  return await renderButtonGroup(data.data.options); // returns chosen label
}
```
