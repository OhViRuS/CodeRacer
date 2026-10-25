import Modal from './Modal';

function CloseLobbyModal({ isOpen, onClose, onConfirm }) {
    function handleConfirm() {
        onConfirm();
        onClose();
    }

    return (
        <Modal
            isOpen={isOpen}
            title="Close Lobby"
            onClose={onClose}
        >
            <p>
                Are you sure you want to close and remove the current lobby?
            </p>

            <div className="modal-actions">
                <button
                    className="btn-primary"
                    onClick={handleConfirm}
                >
                    Close Lobby
                </button>

                <button
                    className="btn-secondary"
                    onClick={onClose}
                >
                    Cancel
                </button>
            </div>
        </Modal>
    );
}

export default CloseLobbyModal;
