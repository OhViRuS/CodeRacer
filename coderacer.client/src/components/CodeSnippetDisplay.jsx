import { useEffect, useState } from 'react';

function fetchCodeSnippet(language) {
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

function CodeSnippetDisplay({ language }) {
    const [snippet, setSnippet] = useState(null);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(true);
    const [retryCount, setRetryCount] = useState(0);

    function getDifficultyName(difficulty) {
        switch (difficulty) {
            case 0:
                return 'Easy';
            case 1:
                return 'Medium';
            case 2:
                return 'Hard';
            default:
                return 'Unknown';
        }
    }

    useEffect(() => {
        let cancelled = false; // ignore stale responses if the language changes quickly

        setLoading(true);
        setError(null);
        setSnippet(null);

        fetchCodeSnippet(language)
            .then(data => {
                if (!cancelled) setSnippet(data);
            })
            .catch(e => {
                if (!cancelled) setError(e.message);
            })
            .finally(() => {
                if (!cancelled) setLoading(false);
            });

        return () => {
            cancelled = true;
        };
    }, [language, retryCount]);

    function handleRetry() {
        setRetryCount(count => count + 1);
    }

    if (loading) {
        return <p>Loading code snippet...</p>;
    }

    if (error) {
        return (
            <div>
                <p>{error}</p>
                <button onClick={handleRetry}>Retry</button>
            </div>
        );
    }

    if (!snippet) {
        return null;
    }

    return (
        <div className="code-snippet-section">
            <h2 className="snippet-title">Code Snippet</h2>

            <p>
                <strong>Concept:</strong> {snippet.programmingConcept}
            </p>

            <p>
                <strong>Difficulty:</strong> {getDifficultyName(snippet.difficulty)}
            </p>

            <pre>{snippet.codeText}</pre>
        </div>
    );
}

export default CodeSnippetDisplay;