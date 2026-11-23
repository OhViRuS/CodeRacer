import { useEffect, useState } from 'react';
import { fetchTopics } from '../api/codeSnippetsApi';

function TopicSelector({ selectedLanguage, selectedTopic, onTopicSelected }) {
    const [topics, setTopics] = useState([]);
    const [error, setError] = useState(null);

    useEffect(() => {
        let cancelled = false;

        fetchTopics(selectedLanguage)
            .then(data => {
                if (!cancelled) {
                    setTopics(data);
                    setError(null);
                }
            })
            .catch(e => {
                if (!cancelled) {
                    setError(e.message);
                }
            });

        return () => {
            cancelled = true;
        };
    }, [selectedLanguage]);

    return (
        <div className="filter-group">
            <h3>Topic</h3>

            {error && <p className="filter-error">{error}</p>}

            <div className="button-container wrap-buttons">
                <button
                    type="button"
                    className={
                        !selectedTopic
                            ? 'primary-btn selected'
                            : 'secondary-btn'
                    }
                    aria-pressed={!selectedTopic}
                    onClick={() => onTopicSelected(null)}
                >
                    Any
                </button>
                {topics.map(topic => (
                    <button
                        key={topic}
                        type="button"
                        className={
                            selectedTopic === topic
                                ? 'primary-btn selected'
                                : 'secondary-btn'
                        }
                        aria-pressed={selectedTopic === topic}
                        onClick={() => onTopicSelected(topic)}
                    >
                        {topic}
                    </button>
                ))}
            </div>
        </div>
    );
}

export default TopicSelector;
