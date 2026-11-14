import { useEffect, useState } from 'react';
import {
    fetchCurrentUser,
    loginUser,
    registerUser
} from '../api/authApi';
import {
    readStorage,
    removeStorage,
    STORAGE_KEYS,
    writeStorage
} from '../utils/storage';

export function useAuth() {
    const [token] = useState(() =>
        readStorage(STORAGE_KEYS.AUTH_TOKEN)
    );

    const [user, setUser] = useState(null);
    const [isSessionLoading, setIsSessionLoading] = useState(
        () => Boolean(token)
    );
    const [isAuthenticating, setIsAuthenticating] = useState(false);

    useEffect(() => {
        if (!token) {
            return;
        }

        fetchCurrentUser(token)
            .then(currentUser => {
                setUser(currentUser);
            })
            .catch(() => {
                removeStorage(STORAGE_KEYS.AUTH_TOKEN);
            })
            .finally(() => {
                setIsSessionLoading(false);
            });
    }, [token]);

    async function login(userNameOrEmail, password) {
        setIsAuthenticating(true);

        try {
            const result = await loginUser(
                userNameOrEmail,
                password
            );

            writeStorage(
                STORAGE_KEYS.AUTH_TOKEN,
                result.token
            );

            setUser(result.user);

            return result.user;
        } finally {
            setIsAuthenticating(false);
        }
    }

    async function register(
        userName,
        email,
        password,
        confirmPassword
    ) {
        setIsAuthenticating(true);

        try {
            const result = await registerUser(
                userName,
                email,
                password,
                confirmPassword
            );

            writeStorage(
                STORAGE_KEYS.AUTH_TOKEN,
                result.token
            );

            setUser(result.user);

            return result.user;
        } finally {
            setIsAuthenticating(false);
        }
    }

    function logout() {
        removeStorage(STORAGE_KEYS.AUTH_TOKEN);
        setUser(null);
    }

    return {
        user,
        isSessionLoading,
        isAuthenticating,
        login,
        register,
        logout
    };
}
