async function getErrorMessage(response, fallbackMessage) {
    try {
        const data = await response.json();

        if (data.message) {
            return data.message;
        }

        if (data.errors) {
            return Object.values(data.errors)
                .flat()
                .join(' ');
        }
    } catch {
        return fallbackMessage;
    }

    return fallbackMessage;
}

export async function createResponseError(response, fallbackMessage) {
    let message;

    if (response.status >= 500) {
        message = 'Server error, try again later';
    } else {
        message = await getErrorMessage(response, fallbackMessage);
    }

    const error = new Error(message);
    error.status = response.status;

    return error;
}

export const NETWORK_ERROR_MESSAGE = 'Unable to reach server. Try again later.';
