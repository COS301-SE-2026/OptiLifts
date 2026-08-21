import Body, {type ExtendedBodyPart, type Slug } from 'react-muscle-highlighter'
import type { MuscleName } from '@/types/workout'
import { useAuth } from '@/context/auth-context'

type Props = Readonly<{
  highlightedMuscles: MuscleName[]
  secondaryMuscles?: MuscleName[]
  variant?: 'front' | 'back' | 'both'
  sex?: 'male' | 'female'
}>
//slug lookup table
const MUSCLE_TO_SLUGS: Record<MuscleName, Slug[]>={
  Abdominals: ['abs'],
  Obliques: ['obliques'],
  Abductors: ['gluteal'],
  Adductors: ['adductors'],
  Biceps: ['biceps'],
  Chest: ['chest'],
  'Front Deltoid': ['deltoids'],
  'Middle Deltoid': ['deltoids'],
  'Rear Deltoid': ['deltoids'],
  Shoulders: ['deltoids'],
  Quadriceps: ['quadriceps'],
  Forearms: ['forearm'],
  Calves: ['calves'],
  Glutes: ['gluteal'],
  Hamstrings: ['hamstring'],
  Lats: ['upper-back'],
  'Lower Back': ['lower-back'],
  'Middle Back': ['upper-back'],
  'Upper Back': ['upper-back'],
  Trapezius: ['trapezius'],
  Triceps: ['triceps'],
}
export function MuscleDiagram({ highlightedMuscles, secondaryMuscles = [], variant = 'both', sex }: Props) {
  let userSex : string | undefined = undefined;
  try {
    const auth = useAuth();
    userSex = auth.user?.sex;
  } catch {
    //ignore settings fetch error
  }
  const selectedsex: 'female' | 'male' = sex ?? (userSex?.toLowerCase() === 'male' ? 'male' : 'female');

  const bodyDataMap = new Map<Slug, ExtendedBodyPart>()
  secondaryMuscles.forEach((muscle) => {
    const slugs = MUSCLE_TO_SLUGS[muscle] || []
    slugs.forEach((slug) => {
      bodyDataMap.set(slug, {
        slug,
        color: '#cc5c7d',
        intensity: 0.5
      })
    })
  })
  highlightedMuscles.forEach((muscle) => {
    const slugs = MUSCLE_TO_SLUGS[muscle] || []
    slugs.forEach((slug) => {
      bodyDataMap.set(slug, {
        slug,
        color: '#CC0022',
        intensity: 2
      })
    })
  })
  const bodyData = Array.from(bodyDataMap.values())
  const showFront = variant === 'front' || variant === 'both'
  const showBack = variant === 'back' || variant === 'both'

  return (
    <div className="w-full flex flex-col items-center gap-3 py-2">
    <div className="w-full flex items-center justify-center gap-4 py-2">
      {showFront && (
        <div className="flex flex-col items-center">
          <span className="text-xs font-medium text-muted-foreground mb-1">Front</span>
          <Body data={bodyData} side="front" gender={selectedsex}
          scale={0.9} colors={['#cc5c7d', '#CC0022']}
          defaultFill="var(--surface-2)"
          border="var(--border)"/>
          </div>
      )}
      {showBack && (
        <div className="flex flex-col items-center">
          <span className="text-xs font-medium text-muted-foreground mb-1">Back</span>
          <Body data={bodyData} side="back" gender={selectedsex}
          scale={0.9} colors={['#cc5c7d', '#CC0022']}
          defaultFill="var(--surface-2)"
          border="var(--border)"/>
          </div>
      )}
    </div>

    <div className="flex items-center justify-center gap-6 mt-1 text-xs text-muted-foreground">
      <div className="flex items-center gap-1.5">
        <span className="w-3 h-3 rounded bg-[#CC0022] inline-block border border-black/10"/>
        <span className="font-medium text-foreground">Primary</span>
      </div>
      <div className="flex items-center gap-1.5">
        <span className="w-3 h-3 rounded bg-[#cc5c7d] inline-block border border-black/10" />
        <span className="font-medium text-foreground">Secondary</span>
      </div>
    </div>
</div>
  )
}

export default MuscleDiagram
