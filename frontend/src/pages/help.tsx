import { useState } from "react";
import { PageTitle } from "@/components/ui/page-title";
import { FaqAccordion } from "@/components/help/faq-accordion";
import type { FaqItem } from "@/components/help/faq-accordion";
import { Search, HelpCircle, Video, BookOpen, Sparkles } from "lucide-react";
import { TutorialCard, type TutorialVideo } from "@/components/help/tutorial-card";

const FAQ_DATA: readonly FaqItem[] = [
    // TODO
    {
        id: 'faq-1',
        category: 'Workouts',
        question: 'How do I create and customise a workout routine?',
        answer: 'Navigate to the "Workouts" page via the top navbar, then click the + button. You can name your workout, add exercises, set target sets and repititions, and create custom exercises.',
    },
    {
        id: 'faq-2',
        category: 'Offline Sync',
        question: '',
        answer: '',
    },
    {
        id: 'faq-3',
        category: 'Schedule',
        question: '',
        answer: '',
    },
    {
        id: 'faq-4',
        category: 'Profile',
        question: '',
        answer: '',
    },
]

// tutorial data
const TUTORIAL_DATA: readonly TutorialVideo[] =[
    // TODO
    {
        id: 'tut-1',
        title: 'Creating & Managing Workout Routines',
        description: 'Learn how to build custom workout templates, and configure sets and reps.',
        duration: '2:15', //replace with actual data obv
        youtubeId: '',
        fallbackVideoUrl: '',
    },
    {
        id: 'tut-2',
        title: '',
        description: '',
        duration: '',
        youtubeId: '',
        fallbackVideoUrl: '',
    },
    {
        id: 'tut-3',
        title: '',
        description: '',
        duration: '',
        youtubeId: '',
        fallbackVideoUrl: '',
    }
]

type ActiveTab = 'faqs'|'tutorials'|'resources'
export default function HelpPage(){
    const [activeTab, setActiveTab] = useState<ActiveTab>('faqs')
    const [searchQuery, setSearchQuery] = useState('')

    const tabs=[
        {
            id: 'faqs',
            label: 'FAQS',
            icon: HelpCircle,
            count: FAQ_DATA.length
        },
        {
            id: 'tutorials',
            label: 'Video Tutorials',
            icon: Video,
            count: TUTORIAL_DATA.length
        },
        {
            id: 'resources',
            label: 'Help Centre and Resources',
            icon: BookOpen,
            count: undefined //is this okay?
        },
    ] as const

    return (
        <section className="mx-auto max-w-7xl px-6 py-10 min-h-[calc(100dvh-5rem)] flex flex-col gap-8">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 pb-6">
                <div>
                    <PageTitle title="HELP MENU"/>
                    <p className="mt-2 text-sm text-muted-foreground font-sans">
                        Find answers to FAQs, watch tutorials, and access resources.
                    </p>
                </div>

                {/* searchbar stuff */}
                <div className="relative w-full md:w-80">
                    <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground"/>
                    <input type="text" placeholder="Search FAQs and tutorials" 
                    value={searchQuery} onChange={(e) => setSearchQuery(e.target.value)}
                    className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-border bg-surface text-foreground text-sm focus:outline-none focus:border-brand transition-colors font-sans"/>
                    {searchQuery && (
                        <button type="button" onClick={() => setSearchQuery('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-muted-foreground hover:text-foreground">Clear</button>
                    )}
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
                <aside className="lg:col-span-1 flex flex-col gap-2">
                    <div className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1 px-3 font-sans">Navigation</div>
                    {tabs.map((tab)=> {
                        const Icon = tab.icon
                        const isActive = activeTab ===tab.id
                        return (
                            <button key={tab.id} type="button" 
                            onClick={()=> setActiveTab(tab.id as ActiveTab)}
                            className={`w-full flex items-center justify-between px-4 py-3 rounded-xl font-sans text-sm font-semibold transition-all duration-150 text-left border ${isActive ? 'bg-brand text-white border-brand shadow-sm': 'bg-surface text-muted-foreground border-border hover:border-brand/40 hover:text-foreground'}`}>
                                <div className="flex items-center gap-2.5">
                                    <Icon className={`h-4 w-4 ${isActive ? 'text-white': 'text-brand'}`}/>
                                    <span>{tab.label}</span>
                                </div>
                                {tab.count !== undefined && (
                                    <span className={`px-2 py-0.5 rounded-full text-[11px] font-mono ${isActive ? 'bg-white/20 text-white': 'bg-surface-2 text-muted-foreground'}`}>{tab.count}</span>
                                )}
                            </button>
                        );
                    })}
                    <div className="mt-6 p-5 rounded-xl border border-border bg-surface-2 flex flex-col gap-2">
                        <div className="flex items-center gap-2 text-brand font-display text-lg">
                            <Sparkles className="h-4 w-4"/>
                            <span>NEED MORE HELP?</span>
                        </div>
                        <p className="text-xs text-muted-foreground leading-relaxed">Do you have a question other than the ones here? Reach out to us during the demonstration periods, or check our resource links.</p>
                    </div>
                </aside>

                {/* right main panel */}
                <main className="lg:col-span-3">
                    {activeTab === 'faqs' && (
                        <div className="flex flex-col gap-4">
                            <h2 className="font-display text-2xl text-foreground tracking-wide flex items-center gap-2">
                                <span>FREQUENTLY ASKED QUESTIONS</span>
                            </h2>
                            <FaqAccordion items={FAQ_DATA} searchQuery={searchQuery}/>
                        </div>
                    )}

                    {activeTab === 'tutorials' && (
                        <div className="flex flex-col gap-4">
                            <h2 className="font-display text-2xl text-foreground tracking-wide">VIDEO TUTORIALS</h2>
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                {TUTORIAL_DATA.filter((v) => v.title.toLowerCase().includes(searchQuery.toLowerCase())
                                || v.description.toLowerCase().includes(searchQuery.toLowerCase())
                                ).map((video) => (
                                    <TutorialCard key={video.id} video={video}/>
                                ))}
                            </div>
                        </div>
                    )}

                    {activeTab === 'resources' && (
                        <div className="flex flex-col gap-4">
                            <h2 className="font-display text-2xl text-foreground tracking-wide">HELP CENTRE & RESOURCES</h2>
                            {/* TODO: put the resource panel hier */}
                        </div>
                    )}
                </main>
            </div>
        </section>
    )
}