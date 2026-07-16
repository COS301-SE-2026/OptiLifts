import type { ExerciseTypeDefinition } from "@/types/exercise"
import { metricCheck } from "@/lib/weight-utils"

const check = metricCheck()
export const DEFAULT_EXERCISE_TYPE_OPTIONS: readonly ExerciseTypeDefinition[] = [
  ["weight-reps", "Weight & Reps", "Bench Press, Dumbbell Curls", ["REPS", (check)? "KG" : "LB"]],
  ["bodyweight-reps", "Bodyweight Reps", "Pullups, Sit ups, Burpees", ["REPS"]],
  ["weighted-bodyweight", "Weighted Bodyweight", "Weighted Pull Ups, Weighted Dips", ["REPS", (check)? "+KG" : "+LB"]],
  ["assisted-bodyweight", "Assisted Bodyweight", "Assisted Pullups, Assisted Dips", ["REPS", (check)? "-KG" : "-LB"]],
  ["duration", "Duration", "Planks, Yoga, Stretching", ["TIME"]],
  ["duration-weight", "Duration & Weight", "Weighted Plank, Wall Sit", [(check)? "KG" : "LB", "TIME"]],
  ["distance-duration", "Distance & Duration", "Running, Cycling, Rowing", ["TIME", (check) ? "KM" : "MI"]],
  ["weight-distance", "Weight & Distance", "Farmers walk, Suitcase Carry", [(check) ? "KG" : "LB", (check) ? "KM" : "MI"]],
].map(([value, label, example, metrics]) => ({
  value: value as string,
  label: label as string,
  example: example as string,
  metrics: metrics as readonly string[],
})) as readonly ExerciseTypeDefinition[]
