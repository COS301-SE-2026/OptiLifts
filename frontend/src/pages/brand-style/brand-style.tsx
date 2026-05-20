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
            <div className="section-row">
                <div>
                    <h2 className="section-heading">Typography</h2>
                    <div className="typography-section">
                        <p className="type-body">OptiLifts uses a two-font system: a display font for headings and a high-legibility UI font for body and interface text. Below is a concise, scannable reference of roles and canonical values.</p>

                        <div className="typography-grid">
                            <Card className="typography-item">
                                <CardContent>
                                    <div className="type-page-title">Page Title - OPTILIFTS</div>
                                    <div className="type-meta">Bebas Neue · 42px · 400 · +2px · Page titles, wordmarks</div>
                                </CardContent>
                            </Card>

                            <Card className="typography-item">
                                <CardContent>
                                    <div className="type-section-title">Section Title</div>
                                    <div className="type-meta">Bebas Neue · 18px · 400 · +1.5px</div>
                                </CardContent>
                            </Card>

                            <Card className="typography-item">
                                <CardContent>
                                    <div className="type-card-value">Card Value</div>
                                    <div className="type-meta">Bebas Neue · 40px · 400 · +1px</div>
                                </CardContent>
                            </Card>

                            <Card className="typography-item">
                                <CardContent>
                                    <div className="type-button">BUTTON</div>
                                    <div className="type-meta">Barlow · 13px · 700 · Uppercase</div>
                                </CardContent>
                            </Card>

                            <Card className="typography-item">
                                <CardContent>
                                    <div className="type-body">Body text sample</div>
                                    <div className="type-meta">Barlow · 14px · 400</div>
                                </CardContent>
                            </Card>

                            <Card className="typography-item">
                                <CardContent>
                                    <div className="type-label">LABEL / META</div>
                                    <div className="type-meta">Barlow · 10–12px · 600–700 · Uppercase</div>
                                </CardContent>
                            </Card>
                        </div>

                        <div style={{ marginTop: '1rem' }}>
                            <h3 className="type-section-title">Typeface connotations</h3>
                            <p className="type-body">The 'Bebas Neue' font is condensed and bold, conveying athleticism and impact. An excellent font for headlines, it portrays our brands character. 'Barlow' is a highly legible font, chosen for its contribution to a clear UI hierarchy, and comfortable reading. Together, these two fonts create a balance of boldness and usability.
                            </p>
                        </div>
                    </div>
                </div>


            </div>

            <div className="section-row">
                <div>
                    <h2 className="section-heading">Logo and Iconography</h2>
                    <div className="logo-guidelines">
                        <p><strong>Overview</strong>: The OptiLifts logo system comprises an <strong>icon mark</strong> (dumbbell + hexagon) and a <strong>wordmark</strong>. Each is provided in light and dark static SVGs (logo-light.svg, logo-dark.svg). Animated icon variants are available for motion contexts.</p>

                        <h3 className="type-section-title">Colour & variants</h3>
                        <p>Use the CSS tokens defined in <code>src/index.css</code> for all logo colouring: <code>--foreground</code> (structural) and <code>--brand</code>/<code>--brand-2</code> (accent).</p>

                        <h3 className="type-section-title">Sizing & clearspace</h3>
                        <ul>
                            <li><strong>Icon mark minimum:</strong> 20×20px (UI/icon contexts). Prefer vector SVGs so they scale cleanly.</li>
                            <li><strong>Wordmark minimum height:</strong> 32px. Use larger sizes for hero/header contexts.</li>
                            <li><strong>Clearspace:</strong> Maintain at least 50% of the mark height as clearspace around the logo.</li>
                        </ul>

                        <h3 className="type-section-title">Iconography rules</h3>
                        <ul>
                            <li>Source icons from <strong>Lucide Icons</strong> for consistency (the repo uses <code>lucide-react</code>).</li>
                            <li>Render icons at <strong>18×18px</strong> in sidebars/navigation; buttons may use 20px for balance.</li>
                            <li>Use <code>currentColor</code> so icons inherit parent colour tokens.</li>
                            <li>Ensure minimum touch target of <strong>44×44px</strong> on mobile.</li>
                        </ul>

                        <div className="logo-visuals">
                            <div className="logo-sample">
                                <img src="/logo-light.svg" alt="OptiLifts logo light" className="logo-sample__img" />
                                <div className="logo-caption">Light variant - logo-light.svg</div>
                                <div className="clearspace" />
                            </div>

                            <div className="logo-sample dark">
                                <img src="/logo-dark.svg" alt="OptiLifts logo dark" className="logo-sample__img" />
                                <div className="logo-caption">Dark variant - logo-dark.svg</div>
                                <div className="clearspace" />
                            </div>

                            <div className="logo-sample">
                                <img src="/logo-light.svg" alt="Icon mark light" className="logo-sample__img small" />
                                <div className="logo-caption">Icon mark - 48×48 preview</div>
                            </div>

                            <div className="logo-sample dark">
                                <img src="/logo-dark.svg" alt="Icon mark dark" className="logo-sample__img small" />
                                <div className="logo-caption">Icon mark - 48×48 preview (dark)</div>
                            </div>
                        </div>

                        <div style={{ marginTop: '0.75rem' }}>
                            <p style={{ marginBottom: '.25rem' }}><strong>Iconography library</strong></p>
                            <div className="icon-library">
                                <div className="icon-sample"><div className="icon-box"><Plus size={18} /></div><div className="icon-caption">Plus -Add action (Workouts, Create)</div></div>
                                <div className="icon-sample"><div className="icon-box"><MoreHorizontal size={18} /></div><div className="icon-caption">More - overflow menu / contextual actions</div></div>
                                <div className="icon-sample"><div className="icon-box"><X size={18} /></div><div className="icon-caption">X - Close / dismiss</div></div>
                                <div className="icon-sample"><div className="icon-box"><Eye size={18} /></div><div className="icon-caption">Eye - Toggle visibility</div></div>
                                <div className="icon-sample"><div className="icon-box"><LogOut size={18} /></div><div className="icon-caption">LogOut - Sign out</div></div>
                                <div className="icon-sample"><div className="icon-box"><ChevronDown size={18} /></div><div className="icon-caption">ChevronDown - Expand / collapse</div></div>
                                <div className="icon-sample"><div className="icon-box"><User size={18} /></div><div className="icon-caption">User - Profile / avatar placeholder</div></div>
                                <div className="icon-sample"><div className="icon-box"><Dumbbell size={18} /></div><div className="icon-caption">Dumbbell - Exercise / workout concept</div></div>
                                <div className="icon-sample"><div className="icon-box"><Info size={18} /></div><div className="icon-caption">Info - System messages</div></div>
                                <div className="icon-sample"><div className="icon-box"><Sun size={18} /></div><div className="icon-caption">Sun - Theme toggle</div></div>
                            </div>
                        </div>
                    </div>
                </div>


            </div>


        </section>
    )
}