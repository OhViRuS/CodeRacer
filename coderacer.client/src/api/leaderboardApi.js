import {
    createResponseError,
    NETWORK_ERROR_MESSAGE
} from './apiErrors';

export async function fetchLeaderboard({
    sortBy = 'TypingSpeed',
    language = 'Python',
    difficulty = 'Easy',
    topic = null,
    snippetId = null,
    limit = 10
} = {}) {
    const params = new URLSearchParams();
    if (sortBy) {
        params.append('sortBy', sortBy);
    }
    if (language) {
        params.append('language', language);
    }
    if (difficulty) {
        params.append('difficulty', difficulty);
    }
    if (topic) {
        params.append('topic', topic);
    }
    if (snippetId) {
        params.append('snippetId', snippetId);
    }
    if (limit) {
        params.append('limit', limit.toString());
    }

    let response;
    try {
        response = await fetch(`/api/leaderboard?${params.toString()}`);
    } catch {
        throw new Error(NETWORK_ERROR_MESSAGE);
    }

    if (!response.ok) {
        throw await createResponseError(response, 'Failed to load leaderboard');
    }

    return response.json();
}
