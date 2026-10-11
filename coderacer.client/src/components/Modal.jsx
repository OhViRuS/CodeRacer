import { useEffect, useState } from 'react';
import '../styles/Modal.css';

const CLOSE_ANIMATION_MS = 200;
function Modal({ isOpen, title, children, onClose }) {
    const [mounted, setMounted] = useState(isOpen);

    if (isOpen && !mounted) {
        setMounted(true);
    }

    useEffect(() => {
        if (isOpen || !mounted) {
            return;
        }

        const timeoutId = setTimeout(() => setMounted(false), CLOSE_ANIMATION_MS);
        return () => clearTimeout(timeoutId);
    }, [isOpen, mounted]);

    if (!mounted) {
        return null;
    }

    return (
        <div className={isOpen ? 'modal-overlay' : 'modal-overlay closing'} onClick={onClose}>
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
