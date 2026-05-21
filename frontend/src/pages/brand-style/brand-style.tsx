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
            <section className="design-principles">
                <h2 className="section-heading">Design Principles</h2>
                <p className="type-body">
                    These principles guide visuals and interactions in OptiLifts. They prioritise clarity, accessibility, and predictable behaviour. They are short so they fit easily into designs, code, and conversations.
                </p>

                <ol className="principle-grid">
                    <li className="principle-card">
                        <div className="principle-num">1</div>
                        <h3 className="principle-title">Data first</h3>
                        <p className="principle-copy">Lead with the key number or action so users can act quickly.</p>
                    </li>

                    <li className="principle-card">
                        <div className="principle-num">2</div>
                        <h3 className="principle-title">Progressive disclosure</h3>
                        <p className="principle-copy">Show summaries first. Reveal details on demand.</p>
                    </li>

                    <li className="principle-card">
                        <div className="principle-num">3</div>
                        <h3 className="principle-title">AI visible, not intrusive</h3>
                        <p className="principle-copy">Make AI suggestions visible but never override user choices.</p>
                    </li>

                    <li className="principle-card">
                        <div className="principle-num">4</div>
                        <h3 className="principle-title">Consistency over creativity</h3>
                        <p className="principle-copy">Keep layouts and components consistent for predictability.</p>
                    </li>

                    <li className="principle-card">
                        <div className="principle-num">5</div>
                        <h3 className="principle-title">Responsive by default</h3>
                        <p className="principle-copy">Layouts adapt across sizes. Mobile uses sheets and reflowing grids.</p>
                    </li>

                    <li className="principle-card">
                        <div className="principle-num">6</div>
                        <h3 className="principle-title">Accessibility is non‑negotiable</h3>
                        <p className="principle-copy">All controls include labels, visible focus, and WCAG AA contrast.</p>
                    </li>
                </ol>


            </section>

            <section className="ui-components">
                <h2 className="section-heading">UI Component Styling</h2>
                <p className="type-body">
                    A cohesive library of reusable components that establish consistency across OptiLifts. Each component follows the brand's visual language and accessibility standards.
                </p>

                <div className="component-section">
                    <div className="component-visual">
                        <div className="component-showcase">
                            <div className="component-showcase__group">
                                <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', justifyContent: 'center' }}>
                                    <Button>Start Session</Button>
                                </div>
                                <div className="component-label">Primary Button</div>
                            </div>
                            <div className="component-showcase__group">
                                <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', justifyContent: 'center' }}>
                                    <Button variant="secondary">Secondary</Button>
                                </div>
                                <div className="component-label">Secondary Button</div>
                            </div>
                            <div className="component-showcase__group">
                                <div style={{ display: 'flex', gap: '0.5rem', alignItems: 'center', justifyContent: 'center' }}>
                                    <Button variant="outline">+ Add Set</Button>
                                </div>
                                <div className="component-label">Outline Button</div>
                            </div>
                        </div>
                    </div>
                    <div className="component-description">
                        <h3 className="component-title">Button</h3>
                        <p className="component-copy"><strong>Primary:</strong> Call-to-action buttons for main workflows (Start Session, Save Workout). Uses brand colour with hover and focus states.</p>
                        <p className="component-copy"><strong>Secondary:</strong> Alternative actions with lower visual weight. Uses surface colour.</p>
                        <p className="component-copy"><strong>Outline:</strong> Tertiary actions like "Add Set" or "Create Exercise". Dashed border for visual distinction.</p>
                        <p className="component-copy"><strong>Icon, Ghost, Text:</strong> Additional variants for specific contexts (icon-only controls, text links).</p>
                    </div>
                </div>

                <div className="component-section">
                    <div className="component-visual">
                        <div className="component-showcase" style={{ padding: 0, flexDirection: 'column' }}>
                            <Card>
                                <CardHeader>
                                    <CardTitle>Workout Name</CardTitle>
                                    <CardDescription>Upper Body Focus</CardDescription>
                                </CardHeader>
                                <CardContent>
                                    5 exercises · 45 minutes
                                </CardContent>
                            </Card>
                        </div>
                    </div>
                    <div className="component-description">
                        <h3 className="component-title">Card</h3>
                        <p className="component-copy">Container component for content grouping. Includes header, title, description, content area, and footer slots.</p>
                        <p className="component-copy"><strong>Usage:</strong> Workout summaries, exercise lists, session details, and other self-contained information blocks.</p>
                        <p className="component-copy"><strong>Structure:</strong> Rounded corners (0.75rem), consistent padding (1.25rem), subtle border and shadow for depth.</p>
                        <p className="component-copy">Cards support a "small" size variant for compact contexts.</p>
                    </div>
                </div>

                <div className="component-section">
                    <div className="component-visual">
                        <div className="component-showcase" style={{ flexDirection: 'column', gap: '1rem' }}>
                            <div>
                                <label htmlFor="example-exercise-name" style={{ display: 'block', fontSize: '0.875rem', fontWeight: '600', marginBottom: '0.5rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Exercise Name</label>
                                <Input id="example-exercise-name" type="text" placeholder="Enter exercise name" />
                            </div>
                            <div>
                                <label htmlFor="example-weight" style={{ display: 'block', fontSize: '0.875rem', fontWeight: '600', marginBottom: '0.5rem', textTransform: 'uppercase', letterSpacing: '0.05em' }}>Weight (kg)</label>
                                <NumericalUnderscoreInput id="example-weight" placeholder="0" />
                            </div>
                        </div>
                    </div>
                    <div className="component-description">
                        <h3 className="component-title">Input</h3>
                        <p className="component-copy">Form field for text, number, and file input. Two variants: bordered (default) and underscored.</p>
                        <p className="component-copy"><strong>Features:</strong> Focus states with ring styling, error styling for invalid inputs, disabled state, and placeholder text support.</p>
                        <p className="component-copy"><strong>Usage:</strong> Workout names, set details, user profile fields, search inputs, and all form data collection.</p>
                        <p className="component-copy">Always pair with an associated label element for accessibility.</p>
                    </div>
                </div>

                <div className="component-section">
                    <div className="component-visual">
                        <div className="component-showcase" style={{ flexDirection: 'column', gap: '0.75rem' }}>
                            <div style={{ borderLeft: '4px solid var(--success)', background: 'rgba(27, 110, 31, 0.08)', padding: '0.875rem 1rem', borderRadius: '0.25rem', fontSize: '0.95rem' }}>
                                <div style={{ fontWeight: '700', fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '1px', marginBottom: '0.2rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}><CheckCircle2 size={16} />SUCCESS</div>
                                <div>Workout saved successfully.</div>
                            </div>
                            <div style={{ borderLeft: '4px solid var(--warning)', background: 'rgba(180, 92, 0, 0.08)', padding: '0.875rem 1rem', borderRadius: '0.25rem', fontSize: '0.95rem' }}>
                                <div style={{ fontWeight: '700', fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '1px', marginBottom: '0.2rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}><AlertTriangle size={16} />WARNING</div>
                                <div>High fatigue detected. Consider rest.</div>
                            </div>
                            <div style={{ borderLeft: '4px solid var(--brand)', background: 'rgba(204, 0, 34, 0.08)', padding: '0.875rem 1rem', borderRadius: '0.25rem', fontSize: '0.95rem' }}>
                                <div style={{ fontWeight: '700', fontSize: '0.75rem', textTransform: 'uppercase', letterSpacing: '1px', marginBottom: '0.2rem', display: 'flex', alignItems: 'center', gap: '0.5rem' }}><Info size={16} />INFO</div>
                                <div>You have 3 new workout recommendations.</div>
                            </div>
                        </div>
                    </div>
                    <div className="component-description">
                        <h3 className="component-title">Alert / Toast</h3>
                        <p className="component-copy">Non-blocking feedback messages for system events. Appears at the top centre and auto-dismisses after 5 seconds.</p>
                        <p className="component-copy"><strong>Variants:</strong> Info (blue), Success (green), Warning (amber), Error (red). Each variant has an icon and left border accent.</p>
                        <p className="component-copy"><strong>Usage:</strong> Confirm saves, warn about high fatigue, notify PR achievements, or provide helpful tips.</p>
                        <p className="component-copy">Triggered via the <code>toast</code> API with optional title and message.</p>
                    </div>
                </div>

                <div className="component-section">
                    <div className="component-visual">
                        <div className="component-showcase" style={{ flexDirection: 'column', gap: '1rem' }}>
                            <PageTitle title="Workouts" />
                        </div>
                    </div>
                    <div className="component-description">
                        <h3 className="component-title">PageTitle & Navbar</h3>
                        <p className="component-copy"><strong>PageTitle:</strong> Large heading with left accent bar (brand colour). Used at the top of major pages.</p>
                        <p className="component-copy"><strong>Navbar:</strong> Sticky header with logo, nav links, user avatar, and theme toggle. Responsive: sidebar uses the sheet component on mobile.</p>
                        <p className="component-copy"><strong>Theme Toggle:</strong> Simple icon button to switch between light and dark modes. Updates the app theme instantly.</p>
                        <p className="component-copy">All navigation components are responsive and follow touch-friendly sizing rules (min 44×44px).</p>
                    </div>
                </div>

                <div className="component-section">
                    <div className="component-visual">
                        <div className="component-showcase" style={{ flexDirection: 'column', gap: '1.5rem' }}>
                            <div>
                                <Avatar size="lg">
                                    <AvatarImage src="https://github.com/shadcn.png" alt="User" />
                                    <AvatarFallback>JD</AvatarFallback>
                                </Avatar>
                                <div className="component-label" style={{ marginTop: '0.5rem' }}>Avatar</div>
                            </div>
                            <div className="circular-sample">
                                <CircularProfileImage src="/logo-light.svg" alt="Profile" />
                                <div className="component-label" style={{ marginTop: '0.5rem' }}>Circular Image</div>
                            </div>
                        </div>
                    </div>
                    <div className="component-description">
                        <h3 className="component-title">Avatar & CircularImage</h3>
                        <p className="component-copy"><strong>Avatar:</strong> User profile images with fallback initials. Used in navbar, comments, and user lists.</p>
                        <p className="component-copy"><strong>CircularImage:</strong> Flexible circular image container for profile pictures and media. Accepts src and alt props.</p>
                        <p className="component-copy">Both components use perfect circles (border-radius: 50%) and include error handling for missing images.</p>
                    </div>
                </div>

                <div className="component-section">
                    <div className="component-visual">
                        <div className="component-showcase" style={{ fontSize: '0.875rem', flexDirection: 'column', gap: '0.75rem', width: '100%' }}>
                            <div style={{ background: 'var(--surface)', border: '1px solid var(--border)', padding: '0.75rem', borderRadius: '0.5rem' }}>
                                <div style={{ fontWeight: '600', marginBottom: '0.25rem' }}>Benchpress</div>
                                <div style={{ color: 'var(--muted-text)' }}>Chest • 3 × 8 reps</div>
                            </div>
                            <Input placeholder="Search exercises..." />
                        </div>
                    </div>
                    <div className="component-description">
                        <h3 className="component-title">Specialized Components</h3>
                        <p className="component-copy"><strong>ExerciseCard:</strong> Displays exercise name, targeted muscles, and set/rep info in a compact card format.</p>
                        <p className="component-copy"><strong>SearchInput:</strong> Text input with debounced search and optional filtering. Used for finding exercises and workouts.</p>
                        <p className="component-copy"><strong>MuscleDiagram:</strong> Interactive SVG showing muscle groups. Highlights muscles targeted by selected exercises.</p>
                        <p className="component-copy"><strong>CreateExercise:</strong> Modal or form for adding new custom exercises. Includes muscle selection and description.</p>
                        <p className="component-copy"><strong>DropdownMenu:</strong> Context menu for actions on cards (edit, delete, duplicate). Uses Radix primitives for accessibility.</p>
                    </div>
                </div>
            </section>

            <section className="accessibility-section">
                <h2 className="section-heading">Accessibility</h2>

                <p className="type-body" style={{ maxWidth: 920, margin: '0.25rem 0 1rem' }}>
                    OptiLifts targets WCAG 2.1 AA as the minimum standard across all screens and both themes. The implementation is split across Colour Contrast, Keyboard Navigability, Screen Reader Compatibility, and WCAG compliance mappings.
                </p>

                <div className="accessibility-grid">
                    <div className="accessibility-left">
                        <div className="type-section-title">Colour Contrast</div>
                        <p className="type-body" style={{ marginTop: '0.25rem' }}>All primary text/background combinations meet WCAG 2.1 AA (4.5:1 minimum for normal text, 3:1 for large text and UI components). Key verified pairs are shown below.</p>

                        <div className="swatch-list">
                            <div className="swatch-item">
                                <div className="swatch-samples">
                                    <div className="swatch-sample" style={{ background: '#E8E8EC' }} />
                                    <div className="swatch-sample" style={{ background: '#1C1C1F' }} />
                                </div>
                                <div className="swatch-meta">
                                    <div className="swatch-role">#E8E8EC on #1C1C1F</div>
                                    <div className="swatch-ratio">Dark · 13.2:1 · AAA</div>
                                </div>
                            </div>

                            <div className="swatch-item">
                                <div className="swatch-samples">
                                    <div className="swatch-sample" style={{ background: '#B01030' }} />
                                    <div className="swatch-sample" style={{ background: '#1C1C1F' }} />
                                </div>
                                <div className="swatch-meta">
                                    <div className="swatch-role">#B01030 on #1C1C1F</div>
                                    <div className="swatch-ratio">Dark · 5.1:1 · AA</div>
                                </div>
                            </div>

                            <div className="swatch-item">
                                <div className="swatch-samples">
                                    <div className="swatch-sample" style={{ background: '#9A9AA8' }} />
                                    <div className="swatch-sample" style={{ background: '#1C1C1F' }} />
                                </div>
                                <div className="swatch-meta">
                                    <div className="swatch-role">#9A9AA8 on #1C1C1F</div>
                                    <div className="swatch-ratio">Dark · 4.6:1 · AA</div>
                                </div>
                            </div>

                            <div className="swatch-item">
                                <div className="swatch-samples">
                                    <div className="swatch-sample" style={{ background: '#4CAF50' }} />
                                    <div className="swatch-sample" style={{ background: '#1C1C1F' }} />
                                </div>
                                <div className="swatch-meta">
                                    <div className="swatch-role">#4CAF50 on #1C1C1F</div>
                                    <div className="swatch-ratio">Dark · 5.8:1 · AA</div>
                                </div>
                            </div>

                            <div className="swatch-item">
                                <div className="swatch-samples">
                                    <div className="swatch-sample" style={{ background: '#FF9800' }} />
                                    <div className="swatch-sample" style={{ background: '#1C1C1F' }} />
                                </div>
                                <div className="swatch-meta">
                                    <div className="swatch-role">#FF9800 on #1C1C1F</div>
                                    <div className="swatch-ratio">Dark · 8.2:1 · AAA</div>
                                </div>
                            </div>

                            <div className="swatch-item">
                                <div className="swatch-samples">
                                    <div className="swatch-sample" style={{ background: '#1A1A1A' }} />
                                    <div className="swatch-sample" style={{ background: '#FAF8F8' }} />
                                </div>
                                <div className="swatch-meta">
                                    <div className="swatch-role">#1A1A1A on #FAF8F8</div>
                                    <div className="swatch-ratio">Light · 16.8:1 · AAA</div>
                                </div>
                            </div>

                            <div className="swatch-item">
                                <div className="swatch-samples">
                                    <div className="swatch-sample" style={{ background: '#CC0022' }} />
                                    <div className="swatch-sample" style={{ background: '#FFFFFF' }} />
                                </div>
                                <div className="swatch-meta">
                                    <div className="swatch-role">#CC0022 on #FFFFFF</div>
                                    <div className="swatch-ratio">Light · 5.9:1 · AA</div>
                                </div>
                            </div>

                            <div className="swatch-item">
                                <div className="swatch-samples">
                                    <div className="swatch-sample" style={{ background: '#666666' }} />
                                    <div className="swatch-sample" style={{ background: '#FAF8F8' }} />
                                </div>
                                <div className="swatch-meta">
                                    <div className="swatch-role">#666666 on #FAF8F8</div>
                                    <div className="swatch-ratio">Light · 5.2:1 · AA</div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div className="accessibility-right">
                        <h3 className="type-section-title">Keyboard Navigability</h3>
                        <ul className="access-list">
                            <li>Tab order follows logical top-to-bottom, left-to-right reading flow on every screen.</li>
                            <li>Radix/shadcn modals and sheets trap focus and return it on close.</li>
                            <li>Roving tabindex on ToggleGroups; arrow keys move, Enter/Space selects.</li>
                            <li>Custom components implement <code>role</code>, <code>tabIndex</code>=0 and key handlers for Enter/Space.</li>
                            <li>All interactive elements show visible focus with <code>focus-visible</code> ring styling.</li>
                        </ul>

                        <h3 className="type-section-title">Screen Reader Compatibility</h3>
                        <ul className="access-list">
                            <li>All inputs use explicit labels; placeholders are never the sole label.</li>
                            <li>Validation errors link to inputs via <code>aria-describedby</code> and use <code>role="alert"</code>.</li>
                            <li>Dynamic updates use <code>aria-live="polite"</code> (or <code>assertive</code> for critical alerts).</li>
                            <li>Progress indicators use <code>role="progressbar"</code> with proper ARIA values.</li>
                            <li>Non-standard interactive controls expose correct roles and state via ARIA attributes.</li>
                        </ul>

                        <h3 className="type-section-title">WCAG 2.1 AA Mappings</h3>
                        <ul className="access-list">
                            <li><strong>1.4.3 Contrast</strong>: All theme colour pairs verified (see table).</li>
                            <li><strong>1.4.4 Resize text</strong>: rem-based sizing; pages scale to 200% without layout breakage.</li>
                            <li><strong>2.1.1 Keyboard operable</strong>: All interactions are keyboard-accessible.</li>
                            <li><strong>2.4.7 Focus visible</strong>: focus-visible ring applied to interactive elements.</li>
                            <li><strong>3.3.1 Error identification</strong>: Inline errors surfaced and announced to assistive tech.</li>
                        </ul>
                    </div>
                </div>
            </section>

        </section>
    )
}