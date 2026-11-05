import './App.css';
import { useState } from 'react';
import TypingChallenge from './components/TypingChallenge';
import Lobby from './components/Lobby';

function App() {
    const [gameLanguage, setGameLanguage] = useState(null);
    const [importedSnippets, setImportedSnippets] = useState([]);
    const [currentSnippetIndex, setCurrentSnippetIndex] = useState(0);

    function handleStartImportedChallenge(language, snippets) {
        setImportedSnippets(snippets);
        setCurrentSnippetIndex(0);
        setGameLanguage(language);
    }

    function handleNextSnippet() {
        setCurrentSnippetIndex(index => index + 1);
    }

    const importedSnippet =
        importedSnippets[currentSnippetIndex] ?? null;
    const hasNextSnippet =
        importedSnippets.length > 0 &&
        currentSnippetIndex < importedSnippets.length - 1;

    if (gameLanguage) {
        return (
            <div className="center-container">
                <h1 className="brand">CodeRacer</h1>

                <TypingChallenge
                    language={gameLanguage}
                    importedSnippet={importedSnippet}
                    hasNextSnippet={hasNextSnippet}
                    onNextSnippet={handleNextSnippet}
                    onExit={() => {
                        setGameLanguage(null);
                        setImportedSnippets([]);
                        setCurrentSnippetIndex(0);
                    }}
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

            <Lobby
                onStartGame={setGameLanguage}
                onStartImportedChallenge={handleStartImportedChallenge}
            />
        </div>
    );
}

export default App;
