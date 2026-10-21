import { useState } from 'react';
import CodeSnippetDisplay from './CodeSnippetDisplay';
import CreateLobbyModal from './CreateLobbyModal';
import JoinLobbyModal from './JoinLobbyModal';
import CloseLobbyModal from './CloseLobbyModal';
import useLobby from '../hooks/useLobby';
import useLanguage from '../hooks/useLanguage';

function Lobby({ onStartGame }) {
    const {
        lobby,
        createLobby,
        joinLobby,
        closeLobby
    } = useLobby();

    const { selectedLanguage, selectLanguage } = useLanguage();

    const [createLobbyModalOpen, setCreateLobbyModalOpen] = useState(false);
    const [joinLobbyModalOpen, setJoinLobbyModalOpen] = useState(false);
    const [closeLobbyModalOpen, setCloseLobbyModalOpen] = useState(false);

    if (lobby) {
        return (
            <div key="lobby" className="lobby-card">
                <h2>Lobby</h2>

                <p>
                    <strong>ID:</strong> {lobby.id}
                </p>

                <p>
                    <strong>Host:</strong> {lobby.host}
                </p>

                <p>
                    <strong>Players:</strong>
                </p>

                <ul>
                    {lobby.players.map((player, index) => (
                        <li key={index}>{player}</li>
                    ))}
                </ul>

                <CodeSnippetDisplay
                    selectedLanguage={selectedLanguage}
                    onLanguageSelected={selectLanguage}
                />

                <div className="lobby-actions">
                    <button
                        className="secondary-btn"
                        onClick={() => setCloseLobbyModalOpen(true)}
                    >
                        Close Lobby
                    </button>

                    <button
                        className="primary-btn"
                        onClick={() => onStartGame(selectedLanguage)}
                        disabled={!selectedLanguage}
                    >
                        Start Game
                    </button>
                </div>

                <CloseLobbyModal
                    isOpen={closeLobbyModalOpen}
                    onClose={() => setCloseLobbyModalOpen(false)}
                    onConfirm={closeLobby}
                />
            </div>
        );
    }

    return (
        <div key="landing" className="screen-enter">
            <div className="button-container">
                <button
                    className="primary-btn"
                    onClick={() => setCreateLobbyModalOpen(true)}
                >
                    Create Lobby
                </button>

                <button
                    className="primary-btn"
                    onClick={() => setJoinLobbyModalOpen(true)}
                >
                    Join Lobby
                </button>
            </div>

            <CreateLobbyModal
                isOpen={createLobbyModalOpen}
                onClose={() => setCreateLobbyModalOpen(false)}
                onCreate={createLobby}
            />

            <JoinLobbyModal
                isOpen={joinLobbyModalOpen}
                onClose={() => setJoinLobbyModalOpen(false)}
                onJoin={joinLobby}
            />
        </div>
    );
}

export default Lobby;
