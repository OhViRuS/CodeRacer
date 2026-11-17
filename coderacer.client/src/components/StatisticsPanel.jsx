import { useEffect, useRef, useState } from 'react';
import {
    fetchRecentResults,
    fetchUserStatistics
} from '../api/raceResultsApi';
import {
    formatDateTime,
    formatDuration,
    parseTimeSpan
} from '../utils/timeFormat';
import '../styles/Statistics.css';

const RECENT_RACE_COUNT = 10;

function StatCard({ label, value }) {
    return (
        <div className="stat-card">
            <span className="stat-value">{value}</span>
            <span className="stat-label">{label}</span>
        </div>
    );
}

function StatisticsPanel({ user, token, onSessionExpired }) {
    const [data, setData] = useState(null);
    const [error, setError] = useState(null);

    // Keep the latest callback without making it an effect dependency,
    // so a non-memoized logout can't trigger a refetch on every render.
    const onSessionExpiredRef = useRef(onSessionExpired);

    useEffect(() => {
        onSessionExpiredRef.current = onSessionExpired;
    }, [onSessionExpired]);

    useEffect(() => {
        let cancelled = false;

        Promise.all([
            fetchUserStatistics(user.id),
            fetchRecentResults(token, RECENT_RACE_COUNT)
        ])
            .then(([statistics, recentResults]) => {
                if (!cancelled) {
                    setData({ statistics, recentResults });
                }
            })
            .catch(error => {
                if (cancelled) {
                    return;
                }

                if (error.status === 401) {
                    // Session expired: log out instead of showing an error.
                    onSessionExpiredRef.current?.();
                    return;
                }

                setError(error.message);
            });

        return () => {
            cancelled = true;
        };
    }, [user.id, token]);

    if (error) {
        return (
            <div className="stats-panel">
                <p className="auth-error" role="alert">{error}</p>
            </div>
        );
    }

    if (!data) {
        return (
            <div className="stats-panel">
                <p className="auth-status">Loading statistics...</p>
            </div>
        );
    }

    const { statistics, recentResults } = data;

    if (statistics.totalRaces === 0) {
        return (
            <div className="stats-panel">
                <p className="auth-status">
                    No races yet. Finish a typing challenge and your statistics will show up here.
                </p>
            </div>
        );
    }

    return (
        <div className="stats-panel">
            <h2>My statistics</h2>

            <div className="stats-grid">
                <StatCard label="Races" value={statistics.totalRaces} />
                <StatCard
                    label="Average WPM"
                    value={statistics.averageTypingSpeed.toFixed(1)}
                />
                <StatCard
                    label="Best WPM"
                    value={statistics.bestTypingSpeed.toFixed(1)}
                />
                <StatCard
                    label="Average accuracy"
                    value={`${statistics.averageAccuracy.toFixed(1)}%`}
                />
                <StatCard
                    label="Best time"
                    value={formatDuration(parseTimeSpan(statistics.bestCompletionTime))}
                />
                <StatCard label="Total mistakes" value={statistics.totalMistakes} />
            </div>

            <p className="stats-dates">
                First race: {formatDateTime(statistics.firstRaceDate)}
                {' · '}
                Last race: {formatDateTime(statistics.lastRaceDate)}
            </p>

            <h3>Recent races</h3>

            <div className="stats-table-wrapper">
                <table className="stats-table">
                    <thead>
                        <tr>
                            <th>Date</th>
                            <th>WPM</th>
                            <th>Accuracy</th>
                            <th>Mistakes</th>
                            <th>Time</th>
                        </tr>
                    </thead>

                    <tbody>
                        {recentResults.map(result => (
                            <tr key={result.id}>
                                <td>{formatDateTime(result.createdAt)}</td>
                                <td>{result.typingSpeed.toFixed(1)}</td>
                                <td>{result.accuracy.toFixed(1)}%</td>
                                <td>{result.mistakes}</td>
                                <td>{formatDuration(parseTimeSpan(result.completionTime))}</td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
    );
}

export default StatisticsPanel;
