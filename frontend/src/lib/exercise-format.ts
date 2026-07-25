import type { LoggedExerciseSet, PlannedExerciseSet } from '@/types/exercise-format'
import { metricCheck, outputWeight } from './weight-utils'

function formatWeight(weight: number) {
  if (metricCheck()) {
    return `${Number.isInteger(weight) ? weight : weight.toFixed(1)} KG`
  }
  else {
    return `${Number.isInteger(outputWeight(weight)) ? outputWeight(weight) : outputWeight(weight).toFixed(1)} LB`
  }
}

function formatRpe(rpe: number) {
  return Number.isInteger(rpe) ? `${rpe}` : rpe.toFixed(1)
}

function formatRepsValue(reps: number | null) {
  if (reps === null) {
    return ''
  }

  return `${reps}`
}

function formatWeightValue(weight: number | null) {
  if (weight === null) {
    return ''
  }

  return formatWeight(weight)
}

function formatDurationValue(durationSeconds: number | null) {
  if (durationSeconds !== null) {
    if (durationSeconds < 60) {
      return `${durationSeconds}s`
    }

    const minutes = Math.floor(durationSeconds / 60)
    const seconds = durationSeconds % 60
    return seconds === 0 ? `${minutes}m` : `${minutes}:${String(seconds).padStart(2, '0')}`
  }

  return ''
}

function formatDistanceValue(distance: number | null) {
  if (distance !== null) {
    if (metricCheck()) {
      return `${Number.isInteger(distance) ? distance : distance.toFixed(1)} KM`
    }else{
      const miles = distance * 0.621371
      return `${Number.isInteger(miles) ? miles : miles.toFixed(1)} MI`
    }
  }

  return ''
}

function joinDefinedParts(parts: readonly string[]) {
  return parts.filter(Boolean).join(' ')
}

function appendRestTime(text: string, restTime: string, includeRestTime = true) {
  if (!includeRestTime || text === '') {
    return text
  }

  return restTime ? `${text} (${restTime})` : text
}

function formatPlannedWeightReps(set: PlannedExerciseSet, includeRestTime: boolean) {
  const text = joinDefinedParts([formatWeightValue(set.weight), formatRepsValue(set.reps) ? `x ${formatRepsValue(set.reps)} reps` : ''])
  return appendRestTime(text, set.restTime, includeRestTime)
}

function formatPlannedBodyweightReps(set: PlannedExerciseSet, includeRestTime: boolean) {
  const reps = formatRepsValue(set.reps)
  const text = reps ? `${reps} reps` : ''
  return appendRestTime(text, set.restTime, includeRestTime)
}

function formatPlannedWeightedBodyweight(set: PlannedExerciseSet, includeRestTime: boolean, sign: '+' | '-') {
  const weight = formatWeightValue(set.weight)
  const reps = formatRepsValue(set.reps)
  const text = joinDefinedParts([reps ? `${reps} reps` : '', weight ? `(${sign}${weight})` : ''])
  return appendRestTime(text, set.restTime, includeRestTime)
}

function formatPlannedDurationWeight(set: PlannedExerciseSet, includeRestTime: boolean) {
  const text = joinDefinedParts([formatDurationValue(set.duration), formatWeightValue(set.weight) ? `@ ${formatWeightValue(set.weight)}` : ''])
  return appendRestTime(text, set.restTime, includeRestTime)
}

function formatPlannedDistanceDuration(set: PlannedExerciseSet, includeRestTime: boolean) {
  const text = joinDefinedParts([formatDistanceValue(set.distance), formatDurationValue(set.duration) ? `in ${formatDurationValue(set.duration)}` : ''])
  return appendRestTime(text, set.restTime, includeRestTime)
}

function formatPlannedWeightDistance(set: PlannedExerciseSet, includeRestTime: boolean) {
  const text = joinDefinedParts([formatWeightValue(set.weight), formatDistanceValue(set.distance) ? `for ${formatDistanceValue(set.distance)}` : ''])
  return appendRestTime(text, set.restTime, includeRestTime)
}

const PLANNED_EXERCISE_FORMATTERS: Record<string, (set: PlannedExerciseSet, includeRestTime: boolean) => string> = {
  'bodyweight-reps': (set, includeRestTime) => formatPlannedBodyweightReps(set, includeRestTime),
  'weighted-bodyweight': (set, includeRestTime) => formatPlannedWeightedBodyweight(set, includeRestTime, '+'),
  'assisted-bodyweight': (set, includeRestTime) => formatPlannedWeightedBodyweight(set, includeRestTime, '-'),
  duration: (set, includeRestTime) => appendRestTime(formatDurationValue(set.duration), set.restTime, includeRestTime),
  'duration-weight': (set, includeRestTime) => formatPlannedDurationWeight(set, includeRestTime),
  'distance-duration': (set, includeRestTime) => formatPlannedDistanceDuration(set, includeRestTime),
  'weight-distance': (set, includeRestTime) => formatPlannedWeightDistance(set, includeRestTime),
  'weight-reps': (set, includeRestTime) => formatPlannedWeightReps(set, includeRestTime),
}

export function formatPlannedExerciseSetText(
  exerciseType: string,
  set: PlannedExerciseSet,
  options?: Readonly<{ includeRestTime?: boolean }>,
) {
  const formatter = PLANNED_EXERCISE_FORMATTERS[exerciseType] ?? PLANNED_EXERCISE_FORMATTERS['weight-reps']
  return formatter(set, options?.includeRestTime !== false)
}

export function formatLoggedExerciseSetText(exerciseType: string, set: LoggedExerciseSet) {
  const reps = `${set.reps}`
  const weight = formatWeight(set.weight)
  const rpe = formatRpe(set.rpe)
  const duration = formatDurationValue(set.duration)
  const distance = formatDistanceValue(set.distance)

  switch (exerciseType) {
    case 'bodyweight-reps':
      return `${reps} reps @ ${rpe} RPE`
    case 'weighted-bodyweight':
      return `${reps} reps (+${weight}) @ ${rpe} RPE`
    case 'assisted-bodyweight':
      return `${reps} reps (-${weight}) @ ${rpe} RPE`
    case 'duration':
      return `${duration || '--'} @ ${rpe} RPE`
    case 'duration-weight':
      return `${duration || '--'} + ${weight} @ ${rpe} RPE`
    case 'distance-duration': {
      const durationPart = duration ? ` in ${duration}` : ''
      return `${distance || '--'}${durationPart} @ ${rpe} RPE`
    }
    case 'weight-distance': {
      const distancePart = distance ? ` for ${distance}` : ''
      return `${weight}${distancePart} @ ${rpe} RPE`
    }
    case 'weight-reps':
    default:
      return `${weight} x ${reps} reps @ ${rpe} RPE`
  }
}
