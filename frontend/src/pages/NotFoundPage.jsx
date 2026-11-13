import { Link } from 'react-router-dom';

export function NotFoundPage() {
  return (
    <div className="container page">
      <div className="page-state">
        <span className="eyebrow">404</span>
        <h1>That page is not here</h1>
        <p>The link may be old, or the product may no longer be available.</p>
        <Link to="/" className="button">
          Back to the shop
        </Link>
      </div>
    </div>
  );
}
