import { useState } from 'react';
import LanguageSelector from './LanguageSelector';
import useCodeSnippet from '../hooks/useCodeSnippet';
import SnippetDisplay from './SnippetDisplay';

function CodeSnippetDisplay({ onLanguageSelected }) {
    const [selectedLanguage, setSelectedLanguage] = useState(() => {
        try {
            return localStorage.getItem('language');
        } catch (e) {
            console.error('Failed to load language from localStorage', e);
            return null;
        }
    });

    const { snippet, error, loading, retry, startLoading } = useCodeSnippet(selectedLanguage);

    function handleLanguageChange(name) {
        if (name === selectedLanguage) {
            return;
        }

        startLoading();
        // Notify the parent component about the selected language
        setSelectedLanguage(name);
        onLanguageSelected?.(name);

        try {
            localStorage.setItem('language', name);
        } catch (e) {
            console.error('Failed to save language to localStorage', e);
        }
    }

    return (
        <div>
            <LanguageSelector
                selectedLanguage={selectedLanguage}
                onLanguageSelected={handleLanguageChange}
            />

            <SnippetDisplay
                selectedLanguage={selectedLanguage}
                snippet={snippet}
                error={error}
                loading={loading}
                onRetry={retry}
            />
        </div>
    );
}

export default CodeSnippetDisplay;
