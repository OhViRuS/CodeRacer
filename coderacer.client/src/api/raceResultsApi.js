import {
    createResponseError,
    NETWORK_ERROR_MESSAGE
} from './apiErrors';

async function sendRequest(url, options, fallbackMessage) {
    let response;

    try {
        response = await fetch(url, options);
    } catch {
        throw new Error(NETWORK_ERROR_MESSAGE);
    }

    if (!response.ok) {
        throw await createResponseError(response, fallbackMessage);
    }

    return response.json();
}

function authorizationHeader(token) {
    return token ? { Authorization: `Bearer ${token}` } : {};
}

// Without a token the server saves the result under the shared guest account.
export function saveRaceResult(token, result) {
    return sendRequest(
        '/api/raceresults',
        {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                ...authorizationHeader(token)
            },
            body: JSON.stringify(result)
        },
        'Failed to save result'
    );
}

export function fetchRecentResults(token, count = 10) {
    return sendRequest(
        `/api/raceresults/recent?count=${count}`,
        { headers: authorizationHeader(token) },
        'Failed to load race history'
    );
}

export function fetchUserStatistics(userId) {
    return sendRequest(
        `/api/users/${encodeURIComponent(userId)}/statistics`,
        undefined,
        'Failed to load statistics'
    );
}
