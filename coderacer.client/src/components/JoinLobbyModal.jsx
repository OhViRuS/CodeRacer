import { useState } from 'react';
import Modal from './Modal';

function JoinLobbyModal({
    isOpen,
    onClose,
    onJoin,
    userName
}) {
    const [lobbyId, setLobbyId] = useState('');
    const [name, setName] = useState('');
    const [error, setError] = useState('');

    function handleSubmit() {
        const trimmedId = lobbyId.trim();

        if (!trimmedId) {
            setError('Lobby ID cannot be empty. Please enter a lobby ID.');
            return;
        }

        const playerName = userName ?? name.trim();

        if (!playerName) {
            setError('Name cannot be empty. Please enter a name.');
            return;
        }

        onJoin(trimmedId, playerName);

        setLobbyId('');
        setName('');
        setError('');
        onClose();
    }

    function handleClose() {
        setLobbyId('');
        setName('');
        setError('');
        onClose();
    }

    return (
        <Modal
            isOpen={isOpen}
            title="Join Lobby"
            onClose={handleClose}
        >
            {error && (
                <div className="modal-error">
                    {error}
                </div>
            )}

            <input
                type="text"
                placeholder="Enter lobby ID"
                value={lobbyId}
                onChange={(e) => setLobbyId(e.target.value)}
                autoFocus
            />

            {!userName && (
                <input
                    type="text"
                    placeholder="Enter your name"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    onKeyPress={(e) => e.key === 'Enter' && handleSubmit()}
                />
            )}

            <div className="modal-actions">
                <button
                    className="btn-primary"
                    onClick={handleSubmit}
                >
                    Join
                </button>

                <button
                    className="btn-secondary"
                    onClick={handleClose}
                >
                    Cancel
                </button>
            </div>
        </Modal>
    );
}

export default JoinLobbyModal;
