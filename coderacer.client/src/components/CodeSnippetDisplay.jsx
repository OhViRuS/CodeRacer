import { useEffect, useState } from 'react';

// Enum names from the API -> labels shown on the buttons
const LANGUAGE_LABELS = { CSharp: 'C#', Cpp: 'C++', Python: 'Python', JavaScript: 'JavaScript' };
function fetchLanguages() {
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

function CodeSnippetDisplay() {
    const [languages, setLanguages] = useState([]);
    const [languagesError, setLanguagesError] = useState(null);
    const [selectedLanguage, setSelectedLanguage] = useState(() => {
        try {
            return localStorage.getItem('language');
        } catch (e) {
            console.error('Failed to load language from localStorage', e);
            return null;
        }
    });

    const [snippet, setSnippet] = useState(null);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(selectedLanguage !== null);
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

    function startLoading() {
        setLoading(true);
        setError(null);
        setSnippet(null);
    }

    function handleLanguageChange(name) {
        if (name === selectedLanguage) {
            return; // same language: the effect won't re-run, so loading would get stuck
        }

        startLoading();
        setSelectedLanguage(name);

        try {
            localStorage.setItem('language', name);
        } catch (e) {
            console.error('Failed to save language to localStorage', e);
        }
    }

    function handleRetry() {
        startLoading();
        setRetryCount(count => count + 1);
    }

    // Load the list of available languages once.
    useEffect(() => {
        fetchLanguages()
            .then(setLanguages)
            .catch(e => setLanguagesError(e.message));
    }, []);

    // Load a snippet whenever the selected language changes (or on retry).
    useEffect(() => {
        if (!selectedLanguage) {
            return;
        }

        let cancelled = false; // ignore stale responses if the language changes quickly

        fetchCodeSnippet(selectedLanguage)
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
    }, [selectedLanguage, retryCount]);

    function renderSnippet() {
        if (!selectedLanguage) {
            return <p>Pick a language to get a code snippet.</p>;
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

    return (
        <div>
            <h3>Select a programming language</h3>

            {languagesError && <p>{languagesError}</p>}

            {languages.map(name => (
                <button
                    key={name}
                    className={selectedLanguage === name ? 'primary-btn' : 'secondary-btn'}
                    style={{ fontFamily: 'inherit', fontSize: 16, padding: '8px 12px', marginRight: 8 }}
                    onClick={() => handleLanguageChange(name)}
                >
                    {LANGUAGE_LABELS[name] ?? name}
                </button>
            ))}

            {renderSnippet()}
        </div>
    );
}

export default CodeSnippetDisplay;
