import { useState } from 'react';
import StatisticsPanel from './StatisticsPanel';

// Toggle for the statistics panel. The panel loads its data each time it is opened.
function StatisticsSection({ user, token }) {
    const [isOpen, setIsOpen] = useState(false);

    return (
        <div className="stats-section">
            <button
                className="secondary-btn"
                type="button"
                onClick={() => setIsOpen(open => !open)}
                aria-expanded={isOpen}
            >
                {isOpen ? 'Hide my statistics' : 'My statistics'}
            </button>

            {isOpen && <StatisticsPanel user={user} token={token} />}
        </div>
    );
}

export default StatisticsSection;
