import type { ExerGuideData, VisionExercise } from '@/types/optivision'

export const EXERCISE_GUIDES: Readonly<Record<VisionExercise, ExerGuideData>> = {
  squat: {
    name: 'Barbell squat',
    note: 'Filmed from the side.',
    gifUrl: '/exercises/squat.gif',
    checks: [
      { label: 'Depth', tip: 'Lower until the crease of your hip is below the top of your kneecap, shallow squats are flagged.' },
      { label: 'Forward lean', tip: 'Keep your chest up and your torso upright, folding forward like a good morning is flagged.' },
    ],
  },
  deadlift: {
    name: 'Conventional deadlift',
    note: 'Conventional stance only, filmed from the side.',
    gifUrl: '/exercises/deadlift.gif',
    checks: [
      { label: 'Lower back', tip: 'Keep your spine neutral and straight through the pull, a rounded lower back is flagged.' },
      { label: 'Bad hip movement', tip: 'Start with your hips low and raise them simultaneously with your back, hips starting too high or shooting up early are flagged.' },
      { label: 'Bar path', tip: 'Start with the bar over the middle of your foot and drag it up your shins, a bar drifting away from your legs is flagged.' },
      { label: 'Knees', tip: 'Keep your knees from travelling far over your toes at the start, knees pushed too far forward are flagged.' },
    ],
  },
  bench: {
    name: 'Barbell bench press',
    note: 'Filmed from the side.',
    gifUrl: '/exercises/bench.gif',
    checks: [
      { label: 'Elbow flare', tip: 'Keep your elbows tucked in, elbows flared out to 90 degrees from your torso are flagged.' },
      { label: 'Chest touch', tip: 'Lower the bar all the way to your chest, half reps that never touch are flagged.' },
      { label: 'Bar path', tip: 'Touch the bar to your mid to lower sternum, touching near your neck or down at your stomach is flagged.' },
      { label: 'Arch', tip: 'Keep a slight natural arch with your chest up and shoulder blades pinched together, lying completely flat is flagged.' },
    ],
  },
}
