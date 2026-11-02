import { useRef, useState } from 'react';
import { SUPPORTED_EXTENSIONS, importSourceFile, validateSourceFile } from '../api/importApi';
import { LANGUAGE_LABELS } from '../utils/languageLabels';
import '../styles/SourceFileImport.css';

const PREVIEW_LINES = 10;

function SourceFileImport() {
    const inputRef = useRef(null);
    const [imported, setImported] = useState(null);
    const [error, setError] = useState(null);
    const [isImporting, setIsImporting] = useState(false);

    function handleFileChange(event) {
        const input = event.target;
        const file = input.files[0];

        // Reset so the same file can be selected again.
        input.value = '';

        if (!file) {
            return;
        }

        const validationError = validateSourceFile(file);
        if (validationError) {
            setImported(null);
            setError(validationError);
            return;
        }

        setError(null);
        setIsImporting(true);

        importSourceFile(file)
            .then(setImported)
            .catch(e => {
                setImported(null);
                setError(e.message);
            })
            .finally(() => setIsImporting(false));
    }

    const lines = imported ? imported.content.split('\n') : [];

    return (
        <div className="lobby-card import-card">
            <h3>Import your own code</h3>

            <p>
                Supported: {SUPPORTED_EXTENSIONS.join(', ')} (max 5 MB)
            </p>

            <input
                ref={inputRef}
                type="file"
                accept={SUPPORTED_EXTENSIONS.join(',')}
                onChange={handleFileChange}
                hidden
            />

            <button
                className="secondary-btn"
                onClick={() => inputRef.current.click()}
                disabled={isImporting}
            >
                {isImporting ? 'Importing...' : 'Choose file'}
            </button>

            {error && <p className="import-error" role="alert">{error}</p>}

            {imported && (
                <>
                    <p>
                        <strong>{imported.fileName}</strong>
                        {' - '}
                        {LANGUAGE_LABELS[imported.language] ?? imported.language}
                        {', '}
                        {lines.length} lines
                    </p>

                    <pre>
                        {lines.slice(0, PREVIEW_LINES).join('\n')}
                        {lines.length > PREVIEW_LINES ? '\n...' : ''}
                    </pre>
                </>
            )}
        </div>
    );
}

export default SourceFileImport;
