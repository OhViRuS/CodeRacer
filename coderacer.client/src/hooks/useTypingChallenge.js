import { useEffect, useState } from 'react';

function useTypingChallenge(snippet) {
    const [typedText, setTypedText] = useState('');
    const [mistakes, setMistakes] = useState(0);
    const [totalKeystrokes, setTotalKeystrokes] = useState(0);
    const [startTime, setStartTime] = useState(null);
    const [elapsedTime, setElapsedTime] = useState(0);

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

    const accuracy = totalKeystrokes === 0
        ? 100
        : Math.round(
            ((totalKeystrokes - mistakes) / totalKeystrokes) * 100
        );

    const wpm = elapsedTime > 0
        ? Math.round((totalKeystrokes / 5) / (elapsedTime / 60))
        : 0;

    function handleTyping(event) {
        if (!snippet) {
            return;
        }

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
            const elapsedSeconds =
                (Date.now() - currentStartTime) / 1000;

            setElapsedTime(elapsedSeconds);
        }

        setTypedText(newText);
    }

    return {
        typedText,
        mistakes,
        elapsedTime,
        accuracy,
        wpm,
        isCorrectSoFar,
        isCompleted,
        correctCharacters,
        handleTyping
    };
}

export default useTypingChallenge;
