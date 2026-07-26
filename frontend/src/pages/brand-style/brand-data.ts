export interface PaletteItem{
    name: string;
    variable: string;
    usage: string;
    lightHex: string;
    lightRgb: string;
    lightHsl:string;
    darkHex: string;
    darkRgb: string;
    darkHsl: string;
    contrastLight: string;
    contrastDark: string;
    swatchClass: string;
    tailwindClass: string;
}

export const paletteData: PaletteItem[]=[
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
            swatchClass: "swatch--brand",
            tailwindClass: "bg-brand/text-brand/bg-primary"
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
            swatchClass:"swatch--brand-2",
            tailwindClass: "bg-brand-2/text-brand-2"
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
            swatchClass: "swatch--background",
            tailwindClass: "bg-background"
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
            swatchClass: "swatch--background",
            tailwindClass: "bg-surface/bg-card"
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
            swatchClass: "swatch--border",
            tailwindClass: "bg-surface-2/bg-secondary"
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
            swatchClass: "swatch--foreground",
            tailwindClass: "text-foreground"
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
            swatchClass: "swatch--muted-text",
            tailwindClass: "text-muted-foreground"
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
            swatchClass: "swatch--success",
            tailwindClass: "bg-success/text-success"
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
            swatchClass: "swatch--warning",
            tailwindClass: "text-warning/bg-warning"
        }
    ];