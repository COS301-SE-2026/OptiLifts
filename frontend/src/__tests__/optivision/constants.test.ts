import { describe, expect, it } from 'vitest'
import { VISION_EXERCISES } from '@/constants/optivision'
import { EXERCISE_GUIDES } from '@/constants/optivision-guides'

describe('exercise guides', () => {
  it('has a guide for every exercise in the picker', () => {
    for (const { value } of VISION_EXERCISES) {
      expect(EXERCISE_GUIDES[value].checks.length).toBeGreaterThan(0)
    }
  })

  it('lists one check per output of the trained models', () => {
    expect(EXERCISE_GUIDES.squat.checks).toHaveLength(3)
    expect(EXERCISE_GUIDES.deadlift.checks).toHaveLength(5)
    expect(EXERCISE_GUIDES.bench.checks).toHaveLength(5)
  })
})
