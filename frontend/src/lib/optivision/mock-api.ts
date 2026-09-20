import type { AnalyzeResponse, ResultResponse } from '@/types/optivision'

const MOCK_PROCESSING_MS = 8000
const submitAt = new Map<string, number>()

export async function mockSubmitAnalysis(): Promise<AnalyzeResponse> {
  const jobId = crypto.randomUUID()
  submitAt.set(jobId, Date.now())
  return { jobId }
}

export async function mockFetchResult(jobId: string): Promise<ResultResponse> {
  const start = submitAt.get(jobId)
  if (start === undefined) {
    return { status: 'failed' }
  }

  const elap = Date.now() - start
  if (elap < MOCK_PROCESSING_MS / 2) {
    return { status: 'pending' }
  }
  if (elap < MOCK_PROCESSING_MS) {
    return { status: 'processing' }
  }

  return {
    status: 'completed',
    coach_summary:
      'Great effort! You must go a bit deeper, and keep your chest up rather than down. Looking good otherwise!',
  }
}
