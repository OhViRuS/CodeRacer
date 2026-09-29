import { useEffect, useState } from 'react';

function fetchCodeSnippet() {
    return fetch('/api/codesnippets')
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
                throw new Error('No code snippets available');
            }

            return data[0];
        });
}

function CodeSnippetDisplay() {
    const [snippet, setSnippet] = useState(null);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(true);

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
        fetchCodeSnippet()
            .then(data => {
                setSnippet(data);
            })
            .catch(error => {
                setError(error.message);
            })
            .finally(() => {
                setLoading(false);
            });
    }, []);

    function handleRetry() {
        setLoading(true);
        setError(null);
        setSnippet(null);

        fetchCodeSnippet()
            .then(data => {
                setSnippet(data);
            })
            .catch(error => {
                setError(error.message);
            })
            .finally(() => {
                setLoading(false);
            });
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