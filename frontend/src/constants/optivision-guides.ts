import type { ExerGuideData, VisionExercise } from '@/types/optivision'

const GIF_BASE = 'https://raw.githubusercontent.com/hasaneyldrm/exercises-dataset/main/videos'

export const EXERCISE_GUIDES: Readonly<Record<VisionExercise, ExerGuideData>> = {
  squat: {
    name: 'Barbell squat',
    note: 'Filmed from the side.',
    gifUrl: `${GIF_BASE}/0043-qXTaZnJ.gif`,
    checks: [
      { label: 'Depth', tip: 'Lower until the crease of your hip is below the top of your kneecap, shallow squats are flagged.' },
      { label: 'Forward lean', tip: 'Keep your chest up and your torso upright, folding forward like a good morning is flagged.' },
      { label: 'Heels', tip: 'Keep your heels flat on the floor, heels lifting at the bottom are flagged.' },
    ],
  },
  deadlift: {
    name: 'Conventional deadlift',
    note: 'Conventional stance only, filmed from the side.',
    gifUrl: `${GIF_BASE}/0032-ila4NZS.gif`,
    checks: [
      { label: 'Lower back', tip: 'Keep your spine neutral and straight through the pull, a rounded lower back is flagged.' },
      { label: 'Hip rise', tip: 'Keep your hips down until the bar leaves the floor, hips shooting up early are flagged.' },
      { label: 'Bar path', tip: 'Start with the bar over the middle of your foot and drag it up your shins, a bar drifting away from your legs is flagged.' },
      { label: 'Knees', tip: 'Keep your knees from travelling far over your toes at the start, knees pushed too far forward are flagged.' },
      { label: 'Start position', tip: 'Start with your hips low to the ground, high hips and glutes are flagged.' },
    ],
  },
  bench: {
    name: 'Barbell bench press',
    note: 'Filmed from the side.',
    gifUrl: `${GIF_BASE}/0025-EIeI8Vf.gif`,
    checks: [
      { label: 'Glutes', tip: 'Keep your glutes on the bench for the whole press, glutes lifting off are flagged.' },
      { label: 'Elbow flare', tip: 'Keep your elbows tucked in, elbows flared out to 90 degrees from your torso are flagged.' },
      { label: 'Chest touch', tip: 'Lower the bar all the way to your chest, half reps that never touch are flagged.' },
      { label: 'Bar path', tip: 'Touch the bar to your mid to lower sternum, touching near your neck or down at your stomach is flagged.' },
      { label: 'Arch', tip: 'Keep a slight natural arch with your chest up and shoulder blades pinched together, lying completely flat is flagged.' },
    ],
  },
}
