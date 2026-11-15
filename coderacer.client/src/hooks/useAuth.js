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
    const [initialToken] = useState(() =>
        readStorage(STORAGE_KEYS.AUTH_TOKEN)
    );

    const [token, setToken] = useState(initialToken);
    const [user, setUser] = useState(null);
    const [isSessionLoading, setIsSessionLoading] = useState(
        () => Boolean(initialToken)
    );
    const [isAuthenticating, setIsAuthenticating] = useState(false);

    useEffect(() => {
        if (!initialToken || token !== initialToken) {
            return;
        }

        let cancelled = false;

        fetchCurrentUser(initialToken)
            .then(currentUser => {
                if (!cancelled) {
                    setUser(currentUser);
                }
            })
            .catch(error => {
                if (cancelled) {
                    return;
                }

                if (error.status === 401 || error.status === 404) {
                    removeStorage(STORAGE_KEYS.AUTH_TOKEN);
                    setToken(null);
                    setUser(null);
                }
            })
            .finally(() => {
                if (!cancelled) {
                    setIsSessionLoading(false);
                }
            });

        return () => {
            cancelled = true;
        };
    }, [initialToken, token]);

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

            setToken(result.token);
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

            setToken(result.token);
            setUser(result.user);

            return result.user;
        } finally {
            setIsAuthenticating(false);
        }
    }

    function logout() {
        removeStorage(STORAGE_KEYS.AUTH_TOKEN);
        setToken(null);
        setUser(null);
    }

    return {
        token,
        user,
        isSessionLoading,
        isAuthenticating,
        login,
        register,
        logout
    };
}
