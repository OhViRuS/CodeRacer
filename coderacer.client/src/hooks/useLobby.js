import { useState } from 'react';

function useLobby() {
    const [lobby, setLobby] = useState(() => {
        try {
            const stored = localStorage.getItem('lobby');
            return stored ? JSON.parse(stored) : null;
        } catch (e) {
            console.error('Failed to load lobby from localStorage', e);
            return null;
        }
    });

    function saveLobbyToStorage(newLobby) {
        try {
            localStorage.setItem('lobby', JSON.stringify(newLobby));
        } catch (e) {
            console.error('Failed to save lobby to localStorage', e);
        }
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

        try {
            localStorage.removeItem('lobby');
        } catch (e) {
            console.error('Failed to remove lobby from localStorage', e);
        }
    }

    return {
        lobby,
        createLobby,
        joinLobby,
        closeLobby
    };
}

export default useLobby;
