import { useEffect, useMemo, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { api } from '../api/client';
import { EmptyState, ErrorState } from '../components/Feedback';
import { ProductCard } from '../components/ProductCard';
import { useAuth } from '../context/AuthContext';
import { pluralize } from '../utils/format';

const SORT_OPTIONS = [
  { value: 'newest', label: 'Newest' },
  { value: 'price_asc', label: 'Price: low to high' },
  { value: 'price_desc', label: 'Price: high to low' },
  { value: 'name', label: 'Name' },
];

export function HomePage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const { user } = useAuth();
  const [searchText, setSearchText] = useState(searchParams.get('search') || '');
  const [categories, setCategories] = useState([]);
  const [result, setResult] = useState(null);
  const [recommendations, setRecommendations] = useState(null);
  const [suggestions, setSuggestions] = useState([]);
  const [showSuggestions, setShowSuggestions] = useState(false);
  const [searchMeta, setSearchMeta] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [reloadKey, setReloadKey] = useState(0);

  const search = searchParams.get('search') || '';
  const categoryId = searchParams.get('category') || '';
  const sort = searchParams.get('sort') || 'newest';
  const page = Number(searchParams.get('page')) || 1;

  useEffect(() => {
    setSearchText(searchParams.get('search') || '');
  }, [searchParams]);

  useEffect(() => {
    const controller = new AbortController();

    api
      .getCategories(controller.signal)
      .then(setCategories)
      .catch((requestError) => {
        if (requestError.name !== 'AbortError') setCategories([]);
      });

    return () => controller.abort();
  }, []);

  useEffect(() => {
    const controller = new AbortController();

    api
      .getHomeRecommendations(controller.signal)
      .then(setRecommendations)
      .catch((requestError) => {
        if (requestError.name !== 'AbortError') setRecommendations(null);
      });

    return () => controller.abort();
  }, [user?.id]);

  useEffect(() => {
    const query = searchText.trim();

    if (query.length < 2) {
      setSuggestions([]);
      return undefined;
    }

    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      api
        .getSuggestions(query, controller.signal)
        .then((items) => {
          if (!controller.signal.aborted) setSuggestions(items);
        })
        .catch((requestError) => {
          if (requestError.name !== 'AbortError') setSuggestions([]);
        });
    }, 250);

    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [searchText]);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError('');

    const request = search
      ? api.searchProducts(
          {
            query: search,
            categoryId: categoryId ? Number(categoryId) : undefined,
            page,
            pageSize: 12,
          },
          controller.signal,
        )
      : api.getProducts(
          {
            search: undefined,
            categoryId: categoryId || undefined,
            sort,
            page,
            pageSize: 12,
          },
          controller.signal,
        );

    request
      .then((data) => {
        setResult(data);
        setSearchMeta(search ? data : null);
      })
      .catch((requestError) => {
        if (requestError.name !== 'AbortError') setError(requestError.message);
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });

    return () => controller.abort();
  }, [search, categoryId, sort, page, reloadKey]);

  const totalPages = useMemo(() => {
    if (!result) return 1;
    return Math.max(1, Math.ceil(result.totalCount / result.pageSize));
  }, [result]);

  function updateParams(values) {
    const next = new URLSearchParams(searchParams);

    Object.entries(values).forEach(([key, value]) => {
      if (value === undefined || value === null || value === '') {
        next.delete(key);
      } else {
        next.set(key, String(value));
      }
    });

    setSearchParams(next);
  }

  function handleSearch(event) {
    event.preventDefault();
    setShowSuggestions(false);
    updateParams({ search: searchText.trim(), page: 1 });
  }

  function chooseSuggestion(suggestion) {
    setSearchText(suggestion.name);
    setShowSuggestions(false);
    updateParams({ search: suggestion.name, page: 1 });
  }

  function chooseCategory(id) {
    updateParams({ category: id, page: 1 });
  }

  const hasFilters = Boolean(search || categoryId);
  const showHero = !hasFilters && page === 1;

  return (
    <div className="container page">
      {showHero ? (
        <section className="hero">
          <div>
            <span className="eyebrow">Everyday goods, thoughtfully chosen</span>
            <h1>Useful things for work, home and the weekend.</h1>
            <p>
              Browse a small catalog of tech, books and essentials. Free shipping on every order in
              this demo store.
            </p>
            <a className="button" href="#catalog">
              Browse the catalog
            </a>
          </div>
          <div className="hero__panel">
            <span className="hero__number">8</span>
            <span>starter products across four categories</span>
          </div>
        </section>
      ) : null}

      {showHero && recommendations?.items?.length > 0 ? (
        <section className="recommendations" aria-labelledby="recommendations-title">
          <div className="catalog__header">
            <div>
              <span className="eyebrow">
                {recommendations.personalized ? 'Based on your orders' : 'Popular right now'}
              </span>
              <h2 id="recommendations-title">
                {recommendations.personalized ? 'Picked for you' : 'A good place to start'}
              </h2>
            </div>
            {recommendations.aiRanked ? <span className="badge badge--processing">AI ranked</span> : null}
          </div>
          <div className="product-grid">
            {recommendations.items.slice(0, 4).map((product) => (
              <ProductCard key={product.id} product={product} />
            ))}
          </div>
        </section>
      ) : null}

      <section id="catalog" className="catalog" aria-labelledby="catalog-title">
        <div className="catalog__header">
          <div>
            <h2 id="catalog-title">{showHero ? 'Shop all products' : 'Browse products'}</h2>
            {result ? <p>{pluralize(result.totalCount, 'product')} found</p> : null}
          </div>

          <form className="search-form" onSubmit={handleSearch} role="search">
            <label className="visually-hidden" htmlFor="product-search">
              Search products
            </label>
            <div className="search-form__field">
              <input
                id="product-search"
                type="search"
                placeholder="Describe what you need"
                autoComplete="off"
                aria-autocomplete="list"
                aria-controls="product-suggestions"
                aria-expanded={showSuggestions && suggestions.length > 0}
                value={searchText}
                onChange={(event) => {
                  setSearchText(event.target.value);
                  setShowSuggestions(true);
                }}
                onFocus={() => setShowSuggestions(true)}
                onBlur={() => {
                  window.setTimeout(() => setShowSuggestions(false), 150);
                }}
              />
              {showSuggestions && suggestions.length > 0 ? (
                <ul id="product-suggestions" className="search-suggestions" role="listbox">
                  {suggestions.map((suggestion) => (
                    <li key={suggestion.id} role="option" aria-selected="false">
                      <button type="button" onMouseDown={() => chooseSuggestion(suggestion)}>
                        {suggestion.name}
                      </button>
                    </li>
                  ))}
                </ul>
              ) : null}
            </div>
            <button type="submit" className="button button--secondary">
              Search
            </button>
          </form>
        </div>

        {searchMeta ? (
          <p className="search-hint">
            {searchMeta.usedAi
              ? `AI understood your search as “${searchMeta.interpretedQuery}”.`
              : `Showing the closest matches for “${search}”.`}
          </p>
        ) : null}

        <div className="catalog__toolbar">
          <div className="chip-row" aria-label="Filter by category">
            <button
              type="button"
              className={!categoryId ? 'chip chip--active' : 'chip'}
              onClick={() => chooseCategory('')}
            >
              All
            </button>
            {categories.map((category) => (
              <button
                key={category.id}
                type="button"
                className={String(category.id) === categoryId ? 'chip chip--active' : 'chip'}
                onClick={() => chooseCategory(String(category.id))}
              >
                {category.name}
              </button>
            ))}
          </div>

          <label className="sort-control">
            Sort
            <select
              value={sort}
              disabled={Boolean(search)}
              title={search ? 'Sorting is based on the search relevance.' : undefined}
              onChange={(event) => updateParams({ sort: event.target.value, page: 1 })}
            >
              {SORT_OPTIONS.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
        </div>

        {loading ? (
          <div className="product-grid" aria-busy="true" aria-label="Loading products">
            {Array.from({ length: 8 }).map((_, index) => (
              <div key={index} className="product-card product-card--skeleton">
                <div className="skeleton skeleton--image" />
                <div className="product-card__body">
                  <div className="skeleton skeleton--text" />
                  <div className="skeleton skeleton--text skeleton--short" />
                </div>
              </div>
            ))}
          </div>
        ) : error ? (
          <ErrorState message={error} onRetry={() => setReloadKey((key) => key + 1)} />
        ) : result && result.items.length > 0 ? (
          <>
            <div className="product-grid">
              {result.items.map((product) => (
                <ProductCard key={product.id} product={product} />
              ))}
            </div>

            {totalPages > 1 ? (
              <nav className="pagination" aria-label="Product pages">
                <button
                  type="button"
                  className="button button--secondary"
                  disabled={page <= 1}
                  onClick={() => updateParams({ page: page - 1 })}
                >
                  Previous
                </button>
                <span>
                  Page {page} of {totalPages}
                </span>
                <button
                  type="button"
                  className="button button--secondary"
                  disabled={page >= totalPages}
                  onClick={() => updateParams({ page: page + 1 })}
                >
                  Next
                </button>
              </nav>
            ) : null}
          </>
        ) : (
          <EmptyState
            title="No products match those filters"
            message="Try a different search term or clear the category filter."
            action={
              <button type="button" className="button" onClick={() => setSearchParams({})}>
                Clear filters
              </button>
            }
          />
        )}
      </section>

      <section className="service-strip" aria-label="Store benefits">
        <div>
          <strong>Simple checkout</strong>
          <span>One page, no account juggling.</span>
        </div>
        <div>
          <strong>Live order updates</strong>
          <span>Status changes arrive while you browse.</span>
        </div>
        <div>
          <strong>Clear stock counts</strong>
          <span>See availability before adding to cart.</span>
        </div>
      </section>
    </div>
  );
}
