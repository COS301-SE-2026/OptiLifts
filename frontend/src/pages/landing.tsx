import { useEffect, useRef, useState, type CSSProperties } from 'react'
import { Link } from 'react-router-dom'
import { TrendingUp, Activity, Dumbbell, LayoutDashboard, Calendar } from 'lucide-react'
import { Navbar } from '@/components/ui/navbar'
import { Button } from '@/components/ui/button'
import background from '@/assets/gym.png'

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
    { Icon: Dumbbell, label: 'Workout Creation' },
    { Icon: Activity, label: 'Sessions' },
    { Icon: LayoutDashboard, label: 'Dashboard' },
    { Icon: TrendingUp, label: 'Progressive Overload Engine' },
    { Icon: Calendar, label: 'Scheduling'}
]

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
                        <Link to="/register">Get Started</Link>
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
                        Workouts that adapt for <span className="text-brand">you</span> after every session to ensure progressive overload
                    </h2>
                </section>

                <section className="bg-white pb-24 pt-16 text-center">
                    <h3 className="mb-12 text-[clamp(20px,3vw,32px)] text-black">
                        How do we achieve this?
                    </h3>

                    <div className="carouselview">
                        <div className="carousel-items-holder">
                            {[...FEATS, ...FEATS].map(({ Icon, label }, i) => (
                                <div key={`${label}-${i}`} className="items">
                                    <Icon size={168} strokeWidth={2.5} className="text-black" />
                                    <span className="item-labels">{label}</span>
                                </div>
                            ))}
                        </div>
                    </div>
                </section>
            </main>
        </div>
    )
}
