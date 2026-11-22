import LanguageSelector from './LanguageSelector';
import DifficultySelector from './DifficultySelector';
import TopicSelector from './TopicSelector';
import useCodeSnippet from '../hooks/useCodeSnippet';
import SnippetDisplay from './SnippetDisplay';

function CodeSnippetDisplay({
    selectedLanguage,
    onLanguageSelected,
    selectedDifficulty = null,
    onDifficultySelected,
    selectedTopic = null,
    onTopicSelected
}) {
    const { snippet, error, loading, retry, startLoading } = useCodeSnippet(
        selectedLanguage,
        selectedDifficulty,
        selectedTopic
    );

    function handleLanguageChange(name) {
        if (name === selectedLanguage) {
            return;
        }

        startLoading();
        onLanguageSelected(name);
        onTopicSelected?.(null);
    }

    function handleDifficultyChange(diff) {
        if (diff === selectedDifficulty) {
            return;
        }

        startLoading();
        onDifficultySelected?.(diff);
    }

    function handleTopicChange(topic) {
        if (topic === selectedTopic) {
            return;
        }

        startLoading();
        onTopicSelected?.(topic);
    }

    return (
        <div>
            <LanguageSelector
                selectedLanguage={selectedLanguage}
                onLanguageSelected={handleLanguageChange}
            />

            <DifficultySelector
                selectedDifficulty={selectedDifficulty}
                onDifficultySelected={handleDifficultyChange}
            />

            <TopicSelector
                selectedLanguage={selectedLanguage}
                selectedTopic={selectedTopic}
                onTopicSelected={handleTopicChange}
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
