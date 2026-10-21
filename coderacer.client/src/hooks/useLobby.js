import { useState } from 'react';
import { readStorage, writeStorage, removeStorage, STORAGE_KEYS } from '../utils/storage';

function useLobby() {
    const [lobby, setLobby] = useState(() => {
        try {
            const storedLobby = readStorage(STORAGE_KEYS.LOBBY);

            return storedLobby ? JSON.parse(storedLobby) : null;
        } catch (e) {
            console.error('Failed to parse lobby from localStorage', e);
            return null;
        }
    });

    function saveLobbyToStorage(newLobby) {
        writeStorage(STORAGE_KEYS.LOBBY, JSON.stringify(newLobby));
    }

    function createLobby(name) {
        const newLobby = {
            id: Math.random().toString(36).slice(2, 9),
            host: name,
            players: [name],
            createdAt: new Date().toISOString(),
        };

        setLobby(newLobby);
        saveLobbyToStorage(newLobby);
    }

    function joinLobby(id, name) {
        const joinedLobby = {
            id,
            host: 'Unknown',
            players: [name],
            createdAt: new Date().toISOString(),
        };

        setLobby(joinedLobby);
        saveLobbyToStorage(joinedLobby);
    }

    function closeLobby() {
        setLobby(null);
        removeStorage(STORAGE_KEYS.LOBBY);
    }

    return { lobby, createLobby, joinLobby, closeLobby };
}

export default useLobby;
