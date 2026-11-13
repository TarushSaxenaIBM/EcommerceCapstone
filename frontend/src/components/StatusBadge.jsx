import { statusLabel } from '../utils/format';

export function StatusBadge({ status }) {
  const className = `badge badge--${String(status).toLowerCase()}`;
  return <span className={className}>{statusLabel(status)}</span>;
}
