import { useEffect, useRef, useState, type CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { Activity, LayoutDashboard, Calendar, UserRound, TrendingUp } from 'lucide-react'
import { Navbar } from '@/components/ui/navbar'
import { Button } from '@/components/ui/button'
import background from '@/assets/gym.png'
import dashboardPageImage from '@/assets/Dashboard_Page.png'
import profilePageImage from '@/assets/Profile_Page.png'
import schedulePageImage from '@/assets/Schedule_Page.png'
import sessionPageImage from '@/assets/Session_Page.png'
import type { LucideIcon } from 'lucide-react'

type Rect = { left: number; top: number; height: number }

//gets element's pos of navbar logo (constant)
function toVPRect(el: HTMLElement): Rect {
    const pos = el.getBoundingClientRect()

    return { 
        left: pos.left, top: pos.top, height: pos.height 
    }
}

//get element's pos of hero logo (moving with scroll)
function toDocRect(el: HTMLElement): Rect {
    const pos = el.getBoundingClientRect()

    return { 
        left: pos.left, top: pos.top + window.scrollY, height: pos.height 
    }
}

//linear interpolation for anims
const linear = (a: number, b: number, t: number) => a + (b - a) * t

const FEATS = [
    { Icon: LayoutDashboard, label: 'Dashboard', tabIndex: 0, page: 'card' as const },
    { Icon: UserRound, label: 'Profile', tabIndex: 1, page: 'card' as const },
    { Icon: Calendar, label: 'Scheduling', tabIndex: 2, page: 'card' as const },
    { Icon: Activity, label: 'Sessions', tabIndex: 3, page: 'card' as const },
    { Icon: TrendingUp, label: 'Progressive Overload', page: 'progressive' as const },
]

const CARD_TABS = [
    { Icon: LayoutDashboard, label: 'Dashboard', blurb: 'View your training overview with your volume history, upcoming workouts and more.', image: dashboardPageImage, imageAlt: 'Dashboard page preview' },
    { Icon: UserRound, label: 'Profile', blurb: 'Manage your profile, preferences, and see your achievements, badges and more.', image: profilePageImage, imageAlt: 'Profile page preview' },
    { Icon: Calendar, label: 'Scheduling', blurb: 'Plan upcoming sessions with structure that fits your week.', image: schedulePageImage, imageAlt: 'Scheduling page preview' },
    { Icon: Activity, label: 'Active Sessions', blurb: 'Track your live workout execution and completed sets in one flow.', image: sessionPageImage, imageAlt: 'Sessions page preview' },
]

type FlyIconAnim = {
    id: number
    Icon: LucideIcon
    fromX: number
    fromY: number
    toX: number
    toY: number
}

type TabTransitionOptions = {
    scrollToCard?: boolean
}

//didn't want seperate CSS file.
const CAROUSEL_STYLE = `
    .carouselview 
    {
        overflow: hidden;
        width: 100%;
        -webkit-mask-image: linear-gradient(to right, transparent, black 10%, black 90%, transparent);
        mask-image: linear-gradient(to right, transparent, black 10%, black 90%, transparent);
    }

    .carousel-items-holder
    {
        display: flex;
        width: max-content;
        animation: slide-anim 28s linear infinite;
        /* all 3 below fix flickering */
        will-change: transform;
        transform: translateZ(0);
        backface-visibility: hidden;
    }

    .carousel-items-holder:has(.items:hover) 
    {
        animation-play-state: paused;
    }

    @keyframes slide-anim 
    {
        from { transform: translateX(0); }
        to { transform: translateX(-50%); }
    }

    .items 
    {
        display: flex;
        flex-direction: column;
        align-items: center;
        flex-shrink: 0;
        padding: 0 72px;
    }

    .items svg 
    {
        transition: color 150ms linear;
    }

    .items:hover svg 
    {
        color: var(--brand);
    }

    .item-labels 
    {
        margin-top: 20px;
        font-family: 'Barlow', sans-serif;
        font-size: 14px;
        font-weight: 600;
        letter-spacing: 1px;
        text-transform: uppercase;
        color: var(--brand);
        text-decoration: underline;
        text-decoration-color: var(--brand);
        text-underline-offset: 8px;
        opacity: 0;
        transition: opacity 150ms linear;
    }

    .items:hover .item-labels 
    {
        opacity: 1;
    }
`

export default function LandingPage() {
    //refs and states
    const heroRef = useRef<HTMLElement>(null)
    const iconSpacerRef = useRef<HTMLDivElement>(null)
    const navWrapRef = useRef<HTMLDivElement>(null)
    const [scrollY, setScrollY] = useState(0)
    const [heroHeight, setHeroHeight] = useState(0)
    const [startRect, setStartRect] = useState<Rect | null>(null)
    const [endRect, setEndRect] = useState<Rect | null>(null)
    const [activeCardTab, setActiveCardTab] = useState(0)
    const [flyIconAnim, setFlyIconAnim] = useState<FlyIconAnim | null>(null)
    const tabButtonRefs = useRef<Array<HTMLButtonElement | null>>([])
    const focusSlotRef = useRef<HTMLDivElement>(null)
    const cardSectionRef = useRef<HTMLElement>(null)
    const cardFrameRef = useRef<HTMLDivElement>(null)
    const progressiveSectionRef = useRef<HTMLElement>(null)
    const flyAnimIdRef = useRef(0)

    useEffect(() => {
        const root = document.documentElement
        const darkTheme = root.classList.contains('dark')

        root.classList.remove('dark')

        return () => {
            if (darkTheme) {
                root.classList.add('dark')
            }
        }
    }, [])

    //measure hero height, navbar logo and resting logo positions.
    useEffect(() => {
        function measurePosits() {
            const imageStart = iconSpacerRef.current?.querySelector('img') ?? null
            const imageEnd = navWrapRef.current?.querySelector<HTMLImageElement>('img[src="/logo-light.svg"]') ?? null
            
            if (imageStart) setStartRect(toDocRect(imageStart))
            if (imageEnd) setEndRect(toVPRect(imageEnd))
            if (heroRef.current) setHeroHeight(heroRef.current.offsetHeight)
        }

        measurePosits()

        const idk = requestAnimationFrame(() => requestAnimationFrame(measurePosits))
        
        window.addEventListener('load', measurePosits)
        window.addEventListener('resize', measurePosits)

        return () => {
        cancelAnimationFrame(idk)
        window.removeEventListener('load', measurePosits)
        window.removeEventListener('resize', measurePosits)
        }
    }, [])

    //makes it so setScrollY only re-renders once per screen repaint
    useEffect(() => {
        let throttle = false

        function onScroll() {
            if (!throttle) {
                window.requestAnimationFrame(() => {
                    setScrollY(window.scrollY)
                    throttle = false
                })
                throttle = true
            }
        }

        onScroll()
        window.addEventListener('scroll', onScroll, { passive: true })
        return () => window.removeEventListener('scroll', onScroll)
    }, [])

    //icon to corner, navbar fades in after, then icon fades out after (all in sequence)
    //durations of phases in pixels
    const travelDist = heroHeight * 0.35
    const revealDist = heroHeight * 0.15
    const exitDist = heroHeight * 0.1
    
    const moveProg = travelDist > 0 ? Math.min(1, Math.max(0, scrollY / travelDist)) : 0
    const navProg = revealDist > 0 ? Math.min(1, Math.max(0, (scrollY - travelDist) / revealDist)) : 0
    const exitProg = exitDist > 0 ? Math.min(1, Math.max(0, (scrollY - travelDist - revealDist) / exitDist)) : 0

    //flags
    const done = navProg >= 1
    const ready = startRect && endRect

    //icon style 
    const style: CSSProperties = {
        position: 'fixed',
        left: ready ? linear(startRect.left, endRect.left, moveProg) : 0,
        top: ready ? linear(startRect.top, endRect.top, moveProg) : 0,
        height: ready ? linear(startRect.height, endRect.height, moveProg) : 0,
        opacity: ready ? 1 - exitProg : 0,
        zIndex: 150,
        pointerEvents: 'none',
    }

    //opacities fading as scrolling
    const OPTILIFT = 1 - moveProg
    const slogan = 1 - moveProg
    const navbar = navProg
    const activeTabMeta = CARD_TABS[activeCardTab]
    const ActiveFocusIcon = activeTabMeta.Icon

    function scrollToElementWithOffset(el: HTMLElement, offset: number) {
        const targetY = window.scrollY + el.getBoundingClientRect().top - offset
        window.scrollTo({ top: Math.max(0, targetY), behavior: 'smooth' })
    }

    function triggerTabTransition(tabIndex: number, options?: TabTransitionOptions) {
        const tabButton = tabButtonRefs.current[tabIndex]
        const focusSlot = focusSlotRef.current
        const tabMeta = CARD_TABS[tabIndex]

        setActiveCardTab(tabIndex)

        if (options?.scrollToCard) {
            const cardTarget = cardFrameRef.current ?? cardSectionRef.current
            if (cardTarget) {
                scrollToElementWithOffset(cardTarget, 104)
            }
        }

        if (!tabButton || !focusSlot || !tabMeta) return

        const tabRect = tabButton.getBoundingClientRect()
        const focusRect = focusSlot.getBoundingClientRect()
        const nextId = flyAnimIdRef.current + 1

        flyAnimIdRef.current = nextId

        setFlyIconAnim({
            id: nextId,
            Icon: tabMeta.Icon,
            fromX: tabRect.left + tabRect.width / 2,
            fromY: tabRect.top + tabRect.height / 2,
            toX: focusRect.left + focusRect.width / 2,
            toY: focusRect.top + focusRect.height / 2,
        })
    }

    function handleCarouselClick(feature: (typeof FEATS)[number]) {
        if (feature.page === 'progressive') {
            progressiveSectionRef.current?.scrollIntoView({
                behavior: 'smooth',
                block: 'start',
            })
            return
        }

        triggerTabTransition(feature.tabIndex, { scrollToCard: true })
    }

    useEffect(() => {
        if (!flyIconAnim) return

        const timeoutId = window.setTimeout(() => {
            setFlyIconAnim((current) => (current?.id === flyIconAnim.id ? null : current))
        }, 420)

        return () => window.clearTimeout(timeoutId)
    }, [flyIconAnim])

    return (
        <div>
            <style>{CAROUSEL_STYLE}</style>

            <div
                ref={navWrapRef} className="fixed inset-x-0 top-0 z-[100]"
                style={{ opacity: navbar, pointerEvents: done ? 'auto' : 'none' }}
            >
                <Navbar/>
            </div>

            <section ref={heroRef} className="relative flex min-h-dvh flex-col items-center justify-center overflow-hidden">
                <img src={background} alt="" className="absolute inset-0 h-full w-full object-cover" />
                <div className="absolute inset-0 bg-black/55" />

                <div ref={iconSpacerRef} className="invisible flex h-[clamp(112px,24vh,280px)] items-center">
                    <img src="/logo-dark.svg" className="h-full w-auto" alt="" />
                </div>

                <img src="/logo-dark.svg" className="w-auto" style={style} alt="OptiLifts" />

                <span
                    className="relative z-10 mt-4 font-display text-[clamp(32px,6vh,64px)] leading-none tracking-[2px]"
                    style={{ opacity: OPTILIFT }}
                >
                    <span className="text-white">OPTI</span><span className="text-brand">LIFTS</span>
                </span>

                <p
                    className="relative z-10 mt-6 text-center font-sans text-[clamp(16px,2.4vw,24px)] tracking-[1px] text-white"
                    style={{ opacity: slogan }}
                >
                Your next PR is already planned.
                </p>

                <div className="relative z-10 mt-8 flex gap-4" style={{ opacity: slogan }}>

                    <Button asChild size="sm">
                        <Link to="/register">Register</Link>
                    </Button>
                    <Button asChild variant="outline" size="sm" className="border-white text-white hover:bg-white/10">
                        <Link to="/login">Login</Link>
                    </Button>
                </div>
            </section>

            <main className="relative">
                <div className="h-24 bg-gradient-to-b from-black to-white sm:h-32" />

                <section className="bg-white px-6 pt-24 text-center">
                    <h2 
                        className="mx-auto max-w-3xl text-[clamp(28px,5vw,48px)] leading-tight text-black"
                    >
                        Workouts that adapt for <span className="text-brand">you</span> after every session to ensure progression
                    </h2>
                </section>

                <section className="bg-white pb-24 pt-16 text-center">
                    <h3 className="mb-12 text-[clamp(20px,3vw,32px)] text-black">
                        How do we achieve this?
                    </h3>

                    <div className="carouselview">
                        <div className="carousel-items-holder">
                            {[...FEATS, ...FEATS].map((feature, i) => (
                                <button
                                    key={`${feature.label}-${i}`}
                                    type="button"
                                    className="items cursor-pointer border-0 bg-transparent"
                                    onClick={() => handleCarouselClick(feature)}
                                >
                                    <feature.Icon size={168} strokeWidth={2.5} className="text-black" />
                                    <span className="item-labels">{feature.label}</span>
                                </button>
                            ))}
                        </div>
                    </div>
                </section>

                <section ref={cardSectionRef} className="scroll-mt-24 bg-white px-6 pb-28 pt-20 sm:pt-50">
                    <div className="mx-auto max-w-6xl">
                        <div ref={cardFrameRef} className="relative overflow-visible rounded-3xl border-2 border-brand/40 bg-white p-4 sm:p-6">
                            <div className="absolute left-0 top-1/2 z-10 flex -translate-x-1/2 -translate-y-1/2 flex-col items-center gap-3">
                                    {CARD_TABS.map(({ Icon, label }, index) => {
                                        const isActive = index === activeCardTab

                                        return (
                                            <button
                                                key={label}
                                                type="button"
                                                aria-label={label}
                                                aria-pressed={isActive}
                                                onClick={() => triggerTabTransition(index)}
                                                ref={(el) => {
                                                    tabButtonRefs.current[index] = el
                                                }}
                                                className={[
                                                    'relative flex h-11 w-11 items-center justify-center rounded-full border-2 transition-all duration-150 sm:h-12 sm:w-12',
                                                    isActive
                                                        ? 'border-brand bg-white text-white shadow-[0_0_0_6px_rgba(204,0,34,0.12)]'
                                                        : 'border-black/15 bg-white text-black hover:border-brand hover:text-brand',
                                                ].join(' ')}
                                            >
                                                <Icon size={20} strokeWidth={2.2} className={isActive ? 'opacity-0' : ''} />
                                                {isActive && <span className="absolute h-2.5 w-2.5 rounded-full bg-brand" />}
                                            </button>
                                        )
                                    })}
                            </div>

                            <div className="pl-8 sm:pl-10">
                                <div className="grid min-h-[420px] grid-cols-1 gap-6 rounded-2xl border-2 border-dashed border-brand/35 bg-white p-6 md:min-h-[520px] md:grid-cols-[210px_1fr] md:p-8">
                                    <div className="flex items-center justify-center">
                                        <div ref={focusSlotRef} className="relative flex h-28 w-28 items-center justify-center rounded-full border-2 border-brand bg-brand/10 text-brand md:h-32 md:w-32">
                                            <ActiveFocusIcon
                                                key={`${activeTabMeta.label}-${activeCardTab}`}
                                                size={56}
                                                strokeWidth={2.2}
                                                className="animate-[focus-icon-pop_320ms_cubic-bezier(0.2,0.8,0.2,1)]"
                                            />
                                        </div>
                                    </div>

                                    <div className="flex flex-col justify-center text-left">
                                        <h4 className="mt-3 text-[clamp(24px,3vw,36px)] leading-tight text-black">
                                            {activeTabMeta.label}
                                        </h4>
                                        <p className="mt-3 max-w-xl font-sans text-[15px] leading-relaxed text-black/70">
                                            {activeTabMeta.blurb}
                                        </p>
                                        <div className="mt-5 rounded-2xl border border-brand/25 bg-brand/[0.08] p-2">
                                            <div className="h-[320px] overflow-hidden rounded-xl bg-white md:h-[380px]">
                                                <img
                                                    src={activeTabMeta.image}
                                                    alt={activeTabMeta.imageAlt}
                                                    className="block h-full w-full scale-[1.06] rounded-xl object-cover object-top"
                                                    style={{ clipPath: 'inset(0 round 0.75rem)' }}
                                                />
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </section>

                <section ref={progressiveSectionRef} className="scroll-mt-24 bg-white px-6 pb-28 pt-4 sm:pt-8">
                    <div className="mx-auto max-w-6xl rounded-3xl border-2 border-brand/40 bg-white p-6 sm:p-8">
                        <div className="mt-3 flex items-center justify-center gap-3">
                            <TrendingUp size={50} strokeWidth={2.2} className="text-brand" />
                            <h3 className="text-[clamp(28px,4vw,44px)] leading-tight text-black">Progressive Overload</h3>
                        </div>
                            <p className="mx-auto mt-4 max-w-3xl text-center font-sans text-[20px] leading-relaxed text-black/70">
                            The Progressive Overload Engine is the core Optilifts. By analyzing your performance over your last few workouts, it dynamically adapts your upcoming sessions so you're always progressing at the right pace. Built on proven scientific research, it ensures that every time you train, you are pushing yourself safely and effectively.
                            The engine also factors in your rest days, recommends new exercises, and detects plateaus and long-term fatigue that trigger our other systems to make the right adjustments for your fitness journey.
                        </p>
                    </div>
                </section>
                
                <footer className="border-t-2 border-brand/40 bg-white px-6 py-10 sm:py-12">
                    <p className="mx-auto max-w-6xl text-center font-display text-[clamp(24px,4vw,42px)] leading-tight text-black">
                        are you ready to start you fitness journey?
                    </p>
                </footer>
            </main>

            {flyIconAnim && (
                <div
                    key={flyIconAnim.id}
                    className="pointer-events-none fixed z-[180] text-brand"
                    style={{
                        left: flyIconAnim.fromX,
                        top: flyIconAnim.fromY,
                        transform: 'translate(-50%, -50%)',
                        animation: `tab-icon-fly 420ms cubic-bezier(0.2, 0.8, 0.2, 1) forwards`,
                        ['--fly-to-x' as string]: `${flyIconAnim.toX - flyIconAnim.fromX}px`,
                        ['--fly-to-y' as string]: `${flyIconAnim.toY - flyIconAnim.fromY}px`,
                    }}
                >
                    <flyIconAnim.Icon size={26} strokeWidth={2.3} />
                </div>
            )}

            <style>{`
                @keyframes tab-icon-fly {
                    0% {
                        transform: translate(-50%, -50%) translate(0, 0) scale(1);
                        opacity: 0.95;
                    }
                    70% {
                        transform: translate(-50%, -50%) translate(var(--fly-to-x), var(--fly-to-y)) scale(1.65);
                        opacity: 1;
                    }
                    100% {
                        transform: translate(-50%, -50%) translate(var(--fly-to-x), var(--fly-to-y)) scale(1.8);
                        opacity: 0;
                    }
                }

                @keyframes focus-icon-pop {
                    0% {
                        transform: scale(0.72);
                        opacity: 0.35;
                    }
                    70% {
                        transform: scale(1.08);
                        opacity: 1;
                    }
                    100% {
                        transform: scale(1);
                        opacity: 1;
                    }
                }
            `}</style>
        </div>
    )
}
