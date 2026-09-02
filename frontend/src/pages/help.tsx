import { useState } from "react";
import { PageTitle } from "@/components/ui/page-title";
import { FaqAccordion } from "@/components/help/faq-accordion";
import type { FaqItem } from "@/components/help/faq-accordion";
import { Search, HelpCircle, Video, BookOpen, Sparkles, Layers, Flame, Zap, RefreshCw, Info, CheckCircle2, ArrowRight } from "lucide-react";
import { TutorialCard, type TutorialVideo } from "@/components/help/tutorial-card";
import { ResourcePanel } from "@/components/help/resource-panel";
import { ExerciseCard } from "@/components/ui/exercise-card";
import { ChainLink } from "./create-workout";
import type { WorkoutExercise } from "@/types/create-workout";
import { FAQ_DATA } from "@/types/faqs";


// tutorial data
const TUTORIAL_DATA: readonly TutorialVideo[] = [
    //NOSONAR
    {
        id: 'tut-1', title: 'Creating a Workout',
        description: 'Learn how to build custom workout templates, and configure sets and reps.',
        duration: '4:19',
        youtubeId: 'A1WpR8i2ebo',
        fallbackVideoUrl: '',
    },
    {
        id: 'tut-2', title: 'Active Session Logging & Offline Mode',
        description: 'A step-by-step walkthrough of starting an active workout session, logging sets, and syncing offline logs.',
        duration: '3:36', youtubeId: 'qbOXVDvbRLU', fallbackVideoUrl: '',
    },
    {
        id: 'tut-3', title: 'Weekly Schedule Management',
        description: 'Discover how to map routines across your week and keep your fitness consistency on track.',
        duration: '2:56', youtubeId: '7LVbhVXriP8',
        fallbackVideoUrl: '',
    },
    {
        id: 'tut-4', title: 'Editing a Workout',
        description: 'Tutorial on how to update your workout routine, including its name, sets, reps and exercises.', duration: '1:19',
        youtubeId: 'IpNvgS0drxo', fallbackVideoUrl: '',
    },
    {
        id: 'tut-5',
        title: 'Configuring User Settings',
        description: 'Learn how to update your personal details, password, and app settings.',
        duration: '2:07',
        youtubeId: 'Zq-3h3d25ww', fallbackVideoUrl: '',
    }
]

//set type data
const DEMO_EXERCISES: WorkoutExercise[] = [
    {
        id: 'demo-1',
        name: 'Barbell Bench Press',
        muscle: 'Chest',
        exerciseType: 'WeightReps',
        sets: [
            {
                id: 's1', type: 'I', kg: 100, reps: 8, time: '', distance: ''
            },
            {
                id: 's2', type: 'I', kg: 100, reps: 8, time: '', distance: ''
            },
        ],
        exerciseCatalogId: 'cat-1',
        // restTime: 120,
    },
    {
        id: 'demo-2',
        name: 'Incline Dumbbell Flys',
        muscle: 'Chest',
        exerciseType: 'WeightReps',
        sets: [
            {
                id: 's3', type: 'I', kg: 24, reps: 10, time: '', distance: ''
            },
            {
                id: 's4', type: 'I', kg: 24, reps: 10, time: '', distance: ''
            },
        ],
        exerciseCatalogId: 'cat-2',
        // restTime: 90,
    },
    {
        id: 'demo-3',
        name: 'Push-ups',
        muscle: 'Chest',
        exerciseType: 'BodyWeightReps',
        sets: [
            {
                id: 's5', type: 'I', kg: '', reps: 15, time: '', distance: ''
            },
            {
                id: 's6', type: 'I', kg: '', reps: 15, time: '', distance: ''
            },
        ],
        exerciseCatalogId: 'cat-3',
        // restTime: 60,
    }
];

