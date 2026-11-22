export function fetchLanguages() {
    return fetch('/api/codesnippets/languages')
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to load languages');
            }

            return response.json();
        })
        .then(data => {
            if (!Array.isArray(data)) {
                throw new Error('Invalid languages response');
            }

            return data;
        });
}

export function fetchDifficulties() {
    return fetch('/api/codesnippets/difficulties')
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to load difficulties');
            }

            return response.json();
        })
        .then(data => {
            if (!Array.isArray(data)) {
                throw new Error('Invalid difficulties response');
            }

            return data;
        });
}

export function fetchTopics(language = null) {
    const url = language
        ? `/api/codesnippets/topics?language=${encodeURIComponent(language)}`
        : '/api/codesnippets/topics';

    return fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to load topics');
            }

            return response.json();
        })
        .then(data => {
            if (!Array.isArray(data)) {
                throw new Error('Invalid topics response');
            }

            return data;
        });
}

export function fetchCodeSnippet(language, difficulty = null, topic = null) {
    const params = new URLSearchParams();
    if (language) {
        params.append('language', language);
    }
    if (difficulty) {
        params.append('difficulty', difficulty);
    }
    if (topic) {
        params.append('topic', topic);
    }

    const query = params.toString();
    const url = query ? `/api/codesnippets?${query}` : '/api/codesnippets';

    return fetch(url)
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to load code snippet');
            }

            return response.json();
        })
        .then(data => {
            if (!Array.isArray(data)) {
                throw new Error('Invalid code snippet response');
            }

            if (data.length === 0) {
                throw new Error('No code snippets available for the selected filters');
            }

            return data[0];
        });
}
