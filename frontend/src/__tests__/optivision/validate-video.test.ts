import { describe, expect, it } from 'vitest'
import { MAX_VIDEO_BYTES } from '@/constants/optivision'
import { validateVideoFile } from '@/lib/optivision/validate-video'

function makeFile(type: string, size = 1024): File {
  const file = new File(['x'], 'clip', { type })
  Object.defineProperty(file, 'size', { value: size })
  return file
}

describe('validateVideoFile', () => {
  it('accepts a video file', () => {
    expect(validateVideoFile(makeFile('video/mp4'))).toBeNull()
  })

  it('rejects a file that is not a video', () => {
    expect(validateVideoFile(makeFile('image/png'))).toBe('Please choose a video file.')
  })

  it('rejects a video over the size limit', () => {
    expect(validateVideoFile(makeFile('video/mp4', MAX_VIDEO_BYTES + 1))).toContain('too large')
  })
})
