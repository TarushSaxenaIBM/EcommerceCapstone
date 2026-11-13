import { useEffect } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { getToken } from '../api/client';
import { useAuth } from '../context/AuthContext';
import { useToast } from '../context/ToastContext';
import { statusLabel } from '../utils/format';

const HUB_URL = import.meta.env.VITE_HUB_URL || '/hubs/orders';

export function OrderNotifications() {
  const { isAuthenticated, user } = useAuth();
  const { notify } = useToast();

  useEffect(() => {
    if (!isAuthenticated || !user) return undefined;

    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL, { accessTokenFactory: () => getToken() || '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('orderStatusChanged', (payload) => {
      notify(`Order #${payload.orderId} is now ${statusLabel(payload.status)}.`, 'info');
      window.dispatchEvent(new CustomEvent('orderStatusChanged', { detail: payload }));
    });

    let disposed = false;

    connection
      .start()
      .then(() => {
        if (disposed) return connection.stop();
        return undefined;
      })
      .catch(() => {
        // The storefront still works when live updates are unavailable.
      });

    return () => {
      disposed = true;
      connection.stop();
    };
  }, [isAuthenticated, user, notify]);

  return null;
}
