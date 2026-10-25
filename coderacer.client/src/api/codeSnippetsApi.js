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

export function fetchCodeSnippet(language) {
    return fetch(`/api/codesnippets?language=${encodeURIComponent(language)}`)
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
                throw new Error('No code snippets available for this language');
            }

            return data[0];
        });
}
