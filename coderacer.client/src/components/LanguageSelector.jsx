import { useEffect, useState } from 'react';
import { fetchLanguages } from '../api/codeSnippetsApi';

// Enum names from the API -> labels shown on the buttons
const LANGUAGE_LABELS = { CSharp: 'C#', Cpp: 'C++', Python: 'Python', JavaScript: 'JavaScript' };

function LanguageSelector({ selectedLanguage, onLanguageSelected }) {
    const [languages, setLanguages] = useState([]);
    const [error, setError] = useState(null);

    // Load the list of available languages once.
    useEffect(() => {
        fetchLanguages()
            .then(setLanguages)
            .catch(e => setError(e.message));
    }, []);

    return (
        <>
            <h3>Select a programming language</h3>

            {error && <p>{error}</p>}

            <div className="button-container">
                {languages.map(name => (
                    <button
                        key={name}
                        className={
                            selectedLanguage === name
                                ? 'primary-btn selected'
                                : 'secondary-btn'
                        }
                        aria-pressed={selectedLanguage === name}
                        onClick={() => onLanguageSelected(name)}
                    >
                        {LANGUAGE_LABELS[name] ?? name}
                    </button>
                ))}
            </div>
        </>
    );
}

export default LanguageSelector;
