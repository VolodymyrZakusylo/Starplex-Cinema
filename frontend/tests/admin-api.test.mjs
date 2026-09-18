import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import test from 'node:test';
import ts from 'typescript';

const stubUrl = `data:text/javascript,${encodeURIComponent('export default globalThis.adminApiStub')}`;
const calls = [];
globalThis.adminApiStub = {
  put: async (...args) => { calls.push(args); },
  delete: async () => ({ data: { outcome: 'Deactivated' } })
};

async function load(relativePath) {
  const source = await readFile(new URL(relativePath, import.meta.url), 'utf8');
  const { outputText } = ts.transpileModule(source, { compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2022 } });
  const code = outputText
    .replace(/from ['"]\.\/axios['"]/, `from ${JSON.stringify(stubUrl)}`)
    .replace(/from ['"]axios['"]/, `from ${JSON.stringify(import.meta.resolve('axios'))}`);
  return import(`data:text/javascript;base64,${Buffer.from(code).toString('base64')}`);
}

test('role API sends numeric enum values and matching property names', async () => {
  const { UserRole } = await load('../src/types/admin.ts');
  const { usersApi } = await load('../src/api/users.ts');
  assert.deepEqual(UserRole, { SuperAdmin: 0, CinemaManager: 1, Cashier: 2, Customer: 3 });
  for (const role of Object.values(UserRole)) {
    await usersApi.updateRole('user-id', role, null);
    assert.deepEqual(calls.at(-1), ['/Users/user-id/role', { newRole: role, cinemaId: null }]);
  }
});

test('promo API preserves the delete/deactivate outcome', async () => {
  const { discountsApi } = await load('../src/api/discounts.ts');
  assert.deepEqual(await discountsApi.delete('promo-id'), { outcome: 'Deactivated' });
});

test('admin errors display validation details, ProblemDetails and fallback messages', async () => {
  const { getApiErrorMessage } = await load('../src/api/errors.ts');
  const error = data => ({ isAxiosError: true, response: { data } });
  assert.equal(getApiErrorMessage(error({ detail: 'Hall has history' }), 'fallback'), 'Hall has history');
  assert.equal(getApiErrorMessage(error({ detail: 'Validation failed', errors: { NewRole: ['Choose a valid role'] } }), 'fallback'), 'Choose a valid role');
  assert.equal(getApiErrorMessage(error({ message: 'Identity rejected update' }), 'fallback'), 'Identity rejected update');
  assert.equal(getApiErrorMessage(new Error('network'), 'fallback'), 'fallback');
});
