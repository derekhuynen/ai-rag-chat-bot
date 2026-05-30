// Captures the five README marketing screenshots from a running deployment.
//
// It logs in as the admin, runs two demo chats (a RAG answer with citations and
// a markdown/code answer), and visits the admin pages, writing pic1..pic5.png.
// Optionally registers a neutral demo user first so the admin user table has more
// than one row. Everything is driven by env vars so no secrets live in the repo.
//
// Usage (PowerShell):
//   cd scripts/screenshots
//   npm install                       # also installs the chromium browser
//   $env:SPA_URL = "https://<web endpoint>"
//   $env:ADMIN_PASSWORD = "<admin password>"
//   npm run capture
//
// Env vars:
//   SPA_URL            (required) the static-website URL of the SPA
//   ADMIN_EMAIL        admin login (default admin@example.com)
//   ADMIN_PASSWORD     (required) admin password
//   ADMIN_NAME         admin display name shown in the sidebar (default Administrator)
//   CREATE_DEMO_USER   "true" to register a demo user for the table (default "true")
//   DEMO_EMAIL         demo user email (default demo@example.com)
//   DEMO_PASSWORD      demo user password (default DemoPassword123!)
//   DEMO_NAME          demo user name (default Demo User)
//   OUT_DIR            output folder (default ../../images)
//   HERO_PROMPT        first chat prompt (RAG + citations)
//   CODE_PROMPT        second chat prompt (markdown + code)

import { chromium } from 'playwright';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
import { mkdirSync } from 'node:fs';

const __dirname = dirname(fileURLToPath(import.meta.url));

const cfg = {
  spaUrl: (process.env.SPA_URL || '').replace(/\/$/, ''),
  adminEmail: process.env.ADMIN_EMAIL || 'admin@example.com',
  adminPassword: process.env.ADMIN_PASSWORD || '',
  adminName: process.env.ADMIN_NAME || 'Administrator',
  createDemoUser: (process.env.CREATE_DEMO_USER || 'true').toLowerCase() !== 'false',
  demoEmail: process.env.DEMO_EMAIL || 'demo@example.com',
  demoPassword: process.env.DEMO_PASSWORD || 'DemoPassword123!',
  demoName: process.env.DEMO_NAME || 'Demo User',
  outDir: process.env.OUT_DIR || resolve(__dirname, '../../images'),
  heroPrompt: process.env.HERO_PROMPT || 'Tell me about projects that use AI.',
  codePrompt:
    process.env.CODE_PROMPT ||
    'Show me the exact commands to run the backend and frontend locally, in code blocks.',
};

if (!cfg.spaUrl) throw new Error('SPA_URL is required');
if (!cfg.adminPassword) throw new Error('ADMIN_PASSWORD is required');

mkdirSync(cfg.outDir, { recursive: true });

const shot = (page, name) =>
  page.screenshot({ path: resolve(cfg.outDir, name), animations: 'disabled' });

// Wait for a streamed answer to finish: the message input is disabled while
// streaming (ChatPage passes disabled={isStreaming}), so we wait for it to
// settle back to enabled, then give the DOM a moment to paint citations.
async function waitForAnswer(page) {
  const input = page.getByPlaceholder('Send a message...');
  await page.waitForTimeout(1000); // let streaming start
  await input.waitFor({ state: 'visible' });
  // Poll until the input is interactable again (streaming finished).
  await page
    .waitForFunction(
      () => {
        const el = document.querySelector('textarea[placeholder="Send a message..."]');
        return el && !el.disabled;
      },
      { timeout: 180000 }
    )
    .catch(() => {});
  await page.waitForTimeout(1500); // settle
}

// Both the login and register modals are always mounted in the sidebar and share
// field labels, and they overlap briefly during the fade transition, so every
// field lookup is scoped to the specific dialog (matched by its heading).
const dialogByHeading = (page, name) =>
  page.getByRole('dialog').filter({ has: page.getByRole('heading', { name }) });

async function login(page, email, password, remember) {
  await page.getByRole('button', { name: 'Log in' }).click();
  const dlg = dialogByHeading(page, 'Sign In');
  await dlg.waitFor();
  await dlg.getByLabel('Email Address').fill(email);
  await dlg.getByLabel('Password').fill(password);
  if (remember) await dlg.getByRole('checkbox', { name: 'Remember me' }).check();
  await dlg.getByRole('button', { name: 'Sign In' }).click();
  // Logged-in chat view shows the message input.
  await page.getByPlaceholder('Send a message...').waitFor({ timeout: 30000 });
}

