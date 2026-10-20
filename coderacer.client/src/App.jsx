import './App.css';
import { useState } from 'react';
import TypingChallenge from './components/TypingChallenge';
import Lobby from './components/Lobby';

function App() {
    const [gameLanguage, setGameLanguage] = useState(null);

    if (gameLanguage) {
        return (
            <div className="center-container">
                <h1 className="brand">CodeRacer</h1>

                <TypingChallenge
                    language={gameLanguage}
                    onExit={() => setGameLanguage(null)}
                />
            </div>
        );
    }

    return (
        <div className="center-container">
            <h1 className="brand">CodeRacer</h1>

            <p className="tagline">
                Become the fastest coder.
            </p>

            <p className="hero-description">
                Practice real code snippets, improve your typing speed,
                and race against other programmers.
            </p>

            <Lobby onStartGame={setGameLanguage} />
        </div>
    );
}

export default App;
