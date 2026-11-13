import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';

export function AuthPage({ mode }) {
  const isRegister = mode === 'register';
  const { login, register } = useAuth();
  const { notify } = useToast();
  const navigate = useNavigate();
  const location = useLocation();
  const from = location.state?.from;
  const redirectTo = from ? `${from.pathname}${from.search || ''}` : '/';

  const [form, setForm] = useState({
    firstName: '',
    lastName: '',
    email: '',
    password: '',
  });
  const [error, setError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  function updateField(event) {
    setForm((current) => ({ ...current, [event.target.name]: event.target.value }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    setError('');
    setSubmitting(true);

    try {
      if (isRegister) {
        await register(form);
        notify('Your account is ready. Happy shopping!', 'success');
      } else {
        await login({ email: form.email, password: form.password });
        notify('Welcome back.', 'success');
      }

      navigate(redirectTo, { replace: true });
    } catch (requestError) {
      setError(requestError.message);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="container page">
      <div className="auth-layout">
        <section className="auth-card">
          <span className="eyebrow">{isRegister ? 'Join IBM ecommerce' : 'Welcome back'}</span>
          <h1>{isRegister ? 'Create your account' : 'Sign in'}</h1>
          <p className="auth-card__intro">
            {isRegister
              ? 'An account lets you keep a cart, check out and follow your orders.'
              : 'Sign in to see your cart, check out and track orders.'}
          </p>

          <form className="form" onSubmit={handleSubmit}>
            {isRegister ? (
              <div className="form-row">
                <label>
                  First name
                  <input
                    name="firstName"
                    value={form.firstName}
                    onChange={updateField}
                    autoComplete="given-name"
                    required
                    maxLength={50}
                  />
                </label>
                <label>
                  Last name
                  <input
                    name="lastName"
                    value={form.lastName}
                    onChange={updateField}
                    autoComplete="family-name"
                    required
                    maxLength={50}
                  />
                </label>
              </div>
            ) : null}

            <label>
              Email address
              <input
                type="email"
                name="email"
                value={form.email}
                onChange={updateField}
                autoComplete="email"
                required
                maxLength={200}
              />
            </label>

            <label>
              Password
              <input
                type="password"
                name="password"
                value={form.password}
                onChange={updateField}
                autoComplete={isRegister ? 'new-password' : 'current-password'}
                required
                minLength={isRegister ? 8 : undefined}
              />
              {isRegister ? <small>At least 8 characters.</small> : null}
            </label>

            {error ? (
              <p className="form-error" role="alert">
                {error}
              </p>
            ) : null}

            <button type="submit" className="button button--wide" disabled={submitting}>
              {submitting ? 'Please wait...' : isRegister ? 'Create account' : 'Sign in'}
            </button>
          </form>

          <p className="auth-card__switch">
            {isRegister ? 'Already have an account?' : 'New to IBM ecommerce?'}{' '}
            <Link to={isRegister ? '/login' : '/register'} state={location.state}>
              {isRegister ? 'Sign in' : 'Create one'}
            </Link>
          </p>
        </section>

        <aside className="auth-aside">
          <h2>A small store, built to be understood.</h2>
          <p>
            IBM ecommerce is a demo storefront. Product data, payments and order updates are handled
            by a local ASP.NET Core API.
          </p>
          <ul>
            <li>Live stock checks as you shop</li>
            <li>One-page checkout with an address</li>
            <li>Order updates over SignalR</li>
          </ul>
        </aside>
      </div>
    </div>
  );
}
