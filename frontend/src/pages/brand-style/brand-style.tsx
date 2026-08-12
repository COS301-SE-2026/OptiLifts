import "./brand-style.css";
import { Plus, MoreHorizontal, X, Eye, LogOut, ChevronDown, User, Dumbbell, Info, Sun, CheckCircle2, AlertTriangle, ArrowLeft } from 'lucide-react'
import { Button } from '@/components/ui/button';
import { Card, CardHeader, CardTitle, CardDescription, CardContent } from '@/components/ui/card';
import { Input, NumericalUnderscoreInput } from '@/components/ui/input';
import { Avatar, AvatarImage, AvatarFallback } from '@/components/ui/avatar';
import { CircularProfileImage } from '@/components/ui/circular-image';
import { PageTitle } from '@/components/ui/page-title';
import Badge from "@/components/ui/badge";
import { MuscleDiagram } from '@/components/ui/muscle-diagram';
import { BarChart } from '@/components/ui/barchart';
import { VolumeChart } from '@/components/ui/volume-chart';
import { SpiderGraph } from '@/components/ui/spider-graph';
import { Calendar } from '@/components/ui/calendar';
import { paletteData } from "./brand-data";
import { useNavigate } from 'react-router-dom'

function BrandHeader() {
    return(
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
    );
}
function BrandIntroSection() {
    return (
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
    );
}

