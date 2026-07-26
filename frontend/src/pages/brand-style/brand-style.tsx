import "./brand-style.css";
import { Plus, MoreHorizontal, X, Eye, LogOut, ChevronDown, User, Dumbbell, Info, Sun, CheckCircle2, AlertTriangle } from 'lucide-react'
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
//going to make a separate const for the details, as theres a lot of info
//for every colour that needs to be shown. tis easier

function ColourPaletteSection(){
    const paletteData=[
        {
            name: "Primary Brand/Action",
            variable: "--brand/--primary",
            usage:"Primary CTA buttons, active state indicators, key brand accents, and focus rings.",
            lightHex: "#CC0022",
            lightRgb: "204, 0, 34",
            lightHsl:"350, 100%, 40%",
            darkHex: "#CC0022",
            darkRgb: "204, 0, 34",
            darkHsl: "350, 100%, 40%",
            contrastLight: "5.85:1 (Passes AA)", //calculation
            contrastDark: "2.91:1 (Safe for UI/large text)",
            swatchClass: "swatch--brand"
        },
        {
            name: "Brand Secondary/Hover Accent",
            variable: "--brand-2",
            usage:"Hover state for primary actions (Light)/High visibility secondary brand accent (Dark)",
            lightHex: "#AA0018",
            lightRgb:"170,0,24",
            lightHsl: "351, 100%, 33%",
            darkHex:"#D94060",
            darkRgb: "217, 64, 96",
            darkHsl:"347, 67%, 55%",
            contrastLight:"8.71:1 (Passes AAA)",
            contrastDark:"3.93:1 (Passes for large text)",
            swatchClass:"swatch--brand-2"
        },
        {
            name:"Background",
            variable: "--background",
            usage:"Main page view container background.", 
            lightHex:"#FAF8F8",
            lightRgb:"250, 248, 248",
            lightHsl:"0, 20%, 98%",
            darkHex:"#1C1C1F",
            darkRgb:"28, 28, 31",
            darkHsl:"240, 5%, 11%",
            contrastLight: "15.8:1 (Passes AAA)",
            contrastDark: "13.2:1 (Passes AAA)",
            swatchClass: "swatch--background"
        },
        {
            name: "Surface/Card",
            variable:"--surface/--card",
            usage: "Card components, modals, popovers, and elevated containers.",
            lightHex: "#FFFFFF",
            lightRgb:"255, 255, 255",
            lightHsl: "0, 0%, 100%",
            darkHex: "#26262B",
            darkRgb: "38, 38, 43",
            darkHsl:"240, 6%, 16%",
            contrastLight: "16.1:1 (Passes AAA)",
            contrastDark: "11.4:1 (Passes AAA)",
            swatchClass: "swatch--background"
        },
        {
            name: "Secondary surface/ Container",
            variable: "--surface-2 /--secondary",
            usage: "Table headers, secondary button fills, input background accents",
            lightHex: "#F5F0F0",
            lightRgb:"245, 240, 240",
            lightHsl: "0, 20%, 95%",
            darkHex: "#2E2E34",
            darkRgb: "46, 46, 52",
            darkHsl:"240, 6%, 19%",
            contrastLight:"14.9:1 (Passes AAA)",
            contrastDark: "9.8:1 (Passes AAA)",
            swatchClass: "swatch--border"
        },
        {
            name:"Foreground/Primary Text",
            variable: "--foreground",
            usage:"Primary body text, card titles, section headings, and main UI labels",
            lightHex: "#1A1A1A",
            lightRgb: "26, 26, 26",
            lightHsl:"0, 0%, 10%",
            darkHex: "#E8E8EC",
            darkRgb: "232, 232, 236",
            darkHsl:"240, 11%, 92%",
            contrastLight:"15.8:1 (Passes AAA)",
            contrastDark: "11.4:1 (Passes AAA)",
            swatchClass: "swatch--foreground"
        },
        {
            name:"Muted Text",
            variable: "--muted-text/--muted-foreground",
            usage: "Secondary labels, captions, metadata, timestamps and disabled text placeholders",
            lightHex: "#666666",
            lightRgb:"102, 102, 102",
            lightHsl: "0, 0%, 40%",
            darkHex: "#9A9AA8",
            darkRgb: "154, 154, 168",
            darkHsl:"240, 8%, 63%",
            contrastLight:"5.3:1 (Passes AA)",
            contrastDark: "5.8:1 (Passes AA)",
            swatchClass: "swatch--muted-text"
        },
        {
            name:"Success status",
            variable: "--success",
            usage:"PR achievements, completed workout sets, positive progress indicators",
            lightHex: "#1B6E1F",
            lightRgb: "27, 110, 31",
            lightHsl: "123, 61%, 27%",
            darkHex: "#4CAF50",
            darkRgb:"76, 175, 80",
            darkHsl: "122, 39%, 49%",
            contrastLight: "5.38:1 (Passes AA)",
            contrastDark: "6.76:1 (Passes AA)",
            swatchClass: "swatch--success"
        },
        {
            name: "Warning Status",
            variable: "--warning",
            usage:"Fatigue warnings, unsaved session alerts, cautions",
            lightHex:"#B35C00",
            lightRgb: "179, 92, 0",
            lightHsl:"31, 100%, 35%",
            darkHex:"#FF9800",
            darkRgb: "255, 152, 0",
            darkHsl: "36, 100%, 50%",
            contrastLight: "4.46:1 (Passes AA)",
            contrastDark:"8.5:1 (Passes AAA)",
            swatchClass: "swatch--warning"
        }
    ];



    return (
        <div className="section-row">
            <div>
                <h2 className="section-heading">Colour Palette and WCAG contrast</h2>
                <h3 className="type-section-title">Light Theme</h3>
                <ul className="palette-grid" aria-label="Light Colour palette">
                    {paletteData.map((colour) => (
                        <li key={`light-${colour.name}`} className={`swatch ${colour.swatchClass}`}>
                        <div className="swatch__color" />
                        <div className="swatch__hex">{colour.lightHex}</div>
                        <div className="swatch__role">{colour.name.split('/')[0]}</div>
                    </li>
                    ))}
                </ul>
                    {/* problem: itds only showing the dark mode cus the setting is dark mode */}
                <h3 className="type-section-title">Dark Mode Palette</h3>
                <ul className="palette-grid palette-grid--dark" aria-label="Dark Colour palette">
                    {paletteData.map((colour) => (
                        <li key={`dark-${colour.name}`} className={`swatch ${colour.swatchClass}`}>
                        <div className="swatch__color" />
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
                        <p className="type-body">The 'Bebas Neue' font is condensed and bold, conveying athleticism and impact. An excellent font for headlines, it portrays our brands character. 'Barlow' is a highly legible font, chosen for its contribution to a clear UI hierarchy, and comfortable reading. Together, these two fonts create a balance of boldness and usability.
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
                        <li>Use a consistent sizing scale; <code>16px</code> (tables/badges), <code>18px</code> (sidebards/nav), <code>20px</code> (button actions), <code>24px</code> (empty states)</li>
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
                    <p className="component-copy"><strong>Variants: </strong>Primary (brand call-to-action), Secondary (surface fill), Outling (dashed/bordered actions) and Ghost</p>
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
                    <p className="component-copy"><strong>Variants:</strong> Info (red), Success (green), Warning (amber), Error (red). Each variant has an icon and left border accent.</p>
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
                            <p className="goal-copy mb-2">Highlights primary and secondary targeted muscles</p>
                            <MuscleDiagram highlightedMuscles={["Chest","Quadriceps","Lats"]} variant="both"/>
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
                                <SpiderGraph data={spiderGraphData}/>
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
                            <p className="goal-copy mb-2">Total weight colume line chart over time</p>
                            <VolumeChart title="Total Volume" data={volumeChartData} showFilters={false} />
                        </CardContent>
                    </Card>
                </div>
                
            </div>
        </div>
    )
}
function AccessibilitySection(){
    return (
        <section className="accessibility-section">
            <h2 className="section-heading">Accessibility</h2>

            <p className="type-body" style={{ maxWidth: 920, margin: '0.25rem 0 1rem' }}>
                OptiLifts targets WCAG 2.1 AA as the minimum standard across all screens and both themes. The implementation is split across Colour Contrast, Keyboard Navigability, Screen Reader Compatibility, and WCAG compliance mappings.
            </p>
            {/* i do want to try make this section look nicer */}
            <div className="accessibility-grid">
                <div className="accessibility-left">
                    <div className="type-section-title">Colour Contrast</div>
                    <p className="type-body" style={{ marginTop: '0.25rem' }}>All primary text/background combinations meet WCAG 2.1 AA (4.5:1 minimum for normal text, 3:1 for large text and UI components). Key verified pairs are shown below.</p>

                    {/*i have moved specifically colour accessibility into the table in the colour section */}
                    <h3 className="type-section-title">Keyboard Navigability</h3>
                    <ul className="access-list">
                        <li>Tab order follows logical top-to-bottom, left-to-right reading flow on every screen.</li>
                        <li>Radix/shadcn modals and sheets trap focus and return it on close.</li>
                        <li>Roving tabindex on ToggleGroups; arrow keys move, Enter/Space selects.</li>
                        <li>Custom components implement <code>role</code>, <code>tabIndex</code>=0 and key handlers for Enter/Space.</li>
                        <li>All interactive elements show visible focus with <code>focus-visible</code> ring styling.</li>
                    </ul>
                </div>

                <div className="accessibility-right">
                    

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
    );
}
// function DesignTokenSection(){
//     return (

//     );
// }
// function LayoutMotionSection(){
//     return (

//     );
// }
// function ChangelogSection(){
//     return (

//     );
// }


export default function BrandStylePage() {
    return (
        <section className="brand-style-page">
            <BrandHeader/>
            <h1 className="section-heading">Brand Style</h1>
            {/* tone is in the intro - take it out? */}
            <BrandIntroSection/>
            <ColourPaletteSection/>
            <TypographySection/>
            <LogoIconographySection/>
            {/* design tokens go here?*/}
            <DesignPrincipleSection/>
            <ComponentLibrarySection/>
            <GraphSection/>
            {/* layout and spacing */}
            <AccessibilitySection/>
            {/* changelog goes here */}
        </section>
    )
}