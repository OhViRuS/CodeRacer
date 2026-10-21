import LanguageSelector from './LanguageSelector';
import useCodeSnippet from '../hooks/useCodeSnippet';
import SnippetDisplay from './SnippetDisplay';

function CodeSnippetDisplay({ selectedLanguage, onLanguageSelected }) {
    const { snippet, error, loading, retry, startLoading } = useCodeSnippet(selectedLanguage);

    function handleLanguageChange(name) {
        if (name === selectedLanguage) {
            return;
        }

        startLoading();
        onLanguageSelected(name);
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
