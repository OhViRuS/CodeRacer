import { useState } from 'react';
import '../styles/Modal.css';

function Modal({ isOpen, title, children, onClose }) {
    // Stay mounted after isOpen becomes false, until the closing animation in Modal.css has finished.
    const [mounted, setMounted] = useState(isOpen);

    if (isOpen && !mounted) {
        setMounted(true);
    }

    if (!mounted) {
        return null;
    }

    function handleAnimationEnd(event) {
        // Animations of child elements bubble up to the overlay, so only react to the overlay's own.
        if (!isOpen && event.target === event.currentTarget) {
            setMounted(false);
        }
    }

    return (
        <div
            className={isOpen ? 'modal-overlay' : 'modal-overlay closing'}
            onClick={onClose}
            onAnimationEnd={handleAnimationEnd}
        >
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
                <div className="modal-header">
                    <h2>{title}</h2>
                    <button className="modal-close" onClick={onClose}>✕</button>
                </div>
                <div className="modal-body">
                    {children}
                </div>
            </div>
        </div>
    );
}

export default Modal;
