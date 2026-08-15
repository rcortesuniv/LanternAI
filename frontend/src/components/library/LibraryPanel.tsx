import { useState } from "react";
import { QUERY_LIBRARY, type QueryCategory } from "./libraryData";

interface QueryLibraryProps {
  onAsk: (question: string) => void;
}

export function QueryLibrary({ onAsk }: QueryLibraryProps) {
  const [activeCategory, setActiveCategory] = useState<string | null>(null);

  return (
    <section className="query-library" aria-labelledby="library-heading">
      <div className="query-library__header">
        <p className="eyebrow">QUERY LIBRARY</p>
        <h3 id="library-heading">Investigation templates</h3>
      </div>
      <p className="query-library__hint">Curated questions mapped to your data sources. Click to run.</p>
      <div className="query-library__categories">
        {QUERY_LIBRARY.map((category: QueryCategory) => {
          const isOpen = activeCategory === category.name || activeCategory === null;
          return (
            <div key={category.name} className="query-library__category">
              <button
                type="button"
                className="query-library__category-header"
                onClick={() => setActiveCategory(isOpen && activeCategory !== null ? null : category.name)}
                aria-expanded={isOpen}
              >
                <span className="query-library__icon" aria-hidden="true">{category.icon}</span>
                <span className="query-library__category-name">{category.name}</span>
                <span className="query-library__count">{category.queries.length}</span>
                <span className="query-library__chevron" aria-hidden="true">{isOpen ? "▾" : "▸"}</span>
              </button>
              {isOpen && (
                <ul className="query-library__queries">
                  {category.queries.map((query, idx) => (
                    <li key={idx}>
                      <button
                        type="button"
                        className="query-library__query"
                        onClick={() => onAsk(query.question)}
                        title={query.description}
                      >
                        <span className="query-library__query-text">{query.question}</span>
                      </button>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          );
        })}
      </div>
    </section>
  );
}
