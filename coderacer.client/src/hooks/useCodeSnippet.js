import { useEffect, useState } from 'react';
import { fetchCodeSnippet } from '../api/codeSnippetsApi';

function useCodeSnippet(selectedLanguage) {
    const [snippet, setSnippet] = useState(null);
    const [error, setError] = useState(null);
    const [loading, setLoading] = useState(selectedLanguage !== null);
    const [retryCount, setRetryCount] = useState(0);

    useEffect(() => {
        if (!selectedLanguage) {
            return;
        }

        let cancelled = false;

        fetchCodeSnippet(selectedLanguage)
            .then(data => {
                if (!cancelled) {
                    setSnippet(data);
                }
            })
            .catch(e => {
                if (!cancelled) {
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
    }, [selectedLanguage, retryCount]);

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
