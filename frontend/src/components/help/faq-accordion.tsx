import { useState } from "react";
import { ChevronDown, HelpCircle, Tag } from "lucide-react";

export interface FaqItem{
    id: string
    category: string
    question: string
    answer: string
}

interface FaqAccordionProps{
    readonly items: readonly FaqItem[]
    readonly searchQuery: string
}
export function FaqAccordion({
    items,
    searchQuery
}: FaqAccordionProps){
    const [openId, setOpenId]= useState<string | null>(items[0].id ?? null)

    const filteredItems = items.filter(
        (item) => item.question.toLowerCase().includes(searchQuery.toLowerCase())
        || item.answer.toLowerCase().includes(searchQuery.toLowerCase())
        || item.category.toLowerCase().includes(searchQuery.toLowerCase())
    )
    const toggleAccordion = (id:string) => {
        setOpenId((prev) => (prev===id ? null: id))
    }
    if(filteredItems.length===0){
        return (
            <div className="rounded-xl border border-border bg-surface p-8 text-center">
                <HelpCircle className="mx-auto h-10 w-10 text-muted-foreground mb-3"/>
                <h3 className="font-display text-x1 text-foreground mb-1">No matching FAQs found</h3>
                <p className="text-sem text-muted-foreground">Try searching for a different keyword, such as 'workout', 'schedule' or 'offline'.</p>
            </div>
        )
    }

    return (
        <div className="flex flex-col gap-3">
            {filteredItems.map((item) => {
                const isOpen = openId === item.id
                return (
                    <div key={item.id} className={`rounded-xl border transition-all duration-200 overflow-hidden ${isOpen ? 'border-brand bg-surface shadow-md': 'border-border bg-surface hover:border-brand/40'}`}>
                        <button type="button" onClick={() => toggleAccordion(item.id)}
                        className="w-full flex items-center justify-between p-5 text-left focus:outline-none" aria-expanded={isOpen}>
                            <div className="flex items-center gap-3 pr-4">
                                <span className="px-2.5 py-0.5 rounded-full text-[11px] font-semibold uppercase tracking-wider bg-brand-fill text-brand flex items-center gap-1">
                                    <Tag className="h-3 w-3"/>{item.category}
                                </span>
                                <span className="font-semibold text-foreground text-base font-sans">{item.question}</span>
                            </div>
                            <ChevronDown className={`h-5 w-5 text-muted-foreground flex-shrink-0 transition-transform duration-200 ${isOpen ? 'rotate-180 text-brand': ''}`}/>
                        </button>
                        {isOpen && (
                            <div className="px-5 pb-5 pt-1 text-sm text-muted-foreground border-t border-border/50 leading-relaxed font-sans">{item.answer}</div>
                        )}
                </div>
                )
            })}
        </div>
    )
}