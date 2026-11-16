import { useEffect, useRef, useState } from 'react';
import { saveRaceResult } from '../api/raceResultsApi';
import { toTimeSpanString } from '../utils/timeFormat';

function isRetryable(error) {
    return (
        error.status === undefined ||
        error.status >= 500 ||
        error.status === 408 ||
        error.status === 429
    );
}

// The server rejects typing speeds above 500 WPM and more than 10000 mistakes.
const MAX_TYPING_SPEED = 500;
const MAX_MISTAKES = 10000;

// Saves the finished race once. Without a token the result is saved as a guest.
// If the token turns out to be expired, the session is ended and the result is saved as a guest instead of being lost.
// A failed save can be tried again with retry() when the failure may be temporary (no connection, server error).
// Returns { status, sessionExpired, message, canRetry, retry, reset }:
//   status: 'idle' | 'saving' | 'saved' | 'guest' | 'skipped' | 'failed'
function useSaveRaceResult({
    isCompleted,
    snippetId,
    elapsedTime,
    wpm,
    accuracy,
    mistakes,
    token,
    onSessionExpired
}) {
    const [outcome, setOutcome] = useState(null);
    const [attempt, setAttempt] = useState(0);
    const hasSavedRef = useRef(false);

    useEffect(() => {
        if (!isCompleted || !snippetId || hasSavedRef.current) {
            return;
        }

        hasSavedRef.current = true;

        const result = {
            codeSnippetId: snippetId,
            completionTime: toTimeSpanString(elapsedTime),
            typingSpeed: Math.min(wpm, MAX_TYPING_SPEED),
            accuracy,
            mistakes: Math.min(mistakes, MAX_MISTAKES)
        };

        saveRaceResult(token, result)
            .then(() => {
                setOutcome({ status: token ? 'saved' : 'guest' });
            })
            .catch(error => {
                if (token && error.status === 401) {
                    onSessionExpired();

                    return saveRaceResult(null, result)
                        .then(() => {
                            setOutcome({ status: 'guest', sessionExpired: true });
                        })
                        .catch(guestError => {
                            setOutcome({
                                status: 'failed',
                                message: guestError.message,
                                canRetry: isRetryable(guestError)
                            });
                        });
                }

                setOutcome({
                    status: 'failed',
                    message: error.message,
                    canRetry: isRetryable(error)
                });
            });
    }, [
        isCompleted,
        snippetId,
        elapsedTime,
        wpm,
        accuracy,
        mistakes,
        token,
        onSessionExpired,
        attempt
    ]);

    function retry() {
        hasSavedRef.current = false;
        setOutcome(null);
        setAttempt(count => count + 1);
    }

    function reset() {
        hasSavedRef.current = false;
        setOutcome(null);
    }

    let status = 'idle';

    if (isCompleted) {
        if (!snippetId) {
            status = 'skipped';
        } else {
            status = outcome?.status ?? 'saving';
        }
    }

    return {
        status,
        sessionExpired: outcome?.sessionExpired ?? false,
        message: outcome?.message ?? null,
        canRetry: outcome?.canRetry ?? false,
        retry,
        reset
    };
}

export default useSaveRaceResult;
