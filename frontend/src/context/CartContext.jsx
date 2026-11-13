import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { api } from '../api/client';
import { useAuth } from './AuthContext';

const CartContext = createContext(null);

export function CartProvider({ children }) {
  const { isAuthenticated } = useAuth();
  const [cart, setCart] = useState(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const refresh = useCallback(async () => {
    if (!isAuthenticated) {
      setCart(null);
      setError('');
      return;
    }

    setLoading(true);
    setError('');

    try {
      setCart(await api.getCart());
    } catch (requestError) {
      if (requestError.status !== 401) {
        setError(requestError.message);
      }
    } finally {
      setLoading(false);
    }
  }, [isAuthenticated]);

  useEffect(() => {
    refresh();
  }, [refresh]);

  const addItem = useCallback(async (productId, quantity = 1) => {
    const updated = await api.addCartItem(productId, quantity);
    setCart(updated);
    return updated;
  }, []);

  const updateItem = useCallback(async (itemId, quantity) => {
    const updated = await api.updateCartItem(itemId, quantity);
    setCart(updated);
    return updated;
  }, []);

  const removeItem = useCallback(async (itemId) => {
    const updated = await api.removeCartItem(itemId);
    setCart(updated);
    return updated;
  }, []);

  const clearCart = useCallback(async () => {
    const updated = await api.clearCart();
    setCart(updated);
    return updated;
  }, []);

  const value = useMemo(
    () => ({
      cart,
      loading,
      error,
      itemCount: cart?.itemCount ?? 0,
      totalAmount: cart?.totalAmount ?? 0,
      refresh,
      addItem,
      updateItem,
      removeItem,
      clearCart,
      setCart,
    }),
    [cart, loading, error, refresh, addItem, updateItem, removeItem, clearCart],
  );

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

export function useCart() {
  const context = useContext(CartContext);
  if (!context) {
    throw new Error('useCart must be used inside CartProvider.');
  }
  return context;
}
