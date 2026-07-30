import { BookOpen, ExternalLink, Sparkles, Palette, HelpCircle } from 'lucide-react'
import { Link } from 'react-router-dom'

export function ResourcePanel(){
    const resources = [
        {
            title: 'Documentation & Guides',
            description: 'System specifications, workout schema guides, and API design details.',
            icon: BookOpen,
            link: 'https://github.com/COS301-SE-2026/OptiLifts/blob/main/README.md',
            badge: 'Docs',
            isInternal: false,
        },
        {
            title: 'Brand Style Design',
            description: 'Explore colour tokens, typography, logo rules and component standards.',
            icon: Palette,
            link: '/brand-style',
            badge: 'Support',
            isInternal: true,
        },
        {
            title: 'Youtube Channel',
            description: 'Discover our channel, explore tutorial videos and follow us for updates.',
            icon: Sparkles,
            link: 'https://youtube.com/@hatrock26?si=eXIhv3Z5gElzdw6n',
            badge: 'Socials',
        },
    ]

    return (
        <div className="flex flex-col gap-6">
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {resources.map((item) => {
                    const Icon = item.icon
                    const cardContent =(
                        <>
                            <div>
                                <div className="flex items-center justify-between mb-3">
                                    <div className="p-2.5 rounded-lg bg-brand-fill text-brand">
                                        <Icon className="h-5 w-5" />
                                    </div>
                                    <span className="px-2 py-0.5 rounded text-[10px] font-semibold uppercase tracking-wider bg-surface-2 text-muted-foreground border border-border">{item.badge}</span>
                                </div>
                                <h3 className="font-display text-lg text-foreground group-hover:text-brand transition-colors mb-1">{item.title}</h3>
                                <p className="text-xs text-muted-foreground leading-relaxed">{item.description}</p>
                            </div>

                            <div className="flex items-center text-xs font-semibold text-brand tracking-wider uppercase gap-1 group-hover:translate-x-1 transition-transform">
                                <span>Access Resource</span>
                                <ExternalLink className="h-3.5 w-3.5" />
                            </div>
                        </>
                    )
                    if (item.isInternal){
                        return (
                            <Link key={item.title} to={item.link}
                            className="p-5 rounded-xl border border-border bg-surface hover:border-brand transition-all duration-200 group flex flex-col justify-between gap-4 no-underline">{cardContent}</Link>
                        )
                    }
                    return(
                        <a key={item.title} href={item.link} 
                        target="_blank"
                        rel="noreferrer"
                        className="p-5 rounded-xl border border-border bg-surface hover:border-brand transition-all duration-200 group flex flex-col justify-between gap-4 no-underline">
                            {cardContent}
                        </a>
                    )
                })}
            </div>

            <div className="rounded-xl border border-border bg-surface p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-4">
                <div className="flex items-center gap-4">
                    <div className="p-3 rounded-xl bg-surface-2 text-brand border border-border">
                        <HelpCircle className="h-6 w-6"/>
                    </div>
                    <div>
                        <h4 className="font-display text-xl text-foreground">DID YOU KNOW?</h4>
                        <p className="text-xs text-muted-foreground">The app saves your workout logs when you lose network connection, and automatically syncs when you're back online!</p>
                    </div>
                </div>
            </div>
        </div>
    )
}