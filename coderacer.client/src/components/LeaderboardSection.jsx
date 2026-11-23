import { useState } from 'react';
import LeaderboardPanel from './LeaderboardPanel';

function LeaderboardSection() {
    const [isOpen, setIsOpen] = useState(false);

    return (
        <div className="leaderboard-section">
            <button
                className="secondary-btn"
                type="button"
                onClick={() => setIsOpen(open => !open)}
                aria-expanded={isOpen}
            >
                {isOpen ? 'Hide leaderboard' : 'Leaderboard'}
            </button>

            {isOpen && <LeaderboardPanel />}
        </div>
    );
}

export default LeaderboardSection;
