import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { api } from '../api/client';
import { EmptyState, Spinner } from '../components/Feedback';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { useToast } from '../context/ToastContext';
import { formatCurrency } from '../utils/format';

const emptyAddress = {
  fullName: '',
  addressLine: '',
  city: '',
  state: '',
  postalCode: '',
  country: 'United States',
  phoneNumber: '',
};

export function CheckoutPage() {
  const { user } = useAuth();
  const { cart, loading, refresh } = useCart();
  const { notify } = useToast();
  const navigate = useNavigate();

  const [address, setAddress] = useState({
    ...emptyAddress,
    fullName: user ? `${user.firstName} ${user.lastName}`.trim() : '',
  });
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  function updateField(event) {
    setAddress((current) => ({ ...current, [event.target.name]: event.target.value }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError('');
    setSubmitting(true);

    try {
      const order = await api.checkout(address);
      await refresh();
      notify(`Order #${order.id} placed successfully.`, 'success');
      navigate(`/orders/${order.id}`, { replace: true, state: { justPlaced: true } });
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setSubmitting(false);
    }
  }

  if (loading && !cart) return <Spinner label="Preparing checkout..." />;

  if (!cart || cart.items.length === 0) {
    return (
      <div className="container page">
        <EmptyState
          title="Nothing to check out"
          message="Add a product to your cart before placing an order."
          action={
            <Link to="/" className="button">
              Browse products
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
          <span className="eyebrow">Almost there</span>
          <h1>Checkout</h1>
          <p>Enter a shipping address. Payment is simulated in this demo.</p>
        </div>
      </div>

      <div className="checkout-layout">
        <form className="checkout-form" onSubmit={handleSubmit}>
          <section className="panel">
            <h2>Shipping address</h2>

            <div className="form">
              <label>
                Full name
                <input
                  name="fullName"
                  value={address.fullName}
                  onChange={updateField}
                  autoComplete="name"
                  required
                  maxLength={150}
                />
              </label>

              <label>
                Street address
                <input
                  name="addressLine"
                  value={address.addressLine}
                  onChange={updateField}
                  autoComplete="street-address"
                  required
                  maxLength={300}
                />
              </label>

              <div className="form-row">
                <label>
                  City
                  <input
                    name="city"
                    value={address.city}
                    onChange={updateField}
                    autoComplete="address-level2"
                    required
                    maxLength={100}
                  />
                </label>
                <label>
                  State
                  <input
                    name="state"
                    value={address.state}
                    onChange={updateField}
                    autoComplete="address-level1"
                    required
                    maxLength={100}
                  />
                </label>
              </div>

              <div className="form-row">
                <label>
                  Postal code
                  <input
                    name="postalCode"
                    value={address.postalCode}
                    onChange={updateField}
                    autoComplete="postal-code"
                    required
                    maxLength={20}
                  />
                </label>
                <label>
                  Country
                  <input
                    name="country"
                    value={address.country}
                    onChange={updateField}
                    autoComplete="country-name"
                    required
                    maxLength={100}
                  />
                </label>
              </div>

              <label>
                Phone number
                <input
                  type="tel"
                  name="phoneNumber"
                  value={address.phoneNumber}
                  onChange={updateField}
                  autoComplete="tel"
                  required
                  maxLength={30}
                />
              </label>
            </div>
          </section>

          <section className="panel payment-note">
            <h2>Payment</h2>
            <p>
              This demo uses a mock payment provider. Placing the order creates a successful test
              payment and does not charge a card.
            </p>
          </section>

          {error ? (
            <p className="form-error" role="alert">
              {error}
            </p>
          ) : null}

          <div className="checkout-actions">
            <Link to="/cart" className="text-link">
              Back to cart
            </Link>
            <button type="submit" className="button" disabled={submitting}>
              {submitting ? 'Placing order...' : `Place order · ${formatCurrency(cart.totalAmount)}`}
            </button>
          </div>
        </form>

        <aside className="summary-card">
          <h2>Your order</h2>
          <ul className="mini-items">
            {cart.items.map((item) => (
              <li key={item.id}>
                <span>
                  {item.quantity} × {item.productName}
                </span>
                <span>{formatCurrency(item.lineTotal)}</span>
              </li>
            ))}
          </ul>

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
        </aside>
      </div>
    </div>
  );
}
