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

function fetchRaceResult(snippetId, finalText, elapsedSeconds, keystrokes) {
    return fetch('/api/RaceSessions/result', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            snippetId,
            elapsedSeconds,
            finalText,
            keystrokes
        })
    }).then(response => {
        if (!response.ok) {
            throw new Error('Failed to calculate race result');
        }

        return response.json();
    });
}

function TypingChallenge({ language, onExit }) {
    const [snippet, setSnippet] = useState(null);
    const [typedText, setTypedText] = useState('');
    const [error, setError] = useState(null);
    const [keystrokes, setKeystrokes] = useState([]);
    const [raceResult, setRaceResult] = useState(null);
    const [resultLoading, setResultLoading] = useState(false);
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

    let correctCharacters = 0;

    if (snippet) {
        while (
            correctCharacters < typedText.length &&
            typedText[correctCharacters] === snippet.codeText[correctCharacters]
        ) {
            correctCharacters++;
        }
    }

    useEffect(() => {
        if (startTime === null || isCompleted) {
            return;
        }

        const interval = setInterval(() => {
            setElapsedTime((Date.now() - startTime) / 1000);
        }, 100);

        return () => clearInterval(interval);
    }, [startTime, isCompleted]);

    if (error) {
        return <p>{error}</p>;
    }

    if (!snippet) {
        return <p>Loading challenge...</p>;
    }
    if (isCompleted) {
        if (resultLoading || !raceResult) {
            return (
                <div className="typing-challenge results-screen">
                    <h2 className="challenge-title">Race Complete</h2>
                    <p>Calculating results...</p>
                </div>
            );
        }

        return (
            <div className="typing-challenge results-screen">
                <h2 className="challenge-title">Race Complete</h2>

                <RaceResults
                    time={raceResult.time}
                    wpm={raceResult.wpm}
                    accuracy={raceResult.accuracy}
                    mistakes={raceResult.mistakes}
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

    function handleTyping(event) {
        const newText = event.target.value;

        let currentStartTime = startTime;

        if (startTime === null && newText.length > 0) {
            currentStartTime = Date.now();
            setStartTime(currentStartTime);
        }

        let updatedKeystrokes = keystrokes;

        if (newText.length > typedText.length) {
            const newAttempts = [];

            for (
                let position = typedText.length;
                position < newText.length;
                position++
            ) {
                newAttempts.push({
                    position,
                    character: newText[position]
                });
            }

            updatedKeystrokes = [
                ...keystrokes,
                ...newAttempts
            ];

            setKeystrokes(updatedKeystrokes);
        }

        if (newText === snippet.codeText && currentStartTime !== null) {
            const elapsedSeconds =
                (Date.now() - currentStartTime) / 1000;

            setElapsedTime(elapsedSeconds);
            setResultLoading(true);

            fetchRaceResult(
                snippet.id,
                newText,
                elapsedSeconds,
                updatedKeystrokes
            )
                .then(result => {
                    setRaceResult(result);
                })
                .catch(error => {
                    setError(error.message);
                })
                .finally(() => {
                    setResultLoading(false);
                });
        }

        setTypedText(newText);
    }

    return (
        <div className="typing-challenge">
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
                    maxLength={snippet.codeText.length}
                    disabled={isCompleted}
                    spellCheck={false}
                    autoCorrect="off"
                    autoCapitalize="off"
                    autoComplete="off"
                />
            </div>

            <p>
                Time: {elapsedTime.toFixed(1)} seconds
            </p>

        </div>
    );
}

export default TypingChallenge;
