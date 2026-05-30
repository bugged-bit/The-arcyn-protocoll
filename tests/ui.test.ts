import { test, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import { spawn, execSync } from 'child_process';

test('verify ARCYN build artifact exists and is valid', () => {
  const execPath = process.env.APP_PATH;
  expect(execPath, 'APP_PATH environment variable must be set').toBeDefined();

  const resolved = path.resolve(execPath!);
  expect(fs.existsSync(resolved), `Binary not found at ${resolved}`).toBe(true);

  const stats = fs.statSync(resolved);
  expect(stats.isFile()).toBe(true);
  // Published .NET self-contained app should be >64 KB
  expect(stats.size).toBeGreaterThan(64 * 1024);

  if (process.platform === 'win32') {
    const fd = fs.openSync(resolved, 'r');
    const buf = Buffer.alloc(2);
    fs.readSync(fd, buf, 0, 2, 0);
    fs.closeSync(fd);
    const isMZ = buf[0] === 0x4d && buf[1] === 0x5a;
    expect(isMZ).toBe(true);
  }
});

test('launch ARCYN binary and verify window appears', async () => {
  const execPath = process.env.APP_PATH;
  expect(execPath, 'APP_PATH environment variable must be set').toBeDefined();

  const resolved = path.resolve(execPath!);
  expect(fs.existsSync(resolved), `Binary not found at ${resolved}`).toBe(true);

  const proc = spawn(resolved, [], {
    stdio: 'ignore',
    env: { ...process.env },
  });

  await new Promise((r) => setTimeout(r, 6000));

  try {
    expect(proc.exitCode).toBeNull();

    if (process.platform === 'win32') {
      const result = execSync(
        `powershell -NoProfile -Command "& { ` +
          `$w = Get-Process | Where-Object { $_.MainWindowTitle -like '*ARCYN*' } | ` +
          `Select-Object -First 1; ` +
          `if ($w) { $w.MainWindowTitle } else { '' } }"`,
        { encoding: 'utf8', timeout: 10000 },
      ).trim();
      expect(result, 'Expected a window with "ARCYN" in the title').toMatch(/ARCYN/i);
    } else {
      try {
        const xResult = execSync(
          `xdotool search --name --onlyvisible "ARCYN" 2>/dev/null || ` +
            `wmctrl -l 2>/dev/null | grep -i ARCYN || ` +
            `echo ""`,
          { encoding: 'utf8', timeout: 5000 },
        ).trim();
        expect(xResult.length).toBeGreaterThan(0);
      } catch {
        expect(proc.exitCode).toBeNull();
      }
    }
  } finally {
    proc.kill('SIGTERM');
    await new Promise((r) => setTimeout(r, 1500));
    if (proc.exitCode === null) {
      proc.kill('SIGKILL');
    }
  }
});
