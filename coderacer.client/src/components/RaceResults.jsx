function RaceResults({ time, wpm, accuracy, mistakes }) {
    return (
        <div className="race-results">
            <h2>Race Results</h2>

            <p>
                <strong>Time:</strong> {time.toFixed(1)} seconds
            </p>

            <p>
                <strong>WPM:</strong> {wpm}
            </p>

            <p>
                <strong>Accuracy:</strong> {accuracy}%
            </p>

            <p>
                <strong>Mistakes:</strong> {mistakes}
            </p>
        </div>
    );
}

export default RaceResults;