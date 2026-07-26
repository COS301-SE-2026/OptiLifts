import type { ExerciseTypeDefinition } from "@/types/exercise"
import { metricCheck } from "@/lib/weight-utils"

const check = metricCheck()
export const DEFAULT_EXERCISE_TYPE_OPTIONS: readonly ExerciseTypeDefinition[] = [
  ["WeightReps", "Weight & Reps", "Bench Press, Dumbbell Curls", ["REPS", (check)? "KG" : "LB"]],
  ["BodyweightReps", "Bodyweight Reps", "Pullups, Sit ups, Burpees", ["REPS"]],
  ["WeightedBodyWeight", "Weighted Bodyweight", "Weighted Pull Ups, Weighted Dips", ["REPS", (check)? "+KG" : "+LB"]],
  ["AssistedWeightReps", "Assisted Bodyweight", "Assisted Pullups, Assisted Dips", ["REPS", (check)? "-KG" : "-LB"]],
  ["Duration", "Duration", "Planks, Yoga, Stretching", ["TIME"]],
  ["DurationWeight", "Duration & Weight", "Weighted Plank, Wall Sit", [(check)? "KG" : "LB", "TIME"]],
  ["DistanceDuration", "Distance & Duration", "Running, Cycling, Rowing", ["TIME", (check) ? "KM" : "MI"]],
  ["WeightDistance", "Weight & Distance", "Farmers walk, Suitcase Carry", [(check) ? "KG" : "LB", (check) ? "KM" : "MI"]],
].map(([value, label, example, metrics]) => ({
  value: value as string,
  label: label as string,
  example: example as string,
  metrics: metrics as readonly string[],
})) as readonly ExerciseTypeDefinition[]
