import {
    createResponseError,
    NETWORK_ERROR_MESSAGE
} from './apiErrors';

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
        throw new Error(NETWORK_ERROR_MESSAGE);
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
        throw new Error(NETWORK_ERROR_MESSAGE);
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
        throw new Error(NETWORK_ERROR_MESSAGE);
    }

    if (!response.ok) {
        throw await createResponseError(
            response,
            'Failed to load current user'
        );
    }

    return response.json();
}
