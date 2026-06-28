import type { ExerciseTypeValue, LoggedExerciseSet, PlannedExerciseSet } from '@/types/exercise-format'

function formatWeight(weight: number) {
  return `${Number.isInteger(weight) ? weight : weight.toFixed(1)}kg`
}

function formatRpe(rpe: number) {
  return Number.isInteger(rpe) ? `${rpe}` : rpe.toFixed(1)
}

function formatDurationValue(durationSeconds: number | null) {
  if (durationSeconds === null) {
    return ''
  }

  if (durationSeconds < 60) {
    return `${durationSeconds}s`
  }

  const minutes = Math.floor(durationSeconds / 60)
  const seconds = durationSeconds % 60
  return seconds === 0 ? `${minutes}m` : `${minutes}:${String(seconds).padStart(2, '0')}`
}

function formatDistanceValue(distance: number | null) {
  if (distance === null) {
    return ''
  }

  return `${Number.isInteger(distance) ? distance : distance.toFixed(1)}m`
}

function appendRestTime(text: string, restTime: string) {
  return restTime ? `${text} (${restTime})` : text
}

export function formatPlannedExerciseSetText(exerciseType: ExerciseTypeValue, set: PlannedExerciseSet) {
  const reps = set.reps !== null ? `${set.reps}` : ''
  const weight = set.weight !== null ? formatWeight(set.weight) : ''
  const duration = formatDurationValue(set.duration)
  const distance = formatDistanceValue(set.distance)

  let text = ''

  switch (exerciseType) {
    case 'bodyweight-reps':
      text = reps ? `${reps} reps` : ''
      break
    case 'weighted-bodyweight':
      text = [reps ? `${reps} reps` : '', weight ? `(+${weight})` : ''].filter(Boolean).join(' ')
      break
    case 'assisted-bodyweight':
      text = [reps ? `${reps} reps` : '', weight ? `(-${weight})` : ''].filter(Boolean).join(' ')
      break
    case 'duration':
      text = duration
      break
    case 'duration-weight':
      text = [duration, weight ? `@ ${weight}` : ''].filter(Boolean).join(' ')
      break
    case 'distance-duration':
      text = [distance, duration ? `in ${duration}` : ''].filter(Boolean).join(' ')
      break
    case 'weight-distance':
      text = [weight, distance ? `for ${distance}` : ''].filter(Boolean).join(' ')
      break
    case 'weight-reps':
    default:
      text = [weight, reps ? `x ${reps} reps` : ''].filter(Boolean).join(' ')
      break
  }

  return appendRestTime(text, set.restTime)
}

export function formatLoggedExerciseSetText(exerciseType: ExerciseTypeValue, set: LoggedExerciseSet) {
  const reps = `${set.reps}`
  const weight = formatWeight(set.weight)
  const rpe = formatRpe(set.rpe)

  let text = ''

  switch (exerciseType) {
    case 'bodyweight-reps':
      text = `${reps} reps`
      break
    case 'weighted-bodyweight':
      text = `${reps} reps (+${weight})`
      break
    case 'assisted-bodyweight':
      text = `${reps} reps (-${weight})`
      break
    case 'duration':
      text = `${reps}s`
      break
    case 'duration-weight':
      text = `${reps}s + ${weight}`
      break
    case 'distance-duration':
      text = `${reps}m`
      break
    case 'weight-distance':
      text = `${weight} for ${reps}m`
      break
    case 'weight-reps':
    default:
      text = `${weight} x ${reps} reps`
      break
  }

  return `${text} @ ${rpe} RPE`
}