const DROPSET_EXERCISE: WorkoutExercise = {
    id: 'dropset-demo',
    name: 'Triceps Cable Pushdown',
    muscle: 'Triceps',
    exerciseType: 'WeightReps',
    sets: [
        {
            id: 'ds-1', type: 'I', kg: 50, reps: 8, time: '', distance: ''
        },
        {
            id: 'ds-2', type: 'D', kg: 40, reps: 10, time: '', distance: ''
        },
        {
            id: 'ds-3', type: 'D', kg: 30, reps: 12, time: '', distance: ''
        },
    ],
    exerciseCatalogId: 'cat-4',
    // restTime: 60,
};

function SetGroupDemo() {
    const [linked1To2, setLinked1To2] = useState(false);
    const [linked2To3, setLinked2To3] = useState(false);

    const resetDemo = () => {
        setLinked1To2(false);
        setLinked2To3(false);
    };

    let groupContent;
    if (linked1To2 && linked2To3) {
        groupContent = (
            <div className="rounded-xl border-2 border-amber-500/60 bg-amber-500/5 p-3 space-y-3 transition-all duration-200">
                <div className="flex items-center justify-between px-1">
                    <span className="text-xs font-bold uppercase tracking-wider text-amber-500 flex items-center gap-1.5">
                        <Zap className="h-3.5 w-3.5" />CIRCUIT SET (3 EXERCISES)
                    </span>
                </div>
                <ExerciseCard exercise={DEMO_EXERCISES[0]} readOnly />
                <ChainLink linked onClick={() => setLinked1To2(false)} />
                <ExerciseCard exercise={DEMO_EXERCISES[1]} readOnly />
                <ChainLink linked onClick={() => setLinked2To3(false)} />
                <ExerciseCard exercise={DEMO_EXERCISES[2]} readOnly />
            </div>
        );
    } else if (linked1To2) {
        groupContent = (
            <>
                <div className="rounded-xl border-2 border-brand/60 bg-brand/5 p-3 space-y-3 transition-all duratrion-200">
                    <div className="flex items-center justify-between px-1">
                        <span className="text-xs font-bold uppercase tracking-wider text-brand flex items-center gap-1.5">
                            <Zap className="h-3.5 w-3.5" />SUPERSET (2 EXERCISES)
                        </span>
                    </div>
                    <ExerciseCard exercise={DEMO_EXERCISES[0]} readOnly />
                    <ChainLink linked onClick={() => setLinked1To2(false)} />
                    <ExerciseCard exercise={DEMO_EXERCISES[1]} readOnly />
                </div>
                <ChainLink linked={false} onClick={() => setLinked2To3(true)} />
                <ExerciseCard exercise={DEMO_EXERCISES[2]} readOnly />
            </>
        );
    } else if (linked2To3){
        groupContent = (
            <>
                <ExerciseCard exercise={DEMO_EXERCISES[0]} readOnly />
                <ChainLink linked={false} onClick={() => setLinked1To2(true)} />
                <div className="rounded-xl border-2 border-brand/60 bg-brand/5 p-3 space-y-3 transition-all duratrion-200">
                    <div className="flex items-center justify-between px-1">
                        <span className="text-xs font-bold uppercase tracking-wider text-brand flex items-center gap-1.5">
                            <Zap className="h-3.5 w-3.5" />SUPERSET (2 EXERCISES)
                        </span>
                    </div>
                    <ExerciseCard exercise={DEMO_EXERCISES[1]} readOnly />
                    <ChainLink linked onClick={() => setLinked2To3(false)} />
                    <ExerciseCard exercise={DEMO_EXERCISES[2]} readOnly />
                </div>
            </>
        )
    } else {
        groupContent = (
            <>
                <ExerciseCard exercise={DEMO_EXERCISES[0]} readOnly />
                <ChainLink linked={false} onClick={() => setLinked1To2(true)} />
                <ExerciseCard exercise={DEMO_EXERCISES[1]} readOnly />
                <ChainLink linked={false} onClick={() => setLinked2To3(true)} />
                <ExerciseCard exercise={DEMO_EXERCISES[2]} readOnly />
            </>
        );
    }
    
    return (
        <div className="rounded-2xl border border-border bg-surface p-5 shadow-sm space-y-4">
            <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
                <div>
                    <div className="flex items-center gap-2">
                        <Zap className="h-5 w-5 text-brand" />
                        <h3 className="font-display text-lg text-foreground tracking-wide">
                            PRACTISE CONNECTING EXERCISES
                        </h3>
                    </div>
                    <p className="text-xs text-muted-foreground mt-0.5">
                        Click the join icon between exercises below to see how SuperSets & Circuit Sets are formed.
                    </p>
                </div>

                {(linked1To2 || linked2To3) && (
                    <button type="button" onClick={resetDemo}
                        className="text-xs text-muted-foreground hover:text-foreground px-2.5 py-1 rounded-md bg-surface-2 border border-border flex items-center gap-1.5 cursor-pointer">
                        <RefreshCw className="h-3 w-3" />Reset Demo
                    </button>
                )}
            </div>

            <div className="space-y-3">
                {groupContent}
            </div>

            <div className="rounded-xl bg-surface-2 p-3 text-xs text-muted-foreground flex items-start gap-2 border border-border">
                <Info className="h-4 w-4 text-brand shrink-0 mt-0.5" />
                <p><strong className="text-foregound">OptiLifts Rule: </strong>Joining 2 exercises forms a <span className="text-brand font-semibold">Superset</span>. Joining 3 or more consecutive exercises automatically forms a <span className="text-amber-500 font-semibold">Circuit Set</span></p>
            </div>
        </div>
    );
}

