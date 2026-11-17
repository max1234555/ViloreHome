export interface RetryOptions {
  retries?: number;
  baseDelayMs?: number;
  timeoutMs?: number;
  jitter?: boolean;
}

export interface ResilientRequestInit extends RequestInit {
  retry?: RetryOptions;
  requestId?: string;
  idempotencyKey?: string;
}

const sleep = (durationMs: number) =>
  new Promise<void>((resolve) => window.setTimeout(resolve, durationMs));

const backoff = (baseDelayMs: number, attempt: number, jitter: boolean) =>
  baseDelayMs * 2 ** attempt + (jitter ? Math.floor(Math.random() * 100) : 0);

function getRetryAfterMs(response: Response): number | null {
  const retryAfter = response.headers.get('Retry-After');
  if (!retryAfter) {
    return null;
  }

  const seconds = Number(retryAfter);
  if (Number.isFinite(seconds)) {
    return Math.max(0, seconds * 1000);
  }

  const retryAt = Date.parse(retryAfter);
  return Number.isNaN(retryAt) ? null : Math.max(0, retryAt - Date.now());
}

export async function fetchWithResilience(
  url: string,
  options: ResilientRequestInit = {},
): Promise<Response> {
  const {
    retry = {},
    requestId = crypto.randomUUID(),
    idempotencyKey,
    ...init
  } = options;
  const {
    retries = 2,
    baseDelayMs = 300,
    timeoutMs = 3500,
    jitter = true,
  } = retry;
  const headers = new Headers(init.headers);
  headers.set('X-Request-Id', requestId);
  if (idempotencyKey) {
    headers.set('Idempotency-Key', idempotencyKey);
  }

  const method = (init.method ?? 'GET').toUpperCase();
  const retryAllowed =
    ['GET', 'HEAD', 'OPTIONS'].includes(method) || Boolean(idempotencyKey);
  const maxRetries = retryAllowed ? Math.max(0, retries) : 0;
  const externalSignal = init.signal;

  for (let attempt = 0; ; attempt += 1) {
    const controller = new AbortController();
    const abortFromCaller = () => controller.abort();
    let timedOut = false;
    const timeoutId = window.setTimeout(() => {
      timedOut = true;
      controller.abort();
    }, timeoutMs);

    if (externalSignal?.aborted) {
      window.clearTimeout(timeoutId);
      throw new DOMException('The request was aborted.', 'AbortError');
    }

    externalSignal?.addEventListener('abort', abortFromCaller, { once: true });

    let response: Response;
    try {
      const receivedResponse = await fetch(url, {
        ...init,
        headers,
        signal: controller.signal,
      });
      const responseBody = [204, 205, 304].includes(receivedResponse.status)
        ? null
        : await receivedResponse.arrayBuffer();
      response = new Response(responseBody, {
        status: receivedResponse.status,
        statusText: receivedResponse.statusText,
        headers: receivedResponse.headers,
      });
    } catch (error) {
      window.clearTimeout(timeoutId);
      externalSignal?.removeEventListener('abort', abortFromCaller);

      if (externalSignal?.aborted || attempt >= maxRetries) {
        if (timedOut) {
          throw new Error(`Request timed out after ${timeoutMs} ms.`);
        }
        throw error;
      }

      await sleep(backoff(baseDelayMs, attempt, jitter));
      continue;
    } finally {
      window.clearTimeout(timeoutId);
      externalSignal?.removeEventListener('abort', abortFromCaller);
    }

    const shouldRetry =
      response.status === 429 || response.status >= 500;
    if (!shouldRetry || attempt >= maxRetries) {
      return response;
    }

    const delayMs =
      response.status === 429
        ? getRetryAfterMs(response) ?? backoff(baseDelayMs, attempt, jitter)
        : backoff(baseDelayMs, attempt, jitter);

    await sleep(delayMs);
  }
}
