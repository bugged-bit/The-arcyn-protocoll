import { test, expect } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';

test('verify ARCYN build artifact exists and is valid', () => {
  const execPath = process.env.APP_PATH;
  expect(execPath, 'APP_PATH environment variable must be set').toBeDefined();

  const resolved = path.resolve(execPath!);
  expect(fs.existsSync(resolved), `Binary not found at ${resolved}`).toBe(true);

  const stats = fs.statSync(resolved);
  expect(stats.isFile()).toBe(true);
  // Published .NET self-contained app should be >64 KB
  expect(stats.size).toBeGreaterThan(64 * 1024);

  // PE executables start with MZ magic bytes
  const fd = fs.openSync(resolved, 'r');
  const buf = Buffer.alloc(2);
  fs.readSync(fd, buf, 0, 2, 0);
  fs.closeSync(fd);
  const isMZ = buf[0] === 0x4d && buf[1] === 0x5a;
  expect(isMZ).toBe(true);
});