async function registerDemoUser(page) {
  await page.getByRole('button', { name: 'Log in' }).click();
  const signIn = dialogByHeading(page, 'Sign In');
  await signIn.waitFor();
  await signIn.getByRole('button', { name: 'Sign up' }).click();
  const dlg = dialogByHeading(page, 'Create Account');
  await dlg.waitFor();
  await dlg.getByLabel('Name', { exact: true }).fill(cfg.demoName);
  await dlg.getByLabel('Email Address').fill(cfg.demoEmail);
  await dlg.getByLabel('Password').fill(cfg.demoPassword);
  await dlg.getByRole('button', { name: 'Create Account' }).click();
  // Either it succeeds (input appears) or the user already exists (alert shown).
  const ok = page.getByPlaceholder('Send a message...');
  const alert = dlg.getByRole('alert');
  await Promise.race([
    ok.waitFor({ timeout: 15000 }).catch(() => {}),
    alert.waitFor({ timeout: 15000 }).catch(() => {}),
  ]);
  if (await alert.isVisible().catch(() => false)) {
    console.log('  demo user already exists or registration rejected; continuing.');
  } else {
    console.log('  demo user registered.');
  }
}

async function logout(page) {
  await page.evaluate(() => {
    localStorage.clear();
    sessionStorage.clear();
  });
  await page.context().clearCookies();
  await page.goto(cfg.spaUrl, { waitUntil: 'networkidle' });
}

async function sendChat(page, prompt) {
  const input = page.getByPlaceholder('Send a message...');
  await input.fill(prompt);
  await input.press('Enter');
  await waitForAnswer(page);
}

(async () => {
  const browser = await chromium.launch();
  const context = await browser.newContext({
    viewport: { width: 1440, height: 900 },
    deviceScaleFactor: 2,
  });
  const page = await context.newPage();

  try {
    console.log(`Opening ${cfg.spaUrl}`);
    await page.goto(cfg.spaUrl, { waitUntil: 'networkidle' });

    if (cfg.createDemoUser) {
      console.log('Registering demo user...');
      await registerDemoUser(page);
      await logout(page);
    }

    console.log(`Logging in as ${cfg.adminEmail}...`);
    await login(page, cfg.adminEmail, cfg.adminPassword, true);

    // --- pic1 + pic4: RAG answer with citations -----------------------------
    console.log('Chat: RAG answer...');
    await sendChat(page, cfg.heroPrompt);

    // pic1 (hero): top of the conversation.
    await page.mouse.wheel(0, -5000);
    await page.waitForTimeout(500);
    await shot(page, 'pic1.png');
    console.log('  wrote pic1.png (Chat UI)');

    // pic4: scroll the citations ("Sources:") into view.
    const sources = page.getByText('Sources:', { exact: false }).last();
    if (await sources.isVisible().catch(() => false)) {
      await sources.scrollIntoViewIfNeeded();
      await page.waitForTimeout(500);
    } else {
      await page.mouse.wheel(0, 5000);
      await page.waitForTimeout(500);
      console.log('  note: no "Sources:" found - is the demo corpus seeded?');
    }
    await shot(page, 'pic4.png');
    console.log('  wrote pic4.png (RAG answer with citations)');

    // --- pic5: markdown + code rendering ------------------------------------
    console.log('Chat: markdown/code answer...');
    await sendChat(page, cfg.codePrompt);
    const pre = page.locator('pre').last();
    if (await pre.isVisible().catch(() => false)) {
      await pre.scrollIntoViewIfNeeded();
      await page.waitForTimeout(500);
    }
    await shot(page, 'pic5.png');
    console.log('  wrote pic5.png (Markdown & code rendering)');

    // --- pic2: admin dashboard ----------------------------------------------
    console.log('Admin dashboard...');
    await page.goto(`${cfg.spaUrl}/admin`, { waitUntil: 'networkidle' });
    await page.getByText('Admin Dashboard', { exact: false }).first().waitFor({ timeout: 30000 });
    await page.waitForTimeout(1000);
    await shot(page, 'pic2.png');
    console.log('  wrote pic2.png (Admin Dashboard)');

    // --- pic3: admin documents ----------------------------------------------
    console.log('Admin documents...');
    await page.goto(`${cfg.spaUrl}/admin/documents`, { waitUntil: 'networkidle' });
    await page.getByText('Document Library', { exact: false }).first().waitFor({ timeout: 30000 });
    await page.waitForTimeout(1500); // let the table load
    await shot(page, 'pic3.png');
    console.log('  wrote pic3.png (Admin Documents)');

    console.log(`\nDone. Screenshots written to ${cfg.outDir}`);
  } catch (err) {
    console.error('Capture failed:', err);
    await shot(page, 'capture-error.png').catch(() => {});
    process.exitCode = 1;
  } finally {
    await browser.close();
  }
})();
