import { Link } from 'react-router-dom';

export function ProductCard({ product }) {
  const inStock = product.stockQuantity > 0;

  return (
    <article className="product-card">
      <Link to={`/products/${product.id}`} className="product-card__image-wrap">
        {product.imageUrl ? (
          <img
            src={product.imageUrl}
            alt={product.name}
            className="product-card__image"
            loading="lazy"
            onError={(event) => {
              event.currentTarget.style.display = 'none';
            }}
          />
        ) : null}
        <span className="product-card__initial" aria-hidden="true">
          {product.name.charAt(0)}
        </span>
        {!inStock ? <span className="badge badge--muted product-card__stock">Out of stock</span> : null}
      </Link>

      <div className="product-card__body">
        <span className="eyebrow">{product.categoryName}</span>
        <h3>
          <Link to={`/products/${product.id}`}>{product.name}</Link>
        </h3>
        <div className="product-card__footer">
          <span className="price">${Number(product.price).toFixed(2)}</span>
          <span className={inStock ? 'stock-note' : 'stock-note stock-note--out'}>
            {inStock ? `${product.stockQuantity} in stock` : 'Unavailable'}
          </span>
        </div>
      </div>
    </article>
  );
}
