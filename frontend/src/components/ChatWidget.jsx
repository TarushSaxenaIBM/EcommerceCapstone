import { useEffect, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { api } from '../api/client';
import { formatCurrency } from '../utils/format';

const GREETING = {
  role: 'assistant',
  content:
    'Hi! I can help with shipping, returns, order status or finding a product. What do you need?',
};

export function ChatWidget() {
  const [open, setOpen] = useState(false);
  const [messages, setMessages] = useState([GREETING]);
  const [input, setInput] = useState('');
  const [sending, setSending] = useState(false);
  const [error, setError] = useState('');
  const listRef = useRef(null);
  const inputRef = useRef(null);

  useEffect(() => {
    if (!open) return;
    inputRef.current?.focus();
  }, [open]);

  useEffect(() => {
    if (!open) return;
    const list = listRef.current;
    if (list) list.scrollTop = list.scrollHeight;
  }, [messages, open, sending]);

  async function handleSubmit(event) {
    event.preventDefault();
    const text = input.trim();

    if (!text || sending) return;

    const history = messages
      .slice(-8)
      .map(({ role, content }) => ({ role, content }));

    setMessages((current) => [...current, { role: 'user', content: text }]);
    setInput('');
    setError('');
    setSending(true);

    try {
      const response = await api.sendChat(text, history);
      setMessages((current) => [
        ...current,
        {
          role: 'assistant',
          content: response.reply,
          products: response.products || [],
        },
      ]);
    } catch (requestError) {
      setError(requestError.message);
      setMessages((current) => [
        ...current,
        {
          role: 'assistant',
          content: 'Sorry, I could not answer that just now. Please try again.',
        },
      ]);
    } finally {
      setSending(false);
    }
  }

  return (
    <div className="chat-widget">
      {open ? (
        <section className="chat-panel" role="dialog" aria-label="Store assistant">
          <header className="chat-panel__header">
            <div>
              <strong>Store assistant</strong>
              <span>Shipping, returns and product help</span>
            </div>
            <button
              type="button"
              className="chat-panel__close"
              onClick={() => setOpen(false)}
              aria-label="Close store assistant"
            >
              ×
            </button>
          </header>

          <div className="chat-panel__messages" ref={listRef} aria-live="polite">
            {messages.map((message, index) => (
              <div
                key={`${message.role}-${index}`}
                className={`chat-message chat-message--${message.role}`}
              >
                <p>{message.content}</p>
                {message.products?.length ? (
                  <ul className="chat-message__products">
                    {message.products.map((product) => (
                      <li key={product.id}>
                        <Link to={`/products/${product.id}`} onClick={() => setOpen(false)}>
                          {product.name}
                          <span>{formatCurrency(product.price)}</span>
                        </Link>
                      </li>
                    ))}
                  </ul>
                ) : null}
              </div>
            ))}

            {sending ? (
              <div className="chat-message chat-message--assistant chat-message--typing">
                <span />
                <span />
                <span />
                <span className="visually-hidden">The assistant is typing</span>
              </div>
            ) : null}
          </div>

          {error ? <p className="chat-panel__error">{error}</p> : null}

          <form className="chat-panel__form" onSubmit={handleSubmit}>
            <label className="visually-hidden" htmlFor="chat-message">
              Message the store assistant
            </label>
            <input
              id="chat-message"
              ref={inputRef}
              value={input}
              maxLength={1500}
              placeholder="Ask a question..."
              onChange={(event) => setInput(event.target.value)}
              disabled={sending}
            />
            <button type="submit" className="button button--small" disabled={sending || !input.trim()}>
              Send
            </button>
          </form>
        </section>
      ) : null}

      <button
        type="button"
        className="chat-launcher"
        onClick={() => setOpen((value) => !value)}
        aria-expanded={open}
        aria-label={open ? 'Close store assistant' : 'Open store assistant'}
      >
        {open ? '×' : 'Need help?'}
      </button>
    </div>
  );
}