function ColourPaletteSection(){
    return (
        <div className="section-row">
            <div>
                <h2 className="section-heading">Colour Palette and WCAG contrast</h2>
                <h3 className="type-section-title">Light Theme</h3>
                <ul className="palette-grid" aria-label="Light Colour palette">
                    {paletteData.map((colour) => (
                        <li key={`light-${colour.name}`} className="swatch">
                        <div className="swatch__color" style={{ backgroundColor: colour.lightHex }}/>
                        <div className="swatch__hex">{colour.lightHex}</div>
                        <div className="swatch__role">{colour.name.split('/')[0]}</div>
                    </li>
                    ))}
                </ul>
                    {/* problem: itds only showing the dark mode cus the setting is dark mode */}
                <h3 className="type-section-title">Dark Mode Palette</h3>
                <ul className="palette-grid palette-grid--dark" aria-label="Dark Colour palette">
                    {paletteData.map((colour) => (
                        <li key={`dark-${colour.name}`} className="swatch">
                        <div className="swatch__color" style={{ backgroundColor: colour.darkHex }}/>
                        <div className="swatch__hex">{colour.darkHex}</div>
                        <div className="swatch__role">{colour.name.split('/')[0]}</div>
                    </li>
                    ))}
                </ul>

                {/* new section */}
                <h3 className="type-section-title" style={{
                    marginTop: '2rem'
                }}>Colour tokens, Intended usage and Contrast matrix</h3>
                <div style={{
                    overflowX: 'auto',
                    marginTop: '0.75rem'
                }}>
                    <table style={{
                        width: '100%',
                        borderCollapse: 'collapse',
                        textAlign: 'left',
                        fontSize: '0.85rem'
                    }}>
                        <thead><tr style={{
                            borderBottom: '2px solid var(--border)',
                            background: 'var(--surface-2)'
                        }}>
                            <th style={{
                                padding: '0.6rem 0.75rem'
                            }}>Token & Name</th>
                            <th style={{
                                padding: '0.6rem 0.75rem'
                            }}>Light Mode (HEX/RGB/HSL)</th>
                            <th style={{
                                padding: '0.6rem 0.75rem'
                            }}>Dark Mode (HEX/RGB/HSL)</th>
                            <th style={{
                                padding: '0.6rem 0.75rem'
                            }}>WCAG 2.2 Contrast Ratio</th>
                            <th style={{
                                padding: '0.6rem 0.75rem'
                            }}>Intended Usage</th>
                        </tr>
                            </thead>
                            <tbody>
                                {paletteData.map((item) => (
                                    <tr key={item.variable} style={{borderBottom: '1px solid var(--border)'}}>
                                        <td style={{
                                            padding: '0.6rem 0.75rem',
                                            fontWeight: 600
                                        }}>
                                            <div>{item.name}</div>
                                            <code style={{
                                                fontSize: '0.75rem',
                                                color: 'var(--brand)'
                                            }}>{item.variable}</code>
                                        </td>
                                        <td style={{
                                            padding: '0.6rem 0.75rem',
                                            fontFamily: 'monospace'
                                        }}>
                                            <div><strong>{item.lightHex}</strong></div>
                                            <div style={{
                                                fontSize: '0.75rem',
                                                color: 'var(--muted-text)'
                                            }}>rgb({item.lightRgb})</div>
                                            <div style={{
                                                fontSize: '0.75rem',
                                                color: 'var(--muted-text)'
                                            }}>hsl({item.lightHsl})</div>
                                        </td>
                                        <td style={{
                                            padding: '0.6rem 0.75rem',
                                            fontFamily: 'monospace'
                                        }}>
                                            <div><strong>{item.darkHex}</strong></div>
                                            <div style={{
                                                fontSize: '0.75rem',
                                                color: 'var(--muted-text)'
                                            }}>rgb({item.darkRgb})</div>
                                            <div style={{
                                                fontSize: '0.75rem',
                                                color: 'var(--muted-text)'
                                            }}>hsl({item.darkHsl})</div>
                                        </td>
                                        <td style={{
                                            padding: '0.6rem 0.75rem',
                                            fontSize: '0.8rem'
                                        }}>
                                            <div><strong>Light: </strong>{item.contrastLight}</div>
                                            <div><strong>Dark: </strong>{item.contrastDark}</div>
                                        </td>
                                        <td style={{
                                            padding: '0.6rem 0.75rem',
                                            color: 'var(--muted-text)',
                                            fontSize: '0.825rem'
                                        }}>{item.usage}</td>
                                    </tr>
                                ))}
                            </tbody>
                    </table>
                </div>
                

                <div className="palette-description" style={{marginTop: '1.5rem'}}>
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
                    <p>
                        All colour pairs adhere strictly to <strong>WCAG 2.2 Level AA guidelines</strong> (minimum 4.5:1 for normal text, 3:1 for large text/UI elements), with primary body text achieving <strong>AAA compliance</strong> (exceeding 11:1 contrast).
                    </p>
                </div>
            </div>


        </div>
    );
}
function TypographySection(){
    //added missing details eg exact scale values and font sources + licensing
    return (
        <div className="section-row">
            <div>
                <h2 className="section-heading">Typography</h2>
                <div className="typography-section">
                    <p className="type-body">OptiLifts uses a two-font system: a display font for headings and a high-legibility UI font for body and interface text.</p>

                    <div className="typography-grid">
                        <Card className="typography-item">
                            <CardContent>
                                <div className="type-page-title">Page Title - OPTILIFTS</div>
                                <div className="type-meta">Bebas Neue · 42px (2.625rem) · 400 · +2px · Page titles, wordmarks</div>
                            </CardContent>
                        </Card>

                        <Card className="typography-item">
                            <CardContent>
                                <div className="type-section-title">Section Title</div>
                                <div className="type-meta">Bebas Neue · 18px (1.125rem) · 400 · +1.5px</div>
                            </CardContent>
                        </Card>

                        <Card className="typography-item">
                            <CardContent>
                                <div className="text-base font-semibold text-foreground">Card Header (H3)</div>
                                <div className="type-meta">Barlow · 16px (1.0rem) · 600 · Exercise titles, card headers</div>
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
                                <div className="type-meta">Barlow · 11-12px (0.6875rem) · 600–700 · Uppercase · Form labels, tags</div>
                            </CardContent>
                        </Card>
                    </div>

                    <h3 className="type-section-title" style={{ marginTop: '2rem' }}>Font Licensing</h3>
                    <div className="goals-grid" style={{
                        marginTop: '0.75rem'
                    }}>
                        <Card className="goal-card">
                            <CardContent>
                                <div className="goal-title">Display Font: Bebas Neue</div>
                                <p className="goal-copy">
                                    <strong>Source: </strong>Google Fonts<br/>
                                    <strong>License: </strong>SIL Open Font License v1.1 (Free for commercial & personal use)<br/>
                                    <strong>Fallback Stack: </strong><code>sans-serif</code>
                                </p>
                            </CardContent>
                        </Card>
                        <Card className="goal-card">
                            <CardContent>
                                <div className="goal-title">Body & UI Font: Barlow</div>
                                <p className="goal-copy">
                                    <strong>Source: </strong>Google Fonts<br/>
                                    <strong>License: </strong>SIL Open Font License v1.1 (Free for commercial & personal use)<br/>
                                    <strong>Fallback Stack: </strong><code>system-ui, -apple-system, sans-serif</code>
                                </p>
                            </CardContent>
                        </Card>
                    </div>

                    <div style={{ marginTop: '1rem' }}>
                        <h3 className="type-section-title">Typeface connotations</h3>
                        <p className="type-body">The 'Bebas Neue' font is condensed and bold, conveying athleticism and impact. An excellent font for headlines, it portrays our brand's character. 'Barlow' is a highly legible font, chosen for its contribution to a clear UI hierarchy, and comfortable reading. Together, these two fonts create a balance of boldness and usability.
                        </p>
                    </div>
                </div>
            </div>


        </div>
    );
}

