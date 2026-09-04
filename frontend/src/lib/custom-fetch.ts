let expiredRefToken = false;
let refreshQueue: Array<{
    resolve: () => void;
    reject: (error: unknown) => void;
}> = [];

const AUTH_ENDPOINTS = [
    '/auth/login',
    '/auth/register',
    '/auth/google',
    '/auth/logout',
    '/auth/refresh',
];

const processQueue = (error: Error | null) => {
    refreshQueue.forEach((request) => {
        if (error) {
            request.reject(error);
        } else {
            request.resolve();
        }
    });

    refreshQueue = [];
};

function getRequestUrl(inputURL: RequestInfo | URL): string {
    if (typeof inputURL === 'string') {
        return inputURL;
    }
    if (inputURL instanceof URL) {
        return inputURL.toString();
    }
    return (inputURL as Request).url;
}

function handleRateLimit(response: Response, inputURL: RequestInfo | URL): void {
    const retryAfterHeader = response.headers.get('Retry-After');
    const retryAfterSeconds = retryAfterHeader ? Number.parseInt(retryAfterHeader, 10) : 60;
    const requrl = getRequestUrl(inputURL);

    globalThis.dispatchEvent(new CustomEvent('rateLimitExceeded', {
        detail: {
            retryAfter: Number.isNaN(retryAfterSeconds) ? 60 : retryAfterSeconds,
            url: requrl,
        }
    }));
}

function isAuthUrl(requrl: string): boolean {
    return AUTH_ENDPOINTS.some((endpoint) => requrl.includes(endpoint)) || requrl.endsWith('/refresh');
}

function enqueueRefreshRequest(inputURL: RequestInfo | URL, input: RequestInit): Promise<Response> {
    return new Promise<Response>((resolve, reject) => {
        refreshQueue.push({
            resolve: () => resolve(customFetch(inputURL, input)),
            reject: (err) => reject(err),
        });
    });
}

async function refreshSession(): Promise<void> {
    const refreshRes = await fetch('/api/auth/refresh', {
        method: 'POST',
        credentials: 'include',
    });

    if (!refreshRes.ok) {
        throw new Error('Session expired');
    }
}

async function handleUnauthorized(
    inputURL: RequestInfo | URL,
    input: RequestInit,
    response: Response
): Promise<Response> {
    const requrl = getRequestUrl(inputURL);

    if (isAuthUrl(requrl)) {
        return response;
    }

    if (expiredRefToken) {
        return enqueueRefreshRequest(inputURL, input);
    }

    expiredRefToken = true;
    try {
        await refreshSession();
        expiredRefToken = false;
        processQueue(null); //pass null to show no error, will resolve all requests
        return await customFetch(inputURL, input); //retry the original request
    } catch (error) {
        expiredRefToken = false;

        const typedError = (error instanceof Error) ? error : new Error('Session expired');
        processQueue(typedError);

        //auth context listens for this and will log them out
        globalThis.dispatchEvent(new CustomEvent('logoutUser'));
        throw typedError;
    }
}

export async function customFetch( inputURL: RequestInfo | URL, init?: RequestInit)
:Promise<Response> {
    const input: RequestInit = { ...init, credentials: 'include' }; //adds request into an object, then adds credentails to it
    const response = await fetch(inputURL, input);

    if (response.status === 429) {
        handleRateLimit(response, inputURL);
    }

    if (response.status === 401) {
        return handleUnauthorized(inputURL, input, response);
    }

    return response;
}