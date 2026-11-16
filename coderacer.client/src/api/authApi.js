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

async function createResponseError(response, fallbackMessage) {
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

export async function registerUser(
    userName,
    email,
    password,
    confirmPassword
) {
    let response;

    try {
        response = await fetch('/api/users/register', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                userName,
                email,
                password,
                confirmPassword
            })
        });
    } catch {
        throw new Error('Unable to reach server. Try again later.');
    }

    if (!response.ok) {
        throw await createResponseError(
            response,
            'Registration failed'
        );
    }

    return response.json();
}

export async function loginUser(userNameOrEmail, password) {
    let response;

    try {
        response = await fetch('/api/users/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                userNameOrEmail,
                password
            })
        });
    } catch {
        throw new Error('Unable to reach server. Try again later.');
    }

    if (!response.ok) {
        throw await createResponseError(
            response,
            'Login failed'
        );
    }

    return response.json();
}

export async function fetchCurrentUser(token) {
    let response;

    try {
        response = await fetch('/api/users/me', {
            headers: {
                Authorization: `Bearer ${token}`
            }
        });
    } catch {
        throw new Error('Unable to reach server. Try again later.');
    }

    if (!response.ok) {
        throw await createResponseError(
            response,
            'Failed to load current user'
        );
    }

    return response.json();
}