function LogoIconographySection(){
    return (
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
                     {/*canonical logo formats */}
                    <h3 className ="type-section-title">Canonical Logo Formats</h3>
                    <ul>
                        <li><strong>Full Logo: </strong>Icon mark & wordmark for primary header and branding contexts</li>
                        <li><strong>Monogram/Icon Mark: </strong>Dumbbell & Hexagon container for favicons, and compact UI avatar slots</li>
                        <li><strong>Monochrome: </strong>Single-tone fill for solid high-contrast backgrounds</li>
                        <li><strong>Inverse: </strong>High-contrast light-on-dark/dark-on-light colour mapping using <code>--foreground</code> and <code>--background</code></li>
                    </ul>
                     {/* forbidden treatments */}
                    <h3 className="type-section-title">Forbidden Treatments (Do-Nots)</h3>
                    <ul>
                        <li><strong>NO Stretching: </strong>Do not distort aspect ratios or stretch the icon mark</li>
                        <li><strong>NO Recolouring: </strong>Do not apply custom gradients, fills or non-brand colours</li>
                        <li><strong>NO Drop shadows: </strong>Do not use heavy drop shadows, glows or other unapproved affects</li>
                        <li><strong>NO Low contrast: </strong>Do not place the dark logo on dark surfaces or the light variant on light surfaces</li>
                    </ul>

                    {/* update these rules */}
                    <h3 className="type-section-title">Iconography rules</h3>
                    <ul>
                        <li>Source icons from <strong>Lucide Icons</strong> for consistency (the repo uses <code>lucide-react</code>).</li>

                        <li>Render icons at <strong>18×18px</strong> in sidebars/navigation; buttons may use 20px for balance.</li>
                        <li>Use a consistent sizing scale; <code>16px</code> (tables/badges), <code>18px</code> (sidebars/nav), <code>20px</code> (button actions), <code>24px</code> (empty states)</li>
                        <li>Stroke weight: standard <code>2px</code> stroke width (<code>strokeWidth={2}</code>)</li>
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
    );
}
function DesignPrincipleSection(){
    return (
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
    );
}
function ComponentLibrarySection(){
    return (
        <section className="ui-components">
            <h2 className="section-heading">UI Component Styling</h2>
            <p className="type-body">
                A cohesive library of reusable components that establish consistency across OptiLifts. Each component follows the brand's visual language and accessibility standards.
            </p>

            <div className="component-section">
                <div className="component-visual">
                    <div className="component-showcase"
                    style={{
                        flexDirection: 'column',
                        gap: '1rem'
                    }}>
                        {/* make these actually interactive */}
                            <div style={{ display: 'flex', gap: '0.5rem',
                                flexWrap: 'wrap', alignItems: 'center', justifyContent: 'center' }}>
                                <Button>Default</Button>
                                <Button variant="secondary">Secondary</Button>
                                <Button variant="outline">+ Add Set</Button>
                                <Button variant="ghost">Ghost</Button>
                            </div>
                            <div style={{
                                display: 'flex',
                                gap: '0.5rem',
                                flexWrap: 'wrap',
                                alignItems: 'center',
                                justifyContent: 'center'
                            }}>
                                <Button disabled>Disabled</Button>
                                <Button className="opacity-80 cursor-wait">Loading...</Button>
                            </div>
                            <div className="component-label">Button variants & interactive states</div>



                            {/* <div className="component-label">Primary Button</div> */}

                        {/* <div className="component-showcase__group">
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
                        </div> */}
                    </div>
                </div>
                <div className="component-description">
                    <h3 className="component-title">Button</h3>
                    <p className="component-copy"><strong>Variants: </strong>Primary (brand call-to-action), Secondary (surface fill), Outline (dashed/bordered actions) and Ghost</p>
                    <p className="component-copy"><strong>States: </strong>Default, Hover, Focus-visible, Active/Pressed, Disabled, and Loading (spinner.wait cursor)</p>
                    {/* <p className="component-copy"><strong>Primary:</strong> Call-to-action buttons for main workflows (Start Session, Save Workout). Uses brand colour with hover and focus states.</p>
                    <p className="component-copy"><strong>Secondary:</strong> Alternative actions with lower visual weight. Uses surface colour.</p>
                    <p className="component-copy"><strong>Outline:</strong> Tertiary actions like "Add Set" or "Create Exercise". Dashed border for visual distinction.</p>
                    <p className="component-copy"><strong>Icon, Ghost, Text:</strong> Additional variants for specific contexts (icon-only controls, text links).</p> */}
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
                    <p className="component-copy">Non-blocking feedback messages for system events. Appears at the top right and auto-dismisses after 5 seconds.</p>
                    <p className="component-copy"><strong>Variants:</strong> Info (brand), Success (green), Warning (amber), Error (red). Each variant has an icon and left border accent.</p>
                    <p className="component-copy"><strong>Usage:</strong> Confirm saves, warn about high fatigue, notify PR achievements, or provide helpful tips.</p>
                    <p className="component-copy">Triggered via the <code>toast</code> API with optional title and message.</p>
                </div>
            </div>

            {/* badges and tags? */}
            <div className="component-section">
                <div className="component-visual">
                    <div className="component-showcase"
                    style={{
                        flexDirection: 'column',
                        gap: '1rem',
                        width: '100%',
                        alignItems: 'center'
                    }}>
                        <div style={{
                            display: 'flex',
                            gap: '0.5rem',
                            flexWrap: 'wrap',
                            justifyContent: 'center',
                            alignItems: 'center'
                        }}>
                            <span style={{
                                background: 'var(--brand)',
                                color: '#FFFFFF',
                                padding: '0.25rem 0.5rem',
                                borderRadius: '0.375rem',
                                fontSize: '0.75rem',
                                fontWeight: 700,
                                textTransform: 'uppercase',
                                letterSpacing: '0.05em'
                            }}>NEW PR</span>
                            <span style={{
                                background: 'var(--surface-2)',
                                border: '1px solid var(--border)',
                                color: 'var(--foreground)',
                                padding: '0.2rem 0.5rem',
                                borderRadius: '0.375rem',
                                fontSize: '0.75rem',
                                fontWeight: 600
                            }}>Quadriceps</span>
                            <span style={{
                                background: 'rgba(255, 152, 0, 0.15)',
                                border: '1px solid var(--warning)',
                                color: 'var(--warning)',
                                padding: '0.25rem 0.6rem',
                                borderRadius: '0.375rem',
                                fontSize: '0.75rem',
                                fontWeight: 600
                            }}>High Fatigue</span>
                            <span style={{
                                background: 'rgba(27, 110, 31, 0.15)',
                                border: '1px solid var(--success)',
                                color: 'var(--success)',
                                padding: '0.2rem 0.5rem',
                                borderRadius: '0.375rem',
                                fontSize: '0.75rem',
                                fontWeight: 600
                            }}>Completed</span>
                        </div>
                        <div style={{
                            width: '100%',
                            maxWidth: '280px'
                        }}><Badge name="Consistent Lifter" description="Completed 5 workouts in a week" category="Milestone" earnedAt="2026-07-20"/></div>

                    </div>
                </div>
                <div className="component-description">
                    <h3 className="component-title">Badges & Status Tags</h3>
                    <p className="component-copy"><strong>Achievement Badges: </strong>Card component used in Profile page to display milestones and streaks</p>
                    <p className="component-copy"><strong>PR Badge: </strong>Highlight personal records on past workouts and active session logs</p>
                    <p className="component-copy"><strong>Muscle Group Tags: </strong>Tags indicating targeted muscle groups</p>
                    <p className="component-copy"><strong>Status Indicators: </strong>Success green for completed sets and Warning amber for recover/fatigue alerts</p>
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
    );
}
function GraphSection(){
    const barChartData=[
        {
            label: "Mon",
            value: 1.2
        },
        {
            label: "Tue",
            value: 0.8
        },
        {
            label: "Wed",
            value: 1.5
        },
        {
            label: "Thu",
            value: 0.0
        },
        {
            label: "Fri",
            value: 1.1
        },
        {
            label: "Sat",
            value: 2.0
        },
        {
            label: "Sun",
            value: 0.5
        }
    ];
    const spiderGraphData ={
        Chest: 12, Core: 8, Shoulders: 14, Arms: 10, Legs: 18, Back: 15
    }
    const spiderGraphSecondaryData ={
        Chest: 4, Core: 3, Shoulders: 5, Arms: 4, Legs: 6, Back: 4
    }
    const volumeChartData=[
        {
            label: "Mon",
            value: 2400
        },
        {
            label: "Tue",
            value: 1800
        },
        {
            label: "Wed",
            value: 3100
        },
        {
            label: "Thu",
            value: 0
        },
        {
            label: "Fri",
            value: 2800
        },
        {
            label: "Sat",
            value: 4200
        },
        {
            label: "Sun",
            value: 1200
        }
    ];
    
    return (
        <div className="section-row">
            <div>
                <h2 className="section-heading">Graph Visualisations</h2>
                <p className="type-body" style={{
                    marginBottom: '1.5rem'
                }}>
                    Interactive charts and diagrams used throughout OptiLifts to visualise workout volume, muscle distributions and training quantities.
                </p>
                <div className="goals-grid" style={{
                    gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
                    gap: '1.5rem'
                }}>
                    <Card className="goal-card">
                        <CardContent className="p-4">
                            <h3 className="goal-title mb-2">Muscle Diagram</h3>
                            <p className="goal-copy mb-2">Highlights primary muscles strongly and secondary muscles with a lighter emphasis</p>
                            <MuscleDiagram highlightedMuscles={["Chest","Quadriceps","Lats"]} secondaryMuscles={["Triceps","Hamstrings","Middle Back"]} variant="both"/>
                        </CardContent>
                    </Card>
                    <Card className="goal-card">
                        <CardContent className="p-4">
                            <h3 className="goal-title mb-2">Bar Chart</h3>
                            <p className="goal-copy mb-2">Weekly training duration bar chart</p>
                            <BarChart title="" data={barChartData}/>
                        </CardContent>
                    </Card>
                    <Card className="goal-card">
                        <CardContent className="p-4">
                            <h3 className="goal-title mb-2">Spider Graph</h3>
                            <p className="goal-copy" style={{marginBottom: '40px'}}>Muscle group set distribution radar graph</p>
                            <div style={{
                                height: '220px',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center',
                                marginBottom: '20px'
                            }}>
                                <SpiderGraph data={spiderGraphData} secondaryData={spiderGraphSecondaryData} secondaryMultiplier={0.4}/>
                            </div>
                        </CardContent>
                    </Card>                    

                    <Card className="goal-card">
                        <CardContent className="p-4">
                            <h3 className="goal-title mb-2">Calendar</h3>
                            <p className="goal-copy" style={{marginBottom: '10px'}}>Monthly activity calendar highlighting workout days</p>
                            <Calendar highlightedDates={["2026-07-02", "2026-07-05", "2026-07-10", "2026-07-15","2026-07-20", "2026-07-25"]}/>
                        </CardContent>
                    </Card>
                    <Card className="goal-card" style={{gridColumn: '1/-1'}}>
                        <CardContent className="p-4">
                            <h3 className="goal-title mb-2">Volume Chart</h3>
                            <p className="goal-copy mb-2">Total weight volume line chart over time</p>
                            <VolumeChart title="Total Volume" data={volumeChartData} showFilters={false} />
                        </CardContent>
                    </Card>
                </div>
                
            </div>
        </div>
    )
}
function AccessibilitySection(){
    const auditScores=[
        {
            page: "Brand Style Guide",
            theme: "Light",
            score: "93",
            imgSrc: "/brandstyle-lighthouse-light.png",
            alt: "Lighthouse accessibility audit score for Brand Style page"
        },
        {
            page: "Workouts",
            theme: "Light",
            score: "100",
            imgSrc: "/workouts-lighthouse-light.png",
            alt: "Lighthouse accessibility audit score for Workouts page"
        },
        {
            page: "Brand Style Guide",
            theme: "Dark",
            score: "93",
            imgSrc: "/brandstyle-lighthouse-dark.png",
            alt: "Lighthouse accessibility audit score for Brand Style page"
        },
        {
            page: "Workouts",
            theme: "Dark",
            score: "96",
            imgSrc: "/workouts-lighthouse-dark.png",
            alt: "Lighthouse accessibility audit score for Workouts page"
        }
    ];
    return (
        <section className="accessibility-section">
            <h2 className="section-heading">Accessibility</h2>

            <p className="type-body" style={{ maxWidth: 920, margin: '0.25rem 0 1rem' }}>
                OptiLifts targets WCAG 2.2 AA as the minimum standard across all screens and both colour themes. Our implementation includes strict colour contrast guarantees, visible focus indicators, screen reader ARIA mappings and minimal motion.
            </p>
            {/* i do want to try make this section look nicer */}
            <div className="accessibility-grid">
                <div className="accessibility-left">
                    <h3 className="type-section-title">Colour Contrast & Focus Indicators</h3>
                    <ul className="access-list">
                        <li><strong>WCAG 2.2 AA Baseline:</strong> All primary text/background combinations meet WCAG 2.2 AA (4.5:1 minimum for normal text, 3:1 for large text and UI components)</li>
                        <li><strong>Focus Indicator Style:</strong> Interactive elements show a high-visibility focus ring using <code>outline: 2px solid var(--brand)</code> or Tailwind <code>ring-2 ring-brand/50 ring-offset-2</code> to guarantee a 3:1 contrast ratio against all backgrounds.</li>
                        <li><strong>Color Independence:</strong> Information, status alerts, and chart data never rely solely on color; icons, text labels, and patterns are always provided alongside hues.</li>
                    </ul>

                    {/*i have moved specifically colour accessibility into the table in the colour section */}
                    <h3 className="type-section-title">Keyboard Navigability</h3>
                    <ul className="access-list">
                        <li>Tab order follows logical top-to-bottom, left-to-right reading flow on every screen.</li>
                        <li>Radix/shadcn modals and sheets trap focus and return it on close.</li>
                        <li>Roving tabindex on ToggleGroups; arrow keys move, Enter/Space selects.</li>
                        <li>Custom components implement <code>role</code>, <code>tabIndex</code>=0 and key handlers for Enter/Space.</li>
                    </ul>
                </div>

                <div className="accessibility-right">
                    <h3 className="type-section-title">Motion Support</h3>
                    <ul className="access-list">
                        <li><strong>No Flashing Content:</strong> No UI elements flash or pulse more than 3 times per second, preventing seizure triggers.</li>
                    </ul>

                    <h3 className="type-section-title">Screen Reader Compatibility</h3>
                    <ul className="access-list">
                        <li>All inputs use explicit labels; placeholders are never the sole label.</li>
                        <li>Validation errors link to inputs via <code>aria-describedby</code> and use <code>role="alert"</code>.</li>
                        <li>Dynamic updates use <code>aria-live="polite"</code> (or <code>assertive</code> for critical alerts).</li>
                        <li>Progress indicators use <code>role="progressbar"</code> with proper ARIA values.</li>
                        <li>Non-standard interactive controls expose correct roles and state via ARIA attributes.</li>
                    </ul>

                </div>
            </div>
            <h3 className="type-section-title" style={{ marginTop: '2rem', marginBottom: '0.5rem'}}>Automated Audit Scores (Lighthouse)</h3>
                <p className="type-body" style={{
                    fontSize: '0.875rem',
                    marginBottom: '1.25rem'
                }}>Automated accessibility audits are performed continuously using Google Lighthouse across primary pages in both Light and Dark themes.</p>
                <div className="goals-grid" style={{
                    gridTemplateColumns: 'repeat(auto-fit, minmax(240px, 1fr))',
                    gap: '1.5rem'
                }}>
                    {auditScores.map((item)=>(
                        <Card key={item.page} className="goal-card flex flex-col justify-between">
                        <CardContent className="p-4 flex flex-col h-full justify-between gap-3">
                            <div className="flex items-center justify-between">
                                <div>
                                    <h4 className="font-bold text-sm text-foreground">{item.page}</h4>
                                    <span className="text-[11px] text-muted-foreground">{item.theme}</span>
                                </div>
                                <div className="flex items-center gap-1.5 px-2.5 py-1 rounded-full bg-success/15 border border-success/30 text-success font-extrabold text-xs">
                                    <span className="w-2 h-2 rounded-full bg-success animate-pulse"/>
                                    <span>{item.score}</span>
                                </div>
                            </div>

                            <div className="border border-border rounded-lg overflow-hidden bg-surface-2 mt-1">
                                <img src={item.imgSrc} alt={item.alt} className="w-full h-40 object-cover object-top rounded-md transition-transform duration-200 hover:scale-105"
                                onError={(e) => {
                                    const target = e.target as HTMLElement;
                                    target.style.display = 'none';
                                    if (target.parentElement){
                                        target.parentElement.innerHTML = `<div class="p-6 text-center text-xs text-muted-foreground font-mono">Screenshot: ${item.page} ${item.score}/100</div>`;
                                    }
                                }}/>
                            </div>
                        </CardContent>
                    </Card>
                    ))}
                    
                </div>
        </section>
    );
}
function DesignTokenSection(){
    return (
        <div className="section-row">
            <div>
                <h2 className="section-heading">Design Tokens</h2>
                <p className="type-body" style={{ marginBottom: '1.5rem' }}>
                    OptiLifts design tokens represent the source of truth for all visual values on the platform. These are defined in <code>src/index.css</code> and Tailwind <code>@theme</code> inline definitions.
                </p>
                <h3 className="type-section-title">1. Color Tokens</h3>
                <div style={{
                    overflowX: 'auto',
                    marginBottom: '1.5rem',
                    marginTop: '0.5rem'
                }}>
                    <table style={{
                        width: '100%',
                        borderCollapse: 'collapse',
                        textAlign: 'left',
                        fontSize: '0.85rem'
                    }}>
                        <thead>
                            <tr style={{
                                borderBottom: '2px solid var(--border)',
                                background: 'var(--surface-2)'
                            }}>
                                <th style={{ padding: '0.6rem 0.75rem' }}>CSS Variable</th>
                                <th style={{ padding: '0.6rem 0.75rem' }}>Light Value</th>
                                <th style={{ padding: '0.6rem 0.75rem' }}>Dark Value</th>
                                <th style={{ padding: '0.6rem 0.75rem' }}>Tailwind Mapping</th>
                            </tr>
                        </thead>
                        <tbody style={{ borderBottom: '1px solid var(--border)'}}>
                                {paletteData.map((item)=>(
                                    <tr key={item.variable} style={{borderBottom: '1px solid var(--border)'}}>
                                        <td style={{ padding: '0.5rem 0.75rem' }}><code>{item.variable.split('/')[0]}</code></td>
                                        <td style={{ padding: '0.5rem 0.75rem', fontFamily: 'monospace' }}>{item.lightHex}</td>
                                        <td style={{ padding: '0.5rem 0.75rem', fontFamily: 'monospace' }}>{item.darkHex}</td>
                                        <td style={{ padding: '0.5rem 0.75rem' }}>{item.tailwindClass}</td>
                                    </tr>
                                ))}                                
                        </tbody>
                    </table>
                </div>

                <div className="goals-grid" style={{
                    gridTemplateColumns: 'repeat(auto-fit, minmax(320px, 1fr))',
                    gap: '1.5rem'
                }}>
                    <Card className="goal-card">
                        <CardContent>
                            <div className="goal-title">Spacing Scale</div>
                            <p className="goal-copy">
                                <strong>4px</strong> (0.25rem) | <strong>8px</strong> (0.5rem)<br/>
                                <strong>12px</strong> (0.75rem) | <strong>16px</strong> (1.0rem)<br/>
                                <strong>24px</strong> (1.5rem) | <strong>32px</strong> (2.0rem)<br/>
                                <strong>48px</strong> (3.0rem)
                            </p>
                        </CardContent>
                    </Card>
                    <Card className="goal-card">
                        <CardContent>
                            <div className="goal-title">Border Radius Tokens</div>
                            <p className="goal-copy">
                                {/* is there a nice way to show this visually? */}
                                <code>--radius-sm</code>: 6px (calc(var(--radius)*0.6))<br/>
                                <code>--radius-md</code>: 8px (calc(var(--radius)*0.8))<br/>
                                <code>--radius-lg/--radius</code>: 10px (0.625rem)<br/>
                                <code>--radius-xl</code>: 14px<br/>
                                <code>--radius-2xl</code>: 18px<br/>
                                <code>Pill/Full</code>: 9999px
                            </p>
                        </CardContent>
                    </Card>

                    <Card className="goal-card">
                        <CardContent>
                            <div className="goal-title">Shadows and Focus Rings</div>
                            <p className="goal-copy">
                                <strong>Card Shadow:</strong> <code>shadow-sm/shadow-md</code><br/>
                                <strong>Model Elevation:</strong> <code>shadow-2xl</code><br/>
                                <strong>Focus Ring:</strong> <code>outline-ring/50</code> (uses <code>--brand</code> crimson ring wih 50% opacity)
                            </p>
                        </CardContent>
                    </Card>
                    <Card className="goal-card">
                        <CardContent>
                            <div className="goal-title">Breakpoints</div>
                            <p className="goal-copy">
                                <strong>sm:</strong> 640px (Mobile /small tablet)<br/>
                                <strong>md:</strong> 768px (Tablet/Desktop sidebar trigger)<br/>
                                <strong>lg:</strong> 1024px (Standard desktop)<br/>
                                <strong>xl:</strong> 1280px (Large desktop)
                            </p>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}
function LayoutSection(){
    const spacingScale=[
        {
            token: "0.25rem",
            px: "4px",
            usage: "Tight gaps, badge padding, micro element offsets"
        },
        {
            token: "0.5rem",
            px: "8px",
            usage: "Button icon gaps, small card padding"
        },
        {
            token: "0.75rem",
            px: "12px",
            usage: "List item gaps, input padding"
        },
        {
            token: "1.0rem",
            px: "16px",
            usage: "Standard card padding, default form field spacing"
        },
        {
            token: "1.5rem",
            px: "24px",
            usage: "Card section gaps, modal container padding"
        },
        {
            token: "2.0rem",
            px: "32px",
            usage: "Page section gaps, header outer margins"
        },
        {
            token: "3.0rem",
            px: "48px",
            usage: "major view container dividers"
        }
    ];
    const breakpoints =[
        {
            name: "sm",
            px: "640px",
            behaviour: "Mobile; stacks single column cards into 2-column grids"
        },
        {
            name: "md",
            px: "768px",
            behaviour: "Tablet; adjusts dashboard summaries and nav layout"
        },{
            name: "lg",
            px: "1024px",
            behaviour: "Desktop; full multi-column grid, sidebar and chart views"
        },{
            name: "xl",
            px: "1280px",
            behaviour: "Large desktop; max-width containers"
        }
    ];
    return (
        <div className="section-row">
            <div>
                <h2 className="section-heading">Layout & Spacing</h2>
                <p className="type-body" style={{marginBottom: '1.5rem'}}>
                    Optilifts uses a responsive 12 column grid and a 4px (0.25rem) base spacing scale. Layouts adapt across mobile, table and desktop views.
                </p>

                <h3 className="type-section-title">Spacing Scale</h3>
                <div className="goals-grid" style={{
                    marginTop: '0.75rem',
                    marginBottom: '2rem'
                }}>
                    {spacingScale.map((space) => (
                        <Card key={space.token} className="goal-card">
                            <CardContent className="p-4">
                                <div className="flex items-center justify-between mb-2">
                                    <span className="font-mono text-sm font-bold text-brand">{space.token}</span>
                                    <span className="font-mono text-xs text-muted-foreground">{space.px}</span>
                                </div>
                                <div style={{
                                    height: '12px',
                                    width: space.px === '48px'?'100%': `${Number.parseInt(space.px) * 2.5}px`,
                                    maxWidth: '100%',
                                    backgroundColor: 'var(--brand)',
                                    borderRadius: '2px',
                                    marginBottom: '0.5rem',
                                    opacity: 0.85
                                }}/>
                                <p className="goal-copy text-xs">{space.usage}</p>
                            </CardContent>
                        </Card>
                    ))}
                </div>

                <h3 className="type-section-title">Responsive Breakpoints</h3>
                <div className="goals-grid" style={{
                    marginTop: '0.75rem',
                    marginBottom: '2rem'
                }}>
                    {breakpoints.map((bp)=>(
                        <Card key={bp.name} className="goal-card">
                            <CardContent className="p-4">
                                <div className="flex items-center justify-between mb-1">
                                    <span className="font-display text-xl text-foreground">{bp.name}</span>
                                    <span className="font-mono text-xs font-bold text-brand">{bp.px}</span>
                                </div>
                                <p className="goal-copy text-xs">{bp.behaviour}</p>
                            </CardContent>
                        </Card>
                    ))}
                </div>


                <h3 className="type-section-title">Layout Adaptation (Mobile vs. Desktop)</h3>
                <div className="goals-grid" style={{
                    marginTop: '0.75rem',
                    gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))'
                }}>
                    <Card className="goal-card">
                        <CardContent className="p-4">
                            <div className="goal-title text-foreground">Mobile Adaptation (&lt; 640px)</div>
                            <ul className="type-body text-xs space-y-2 mt-2" style={{
                                paddingLeft: '1rem',
                                listStyleType: 'disc'
                            }}>
                                <li>Single column linear stacked layout</li>
                                <li>Touch-friendly controls with minimum targets</li>
                                <li>Data grids collapse into scrollable swipe cards</li>
                            </ul>
                        </CardContent>
                    </Card>

                    <Card className="goal-card">
                        <CardContent className="p-4">
                            <div className="goal-title text-foreground">Desktop Adaptation (&ge; 1024px)</div>
                            <ul className="type-body text-xs space-y-2 mt-2" style={{
                                paddingLeft: '1rem',
                                listStyleType: 'disc'
                            }}>
                                <li>Multi-column grid views</li>
                                <li>Sticky header with link navigation</li>
                                <li>Analytics are side by side (spider graph, volume charts, etc.)</li>
                            </ul>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}
