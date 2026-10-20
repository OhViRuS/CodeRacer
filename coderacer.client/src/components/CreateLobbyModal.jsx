import { useState } from 'react';
import Modal from './Modal';

function CreateLobbyModal({ isOpen, onClose, onCreate }) {
    const [name, setName] = useState('');
    const [error, setError] = useState('');

    function handleSubmit() {
        const trimmed = name.trim();

        if (!trimmed) {
            setError('Name cannot be empty. Please enter a name.');
            return;
        }

        onCreate(trimmed);

        setName('');
        setError('');
        onClose();
    }

    function handleClose() {
        setName('');
        setError('');
        onClose();
    }

    return (
        <Modal
            isOpen={isOpen}
            title="Create Lobby"
            onClose={handleClose}
        >
            {error && (
                <div className="modal-error">
                    {error}
                </div>
            )}

            <input
                type="text"
                placeholder="Enter your name"
                value={name}
                onChange={(e) => setName(e.target.value)}
                onKeyPress={(e) => e.key === 'Enter' && handleSubmit()}
                autoFocus
            />

            <div className="modal-actions">
                <button
                    className="btn-primary"
                    onClick={handleSubmit}
                >
                    Create
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

export default CreateLobbyModal;
