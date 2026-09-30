import { useEffect, useState } from 'react';

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

function TypingChallenge({ language }) {
    const [snippet, setSnippet] = useState(null);
    const [typedText, setTypedText] = useState('');
    const [error, setError] = useState(null);
    const [mistakes, setMistakes] = useState(0);

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

    if (error) {
        return <p>{error}</p>;
    }

    if (!snippet) {
        return <p>Loading challenge...</p>;
    }

    function handleTyping(event) {
        const newText = event.target.value;

        if (newText.length > typedText.length) {
            const typedIndex = newText.length - 1;
            const typedCharacter = newText[typedIndex];
            const expectedCharacter = snippet.codeText[typedIndex];

            if (typedCharacter !== expectedCharacter) {
                setMistakes(count => count + 1);
            }
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

            {!isCorrectSoFar && (
                <p>Incorrect character!</p>
            )}

            {isCompleted && (
                <p>Challenge completed!</p>
            )}
        </div>
    );
}

export default TypingChallenge;