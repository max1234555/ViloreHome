import { type FormEvent, useEffect, useState } from 'react';
import { fetchWithResilience } from './lib/http';
import {
  clearIdempotencyKey,
  getOrReuseIdempotencyKey,
} from './lib/idempotency';

const ITEMS_URL = 'https://localhost:7060/api/items';
const FAILURE_THRESHOLD = 3;
const DEGRADED_COOLDOWN_MS = 10_000;
const REQUEST_RETRY = {
  retries: 2,
  baseDelayMs: 300,
  timeoutMs: 3500,
  jitter: true,
};

interface Item {
  id: number;
  name: string;
  category: string;
  price: number;
}

interface CreateItemRequest {
  name: string;
  category: string;
  price: number;
}

interface ApiErrorBody {
  error?: string;
  code?: string;
  details?: Array<{ field?: string; message?: string }>;
  requestId?: string;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

async function getResponseError(response: Response): Promise<string> {
  let body: ApiErrorBody = {};
  try {
    const parsed: unknown = await response.json();
    if (isRecord(parsed)) {
      body = {
        error: typeof parsed.error === 'string' ? parsed.error : undefined,
        code: typeof parsed.code === 'string' ? parsed.code : undefined,
        requestId:
          typeof parsed.requestId === 'string' ? parsed.requestId : undefined,
        details: Array.isArray(parsed.details)
          ? parsed.details.filter(
              (detail): detail is { field?: string; message?: string } =>
                isRecord(detail) &&
                (detail.field === undefined || typeof detail.field === 'string') &&
                (detail.message === undefined ||
                  typeof detail.message === 'string'),
            )
          : undefined,
      };
    }
  } catch {
    // Keep the HTTP status as a useful fallback when the body is not JSON.
  }

  const details = body.details
    ?.map((detail) =>
      detail.field && detail.message
        ? `${detail.field}: ${detail.message}`
        : detail.message,
    )
    .filter((detail): detail is string => Boolean(detail));
  const requestId =
    response.headers.get('X-Request-Id') ?? body.requestId;
  const parts = [
    body.error ?? `Помилка сервера: ${response.status}`,
    body.code,
    ...(details ?? []),
    requestId ? `ID запиту: ${requestId}` : undefined,
  ].filter((part): part is string => Boolean(part));

  return parts.join(' — ');
}

function getErrorMessage(error: unknown): string {
  return error instanceof Error
    ? error.message
    : 'Не вдалося виконати запит.';
}

export default function App() {
  const [items, setItems] = useState<Item[]>([]);
  const [activeAction, setActiveAction] = useState<'list' | 'create' | null>(
    null,
  );
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [hasLoaded, setHasLoaded] = useState(false);
  const [consecutiveFailures, setConsecutiveFailures] = useState(0);
  const [isDegraded, setIsDegraded] = useState(false);
  const [name, setName] = useState('');
  const [category, setCategory] = useState('');
  const [price, setPrice] = useState('');

  useEffect(() => {
    if (!isDegraded) {
      return;
    }

    const cooldown = window.setTimeout(() => {
      setIsDegraded(false);
      setConsecutiveFailures(0);
    }, DEGRADED_COOLDOWN_MS);

    return () => window.clearTimeout(cooldown);
  }, [isDegraded]);

  const recordFailure = (message: string) => {
    setError(message);
    const nextFailures = consecutiveFailures + 1;
    setConsecutiveFailures(nextFailures);
    if (nextFailures >= FAILURE_THRESHOLD) {
      setIsDegraded(true);
    }
  };

  const recordSuccess = () => {
    setError(null);
    setSuccess(null);
    setConsecutiveFailures(0);
    setIsDegraded(false);
  };

  const handleLoadItems = async () => {
    setActiveAction('list');
    setError(null);
    setSuccess(null);

    try {
      const response = await fetchWithResilience(ITEMS_URL, {
        retry: REQUEST_RETRY,
      });

      if (!response.ok) {
        throw new Error(await getResponseError(response));
      }

      const data: Item[] = await response.json();
      setItems(data);
      setHasLoaded(true);
      recordSuccess();
    } catch (requestError) {
      console.error(requestError);
      recordFailure(getErrorMessage(requestError));
    } finally {
      setActiveAction(null);
    }
  };

  const handleCreateItem = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setActiveAction('create');
    setError(null);
    setSuccess(null);

    const payload: CreateItemRequest = {
      name: name.trim(),
      category: category.trim(),
      price: Number(price),
    };

    try {
      const idempotencyKey = await getOrReuseIdempotencyKey(payload);
      const response = await fetchWithResilience(ITEMS_URL, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
        idempotencyKey,
        retry: REQUEST_RETRY,
      });

      if (!response.ok) {
        throw new Error(await getResponseError(response));
      }

      const createdItem: Item = await response.json();
      await clearIdempotencyKey(payload);
      setItems((currentItems) => [
        ...currentItems.filter((item) => item.id !== createdItem.id),
        createdItem,
      ]);
      setHasLoaded(true);
      setConsecutiveFailures(0);
      setIsDegraded(false);
      setName('');
      setCategory('');
      setPrice('');
      setSuccess(`Товар «${createdItem.name}» додано до каталогу.`);
    } catch (requestError) {
      console.error(requestError);
      recordFailure(getErrorMessage(requestError));
    } finally {
      setActiveAction(null);
    }
  };

  const isBusy = activeAction !== null;
  const controlsDisabled = isBusy || isDegraded;

  return (
    <div>
      <div className="header-container">
        <img src="/Logo.png" alt="ViloreHome Logo" className="logo" />
        <button
          onClick={handleLoadItems}
          disabled={controlsDisabled}
          type="button"
        >
          {activeAction === 'list' ? 'Завантаження...' : 'Список товарів'}
        </button>
      </div>

      {isDegraded && (
        <div className="degraded-banner" role="status" aria-live="polite">
          Сервіс тимчасово перевантажений. Кнопки буде розблоковано за кілька
          секунд.
        </div>
      )}

      {error && <div className="error-message" role="alert">{error}</div>}
      {success && <div className="success-message" role="status">{success}</div>}

      <form className="item-form" onSubmit={handleCreateItem}>
        <h2>Додати товар</h2>
        <label>
          Назва
          <input
            value={name}
            onChange={(event) => setName(event.target.value)}
            required
            disabled={controlsDisabled}
            maxLength={200}
          />
        </label>
        <label>
          Категорія
          <input
            value={category}
            onChange={(event) => setCategory(event.target.value)}
            required
            disabled={controlsDisabled}
            maxLength={100}
          />
        </label>
        <label>
          Ціна, грн
          <input
            type="number"
            value={price}
            onChange={(event) => setPrice(event.target.value)}
            required
            min="0"
            step="any"
            disabled={controlsDisabled}
          />
        </label>
        <button type="submit" disabled={controlsDisabled}>
          {activeAction === 'create' ? 'Додавання...' : 'Додати товар'}
        </button>
      </form>

      {hasLoaded && items.length === 0 && !error && (
        <p className="empty-message">Список товарів порожній.</p>
      )}

      <ul id="itemsList">
        {items.map((item, index) => (
          <li key={item.id ?? index}>
            <span>
              <strong>{item.name}</strong> ({item.category || 'Декор'})
            </span>
            <span>{item.price ?? 0} грн</span>
          </li>
        ))}
      </ul>
    </div>
  );
}
