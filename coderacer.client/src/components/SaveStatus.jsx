const MESSAGES = {
    saving: 'Saving result...',
    saved: 'Result saved to your account.',
    guest: 'Result saved as a guest. Log in to track your statistics.',
    skipped: 'Results for imported code are not saved yet.'
};

function SaveStatus({ status, sessionExpired, message, canRetry, onRetry }) {
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
        <div className="save-status-wrapper">
            <p
                className={`save-status ${status === 'failed' ? 'save-status-error' : ''}`}
                role="status"
            >
                {text}
            </p>

            {status === 'failed' && canRetry && (
                <button
                    className="secondary-btn"
                    type="button"
                    onClick={onRetry}
                >
                    Try again
                </button>
            )}
        </div>
    );
}

export default SaveStatus;