function ChangelogSection(){
    const changelog =[
        {
            title: "Refined palette & WCAG 2.2 Compliance",
            description: "The light and dark mode tokens were finalised with their HEX, RGB, and HSL values and contrast ratios",
            rationale: "To ensure the application is comfortable to read in any environment, including bright outdoor workouts to dim gym floors, while still keeping the UI accessible"
        },
        {
            title: "Formalised Typographic Scale & Licensing",
            description: "Documented modular scale sizes (px, rem, weights, tracking) and SIL font licenses",
            rationale: "Gives our team a clear hierarchy to follow so that the text stays consistent across screens"
        },
        {
            title: "Design Tokens",
            description: "Mapped design tokens directly to actual CSS custom properties in index.css",
            rationale: "Creates a single source of truth for all styling, preventing hardcoded colours from cluttering the component files"
        },
        {
            title: "Graph Visualisations and Components",
            description: "Added interactive showcases for muscle diagrams, volume charts, spider graphs, and UI controls",
            rationale: "Interactive charts break down complex workout data into easy-to-digest summaries, rather than overwhelming fitness stats"
        }
    ];
    return (
        <div className="section-row">
            <div>
                <h2 className="section-heading">Demo 1 to Demo 2 Changelog</h2>
                <p className="type-body" style={{marginBottom: '1.5rem'}}>
                    As OptiLifts progressed from our initial 5 use cases to the current production implementation, the brand style guide has evolved to reflect our updated design decisions.
                </p>
                <div className="goals-grid" style={{marginTop: '0.75rem' }}>
                    {changelog.map((item, index) => (
                        <Card key={item.title} className="goal-card">
                            <div className="flex items-center gap-2 mb-2">
                                <span className="flex items-center justify-center w-5 h-5 rounded-full bg-brand/10 text-brand text-xs font-bold">
                                {index+1}</span>
                                <span className="font-sans text-sm font-bold text-foreground">{item.title}</span>
                            </div>
                            <p className="type-body text-xs text-foreground/90 mb-2">{item.description}</p>
                            <p className="goal-copy text-xs italic text-muted-foreground"><strong>Rationale: </strong>{item.rationale}</p>
                        </Card>
                    ))}
                </div>
            </div>
        </div>
    );
}


export default function BrandStylePage() {
    const navigate = useNavigate()
    return (
        <section className="brand-style-page">
            <div className="mb-4">
                <Button 
                variant="text"
                size="sm"
                onClick={() => navigate('/help')}
                className="inline-flex items-center gap-2 py-2 text-muted-foreground hover:text-foreground"
              >
                <ArrowLeft className="h-4 w-4" />
                <span>Back to Help</span>
              </Button>
              </div>
            <BrandHeader/>
            <h1 className="section-heading">Brand Style</h1>
            {/* tone is in the intro - take it out? */}
            <BrandIntroSection/>
            <ColourPaletteSection/>
            <TypographySection/>
            <LogoIconographySection/>
            <DesignTokenSection/>
            <DesignPrincipleSection/>
            <ComponentLibrarySection/>
            <GraphSection/>
            <LayoutSection/>
            <AccessibilitySection/>
            <ChangelogSection/>
        </section>
    )
}