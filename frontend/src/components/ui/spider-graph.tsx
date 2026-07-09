import {
    Chart as ChartJS,
    RadialLinearScale,
    PointElement,
    LineElement,
    Filler,
    Tooltip,
    type ChartOptions, type ChartData,
} from 'chart.js'
import { Radar } from 'react-chartjs-2'
import { cn } from '@/lib/utils'

ChartJS.register(RadialLinearScale, PointElement, LineElement, Filler, Tooltip)

export const SPIDER_CATS = ['Chest', 'Core', 'Shoulders', 'Arms', 'Legs','Back'] as const
export interface SpiderGraphProps {
    readonly data: Record<string, number> | number[]
    readonly className?: string
}

//needed because of the way chart.js renders (creates html5 canvas tag)
function getcssVariables(name : string, fallback:string):string {
    if(typeof globalThis === 'undefined' || !('window' in globalThis)) return fallback
    const value = getComputedStyle(document.documentElement).getPropertyValue(name).trim()
    return value || fallback
}

export function SpiderGraph({data, className}: SpiderGraphProps) {
    const brandColor = getcssVariables('--brand', '#CC0022')
    const borderColor = getcssVariables('--border', '#E5E7EB')
    const labelColor = getcssVariables('--muted-text', '#71717A')
    const textColor = getcssVariables('--foreground', '#1A1A1A')
    const fontSans = getcssVariables('--font-sans', 'Barlow, sans-serif')
    const brandFill = getcssVariables('--brand-fill', '#CC002226')

    const chartValues = Array.isArray(data) ? data : SPIDER_CATS.map((cat) => data[cat] ?? 0)

    //configuring the size and mins and maxs
    const highestSet = Math.max(...chartValues, 0)
    const minimum = 4
    const bufferpadding = 2
    const max = Math.max(minimum, highestSet + bufferpadding)
    const stepSize = max > 12 ? 4 : 2 //in case high volume
    const calculatedMax = Math.ceil(max /stepSize) * stepSize
    // const totalSets = chartValues.reduce((sum, value) => sum + value, 0)

    const chartData: ChartData<'radar'> = {
        labels: [...SPIDER_CATS],
        datasets: [
            {
            data: chartValues,
            backgroundColor: brandFill,
            borderColor: brandColor,
            borderWidth: 1.5,
            pointRadius: 0,
            pointHoverRadius: 4,
            pointBackgroundColor: brandColor,
            },
        ],
    }

    const options: ChartOptions<'radar'> = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },                                                  
          tooltip: {                                                                   
            enabled: true,                                                             
            displayColors: false,                                                      
            backgroundColor: getcssVariables('--surface', '#FFFFFF'),                   
            titleColor: textColor,                                                     
            bodyColor: labelColor,                                                     
            borderColor: borderColor,                                                  
            borderWidth: 1,                                                            
            padding: 8,                                                                
            titleFont: { family: fontSans, size: 12, weight: 'bold' },                 
            bodyFont: { family: fontSans, size: 12 },
        },
    },
    scales: {
        r: {
            startAngle: 30, //hexagon
            min: 0,
            max: calculatedMax,
            ticks: {
                display: false,
                stepSize: stepSize
            },
            grid: {
                color: borderColor,
                circular: false, 
            },
            angleLines: {
                color: borderColor,
            },
            pointLabels : { color: labelColor, font : {family: fontSans, size: 13, weight: 500,},},
            suggestedMin: 0,
        },
    },
}
return (
    <div className={cn('relative h-[280px] w-full flex items-center justify-center', className)}>   
        <Radar data={chartData} options={options} />                                 
    </div>
)
}
export default SpiderGraph