import { customFetch } from '@/lib/custom-fetch'
import { mockFetchResult, mockSubmitAnalysis } from '@/lib/optivision/mock-api'
import type { AnalyzeRequest, AnalyzeResponse, ResultResponse } from '@/types/optivision'

const ENV_MOCK = import.meta.env.VITE_OPTIVISION_MOCK === 'true'

export async function submitAnalysis(request: AnalyzeRequest, signal?: AbortSignal): Promise<AnalyzeResponse> {
  if (ENV_MOCK) {
    return mockSubmitAnalysis()
  }

  const response = await customFetch('/api/vision/analyze', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', Accept: 'application/json' },
    body: JSON.stringify(request),
    signal,
  })

  if (!response.ok) {
    throw new Error(`Failed to submit your analysis (${response.status})`)
  }

  return (await response.json()) as AnalyzeResponse
}

export async function fetchResult(jobId: string, signal?: AbortSignal): Promise<ResultResponse> {
  if (ENV_MOCK) {
    return mockFetchResult(jobId)
  }

  const response = await customFetch(`/api/vision/result/${encodeURIComponent(jobId)}`, {
    headers: { Accept: 'application/json' },
    signal,
  })

  if (!response.ok) {
    throw new Error(`Failed to fetch your results (${response.status})`)
  }

  return (await response.json()) as ResultResponse
}
