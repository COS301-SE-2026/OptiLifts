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

  return `${Number.isInteger(distance) ? distance : distance.toFixed(1)}km`
}

function appendRestTime(text: string, restTime: string) {
  return restTime ? `${text} (${restTime})` : text
}

export function formatPlannedExerciseSetText(
  exerciseType: ExerciseTypeValue,
  set: PlannedExerciseSet,
  options?: Readonly<{ includeRestTime?: boolean }>,
) {
  const reps = set.reps !== null ? `${set.reps}` : ''
  const weight = set.weight !== null ? formatWeight(set.weight) : ''
  const duration = formatDurationValue(set.duration)
  const distance = formatDistanceValue(set.distance)

  switch (exerciseType) {
    case 'bodyweight-reps':
      return options?.includeRestTime === false ? (reps ? `${reps} reps` : '') : appendRestTime(reps ? `${reps} reps` : '', set.restTime)
    case 'weighted-bodyweight':
      return options?.includeRestTime === false
        ? [reps ? `${reps} reps` : '', weight ? `(+${weight})` : ''].filter(Boolean).join(' ')
        : appendRestTime([reps ? `${reps} reps` : '', weight ? `(+${weight})` : ''].filter(Boolean).join(' '), set.restTime)
    case 'assisted-bodyweight':
      return options?.includeRestTime === false
        ? [reps ? `${reps} reps` : '', weight ? `(-${weight})` : ''].filter(Boolean).join(' ')
        : appendRestTime([reps ? `${reps} reps` : '', weight ? `(-${weight})` : ''].filter(Boolean).join(' '), set.restTime)
    case 'duration':
      return options?.includeRestTime === false ? duration : appendRestTime(duration, set.restTime)
    case 'duration-weight':
      return options?.includeRestTime === false
        ? [duration, weight ? `@ ${weight}` : ''].filter(Boolean).join(' ')
        : appendRestTime([duration, weight ? `@ ${weight}` : ''].filter(Boolean).join(' '), set.restTime)
    case 'distance-duration':
      return options?.includeRestTime === false
        ? [distance, duration ? `in ${duration}` : ''].filter(Boolean).join(' ')
        : appendRestTime([distance, duration ? `in ${duration}` : ''].filter(Boolean).join(' '), set.restTime)
    case 'weight-distance':
      return options?.includeRestTime === false
        ? [weight, distance ? `for ${distance}` : ''].filter(Boolean).join(' ')
        : appendRestTime([weight, distance ? `for ${distance}` : ''].filter(Boolean).join(' '), set.restTime)
    case 'weight-reps':
    default:
      return options?.includeRestTime === false
        ? [weight, reps ? `x ${reps} reps` : ''].filter(Boolean).join(' ')
        : appendRestTime([weight, reps ? `x ${reps} reps` : ''].filter(Boolean).join(' '), set.restTime)
  }
}

export function formatLoggedExerciseSetText(exerciseType: ExerciseTypeValue, set: LoggedExerciseSet) {
  const reps = `${set.reps}`
  const weight = formatWeight(set.weight)
  const rpe = formatRpe(set.rpe)

  switch (exerciseType) {
    case 'bodyweight-reps':
      return `${reps} reps @ ${rpe} RPE`
    case 'weighted-bodyweight':
      return `${reps} reps (+${weight}) @ ${rpe} RPE`
    case 'assisted-bodyweight':
      return `${reps} reps (-${weight}) @ ${rpe} RPE`
    case 'duration':
      return `${reps}s @ ${rpe} RPE`
    case 'duration-weight':
      return `${reps}s + ${weight} @ ${rpe} RPE`
    case 'distance-duration':
      return `${reps}m @ ${rpe} RPE`
    case 'weight-distance':
      return `${weight} for ${reps}m @ ${rpe} RPE`
    case 'weight-reps':
    default:
      return `${weight} x ${reps} reps @ ${rpe} RPE`
  }
}
