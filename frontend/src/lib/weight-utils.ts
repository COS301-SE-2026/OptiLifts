export function metricCheck(): boolean {
    if (typeof globalThis === 'undefined') {
        return true;
    }

    return (localStorage.getItem('units') === 'metric');
}

//to display the correct weight 
export function outputWeight(weight: number): number {
    if (metricCheck()) {
        return weight;
    } else {
        return Math.round(weight * 2.20462 * 100) / 100;
    }

}

//convert to metric before send to backend
export function inputWeight(weight: number): number {
    if (metricCheck()) {
        return weight;
    } else {
        return Math.round(weight / 2.20462 * 100) / 100;
    }
}