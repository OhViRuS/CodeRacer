import { useState } from 'react';
import '../styles/Modal.css';

function Modal({ isOpen, title, children, onClose }) {
    // True once the closing animation in Modal.css has finished. It is only changed by the
    // overlay's animation events, so nothing has to be synchronised with isOpen.
    const [hidden, setHidden] = useState(!isOpen);

    // While isOpen is true the modal is always shown; after it turns false it stays until the fade-out ends.
    if (!isOpen && hidden) {
        return null;
    }

    function handleAnimationEnd(event) {
        // Animations of child elements bubble up to the overlay, so only react to the overlay's own.
        if (event.target === event.currentTarget) {
            setHidden(!isOpen);
        }
    }

    return (
        <div
            className={isOpen ? 'modal-overlay' : 'modal-overlay closing'}
            onClick={isOpen ? onClose : undefined}
            onAnimationEnd={handleAnimationEnd}
        >
            {/* inert while closing: no clicks, focus or key presses reach the content, so it cannot trigger a second action */}
            <div className="modal-content" inert={!isOpen} onClick={(e) => e.stopPropagation()}>
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
