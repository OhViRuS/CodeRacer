const MESSAGES = {
    saving: 'Saving result...',
    saved: 'Result saved to your account.',
    guest: 'Result saved as a guest. Log in to track your statistics.',
    skipped: 'Results for imported code are not saved yet.'
};

function SaveStatus({ status, sessionExpired, message }) {
    if (status === 'idle') {
        return null;
    }

    let text = MESSAGES[status];

    if (status === 'failed') {
        text = `Could not save the result: ${message}`;
    }

    if (sessionExpired) {
        text = 'Your session expired, so you were logged out. The result was saved as a guest.';
    }

    return (
        <p
            className={`save-status ${status === 'failed' ? 'save-status-error' : ''}`}
            role="status"
        >
            {text}
        </p>
    );
}

export default SaveStatus;
