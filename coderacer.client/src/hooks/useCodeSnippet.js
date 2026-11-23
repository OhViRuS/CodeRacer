import { useEffect, useState } from 'react';
import { fetchCodeSnippet } from '../api/codeSnippetsApi';

function useCodeSnippet(selectedLanguage, selectedDifficulty = null, selectedTopic = null) {
    const [snippet, setSnippet] = useState(null);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(selectedLanguage !== null);
    const [retryCount, setRetryCount] = useState(0);

    useEffect(() => {
        if (!selectedLanguage) {
            return;
        }

        let cancelled = false;

        fetchCodeSnippet(selectedLanguage, selectedDifficulty, selectedTopic)
            .then(data => {
                if (!cancelled) {
                    setSnippet(data);
                    setError(null);
                }
            })
            .catch(e => {
                if (!cancelled) {
                    setSnippet(null);
                    setError(e.message);
                }
            })
            .finally(() => {
                if (!cancelled) {
                    setLoading(false);
                }
            });

        return () => {
            cancelled = true;
        };
    }, [selectedLanguage, selectedDifficulty, selectedTopic, retryCount]);

    function startLoading() {
        setLoading(true);
        setError(null);
    }

    function retry() {
        startLoading();
        setRetryCount(count => count + 1);
    }

    return {
        snippet,
        error,
        loading,
        retry,
        startLoading
    };
}

export default useCodeSnippet;
