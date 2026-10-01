import { useEffect, useState } from 'react';
import RaceResults from './RaceResults';

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

function TypingChallenge({ language, onExit }) {
    const [snippet, setSnippet] = useState(null);
    const [typedText, setTypedText] = useState('');
    const [error, setError] = useState(null);
    const [mistakes, setMistakes] = useState(0);
    const [totalKeystrokes, setTotalKeystrokes] = useState(0);
    const [startTime, setStartTime] = useState(null);
    const [elapsedTime, setElapsedTime] = useState(0);

    useEffect(() => {
        fetchCodeSnippet(language)
            .then(data => {
                setSnippet(data);
            })
            .catch(error => {
                setError(error.message);
            });
    }, [language]);

    const isCorrectSoFar = snippet
        ? snippet.codeText.startsWith(typedText)
        : true;

    const isCompleted = snippet
        ? typedText === snippet.codeText
        : false;

    useEffect(() => {
        if (startTime === null || isCompleted) {
            return;
        }

        const interval = setInterval(() => {
            setElapsedTime((Date.now() - startTime) / 1000);
        }, 100);

        return () => clearInterval(interval);
    }, [startTime, isCompleted]);

    const accuracy = totalKeystrokes === 0
        ? 100
        : Math.round(((totalKeystrokes - mistakes) / totalKeystrokes) * 100);

    const wpm = elapsedTime > 0
        ? Math.round((typedText.length / 5) / (elapsedTime / 60))
        : 0;

    if (error) {
        return <p>{error}</p>;
    }

    if (!snippet) {
        return <p>Loading challenge...</p>;
    }

    function handleTyping(event) {
        const newText = event.target.value;

        let currentStartTime = startTime;

        if (startTime === null && newText.length > 0) {
            currentStartTime = Date.now();
            setStartTime(currentStartTime);
        }

        if (newText.length > typedText.length) {
            setTotalKeystrokes(count => count + 1);

            const typedIndex = newText.length - 1;
            const typedCharacter = newText[typedIndex];
            const expectedCharacter = snippet.codeText[typedIndex];

            if (typedCharacter !== expectedCharacter) {
                setMistakes(count => count + 1);
            }
        }

        if (newText === snippet.codeText && currentStartTime !== null) {
            const elapsedSeconds = (Date.now() - currentStartTime) / 1000;

            setElapsedTime(elapsedSeconds);
        }

        setTypedText(newText);
    }

    return (
        <div className="typing-challenge">
            <h2>Typing Challenge</h2>

            <pre>{snippet.codeText}</pre>

            <textarea
                value={typedText}
                onChange={handleTyping}
                placeholder="Start typing here..."
                rows={6}
                disabled={isCompleted}
            />

            <p>
                Progress: {typedText.length} / {snippet.codeText.length}
            </p>

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

            {!isCorrectSoFar && (
                <p>Incorrect character!</p>
            )}

            {isCompleted && (
                <>
                    <RaceResults
                        time={elapsedTime}
                        wpm={wpm}
                        accuracy={accuracy}
                        mistakes={mistakes}
                    />

                    <div className="back-to-lobby">
                        <button
                            className="secondary-btn"
                            onClick={onExit}
                        >
                            Back to Lobby
                        </button>
                    </div>
                </>
            )}

        </div>
    );
}

export default TypingChallenge;