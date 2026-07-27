import { BookOpen, ExternalLink, MessageCircle, MessageSquare, ShieldCheck, Sparkles, Terminal } from 'lucide-react'

export function ResourcePanel(){
    // TODO
    const resources = [
        {
            title: 'Documentation & Guides',
            description: 'System specifications, workout schema guides, and API design details.',
            icon: BookOpen,
            link: '#',
            badge: 'Docs',
        },
        {
            title: 'Brand Style Design',
            description: '',
            icon: Sparkles,
            link: '#',
            badge: 'Support',
        },
        {
            title: 'Demo 2 Release Notes',
            description: '',
            icon: MessageCircle,
            link: '#',
            badge: 'v2.0',
        },
    ]

    return (
        <div className="flex flex-col gap-6">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {resources.map((item) => {
                    const Icon = item.icon
                    return(
                        <a key={item.title} href={item.link} 
                        target="_blank"
                        rel="noreferrer"
                        className="p-5 rounded-xl border border-border bg-surface hover:border-brand transition-all duration-200 group flex flex-col justify-between gap-4 no-underline">
                            <div>
                                <div className="flex items-center justify-between mb-3">
                                    <div className="p-2.5 rounded-lg bg-brand-fill text-brand">
                                        <Icon className="h-5 w-5"/>
                                    </div>
                                    <span className="px-2 py-0.5 rounded text-[10px] font-semibold uppercase tracking-wider bg-surface-2 text-muted-foreground border border-border">{item.badge}</span>
                                </div>
                                <h3 className="font-display text-lg text-foreground group-hover:text-brand transition-colors mb-1">{item.title}</h3>
                                <p className="text-xs text-muted-foreground leading-relaxed">{item.description}</p>
                            </div>

                            <div className="flex items-center text-xs font-semibold text-brand tracking-wider uppercase gap-1 group-hover:translate-x-1 transition-transform">
                                <span>Access Resource</span>
                                <ExternalLink className="h-3.5 w-3.5"/>
                            </div>
                        </a>
                    )
                })}
            </div>

            <div className="rounded-xl border border-border bg-surface p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
                <div className="flex items-center gap-4">
                    <div className="p-3 rounded-xl bg-surface-2 text-brand border border-border">
                        <Terminal className="h-6 w-6"/>
                    </div>
                    <div>
                        <h4 className="font-display text-xl text-foreground">DID YOU KNOW?</h4>
                        <p className="text-xs text-muted-foreground">Offline sync auto-queues workout logs when your network drops, and syncs once reconnected!</p>
                    </div>
                </div>
                {/* <div className="flex items-center gap-2 text-xs font-semibold uppercase tracking-wider text-success bg-success/10 px-3 py-1.5 rounded-full border border-succes/20">
                    <ShieldCheck className="h-4 w-4"/>
                    
                    <span>System Status: Operational</span>
                </div> */}
            </div>
        </div>
    )
}