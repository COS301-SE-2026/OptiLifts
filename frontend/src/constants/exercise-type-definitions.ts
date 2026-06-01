import type { ExerciseTypeDefinition } from "@/types/exercise"

export const DEFAULT_EXERCISE_TYPE_OPTIONS: readonly ExerciseTypeDefinition[] = [
  ["weight-reps", "Weight & Reps", "Bench Press, Dumbbell Curls", ["REPS", "KG"]],
  ["bodyweight-reps", "Bodyweight Reps", "Pullups, Sit ups, Burpees", ["REPS"]],
  ["weighted-bodyweight", "Weighted Bodyweight", "Weighted Pull Ups, Weighted Dips", ["REPS", "+KG"]],
  ["assisted-bodyweight", "Assisted Bodyweight", "Assisted Pullups, Assisted Dips", ["REPS", "-KG"]],
  ["duration", "Duration", "Planks, Yoga, Stretching", ["TIME"]],
  ["duration-weight", "Duration & Weight", "Weighted Plank, Wall Sit", ["KG", "TIME"]],
  ["distance-duration", "Distance & Duration", "Running, Cycling, Rowing", ["TIME", "KM"]],
  ["weight-distance", "Weight & Distance", "Farmers walk, Suitcase Carry", ["KG", "KM"]],
].map(([value, label, example, metrics]) => ({
  value: value as string,
  label: label as string,
  example: example as string,
  metrics: metrics as readonly string[],
})) as readonly ExerciseTypeDefinition[]
