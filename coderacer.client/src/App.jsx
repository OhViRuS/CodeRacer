import './App.css';
import { useState } from 'react';
import Modal from './components/Modal';
import CodeSnippetDisplay from './components/CodeSnippetDisplay';
import TypingChallenge from './components/TypingChallenge';

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

    const [selectedLanguage, setSelectedLanguage] = useState(() => {
        try {
            return localStorage.getItem('language');
        } catch (e) {
            console.error('Failed to load language from localStorage', e);
            return null;
        }
    });

    const [gameStarted, setGameStarted] = useState(false);
    const [createLobbyModalOpen, setCreateLobbyModalOpen] = useState(false);
    const [joinLobbyModalOpen, setJoinLobbyModalOpen] = useState(false);
    const [closeLobbyModalOpen, setCloseLobbyModalOpen] = useState(false);
    const [createLobbyName, setCreateLobbyName] = useState('');
    const [joinLobbyId, setJoinLobbyId] = useState('');
    const [joinLobbyName, setJoinLobbyName] = useState('');
    const [createLobbyError, setCreateLobbyError] = useState('');
    const [joinLobbyError, setJoinLobbyError] = useState('');

    function saveLobbyToStorage(newLobby) {
        try {
            localStorage.setItem('lobby', JSON.stringify(newLobby));
        } catch (e) {
            console.error('Failed to save lobby to localStorage', e);
        }
    }

    function handleCreateLobbySubmit() {
        const trimmed = createLobbyName.trim();
        if (!trimmed) {
            setCreateLobbyError('Name cannot be empty. Please enter a name.');
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
        setCreateLobbyModalOpen(false);
        setCreateLobbyName('');
        setCreateLobbyError('');
        console.log('Created lobby', newLobby);
    }

    function handleJoinLobbySubmit() {
        const trimmedId = joinLobbyId.trim();
        if (!trimmedId) {
            setJoinLobbyError('Lobby ID cannot be empty. Please enter a lobby ID.');
            return;
        }

        const trimmedName = joinLobbyName.trim();
        if (!trimmedName) {
            setJoinLobbyError('Name cannot be empty. Please enter a name.');
            return;
        }

        // In a real app we'd verify the lobby exists and fetch its data from server.
        const joinedLobby = {
            id: trimmedId,
            host: 'Unknown',
            players: [trimmedName],
            createdAt: new Date().toISOString(),
        };

        setLobby(joinedLobby);
        saveLobbyToStorage(joinedLobby);
        setJoinLobbyModalOpen(false);
        setJoinLobbyId('');
        setJoinLobbyName('');
        setJoinLobbyError('');
        console.log('Joined lobby', joinedLobby);
    }

    function handleClearLobby() {
        setCloseLobbyModalOpen(true);
    }

    function handleConfirmCloseLobby() {
        if (!lobby) return;
        setLobby(null);
        localStorage.removeItem('lobby');
        setCloseLobbyModalOpen(false);
    }

    if (gameStarted && selectedLanguage) {
        return (
            <div className="center-container">
                <h1 className="brand">CodeRacer</h1>

                <TypingChallenge
                    language={selectedLanguage}
                    onExit={() => setGameStarted(false)}
                />
            </div>
        );
    }

    return (
        <div className="center-container">
            <h1 className="brand">CodeRacer</h1>

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

                    <CodeSnippetDisplay onLanguageSelected={setSelectedLanguage} />

                    <div className="lobby-actions">
                        <button
                            className="secondary-btn"
                            onClick={handleClearLobby}
                        >
                            Close Lobby
                        </button>

                        <button
                            className="primary-btn"
                            onClick={() => setGameStarted(true)}
                            disabled={!selectedLanguage}
                        >
                            Start Game
                        </button>
                    </div>

                    {/* Close Lobby Confirmation Modal */}
                    <Modal 
                        isOpen={closeLobbyModalOpen} 
                        title="Close Lobby" 
                        onClose={() => setCloseLobbyModalOpen(false)}
                    >
                        <p>Are you sure you want to close and remove the current lobby?</p>
                        <div className="modal-actions">
                            <button 
                                className="btn-primary"
                                onClick={handleConfirmCloseLobby}
                            >
                                Close Lobby
                            </button>
                            <button 
                                className="btn-secondary" 
                                onClick={() => setCloseLobbyModalOpen(false)}
                            >
                                Cancel
                            </button>
                        </div>
                    </Modal>
                </div>
            ) : (
                <div>
                    <div className="button-container">
                        <button className="primary-btn" onClick={() => {
                            setCreateLobbyModalOpen(true);
                            setCreateLobbyError('');
                        }}>Create Lobby</button>
                        <button className="primary-btn" onClick={() => {
                            setJoinLobbyModalOpen(true);
                            setJoinLobbyError('');
                        }}>Join Lobby</button>
                    </div>

                    {/* Create Lobby Modal */}
                    <Modal 
                        isOpen={createLobbyModalOpen} 
                        title="Create Lobby" 
                        onClose={() => {
                            setCreateLobbyModalOpen(false);
                            setCreateLobbyError('');
                        }}
                    >
                        {createLobbyError && <div className="modal-error">{createLobbyError}</div>}
                        <input
                            type="text"
                            placeholder="Enter your name"
                            value={createLobbyName}
                            onChange={(e) => setCreateLobbyName(e.target.value)}
                            onKeyPress={(e) => e.key === 'Enter' && handleCreateLobbySubmit()}
                            autoFocus
                        />
                        <div className="modal-actions">
                            <button 
                                className="btn-primary" 
                                onClick={handleCreateLobbySubmit}
                            >
                                Create
                            </button>
                            <button 
                                className="btn-secondary" 
                                onClick={() => setCreateLobbyModalOpen(false)}
                            >
                                Cancel
                            </button>
                        </div>
                    </Modal>

                    {/* Join Lobby Modal */}
                    <Modal 
                        isOpen={joinLobbyModalOpen} 
                        title="Join Lobby" 
                        onClose={() => {
                            setJoinLobbyModalOpen(false);
                            setJoinLobbyError('');
                        }}
                    >
                        {joinLobbyError && <div className="modal-error">{joinLobbyError}</div>}
                        <input
                            type="text"
                            placeholder="Enter lobby ID"
                            value={joinLobbyId}
                            onChange={(e) => setJoinLobbyId(e.target.value)}
                            autoFocus
                        />
                        <input
                            type="text"
                            placeholder="Enter your name"
                            value={joinLobbyName}
                            onChange={(e) => setJoinLobbyName(e.target.value)}
                            onKeyPress={(e) => e.key === 'Enter' && handleJoinLobbySubmit()}
                        />
                        <div className="modal-actions">
                            <button 
                                className="btn-primary" 
                                onClick={handleJoinLobbySubmit}
                            >
                                Join
                            </button>
                            <button 
                                className="btn-secondary" 
                                onClick={() => setJoinLobbyModalOpen(false)}
                            >
                                Cancel
                            </button>
                        </div>
                    </Modal>
                </div>
            )}
        </div>
    );
}

export default App;
