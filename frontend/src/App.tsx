import { useState } from 'react';

interface Item {
  id?: number | string;
  name: string;
  category?: string;
  price?: number;
}

export default function App() {
  const [items, setItems] = useState<Item[]>([]);
  const [loading, setLoading] = useState<boolean>(false);
  const [error, setError] = useState<string | null>(null);
  const [hasLoaded, setHasLoaded] = useState<boolean>(false);

  const handleLoadItems = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('http://localhost:5000/items');
      
      if (!response.ok) {
        throw new Error(`Помилка сервера: ${response.status}`);
      }

      const data: Item[] = await response.json();
      setItems(data);
      setHasLoaded(true);
    } catch (err) {
      console.error(err);
      setError('Не вдалося завантажити товари з сервера. Перевірте, чи запущений бекенд на http://localhost:5000');
    } finally {
      setLoading(false);
    }
  };
  

  return (
    <div>

      <div className="header-container">
        <img src="/Logo.png" alt="ViloreHome Logo" className="logo" />
        <button onClick={handleLoadItems} disabled={loading}>
          {loading ? 'Завантаження...' : 'Список товарів'}
        </button>
      </div>

      {error && <div className="error-message">{error}</div>}

      {hasLoaded && items.length === 0 && !error && (
        <p className="empty-message">Список товарів порожній.</p>
      )}

      <ul id="itemsList">
        {items.map((item, index) => (
          <li key={item.id || index}>
            <span>
              <strong>{item.name}</strong> ({item.category || 'Декор'})
            </span> 
            <span>{item.price || 0} грн</span>
          </li>
        ))}
      </ul>
    </div>
  );
}