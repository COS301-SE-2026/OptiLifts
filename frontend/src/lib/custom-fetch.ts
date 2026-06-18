let expiredRefToken = false;
let refreshQueue: Array<{
    resolve: () => void;
    reject: (error: unknown) => void;
}> = [];

const processQueue = (error: Error |null) => {
    refreshQueue.forEach((request) => {
        if (error){
            request.reject(error);
        }else{
            request.resolve();
        }
    });

    refreshQueue = [];
}

export async function customFetch( inputURL: RequestInfo | URL, init?: RequestInit)
:Promise<Response> {
    const input: RequestInit = { ...init, credentials: 'include' }; //adds request into an object, then adds credentails to it
    const response = await fetch(inputURL, input);

    if (response.status == 401){
        let requrl: string;
        if (typeof inputURL == 'string'){
            requrl = inputURL;
        }else{
            requrl = (inputURL as Request).url;
        } 

        if (
            requrl.includes('/auth/login') ||
            requrl.includes('/auth/register') ||
            requrl.includes('/auth/logout') ||
            requrl.includes('/auth/refresh') ||
            requrl.endsWith('/refresh')
        ) {
            return response;
        }

        if (expiredRefToken){
            return new Promise<Response>((resolve, reject) => {
                refreshQueue.push({ 
                    resolve: () => resolve(customFetch(inputURL, input)),
                    reject: (err) => reject(err),
                });
            });
        }

        expiredRefToken = true;
        try {
            const refreshRes = await fetch('/api/auth/refresh', {
                method: 'POST',
                credentials: 'include',
            });
    
            if (!refreshRes.ok){
                throw new Error('Session expired');
            }
    
            expiredRefToken = false;
            processQueue(null); //pass null to show no error, will resolve all requests
            return customFetch(inputURL, input); //retry the original request
        } catch (error) {
            expiredRefToken = false;
    
            const typedError = (error instanceof Error) ? error : new Error('Session expired');
            processQueue(typedError);
            
            //auth context listens for this and will log them out
            globalThis.dispatchEvent(new CustomEvent('logoutUser'));
            throw typedError;
        }
    }

    return response;
}