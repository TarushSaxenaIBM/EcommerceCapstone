export function Spinner({ label = 'Loading...' }) {
  return (
    <div className="page-state" role="status">
      <div className="spinner" aria-hidden="true" />
      <p>{label}</p>
    </div>
  );
}

export function ErrorState({ message, onRetry }) {
  return (
    <div className="page-state page-state--error" role="alert">
      <h2>Something went wrong</h2>
      <p>{message || 'We could not load this part of the store.'}</p>
      {onRetry ? (
        <button type="button" className="button button--secondary" onClick={onRetry}>
          Try again
        </button>
      ) : null}
    </div>
  );
}

export function EmptyState({ title, message, action }) {
  return (
    <div className="page-state page-state--empty">
      <h2>{title}</h2>
      <p>{message}</p>
      {action}
    </div>
  );
}
