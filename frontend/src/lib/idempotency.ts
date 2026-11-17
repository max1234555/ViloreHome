export async function getOrReuseIdempotencyKey(payload: unknown): Promise<string> {
  const serializedPayload = JSON.stringify(payload);
  if (serializedPayload === undefined) {
    throw new TypeError('The request payload cannot be serialized as JSON.');
  }

  const payloadHash = await crypto.subtle.digest(
    'SHA-256',
    new TextEncoder().encode(serializedPayload),
  );
  const hash = Array.from(new Uint8Array(payloadHash))
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('');
  const storageKey = `idem:${hash}`;
  const existingKey = window.localStorage.getItem(storageKey);

  if (existingKey) {
    return existingKey;
  }

  const idempotencyKey = crypto.randomUUID();
  window.localStorage.setItem(storageKey, idempotencyKey);
  return idempotencyKey;
}

export async function clearIdempotencyKey(payload: unknown): Promise<void> {
  const serializedPayload = JSON.stringify(payload);
  if (serializedPayload === undefined) {
    throw new TypeError('The request payload cannot be serialized as JSON.');
  }

  const bytes = new TextEncoder().encode(serializedPayload);
  const digest = await crypto.subtle.digest('SHA-256', bytes);
  const hash = Array.from(new Uint8Array(digest))
    .map((byte) => byte.toString(16).padStart(2, '0'))
    .join('');
  window.localStorage.removeItem(`idem:${hash}`);
}
