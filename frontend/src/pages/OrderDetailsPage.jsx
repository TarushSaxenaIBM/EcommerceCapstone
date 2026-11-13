import { useCallback, useEffect, useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { ErrorState, Spinner } from '../components/Feedback';
import { StatusBadge } from '../components/StatusBadge';
import { formatCurrency, formatDate, statusLabel } from '../utils/format';

const FLOW = ['Pending', 'Confirmed', 'Processing', 'Shipped', 'Delivered'];

export function OrderDetailsPage() {
  const { id } = useParams();
  const location = useLocation();
  const [order, setOrder] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = useCallback(
    async (signal) => {
      try {
        setOrder(await api.getOrder(id, signal));
        setError('');
      } catch (requestError) {
        if (requestError.name !== 'AbortError') setError(requestError.message);
      }
    },
    [id],
  );

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);

    load(controller.signal).finally(() => {
      if (!controller.signal.aborted) setLoading(false);
    });

    const handleStatusChange = (event) => {
      if (Number(event.detail.orderId) === Number(id)) {
        load();
      }
    };

    window.addEventListener('orderStatusChanged', handleStatusChange);

    return () => {
      controller.abort();
      window.removeEventListener('orderStatusChanged', handleStatusChange);
    };
  }, [id, load]);

  if (loading) return <Spinner label="Loading order..." />;

  if (error || !order) {
    return (
      <div className="container page">
        <ErrorState message={error || 'Order not found.'} onRetry={() => window.location.reload()} />
        <p className="text-center">
          <Link to="/orders" className="text-link">
            Back to your orders
          </Link>
        </p>
      </div>
    );
  }

  const cancelled = order.status === 'Cancelled';
  const currentStep = FLOW.indexOf(order.status);
  const justPlaced = location.state?.justPlaced;

  return (
    <div className="container page">
      <nav className="breadcrumbs" aria-label="Breadcrumb">
        <Link to="/orders">Orders</Link>
        <span aria-hidden="true">/</span>
        <span>#{order.id}</span>
      </nav>

      {justPlaced ? (
        <div className="confirmation-banner">
          <h1>Thanks, {order.shippingAddress.fullName.split(' ')[0]}. Your order is in.</h1>
          <p>
            Order #{order.id} was confirmed. You will see status updates here as it moves through
            processing and shipping.
          </p>
        </div>
      ) : null}

      <div className="page-heading">
        <div>
          <span className="eyebrow">Placed {formatDate(order.createdAt)}</span>
          <h1>Order #{order.id}</h1>
        </div>
        <StatusBadge status={order.status} />
      </div>

      <section className="order-tracker" aria-label="Order progress">
        {cancelled ? (
          <p className="order-tracker__cancelled">This order was cancelled.</p>
        ) : (
          FLOW.map((step, index) => (
            <div
              key={step}
              className={
                index < currentStep
                  ? 'order-tracker__step order-tracker__step--done'
                  : index === currentStep
                    ? 'order-tracker__step order-tracker__step--current'
                    : 'order-tracker__step'
              }
            >
              <span className="order-tracker__dot" aria-hidden="true" />
              <span>{step}</span>
            </div>
          ))
        )}
      </section>

      <div className="order-detail-layout">
        <div className="order-detail-main">
          <section className="panel">
            <h2>Items</h2>
            <ul className="order-items">
              {order.items.map((item) => (
                <li key={`${item.productId}-${item.productName}`}>
                  <div>
                    <Link to={`/products/${item.productId}`}>{item.productName}</Link>
                    <span>
                      {item.quantity} × {formatCurrency(item.unitPrice)}
                    </span>
                  </div>
                  <strong>{formatCurrency(item.totalPrice)}</strong>
                </li>
              ))}
            </ul>
            <div className="summary-row summary-row--total">
              <span>Total</span>
              <span>{formatCurrency(order.totalAmount)}</span>
            </div>
          </section>

          {order.payment ? (
            <section className="panel">
              <h2>Payment</h2>
              <dl className="detail-list">
                <div>
                  <dt>Status</dt>
                  <dd>{statusLabel(order.payment.status)}</dd>
                </div>
                <div>
                  <dt>Transaction</dt>
                  <dd>{order.payment.transactionId || 'Pending'}</dd>
                </div>
                <div>
                  <dt>Amount</dt>
                  <dd>{formatCurrency(order.payment.amount)}</dd>
                </div>
              </dl>
            </section>
          ) : null}
        </div>

        <aside className="summary-card">
          <h2>Shipping to</h2>
          <address>
            <strong>{order.shippingAddress.fullName}</strong>
            <span>{order.shippingAddress.addressLine}</span>
            <span>
              {order.shippingAddress.city}, {order.shippingAddress.state}{' '}
              {order.shippingAddress.postalCode}
            </span>
            <span>{order.shippingAddress.country}</span>
            <span>{order.shippingAddress.phoneNumber}</span>
          </address>

          <div className="summary-row">
            <span>Last update</span>
            <span>{formatDate(order.updatedAt)}</span>
          </div>
          <div className="summary-row">
            <span>Items</span>
            <span>{order.items.reduce((sum, item) => sum + item.quantity, 0)}</span>
          </div>
          <Link to="/orders" className="text-link">
            All orders
          </Link>
        </aside>
      </div>
    </div>
  );
}
