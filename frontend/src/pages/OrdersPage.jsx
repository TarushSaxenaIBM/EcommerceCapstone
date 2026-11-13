import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { EmptyState, ErrorState, Spinner } from '../components/Feedback';
import { StatusBadge } from '../components/StatusBadge';
import { formatCurrency, formatDate, pluralize } from '../utils/format';

export function OrdersPage() {
  const [orders, setOrders] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(async (signal) => {
    try {
      const result = await api.getOrders(signal);
      setOrders(result);
      setError('');
    } catch (requestError) {
      if (requestError.name !== 'AbortError') setError(requestError.message);
    }
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);

    load(controller.signal).finally(() => {
      if (!controller.signal.aborted) setLoading(false);
    });

    const handleStatusChange = () => load();
    window.addEventListener('orderStatusChanged', handleStatusChange);

    return () => {
      controller.abort();
      window.removeEventListener('orderStatusChanged', handleStatusChange);
    };
  }, [load]);

  if (loading) return <Spinner label="Loading your orders..." />;

  if (error && !orders) {
    return (
      <div className="container page">
        <ErrorState message={error} onRetry={() => window.location.reload()} />
      </div>
    );
  }

  return (
    <div className="container page">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Order history</span>
          <h1>Your orders</h1>
          <p>Status updates appear here as they happen.</p>
        </div>
        <Link to="/" className="button button--secondary">
          Continue shopping
        </Link>
      </div>

      {!orders || orders.length === 0 ? (
        <EmptyState
          title="No orders yet"
          message="Your completed checkouts will appear here."
          action={
            <Link to="/" className="button">
              Browse products
            </Link>
          }
        />
      ) : (
        <div className="order-list">
          {orders.map((order) => (
            <Link key={order.id} to={`/orders/${order.id}`} className="order-card">
              <div className="order-card__main">
                <span className="order-card__id">Order #{order.id}</span>
                <span className="order-card__meta">
                  {formatDate(order.createdAt)} · {pluralize(order.itemCount, 'item')}
                </span>
              </div>
              <div className="order-card__side">
                <StatusBadge status={order.status} />
                <strong>{formatCurrency(order.totalAmount)}</strong>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
