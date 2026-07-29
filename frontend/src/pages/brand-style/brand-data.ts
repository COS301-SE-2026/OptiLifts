export interface PaletteItem {
    name: string;
    variable: string;
    usage: string;
    lightHex: string;
    lightRgb: string;
    lightHsl: string;
    darkHex: string;
    darkRgb: string;
    darkHsl: string;
    contrastLight: string;
    contrastDark: string;
    swatchClass: string;
    tailwindClass: string;
}

// attempt to solve the duplic
//attempt 2
type ColourTuple = [
    string, string, string, string, string, string, string, string, string, string, string, string, string
];

const rawPalette: ColourTuple[] = [
    ["Primary Brand/Action", "--brand/--primary", "Primary CTA buttons, active state indicators, key brand accents, and focus rings.", "#CC0022", "204, 0, 34", "350, 100%, 40%", "#CC0022", "204, 0, 34", "350, 100%, 40%", "5.85:1 (Passes AA)", "2.91:1 (Safe for UI/large text)", "swatch--brand", "bg-brand/text-brand/bg-primary"],
    ["Brand Secondary/Hover Accent", "--brand-2", "Hover state for primary actions (Light)/High visibility secondary brand accent (Dark)", "#AA0018", "170,0,24", "351, 100%, 33%", "#D94060", "217, 64, 96", "347, 67%, 55%", "8.71:1 (Passes AAA)", "3.93:1 (Passes for large text)", "swatch--brand-2", "bg-brand-2/text-brand-2"],
    ["Background", "--background", "Main page view container background.", "#FAF8F8", "250, 248, 248", "0, 20%, 98%", "#1C1C1F", "28, 28, 31", "240, 5%, 11%", "15.8:1 (Passes AAA)", "13.2:1 (Passes AAA)", "swatch--background", "bg-background"],
    ["Surface/Card", "--surface/--card", "Card components, modals, popovers, and elevated containers.", "#FFFFFF", "255, 255, 255", "0, 0%, 100%", "#26262B", "38, 38, 43", "240, 6%, 16%", "16.1:1 (Passes AAA)", "11.4:1 (Passes AAA)", "swatch--background", "bg-surface/bg-card"],
    ["Secondary surface/ Container", "--surface-2 /--secondary", "Table headers, secondary button fills, input background accents", "#F5F0F0", "245, 240, 240", "0, 20%, 95%", "#2E2E34", "46, 46, 52", "240, 6%, 19%", "14.9:1 (Passes AAA)", "9.8:1 (Passes AAA)", "swatch--border", "bg-surface-2/bg-secondary"],
    ["Foreground/Primary Text", "--foreground", "Primary body text, card titles, section headings, and main UI labels", "#1A1A1A", "26, 26, 26", "0, 0%, 10%", "#E8E8EC", "232, 232, 236", "240, 11%, 92%", "15.8:1 (Passes AAA)", "11.4:1 (Passes AAA)", "swatch--foreground", "text-foreground"],
    ["Muted Text", "--muted-text/--muted-foreground", "Secondary labels, captions, metadata, timestamps and disabled text placeholders", "#666666", "102, 102, 102", "0, 0%, 40%", "#9A9AA8", "154, 154, 168", "240, 8%, 63%", "5.3:1 (Passes AA)", "5.8:1 (Passes AA)", "swatch--muted-text", "text-muted-foreground"],
    ["Success status", "--success", "PR achievements, completed workout sets, positive progress indicators", "#1B6E1F", "27, 110, 31", "123, 61%, 27%", "#4CAF50", "76, 175, 80", "122, 39%, 49%", "5.38:1 (Passes AA)", "6.76:1 (Passes AA)", "swatch--success", "bg-success/text-success"],
    ["Warning Status", "--warning", "Fatigue warnings, unsaved session alerts, cautions", "#B35C00", "179, 92, 0", "31, 100%, 35%", "#FF9800", "255, 152, 0", "36, 100%, 50%", "4.46:1 (Passes AA)", "8.5:1 (Passes AAA)", "swatch--warning", "text-warning/bg-warning"]
];

export const paletteData: PaletteItem[] = rawPalette.map(
    ([name,variable,usage,lightHex,lightRgb,lightHsl,darkHex,darkRgb,darkHsl, contrastLight, contrastDark, swatchClass,tailwindClass])=> ({
        name, variable, usage, 
        lightHex, lightRgb, lightHsl, 
        darkHex,darkRgb,darkHsl,  
        contrastLight,  contrastDark, swatchClass,
        tailwindClass
    })
);