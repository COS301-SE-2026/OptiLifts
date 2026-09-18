import type { ReactNode } from "react";

export interface TabOption<T extends string>{
    id: T;
    label: string;
    count?: number;
    icon?:ReactNode;
}

interface ClashTabsProps<T extends string>{
    tabs: TabOption<T>[];
    activeTab: T;
    onChange: (tabId: T) => void;
    className?: string;
}

export function ClashTabs<T extends string>({
    tabs, activeTab, onChange, className='',
}: Readonly<ClashTabsProps<T>>){
    return (
        <div className={`flex bg-surface-2 border border-border rounded-xl p-1 overflow-x-auto ${className}`}
        role="tablist">
            {tabs.map((tab) => {
                const isActive = activeTab === tab.id;
                return (
                    <button key={tab.id} type="button" role="tab" aria-selected={isActive} onClick={()=> onChange(tab.id)}
                    className={`flex-1 py-2 px-3.5 text-xs font-bold uppercase tracking-[1px] rounded-lg transition font-sans flex items-center justify-center gap-1.5 whitespace-nowrap shrink-0
                    ${isActive ? 'bg-surface text-foreground shadow-sm' : 'text-muted-foreground hover:text-foreground'}`}>
                        {tab.icon}
                        <span className="whitespace-nowrap">
                        {tab.label}
                        </span>
                        {tab.count !== undefined && (
                            <span className={`text-[10px] px-1.5 py-0.2 rounded-full font-bold shrink-0
                                ${isActive ? 'bg-brand/1- text-brand': 'bg-surface text-muted-foreground'}`}>{tab.count}</span>
                        )}
                    </button>
                );
            })}
        </div>
    );
}