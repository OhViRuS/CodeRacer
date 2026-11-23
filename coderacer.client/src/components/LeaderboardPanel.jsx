import { useEffect, useState } from 'react';
import { fetchLeaderboard } from '../api/leaderboardApi';
import { fetchLanguages, fetchDifficulties, fetchTopics } from '../api/codeSnippetsApi';
import { LANGUAGE_LABELS } from '../utils/languageLabels';
import { formatDateTime, formatDuration, parseTimeSpan } from '../utils/timeFormat';
import '../styles/Leaderboard.css';

const SORT_OPTIONS = [
    { value: 'TypingSpeed', label: 'Typing Speed' },
    { value: 'Accuracy', label: 'Accuracy' }
];

function RankBadge({ rank }) {
    if (rank === 1) {
        return <span className="rank-badge rank-1">1</span>;
    }
    if (rank === 2) {
        return <span className="rank-badge rank-2">2</span>;
    }
    if (rank === 3) {
        return <span className="rank-badge rank-3">3</span>;
    }
    return <span className="rank-other">{rank}</span>;
}

function LeaderboardPanel() {
    const [sortBy, setSortBy] = useState('TypingSpeed');
    const [language, setLanguage] = useState('Python');
    const [difficulty, setDifficulty] = useState('Easy');
    const [topic, setTopic] = useState(null);

    const [availableLanguages, setAvailableLanguages] = useState(['Python', 'JavaScript', 'CSharp', 'Cpp']);
    const [availableDifficulties, setAvailableDifficulties] = useState(['Easy', 'Medium', 'Hard']);
    const [availableTopics, setAvailableTopics] = useState([]);

    const [entries, setEntries] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const [refreshKey, setRefreshKey] = useState(0);

    // Load available languages and difficulties once
    useEffect(() => {
        let cancelled = false;

        Promise.all([fetchLanguages(), fetchDifficulties()])
            .then(([langs, diffs]) => {
                if (!cancelled) {
                    setAvailableLanguages(langs);
                    setAvailableDifficulties(diffs);
                }
            })
            .catch(() => {
                // Keep default fallbacks if API call fails
            });

        return () => {
            cancelled = true;
        };
    }, []);

    // Load available topics when language changes
    useEffect(() => {
        let cancelled = false;

        fetchTopics(language)
            .then(topics => {
                if (!cancelled) {
                    setAvailableTopics(topics);
                }
            })
            .catch(() => {
                if (!cancelled) {
                    setAvailableTopics([]);
                }
            });

        return () => {
            cancelled = true;
        };
    }, [language]);

    // Fetch leaderboard entries whenever filters change or refreshed
    useEffect(() => {
        let cancelled = false;
        queueMicrotask(() => {
            if (!cancelled) {
                setLoading(true);
                setError(null);
            }
        });

        fetchLeaderboard({
            sortBy,
            language,
            difficulty,
            topic
        })
            .then(data => {
                if (!cancelled) {
                    setEntries(data);
                    setError(null);
                }
            })
            .catch(err => {
                if (!cancelled) {
                    setError(err.message);
                }
            })
            .finally(() => {
                if (!cancelled) {
                    setLoading(false);
                }
            });

        return () => {
            cancelled = true;
        };
    }, [sortBy, language, difficulty, topic, refreshKey]);

    function handleSortChange(newSort) {
        if (newSort === sortBy) {
            return;
        }
        setSortBy(newSort);
        setLoading(true);
        setError(null);
    }

    function handleLanguageChange(newLang) {
        if (newLang === language) {
            return;
        }
        setLanguage(newLang);
        setTopic(null); // Reset topic filter when language changes
        setAvailableTopics([]); // Clear stale topics while new ones load
        setLoading(true);
        setError(null);
    }

    function handleDifficultyChange(newDiff) {
        if (newDiff === difficulty) {
            return;
        }
        setDifficulty(newDiff);
        setLoading(true);
        setError(null);
    }

    function handleTopicChange(newTopic) {
        if (newTopic === topic) {
            return;
        }
        setTopic(newTopic);
        setLoading(true);
        setError(null);
    }

    function handleRefresh() {
        setLoading(true);
        setError(null);
        setRefreshKey(k => k + 1);
    }

    return (
        <div className="leaderboard-panel">
            <div className="leaderboard-header">
                <h2>Global Leaderboard</h2>
                <button
                    type="button"
                    className="secondary-btn leaderboard-refresh-btn"
                    onClick={handleRefresh}
                    disabled={loading}
                    aria-label="Refresh leaderboard"
                >
                    {loading ? 'Refreshing...' : 'Refresh'}
                </button>
            </div>
            <p className="leaderboard-note">
                New results can take up to 30 seconds to appear.
            </p>

            <div className="leaderboard-filters">
                <div className="leaderboard-filter-group">
                    <span className="leaderboard-filter-label">Sort By:</span>
                    <div className="leaderboard-filter-buttons">
                        {SORT_OPTIONS.map(opt => (
                            <button
                                key={opt.value}
                                type="button"
                                className={`leaderboard-btn ${sortBy === opt.value ? 'selected' : ''}`}
                                onClick={() => handleSortChange(opt.value)}
                            >
                                {opt.label}
                            </button>
                        ))}
                    </div>
                </div>

                <div className="leaderboard-filter-group">
                    <span className="leaderboard-filter-label">Language:</span>
                    <div className="leaderboard-filter-buttons">
                        {availableLanguages.map(lang => (
                            <button
                                key={lang}
                                type="button"
                                className={`leaderboard-btn ${language === lang ? 'selected' : ''}`}
                                onClick={() => handleLanguageChange(lang)}
                            >
                                {LANGUAGE_LABELS[lang] ?? lang}
                            </button>
                        ))}
                    </div>
                </div>

                <div className="leaderboard-filter-group">
                    <span className="leaderboard-filter-label">Difficulty:</span>
                    <div className="leaderboard-filter-buttons">
                        {availableDifficulties.map(diff => (
                            <button
                                key={diff}
                                type="button"
                                className={`leaderboard-btn ${difficulty === diff ? 'selected' : ''}`}
                                onClick={() => handleDifficultyChange(diff)}
                            >
                                {diff}
                            </button>
                        ))}
                    </div>
                </div>

                {availableTopics.length > 0 && (
                    <div className="leaderboard-filter-group">
                        <span className="leaderboard-filter-label">Topic:</span>
                        <div className="leaderboard-filter-buttons">
                            <button
                                type="button"
                                className={`leaderboard-btn ${topic === null ? 'selected' : ''}`}
                                onClick={() => handleTopicChange(null)}
                            >
                                All Topics
                            </button>
                            {availableTopics.map(t => (
                                <button
                                    key={t}
                                    type="button"
                                    className={`leaderboard-btn ${topic === t ? 'selected' : ''}`}
                                    onClick={() => handleTopicChange(t)}
                                >
                                    {t}
                                </button>
                            ))}
                        </div>
                    </div>
                )}
            </div>

            {error && <p className="auth-error" role="alert">{error}</p>}

            {loading && <p className="auth-status">Loading leaderboard...</p>}

            {!loading && !error && entries && entries.length === 0 && (
                <div className="leaderboard-empty">
                    <p>No ranked results for these filters yet.</p>
                </div>
            )}

            {!loading && !error && entries && entries.length > 0 && (
                <div className="leaderboard-table-wrapper">
                    <table className="leaderboard-table">
                        <thead>
                            <tr>
                                <th className="leaderboard-rank">#</th>
                                <th>Player</th>
                                <th>WPM</th>
                                <th>Accuracy</th>
                                <th>Time</th>
                                <th>Mistakes</th>
                                <th>Date</th>
                            </tr>
                        </thead>
                        <tbody>
                            {entries.map(entry => (
                                <tr key={entry.raceResultId}>
                                    <td className="leaderboard-rank">
                                        <RankBadge rank={entry.rank} />
                                    </td>
                                    <td className="leaderboard-user">{entry.userName}</td>
                                    <td className="leaderboard-speed">{entry.typingSpeed.toFixed(1)}</td>
                                    <td>{entry.accuracy.toFixed(1)}%</td>
                                    <td>{formatDuration(parseTimeSpan(entry.completionTime))}</td>
                                    <td>{entry.mistakes}</td>
                                    <td>{formatDateTime(entry.createdAt)}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}

export default LeaderboardPanel;
