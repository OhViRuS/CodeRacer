import { useEffect, useState } from 'react';
import RaceResults from './RaceResults';
import { fetchCodeSnippet } from '../api/codeSnippetsApi';
import useTypingChallenge from '../hooks/useTypingChallenge';

function TypingChallenge({ language, onExit }) {
    const [snippet, setSnippet] = useState(null);
    const [error, setError] = useState(null);

    useEffect(() => {
        fetchCodeSnippet(language)
            .then(data => {
                setSnippet(data);
            })
            .catch(error => {
                setError(error.message);
            });
    }, [language]);

    const {
        typedText,
        mistakes,
        elapsedTime,
        accuracy,
        wpm,
        isCorrectSoFar,
        isCompleted,
        correctCharacters,
        handleTyping
    } = useTypingChallenge(snippet);

    if (error) {
        return <p>{error}</p>;
    }

    if (!snippet) {
        return <p className="challenge-placeholder">Loading challenge...</p>;
    }
    if (isCompleted) {
        return (
            <div key="results" className="typing-challenge results-screen">
                <h2 className="challenge-title">Race Complete</h2>

                <RaceResults
                    time={elapsedTime}
                    wpm={wpm}
                    accuracy={accuracy}
                    mistakes={mistakes}
                />

                <button
                    className="primary-btn"
                    onClick={onExit}
                >
                    Back to Lobby
                </button>
            </div>
        );
    }

    return (
        <div key="challenge" className="typing-challenge">
            <h2 className="challenge-title">Typing Challenge</h2>

            <div className="code-area">
                <p className="section-label">CODE TO TYPE</p>

                <pre className="code-display">
                    {snippet.codeText}
                </pre>

                <div className="progress-section">
                    <div className="progress-labels">
                        <span>START</span>
                        <span>FINISH</span>
                    </div>

                    <div className="progress-track">
                        <div
                            className="progress-fill"
                            style={{
                                width: `${(correctCharacters / snippet.codeText.length) * 100}%`
                            }}
                        />

                        <span
                            className="progress-marker"
                            style={{
                                left: `${(correctCharacters / snippet.codeText.length) * 100}%`
                            }}
                        >
                            &lt;/&gt;
                        </span>
                    </div>

                    <div className="progress-percentage">
                        {Math.round(
                            (correctCharacters / snippet.codeText.length) * 100
                        )}%
                    </div>
                </div>

                <div className="typing-warning-container">
                    {!isCorrectSoFar && (
                        <div className="typing-warning">
                            ✕ Incorrect character
                        </div>
                    )}
                </div>

                <p className="section-label">YOUR INPUT</p>

                <textarea
                    className={`typing-input ${!isCorrectSoFar ? 'typing-input-error' : ''}`}
                    value={typedText}
                    onChange={handleTyping}
                    onPaste={event => event.preventDefault()}
                    placeholder="Start typing here..."
                    rows={6}
                    disabled={isCompleted}
                    spellCheck={false}
                    autoCorrect="off"
                    autoCapitalize="off"
                    autoComplete="off"
                />
            </div>

            <p>
                Mistakes: {mistakes}
            </p>

            <p>
                Accuracy: {accuracy}%
            </p>

            <p>
                WPM: {wpm}
            </p>

            <p>
                Time: {elapsedTime.toFixed(1)} seconds
            </p>
        </div>
    );
}

export default TypingChallenge;
