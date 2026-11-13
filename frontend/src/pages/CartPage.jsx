import { useState } from 'react';
import { Link } from 'react-router-dom';
import { EmptyState, ErrorState, Spinner } from '../components/Feedback';
import { QuantityStepper } from '../components/QuantityStepper';
import { useCart } from '../context/CartContext';
import { useToast } from '../context/ToastContext';
import { formatCurrency } from '../utils/format';

export function CartPage() {
  const { cart, loading, error, refresh, updateItem, removeItem, clearCart } = useCart();
  const { notify } = useToast();
  const [busyItemId, setBusyItemId] = useState(null);
  const [clearing, setClearing] = useState(false);

  async function changeQuantity(item, quantity) {
    setBusyItemId(item.id);

    try {
      await updateItem(item.id, quantity);
    } catch (requestError) {
      notify(requestError.message, 'error');
    } finally {
      setBusyItemId(null);
    }
  }

  async function handleRemove(item) {
    setBusyItemId(item.id);

    try {
      await removeItem(item.id);
      notify(`${item.productName} removed from your cart.`, 'info');
    } catch (requestError) {
      notify(requestError.message, 'error');
    } finally {
      setBusyItemId(null);
    }
  }

  async function handleClear() {
    if (!window.confirm('Remove every item from your cart?')) return;

    setClearing(true);

    try {
      await clearCart();
      notify('Cart cleared.', 'info');
    } catch (requestError) {
      notify(requestError.message, 'error');
    } finally {
      setClearing(false);
    }
  }

  if (loading && !cart) return <Spinner label="Loading your cart..." />;

  if (error && !cart) {
    return (
      <div className="container page">
        <ErrorState message={error} onRetry={refresh} />
      </div>
    );
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="container page">
        <EmptyState
          title="Your cart is empty"
          message="Once you add something, it will show up here."
          action={
            <Link to="/" className="button">
              Start shopping
            </Link>
          }
        />
      </div>
    );
  }

  return (
    <div className="container page">
      <div className="page-heading">
        <div>
          <span className="eyebrow">Your selection</span>
          <h1>Shopping cart</h1>
          <p>{cart.itemCount} item(s) ready for checkout.</p>
        </div>
        <button
          type="button"
          className="link-button"
          onClick={handleClear}
          disabled={clearing}
        >
          {clearing ? 'Clearing...' : 'Clear cart'}
        </button>
      </div>

      <div className="cart-layout">
        <div className="cart-list">
          {cart.items.map((item) => {
            const busy = busyItemId === item.id;
            const maxQuantity = Math.max(1, Math.min(99, item.stockQuantity));

            return (
              <article key={item.id} className="cart-item">
                <div className="cart-item__image">
                  {item.imageUrl ? (
                    <img
                      src={item.imageUrl}
                      alt=""
                      onError={(event) => {
                        event.currentTarget.style.display = 'none';
                      }}
                    />
                  ) : null}
                </div>

                <div className="cart-item__details">
                  <Link to={`/products/${item.productId}`}>{item.productName}</Link>
                  <span>{formatCurrency(item.unitPrice)} each</span>
                  {!item.isAvailable ? (
                    <span className="form-error">This product is no longer available.</span>
                  ) : item.stockQuantity < 5 ? (
                    <span className="stock-note">Only {item.stockQuantity} left</span>
                  ) : null}
                </div>

                <div className="cart-item__quantity">
                  <QuantityStepper
                    value={item.quantity}
                    max={maxQuantity}
                    onChange={(quantity) => changeQuantity(item, quantity)}
                    disabled={busy || !item.isAvailable}
                  />
                  <span className="cart-item__line-total">{formatCurrency(item.lineTotal)}</span>
                  <button
                    type="button"
                    className="link-button link-button--danger"
                    onClick={() => handleRemove(item)}
                    disabled={busy}
                  >
                    Remove
                  </button>
                </div>
              </article>
            );
          })}
        </div>

        <aside className="summary-card">
          <h2>Order summary</h2>
          <div className="summary-row">
            <span>Subtotal</span>
            <span>{formatCurrency(cart.totalAmount)}</span>
          </div>
          <div className="summary-row">
            <span>Shipping</span>
            <span>Free</span>
          </div>
          <div className="summary-row summary-row--total">
            <span>Total</span>
            <span>{formatCurrency(cart.totalAmount)}</span>
          </div>
          <Link to="/checkout" className="button button--wide">
            Continue to checkout
          </Link>
          <Link to="/" className="text-link">
            Keep shopping
          </Link>
        </aside>
      </div>
    </div>
  );
}
