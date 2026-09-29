import { useState, useMemo } from "react";
import { ChevronDown, HelpCircle, Tag, Filter, X } from "lucide-react";

export interface FaqItem {
    id: string;
    category: string;
    question: string;
    answer: string;
}

interface FaqAccordionProps {
    readonly items: readonly FaqItem[];
    readonly searchQuery: string;
    readonly selectedTag?: string | null;
    readonly onSelectTag?: (tag: string | null) => void;
}

export function FaqAccordion({
    items,
    searchQuery,
    selectedTag: propSelectedTag,
    onSelectTag,
}: FaqAccordionProps) {
    const [internalTag, setInternalTag] = useState<string | null>(null);
    const [openId, setOpenId] = useState<string | null>(items[0]?.id ?? null);

    const currentTag = propSelectedTag !== undefined ? propSelectedTag : internalTag;

    const handleTagChange = (tag: string | null) => {
        if (onSelectTag) {
            onSelectTag(tag);
        }
        setInternalTag(tag);
    };

    const availableTags = useMemo(() => {
        const tags = new Set<string>();
        items.forEach((item) => {
            if (item.category) tags.add(item.category);
        });
        return Array.from(tags);
    }, [items]);

    const filteredItems = useMemo(() => {
        const query = searchQuery.trim().toLowerCase();
        return items.filter((item) => {
            const matchesTag = !currentTag || item.category.toLowerCase() === currentTag.toLowerCase();
            const matchesSearch = !query
                || item.question.toLowerCase().includes(query)
                || item.answer.toLowerCase().includes(query)
                || item.category.toLowerCase().includes(query);
            return matchesTag && matchesSearch;
        });
    }, [items, currentTag, searchQuery]);

    const toggleAccordion = (id: string) => {
        setOpenId((prev) => (prev === id ? null : id));
    };

    return (
        <div className="flex flex-col gap-4">
            {/* Tag filter bar */}
            {availableTags.length > 0 && (
                <div className="flex flex-wrap items-center gap-2 pb-1" role="toolbar" aria-label="Filter FAQs by tag">
                    <span className="text-xs font-semibold text-muted-foreground flex items-center gap-1.5 mr-1 font-sans">
                        <Filter className="h-3.5 w-3.5" />
                        <span>Filter by tag:</span>
                    </span>
                    <button
                        type="button"
                        data-testid="tag-filter-all"
                        onClick={() => handleTagChange(null)}
                        className={`px-3 py-1.5 rounded-lg text-xs font-semibold font-sans transition-all duration-150 flex items-center gap-1.5 border cursor-pointer ${
                            currentTag === null
                                ? 'bg-primary text-primary-foreground border-primary shadow-xs'
                                : 'bg-surface text-muted-foreground border-border hover:border-brand/40 hover:text-foreground'
                        }`}
                        aria-pressed={currentTag === null}
                    >
                        <span>All</span>
                        <span className={`text-[10px] font-mono px-1.5 py-0.2 rounded-full ${
                            currentTag === null ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-surface-2 text-muted-foreground'
                        }`}>
                            {items.length}
                        </span>
                    </button>
                    {availableTags.map((tag) => {
                        const count = items.filter((item) => item.category === tag).length;
                        const isSelected = currentTag?.toLowerCase() === tag.toLowerCase();
                        return (
                            <button
                                key={tag}
                                type="button"
                                data-testid={`tag-filter-${tag.toLowerCase().replace(/\s+/g, '-')}`}
                                onClick={() => handleTagChange(isSelected ? null : tag)}
                                className={`px-3 py-1.5 rounded-lg text-xs font-semibold font-sans transition-all duration-150 flex items-center gap-1.5 border cursor-pointer ${
                                    isSelected
                                        ? 'bg-primary text-primary-foreground border-primary shadow-xs'
                                        : 'bg-surface text-muted-foreground border-border hover:border-brand/40 hover:text-foreground'
                                }`}
                                aria-pressed={isSelected}
                            >
                                <Tag className="h-3 w-3" />
                                <span>{tag}</span>
                                <span className={`text-[10px] font-mono px-1.5 py-0.2 rounded-full ${
                                    isSelected ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-surface-2 text-muted-foreground'
                                }`}>
                                    {count}
                                </span>
                            </button>
                        );
                    })}
                    {currentTag !== null && (
                        <button
                            type="button"
                            onClick={() => handleTagChange(null)}
                            className="text-xs text-muted-foreground hover:text-foreground flex items-center gap-1 px-2 py-1 ml-auto cursor-pointer"
                            title="Clear tag filter"
                        >
                            <X className="h-3.5 w-3.5" />
                            <span>Clear filter</span>
                        </button>
                    )}
                </div>
            )}

            {/* Empty state or Accordion list */}
            {filteredItems.length === 0 ? (
                <div className="rounded-xl border border-border bg-surface p-8 text-center">
                    <HelpCircle className="mx-auto h-10 w-10 text-muted-foreground mb-3" />
                    <h3 className="font-display text-xl text-foreground mb-1">No matching FAQs found</h3>
                    <p className="text-sm text-muted-foreground">
                        {currentTag
                            ? `No FAQs found matching tag "${currentTag}"${searchQuery ? ` and search "${searchQuery}"` : ''}.`
                            : "Try searching for a different keyword, such as 'workout', 'schedule' or 'offline'."}
                    </p>
                    {currentTag && (
                        <button
                            type="button"
                            onClick={() => handleTagChange(null)}
                            className="mt-4 inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-semibold bg-surface-2 text-foreground border border-border hover:border-brand/40 cursor-pointer"
                        >
                            <X className="h-3.5 w-3.5" />
                            <span>Clear tag filter</span>
                        </button>
                    )}
                </div>
            ) : (
                <div className="flex flex-col gap-3">
                    {filteredItems.map((item) => {
                        const isOpen = openId === item.id;
                        const isTagSelected = currentTag?.toLowerCase() === item.category.toLowerCase();
                        return (
                            <div
                                key={item.id}
                                className={`rounded-xl border transition-all duration-200 overflow-hidden ${
                                    isOpen ? 'border-brand bg-surface shadow-md' : 'border-border bg-surface hover:border-brand/40'
                                }`}
                            >
                                <button
                                    type="button"
                                    onClick={() => toggleAccordion(item.id)}
                                    className="w-full flex items-center justify-between p-5 text-left focus:outline-none cursor-pointer"
                                    aria-expanded={isOpen}
                                >
                                    <div className="flex items-center gap-3 pr-4 flex-wrap">
                                        <span className="font-semibold text-foreground text-base font-sans">{item.question}</span>
                                        <span
                                            role="button"
                                            tabIndex={0}
                                            data-testid={`faq-tag-${item.id}`}
                                            onClick={(e) => {
                                                e.stopPropagation();
                                                handleTagChange(isTagSelected ? null : item.category);
                                            }}
                                            onKeyDown={(e) => {
                                                if (e.key === 'Enter' || e.key === ' ') {
                                                    e.preventDefault();
                                                    e.stopPropagation();
                                                    handleTagChange(isTagSelected ? null : item.category);
                                                }
                                            }}
                                            title={`Filter by tag: ${item.category}`}
                                            className={`px-2.5 py-0.5 rounded-full text-[11px] font-semibold uppercase tracking-wider border flex items-center gap-1 transition-colors cursor-pointer ${
                                                isTagSelected
                                                    ? 'bg-primary text-primary-foreground border-primary'
                                                    : 'bg-surface-2 text-brand border-border hover:bg-brand/10 hover:border-brand/40'
                                            }`}
                                        >
                                            <Tag className="h-3 w-3" />
                                            <span>{item.category}</span>
                                        </span>
                                    </div>
                                    <ChevronDown
                                        className={`h-5 w-5 text-muted-foreground flex-shrink-0 transition-transform duration-200 ${
                                            isOpen ? 'rotate-180 text-brand' : ''
                                        }`}
                                    />
                                </button>
                                {isOpen && (
                                    <div className="px-5 pb-5 pt-1 text-sm text-muted-foreground border-t border-border/50 leading-relaxed font-sans">
                                        {item.answer}
                                    </div>
                                )}
                            </div>
                        );
                    })}
                </div>
            )}
        </div>
    );
}