// main set type guide
function SetTypeGuideContent() {
    return (
        <div className="flex flex-col gap-8">
            <div>
                <h2 className="font-display text-2xl text-foreground tracking-wide flex items-center gap-2">
                    <span>SET TYPES AND SPECIALISED GROUPS</span>
                </h2>
                <p className="text-sm text-muted-foreground mt-1">Learn how working sets, warmup sets, dropsets, supersets, and circuit sets work in OptiLifts.</p>
            </div>

            <div className="space-y-4">
                <h3 className="font-display text-lg text-foreground tracking-wide flex items-center gap-2">
                    <Layers className="h-5 w-5 text-brand" />
                    <span>SET DROPDOWN OPTIONS</span>
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="rounded-2xl border border-border bg-surface p-5 flex flex-col justify-between space-y-3">
                        <div>
                            <div className="flex items-center justify-between mb-3">
                                <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-primary text-primary-foreground font-mono text-sm font-bold">1</span>
                                <span className="text-[11px] font-semibold uppercase tracking-wider text-brand px-2 py-0.5 rounded-full bg-brand/10 border border-brand/20">Working Set</span>
                            </div>
                            <h4 className="font-bold text-foreground text-base mb-1">Working Sets (1, 2, 3...)</h4>
                            <p className="text-xs text-muted-foreground leading-relaxed">Standard main sets performed at one's target weight and reps for progressive overload.</p>
                        </div>
                        <div className="pt-2 border-t border-border/60 text-[11px] text-muted-foreground flex items-center gap-1.5">
                            <CheckCircle2 className="h-3.5 w-3.5 text-emerald-500 shrink-0" />
                            <span>Counts toward volume & PR stats</span>
                        </div>
                    </div>

                    <div className="rounded-2xl border border-border bg-surface p-5 flex flex-col justify-between space-y-3">
                        <div>
                            <div className="flex items-center justify-between mb-3">
                                <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-amber-500/10 text-amber-500 border border-amber-500/30 font-mono text-sm font-bold">
                                    W</span>
                                <span className="text-[11px] font-semibold uppercase tracking-wider text-amber-500 px-2 py-0.5 rounded-full bg-amber-500/10 border border-amber-500/20">
                                    Warmup Set</span>
                            </div>
                            <h4 className="font-bold text-foreground text-base mb-1">Warmup Sets (W)</h4>
                            <p className="text-xs text-muted-foreground leading-relaxed">
                                Light preparation sets to prime your muscles and joints before heavy working sets.
                            </p>
                        </div>
                        <div className="pt-2 border-t border-border/60 text-[11px] text-muted-foreground flex items-center gap-1.5">
                            <CheckCircle2 className="h-3.5 w-3.5 text-amber-500 shrink-0" />
                            <span>Excluded from PRs</span>
                        </div>
                    </div>

                    <div className="rounded-2xl border border-border bg-surface p-5 flex flex-col justify-between space-y-3">
                        <div>
                            <div className="flex items-center justify-between mb-3">
                                <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-rose-500/10 text-rose-500 border border-rose-500/30 font-mono text-sm font-bold">
                                    D</span>
                                <span className="text-[11px] font-semibold uppercase tracking-wider text-rose-500 px-2 py-0.5 rounded-full bg-rose-500/10 border border-rose-500/20">
                                    Dropset</span>
                            </div>
                            <h4 className="font-bold text-foreground text-base mb-1">Dropsets (D)</h4>
                            <p className="text-xs text-muted-foreground leading-relaxed">
                                High intensity sets completed directly after a working set with reduced weight, as well as no rest.
                            </p>
                        </div>
                        <div className="pt-2 border-t border-border/60 text-[11px] text-muted-foreground flex items-center gap-1.5">
                            <CheckCircle2 className="h-3.5 w-3.5 text-rose-500 shrink-0" />
                            <span>Pushes targeted muscles past failure</span>
                        </div>
                    </div>
                </div>
            </div>

            {/* how to log dropsets */}
            <div className="rounded-2xl border border-border bg-surface p-6 space-y-5">
                <div className="flex items-center gap-2">
                    <Flame className="h-5 w-5 text-rose-500" />
                    <h3 className="font-display text-lg text-foreground tracking-wide">HOW TO LOG DROPSETS PROPERLY</h3>
                </div>

                <p className="text-xs text-muted-foreground leading-relaxed">
                    Log your initial set as a Working Set (Set 1) and select <strong className="text-foreground">Dropset (D)</strong> in the dropdown menu for each immediate weight drop (Set 2, and 3, etc.).
                </p>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                    <div className="p-4 rounded-xl border border-border bg-surface-2 space-y-2">
                        <div className="flex items-center gap-2 text-xs font-bold text-brand uppercase tracking-wider">
                            <span>Step 1</span>
                            <ArrowRight className="h-3.5 w-3.5" />
                        </div>
                        <p className="text-sm font-semibold text-foreground">Heavy Working Set</p>
                        <p className="text-xs text-muted-foreground">
                            Set 1: Primary weight (eg. 50kg x 8reps). Select Set Type <strong className="text-foreground">1</strong>.
                        </p>
                    </div>

                    <div className="p-4 rounded-xl border border-border bg-surface-2 space-y-2">
                        <div className="flex items-center gap-2 text-xs font-bold text-brand uppercase tracking-wider">
                            <span>Step 2</span>
                            <ArrowRight className="h-3.5 w-3.5" />
                        </div>
                        <p className="text-sm font-semibold text-foreground">First Dropset</p>
                        <p className="text-xs text-muted-foreground">
                            Reduce weight immediately with zero rest (eg. 40kg x 10reps). Select Set type <strong className="text-rose-500 font-mono">D</strong>
                        </p>
                    </div>

                    <div className="p-4 rounded-xl border border-border bg-surface-2 space-y-2">
                        <div className="flex items-center gap-2 text-xs font-bold text-brand uppercase tracking-wider">
                            <span>Step 3</span>
                        </div>
                        <p className="text-sm font-semibold text-foreground">Subsequent Drops</p>
                        <p className="text-xs text-muted-foreground">
                            For double/triple dropsets, add another set row (eg. 30kg x 12reps) and select <strong className="text-brand font-mono">D</strong>.
                        </p>
                    </div>
                </div>

                <div className="space-y-2">
                    {/* <span className="text-xs font-semibold uppercase tracking-wider text-muted-foreground block">Live ExerciseCard Preview:</span> */}
                    <ExerciseCard exercise={DROPSET_EXERCISE} readOnly />
                </div>
            </div>

            <div className="space-y-4">
                <h3 className="font-display text-lg text-foreground tracking-wide flex items-center gap-2">
                    <Zap className="h-5 w-5 text-brand" />
                    <span>SPECIALISED SET GROUPS</span>
                </h3>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="rounded-2xl border border-border bg-surface p-5 space-y-2">
                        <div className="flex items-center justify-between">
                            <h4 className="font-bold text-foreground text-base">Supersets (2 Exercises)</h4>
                            <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-brand/10 text-brand border border-brand/20">Pair</span>
                        </div>
                        <p className="text-xs text-muted-foreground leading-relaxed">A Superset pairs two exercises performed back-to-back with minimal to no rest in between (eg. Bench Press followed immediately by Incline Flys). This maximises workout density and efficiency.</p>

                    </div>

                    <div className="rounded-2xl border border-border bg-surface p-5 space-y-2">
                        <div className="flex items-center justify-between">
                            <h4 className="font-bold text-foreground text-base">Circuit Sets (3+ Exercises)</h4>
                            <span className="text-xs font-semibold px-2 py-0.5 rounded-full bg-amber-500/10 text-amber-500 border border-amber-500/20">Group</span>
                        </div>
                        <p className="text-xs text-muted-foreground leading-relaxed">
                            A circuit connects 3 or more exercises performed sequentially before taking a rest. Joining a third exercise automatically converts a superset into a circuit set.
                        </p>
                    </div>
                </div>
            </div>
            <SetGroupDemo />
        </div>
    )
}

