import "./brand-style.css";
import { Plus, MoreHorizontal, X, Eye, LogOut, ChevronDown, User, Dumbbell, Info, Sun, CheckCircle2, AlertTriangle } from 'lucide-react'
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card';
import { Input, NumericalUnderscoreInput } from '@/components/ui/input';
import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar';
import { CircularProfileImage } from '@/components/ui/circular-image';
import { PageTitle } from '@/components/ui/page-title';

export default function BrandStylePage() {
    return (
        <section className="brand-style-page">
            <header className="brand-header">
                <div className="brand-logo">
                    <img src="/logo-light.svg" className="logo-light" alt="OptiLifts" />
                    <img src="/logo-dark.svg" className="logo-dark" alt="OptiLifts" />
                </div>
                <div className="brand-wordmark">
                    <span className="brand-wordmark__opt">OPTI</span>
                    <span className="brand-wordmark__lifts">LIFTS</span>
                </div>
            </header>

            <h1 className="section-heading">Brand Style</h1>

            <div className="brand-intro">
                <p className="type-body">The OptiLifts brand defines how we present ourselves visually and verbally. It ensures a consistent, professional appearance across our products, marketing, and support so users recognize and trust the brand at every touchpoint. These guidelines help designers and developers build interfaces that feel coherent, usable, and reliable.</p>
                <p className="type-body">We are a results-focused fitness brand built for people who train with intent. Our product helps users track progress, celebrate wins, and make steady improvements. We aim to communicate clearly, reduce friction, and motivate action without adding unnecessary noise.</p>

                <h3 className="type-section-title">Goals</h3>
                <div className="goals-grid">
                    <Card className="goal-card">
                        <CardContent>
                            <div className="goal-title">Clarity</div>
                            <p className="goal-copy">Present data and actions simply so users can make quick decisions.</p>
                        </CardContent>
                    </Card>

                    <Card className="goal-card">
                        <CardContent>
                            <div className="goal-title">Trust</div>
                            <p className="goal-copy">Use consistent visuals and copy to build confidence in the product.</p>
                        </CardContent>
                    </Card>

                    <Card className="goal-card">
                        <CardContent>
                            <div className="goal-title">Motivation</div>
                            <p className="goal-copy">Encourage progress with energetic and specific language.</p>
                        </CardContent>
                    </Card>

                    <Card className="goal-card">
                        <CardContent>
                            <div className="goal-title">Accessibility</div>
                            <p className="goal-copy">Ensure interfaces work for as many users as possible (WCAG AA baseline).</p>
                        </CardContent>
                    </Card>

                    <Card className="goal-card">
                        <CardContent>
                            <div className="goal-title">Efficiency</div>
                            <p className="goal-copy">Minimize friction to make common paths fast and obvious.</p>
                        </CardContent>
                    </Card>
                </div>

                <h3 className="type-section-title">Tone</h3>
                <p className="type-body">Tone varies by context but stays clear, confident, and encouraging. Use language that feels human and helpful rather than formal legalese or hype.</p>

                <div className="tone-grid">
                    <Card className="tone-card">
                        <CardContent>
                            <div className="tone-title">In‑app (UI & notifications)</div>
                            <p className="tone-copy">Energetic and direct. Use active verbs and short commands like "Start session"."</p>
                        </CardContent>
                    </Card>

                    <Card className="tone-card">
                        <CardContent>
                            <div className="tone-title">Website & marketing</div>
                            <p className="tone-copy">Motivating and benefit-focused. Highlight outcomes and next steps.</p>
                        </CardContent>
                    </Card>

                    <Card className="tone-card">
                        <CardContent>
                            <div className="tone-title">Support & emails</div>
                            <p className="tone-copy">Professional and helpful. Be polite, concise, and clear.</p>
                        </CardContent>
                    </Card>

                    <Card className="tone-card">
                        <CardContent>
                            <div className="tone-title">Accessibility</div>
                            <p className="tone-copy">Use plain language, avoid slang, and prefer short sentences for clarity.</p>
                        </CardContent>
                    </Card>

                    <Card className="tone-card">
                        <CardContent>
                            <div className="tone-title">Voice tip</div>
                            <p className="tone-copy">Prefer second-person "you" when guiding users. Keep language inclusive and positive.</p>
                        </CardContent>
                    </Card>
                </div>
            </div>
            <div className="section-row">
                <div>
                    <h2 className="section-heading">Colour Palette</h2>
                    <h3 className="type-section-title">Light Mode Palette</h3>
                    <ul className="palette-grid" aria-label="Colour palette">
                        <li className="swatch swatch--background">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#FAF8F8</div>
                            <div className="swatch__role">Background</div>
                        </li>
                        <li className="swatch swatch--border">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#E8DEDE</div>
                            <div className="swatch__role">Secondary</div>
                        </li>
                        <li className="swatch swatch--brand">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#CC0022</div>
                            <div className="swatch__role">Primary</div>
                        </li>
                        <li className="swatch swatch--brand-2">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#AA0018</div>
                            <div className="swatch__role">Secondary</div>
                        </li>
                        <li className="swatch swatch--foreground">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#1A1A1A</div>
                            <div className="swatch__role">Accent</div>
                        </li>
                        <li className="swatch swatch--muted-text">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#666666</div>
                            <div className="swatch__role">Secondary</div>
                        </li>
                        <li className="swatch swatch--success">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#1B6E1F</div>
                            <div className="swatch__role">Accent</div>
                        </li>
                        <li className="swatch swatch--warning">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#B35C00</div>
                            <div className="swatch__role">Accent</div>
                        </li>
                    </ul>

                    <h3 className="type-section-title">Dark Mode Palette</h3>
                    <ul className="palette-grid palette-grid--dark" aria-label="Colour palette dark">
                        <li className="swatch swatch--background">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#1C1C1F</div>
                            <div className="swatch__role">Background</div>
                        </li>
                        <li className="swatch swatch--border">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#3A3A42</div>
                            <div className="swatch__role">Secondary</div>
                        </li>
                        <li className="swatch swatch--brand">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#CC0022</div>
                            <div className="swatch__role">Primary</div>
                        </li>
                        <li className="swatch swatch--brand-2">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#D94060</div>
                            <div className="swatch__role">Secondary</div>
                        </li>
                        <li className="swatch swatch--foreground">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#E8E8EC</div>
                            <div className="swatch__role">Accent</div>
                        </li>
                        <li className="swatch swatch--muted-text">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#9A9AA8</div>
                            <div className="swatch__role">Secondary</div>
                        </li>
                        <li className="swatch swatch--success">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#4CAF50</div>
                            <div className="swatch__role">Accent</div>
                        </li>
                        <li className="swatch swatch--warning">
                            <div className="swatch__color" />
                            <div className="swatch__hex">#FF9800</div>
                            <div className="swatch__role">Accent</div>
                        </li>
                    </ul>

                    <div className="palette-description">
                        <p>
                            OptiLifts's colour palette is inspired by the principle of <strong>progressive overloading</strong>, the core concept of the application. <strong>Bold Crimson</strong> was chosen to inspire action and energy, reminiscent of the drive behind every rep.
                        </p>
                        <p>
                            Neutral surfaces create stark contrast and clarity, with <strong>accent colours</strong> guiding focus without introducing visual clutter. Each hue promotes accessibility, and the layered surface neutrals establish an intuitive visual depth.
                        </p>
                        <p>
                            The dual-theme palette accommodates varied lighting environments. The <strong className="text-brand-2">Dark Mode</strong> variant is tailored for low-light gym settings, while the <strong className="text-brand">Light Mode</strong> variant delivers high-contrast visibility for outdoor training.
                        </p>
                        <p>
                            <strong>Semantic colours</strong> are used intentionally and sparingly. <strong className="text-success">Forest Green</strong> celebrates wins like PRs and completed workouts, while <strong className="text-warning">Amber</strong> serves as a clear, non-intrusive indicator for fatigue or warnings.
                        </p>
                    </div>
                </div>


            </div>

        </section>
    )
}