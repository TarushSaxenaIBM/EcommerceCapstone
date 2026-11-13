import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { ChatWidget } from './ChatWidget';
import { useAuth } from '../context/AuthContext';
import { useCart } from '../context/CartContext';
import { useToast } from '../context/ToastContext';
import { OrderNotifications } from '../hooks/OrderNotifications';

export function Layout() {
  const { user, isAuthenticated, logout } = useAuth();
  const { itemCount } = useCart();
  const { notify } = useToast();
  const navigate = useNavigate();

  function handleLogout() {
    logout();
    notify('You have been signed out.', 'info');
    navigate('/');
  }

  return (
    <div className="app-shell">
      <a className="skip-link" href="#main">
        Skip to content
      </a>
      <OrderNotifications />

      <header className="site-header">
        <div className="container site-header__inner">
          <Link to="/" className="brand" aria-label="IBM ecommerce home">
            <span className="brand__mark">ML</span>
            <span>IBM ecommerce</span>
          </Link>

          <nav className="site-nav" aria-label="Main navigation">
            <NavLink to="/" end>
              Shop
            </NavLink>
            {isAuthenticated ? <NavLink to="/orders">Orders</NavLink> : null}
          </nav>

          <div className="site-actions">
            {isAuthenticated ? (
              <>
                <span className="site-actions__greeting">Hi, {user.firstName}</span>
                <button type="button" className="link-button" onClick={handleLogout}>
                  Sign out
                </button>
              </>
            ) : (
              <>
                <NavLink to="/login" className="link-button">
                  Sign in
                </NavLink>
                <Link to="/register" className="button button--small">
                  Create account
                </Link>
              </>
            )}

            <Link to="/cart" className="cart-link" aria-label={`Cart with ${itemCount} items`}>
              Cart
              <span className="cart-link__count">{itemCount}</span>
            </Link>
          </div>
        </div>
      </header>

      <main id="main" className="site-main">
        <Outlet />
      </main>

      <footer className="site-footer">
        <div className="container site-footer__inner">
          <p>&copy; {new Date().getFullYear()} IBM ecommerce. A demo storefront.</p>
          <p>Built with ASP.NET Core and React.</p>
        </div>
      </footer>

      <ChatWidget />
    </div>
  );
}
