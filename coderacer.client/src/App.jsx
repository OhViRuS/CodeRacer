import './App.css';
import { useState } from 'react';

function App() {
    const [lobby, setLobby] = useState(() => {
        try {
            const stored = localStorage.getItem('lobby');
            return stored ? JSON.parse(stored) : null;
        } catch (e) {
            console.error('Failed to load lobby from localStorage', e);
            return null;
        }
    });

    const sharedBtnStyle = { fontFamily: 'inherit', fontSize: 16, padding: '8px 12px' };

    function saveLobbyToStorage(newLobby) {
        try {
            localStorage.setItem('lobby', JSON.stringify(newLobby));
        } catch (e) {
            console.error('Failed to save lobby to localStorage', e);
        }
    }

    function handleCreateLobby() {
        const name = window.prompt('Enter your name to create a lobby:');
        if (name === null) return; // canceled
        const trimmed = name.trim();
        if (!trimmed) {
            alert('Name cannot be empty. Please try again.');
            return;
        }

        const newLobby = {
            id: Math.random().toString(36).slice(2, 9),
            host: trimmed,
            players: [trimmed],
            createdAt: new Date().toISOString(),
        };

        setLobby(newLobby);
        saveLobbyToStorage(newLobby);
        // TODO: replace with actual API call / navigation to real lobby
        console.log('Created lobby', newLobby);
    }

    function handleJoinLobby() {
        const lobbyId = window.prompt('Enter the lobby ID to join:');
        if (lobbyId === null) return; // canceled
        const trimmedId = lobbyId.trim();
        if (!trimmedId) {
            alert('Lobby ID cannot be empty. Please try again.');
            return;
        }

        const name = window.prompt('Enter your name to join the lobby:');
        if (name === null) return; // canceled
        const trimmed = name.trim();
        if (!trimmed) {
            alert('Name cannot be empty. Please try again.');
            return;
        }

        // In a real app we'd verify the lobby exists and fetch its data from server.
        const joinedLobby = {
            id: trimmedId,
            host: 'Unknown',
            players: [trimmed],
            createdAt: new Date().toISOString(),
        };

        setLobby(joinedLobby);
        saveLobbyToStorage(joinedLobby);
        console.log('Joined lobby', joinedLobby);
    }

    function handleClearLobby() {
        if (!lobby) return;
        if (!window.confirm('Close and remove the current lobby?')) return;
        setLobby(null);
        localStorage.removeItem('lobby');
    }

    return (
        <div className="center-container">
            <h1 className="brand">Coderacers</h1>

            {lobby ? (
                <div className="lobby-card">
                    <h2>Lobby</h2>
                    <p><strong>ID:</strong> {lobby.id}</p>
                    <p><strong>Host:</strong> {lobby.host}</p>
                    <p><strong>Players:</strong></p>
                    <ul>
                        {lobby.players.map((p, i) => (
                            <li key={i}>{p}</li>
                        ))}
                    </ul>
                    <div style={{marginTop: 12}}>
                        <button className="secondary-btn" style={{...sharedBtnStyle, marginLeft:8}} onClick={handleClearLobby}>Close Lobby</button>
                    </div>
                </div>
            ) : (
                <div>
                    <button className="primary-btn" style={sharedBtnStyle} onClick={handleCreateLobby}>Create Lobby</button>
                    <button className="secondary-btn" style={{...sharedBtnStyle, marginLeft:8}} onClick={handleJoinLobby}>Join Lobby</button>
                </div>
            )}
        </div>
    );
}

export default App;
