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

export async function registerUser(
    userName,
    email,
    password,
    confirmPassword
) {
    const response = await fetch('/api/users/register', {
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

    if (!response.ok) {
        const message = await getErrorMessage(
            response,
            'Registration failed'
        );

        throw new Error(message);
    }

    return response.json();
}

export async function loginUser(userNameOrEmail, password) {
    const response = await fetch('/api/users/login', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            userNameOrEmail,
            password
        })
    });

    if (!response.ok) {
        const message = await getErrorMessage(
            response,
            'Login failed'
        );

        throw new Error(message);
    }

    return response.json();
}

export async function fetchCurrentUser(token) {
    const response = await fetch('/api/users/me', {
        headers: {
            Authorization: `Bearer ${token}`
        }
    });

    if (!response.ok) {
        throw new Error('Failed to load current user');
    }

    return response.json();
}
