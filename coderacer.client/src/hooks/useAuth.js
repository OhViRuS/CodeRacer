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
    const [isLoading, setIsLoading] = useState(() => Boolean(token));

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
                setIsLoading(false);
            });
    }, [token]);

    async function login(userNameOrEmail, password) {
        const result = await loginUser(userNameOrEmail, password);

        writeStorage(STORAGE_KEYS.AUTH_TOKEN, result.token);
        setUser(result.user);

        return result.user;
    }

    async function register(
        userName,
        email,
        password,
        confirmPassword
    ) {
        const result = await registerUser(
            userName,
            email,
            password,
            confirmPassword
        );

        writeStorage(STORAGE_KEYS.AUTH_TOKEN, result.token);
        setUser(result.user);

        return result.user;
    }

    function logout() {
        removeStorage(STORAGE_KEYS.AUTH_TOKEN);
        setUser(null);
    }

    return {
        user,
        isLoading,
        login,
        register,
        logout
    };
}
