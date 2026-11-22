import { useEffect, useState } from 'react';
import { fetchDifficulties } from '../api/codeSnippetsApi';

function DifficultySelector({ selectedDifficulty, onDifficultySelected }) {
    const [difficulties, setDifficulties] = useState([]);
    const [error, setError] = useState(null);

    useEffect(() => {
        let cancelled = false;

        fetchDifficulties()
            .then(data => {
                if (!cancelled) {
                    setDifficulties(data);
                }
            })
            .catch(e => {
                if (!cancelled) {
                    setError(e.message);
                }
            });

        return () => {
            cancelled = true;
        };
    }, []);

    return (
        <div className="filter-group">
            <h3>Difficulty</h3>

            {error && <p className="filter-error">{error}</p>}

            <div className="button-container wrap-buttons">
                <button
                    type="button"
                    className={
                        !selectedDifficulty
                            ? 'primary-btn selected'
                            : 'secondary-btn'
                    }
                    aria-pressed={!selectedDifficulty}
                    onClick={() => onDifficultySelected(null)}
                >
                    Any
                </button>
                {difficulties.map(difficulty => (
                    <button
                        key={difficulty}
                        type="button"
                        className={
                            selectedDifficulty === difficulty
                                ? 'primary-btn selected'
                                : 'secondary-btn'
                        }
                        aria-pressed={selectedDifficulty === difficulty}
                        onClick={() => onDifficultySelected(difficulty)}
                    >
                        {difficulty}
                    </button>
                ))}
            </div>
        </div>
    );
}

export default DifficultySelector;