type ActiveTab = 'faqs' | 'set-types' | 'tutorials' | 'resources'
export default function HelpPage() {
    const [activeTab, setActiveTab] = useState<ActiveTab>('faqs')
    const [searchQuery, setSearchQuery] = useState('')

    const tabs = [
        {
            id: 'faqs',
            label: 'FAQS',
            icon: HelpCircle,
            count: FAQ_DATA.length
        },
        {
            id: 'set-types',
            label: 'Set Type Guide',
            icon: Layers,
            count: undefined
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
        <section className="mx-auto max-w-6xl px-6 py-12">
            <div className="flex flex-col md:flex-row md:items-center justify-between gap-6 pb-6">
                <div>
                    <div className="mb-6">
                        <PageTitle title="HELP MENU" />
                    </div>

                    <p className="mt-2 text-sm text-muted-foreground font-sans">
                        Find answers to FAQs, watch tutorials, and access resources.
                    </p>
                </div>

                {/* searchbar stuff */}
                <div className="relative w-full md:w-80">
                    <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                    <input type="text" placeholder="Search FAQs and tutorials"
                        value={searchQuery} onChange={(e) => setSearchQuery(e.target.value)}
                        className="w-full pl-10 pr-4 py-2.5 rounded-xl border border-border bg-surface text-foreground text-sm focus:outline-none focus:ring-2 focus:ring-brand focus:border-brand transition-colors font-sans" />
                    {searchQuery && (
                        <button type="button" onClick={() => setSearchQuery('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-xs text-muted-foreground hover:text-foreground">Clear</button>
                    )}
                </div>
            </div>

            <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
                <aside className="lg:col-span-1 flex flex-col gap-2 lg:sticky lg:top-28 lg:self-start">
                    <div className="text-xs font-semibold uppercase tracking-wider text-muted-foreground mb-1 px-3 font-sans">Navigation</div>
                    {tabs.map((tab) => {
                        const Icon = tab.icon
                        const isActive = activeTab === tab.id
                        return (
                            <button key={tab.id} type="button"
                                onClick={() => setActiveTab(tab.id as ActiveTab)}
                                className={`w-full flex items-center justify-between px-4 py-3 rounded-xl font-sans text-sm font-semibold transition-all duration-150 text-left border ${isActive ? 'bg-primary text-primary-foreground border-primary shadow-sm' : 'bg-surface text-muted-foreground border-border hover:border-brand/40 hover:text-foreground'}`}>
                                <div className="flex items-center gap-2.5">
                                    <Icon className={`h-4 w-4 ${isActive ? 'text-primary-foreground' : 'text-brand'}`} />
                                    <span>{tab.label}</span>
                                </div>
                                {tab.count !== undefined && (
                                    <span className={`px-2 py-0.5 rounded-full text-[11px] font-mono ${isActive ? 'bg-primary-foreground/20 text-primary-foreground' : 'bg-surface-2 text-muted-foreground'}`}>{tab.count}</span>
                                )}
                            </button>
                        );
                    })}
                    <div className="mt-6 p-5 rounded-xl border border-border bg-surface-2 flex flex-col gap-2">
                        <div className="flex items-center gap-2 text-brand font-display text-lg">
                            <Sparkles className="h-4 w-4" />
                            <span>NEED MORE HELP?</span>
                        </div>
                        <p className="text-xs text-muted-foreground leading-relaxed">Do you have a question other than the ones here? Reach out to us during the demonstration periods, or check our resource links.</p>
                        <p className="mt-2 text-xs font-bold text-foreground font-sans">Contact: <a href="mailto:hatrock26@gmail.com" className="text-foreground underline hover:text-brand">hatrock26@gmail.com</a></p>
                    </div>
                </aside>

                {/* right main panel */}
                <main className="lg:col-span-3">
                    {activeTab === 'faqs' && (
                        <div className="flex flex-col gap-4">
                            <h2 className="font-display text-2xl text-foreground tracking-wide flex items-center gap-2">
                                <span>FREQUENTLY ASKED QUESTIONS</span>
                            </h2>
                            <FaqAccordion items={FAQ_DATA} searchQuery={searchQuery} />
                        </div>
                    )}
                    {activeTab === 'set-types' && (
                        <SetTypeGuideContent />
                    )}

                    {activeTab === 'tutorials' && (
                        <div className="flex flex-col gap-4">
                            <h2 className="font-display text-2xl text-foreground tracking-wide">VIDEO TUTORIALS</h2>
                            {TUTORIAL_DATA.filter((v) => v.title.toLowerCase().includes(searchQuery.toLowerCase())
                                || v.description.toLowerCase().includes(searchQuery.toLowerCase())
                            ).length === 0 ? (
                                <div className="rounded x-1 border border-border bg-surface p-8 text-center">
                                    <Video className="mx-auto h-10 w-10 text-muted-foreground mb-3" />
                                    <h3 className="font-display text-xl text-foreground mb-1">NO MATCHING TUTORIALS FOUND</h3>
                                    <p className="text-sm text-muted-foreground">Try searching for a different keyword, such as 'workout' or 'schedule'.</p>
                                </div>
                            ) : (
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                                    {TUTORIAL_DATA.filter((v) =>
                                        v.title.toLowerCase().includes(searchQuery.toLowerCase()) ||
                                        v.description.toLowerCase().includes(searchQuery.toLowerCase())
                                    ).map((video) => (
                                        <TutorialCard key={video.id} video={video} />
                                    ))}
                                </div>
                            )}
                        </div>
                    )}

                    {activeTab === 'resources' && (
                        <div className="flex flex-col gap-4">
                            <h2 className="font-display text-2xl text-foreground tracking-wide">HELP CENTRE & RESOURCES</h2>
                            <ResourcePanel />
                        </div>
                    )}
                </main>
            </div>
        </section>
    )
}