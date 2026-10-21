function SnippetDisplay({
    selectedLanguage,
    snippet,
    error,
    loading,
    onRetry
}) {
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

    if (!selectedLanguage) {
        return <p>Pick a language to get a code snippet.</p>;
    }

    if (error) {
        return (
            <div>
                <p>{error}</p>
                <button onClick={onRetry}>Retry</button>
            </div>
        );
    }

    if (!snippet) {
        return loading ? <p>Loading code snippet...</p> : null;
    }

    return (
        <div className={loading ? 'code-snippet-section is-loading' : 'code-snippet-section'}>
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

export default SnippetDisplay;
