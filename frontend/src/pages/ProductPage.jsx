import { useEffect, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import { api } from '../api/client';
import { ErrorState, Spinner } from '../components/Feedback';
import { ProductCard } from '../components/ProductCard';
import { QuantityStepper } from '../components/QuantityStepper';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { useToast } from '../context/ToastContext';
import { formatCurrency } from '../utils/format';

export function ProductPage() {
  const { id } = useParams();
  const { isAuthenticated } = useAuth();
  const { addItem } = useCart();
  const { notify } = useToast();
  const navigate = useNavigate();
  const location = useLocation();

  const [product, setProduct] = useState(null);
  const [related, setRelated] = useState([]);
  const [quantity, setQuantity] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [adding, setAdding] = useState(false);
  const [reloadKey, setReloadKey] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError('');

    api
      .getProduct(id, controller.signal)
      .then((result) => {
        setProduct(result);
        setQuantity(1);
      })
      .catch((requestError) => {
        if (requestError.name !== 'AbortError') setError(requestError.message);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, [id, reloadKey]);

  useEffect(() => {
    const controller = new AbortController();

    api
      .getRelatedRecommendations(id, controller.signal)
      .then((result) => setRelated(result.items || []))
      .catch((requestError) => {
        if (requestError.name !== 'AbortError') setRelated([]);
      });

    return () => controller.abort();
  }, [id]);

  async function handleAddToCart() {
    if (!isAuthenticated) {
      navigate('/login', { state: { from: location } });
      return;
    }

    setAdding(true);

    try {
      await addItem(product.id, quantity);
      notify(`${quantity} × ${product.name} added to your cart.`, 'success');
    } catch (requestError) {
      notify(requestError.message, 'error');
    } finally {
      setAdding(false);
    }
  }

  if (loading) return <Spinner label="Loading product..." />;
  if (error) {
    return (
      <div className="container page">
        <ErrorState message={error} onRetry={() => setReloadKey((key) => key + 1)} />
      </div>
    );
  }
  if (!product) return null;

  const inStock = product.stockQuantity > 0;
  const maxQuantity = Math.min(10, product.stockQuantity);

  return (
    <div className="container page">
      <nav className="breadcrumbs" aria-label="Breadcrumb">
        <Link to="/">Shop</Link>
        <span aria-hidden="true">/</span>
        <span>{product.categoryName}</span>
      </nav>

      <div className="product-detail">
        <div className="product-detail__media">
          {product.imageUrl ? (
            <img
              src={product.imageUrl}
              alt={product.name}
              onError={(event) => {
                event.currentTarget.style.display = 'none';
              }}
            />
          ) : null}
          <span className="product-detail__initial" aria-hidden="true">
            {product.name.charAt(0)}
          </span>
        </div>

        <div className="product-detail__info">
          <span className="eyebrow">{product.categoryName}</span>
          <h1>{product.name}</h1>
          <p className="product-detail__price">{formatCurrency(product.price)}</p>
          <p className="product-detail__sku">SKU: {product.sku}</p>

          <p className="product-detail__description">{product.description}</p>

          <div className={`stock-callout ${inStock ? '' : 'stock-callout--out'}`}>
            <strong>{inStock ? 'In stock' : 'Out of stock'}</strong>
            <span>
              {inStock
                ? `${product.stockQuantity} unit(s) ready to ship.`
                : 'This product is currently unavailable.'}
            </span>
          </div>

          <div className="product-detail__purchase">
            <QuantityStepper
              value={quantity}
              max={Math.max(1, maxQuantity)}
              onChange={setQuantity}
              disabled={!inStock || adding}
            />
            <button
              type="button"
              className="button button--wide"
              onClick={handleAddToCart}
              disabled={!inStock || adding}
            >
              {adding ? 'Adding...' : 'Add to cart'}
            </button>
          </div>

          <Link to="/cart" className="text-link">
            View cart
          </Link>
        </div>
      </div>

      {related.length > 0 ? (
        <section className="recommendations" aria-labelledby="related-title">
          <div className="catalog__header">
            <div>
              <span className="eyebrow">More like this</span>
              <h2 id="related-title">You may also like</h2>
            </div>
          </div>
          <div className="product-grid">
            {related.slice(0, 4).map((item) => (
              <ProductCard key={item.id} product={item} />
            ))}
          </div>
        </section>
      ) : null}
    </div>
  );
}
