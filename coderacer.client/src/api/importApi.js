// The supported extensions and the maximum size are defined only on the server (SourceFileImporter).
export function fetchImportRules() {
    return fetch('/api/import/rules')
        .then(response => {
            if (!response.ok) {
                throw new Error('Failed to load import rules');
            }

            return response.json();
        });
}

export function formatMegabytes(bytes) {
    return `${bytes / (1024 * 1024)} MB`;
}

// Returns an error message, or null when the file can be sent to the server.
export function validateSourceFile(file, rules) {
    const name = file.name.toLowerCase();

    if (!rules.supportedExtensions.some(extension => name.endsWith(extension))) {
        return `Unsupported file type. Supported: ${rules.supportedExtensions.join(', ')}`;
    }

    if (file.size === 0) {
        return 'The file is empty.';
    }

    if (file.size > rules.maxFileSizeBytes) {
        return `The file is larger than ${formatMegabytes(rules.maxFileSizeBytes)}.`;
    }

    return null;
}

export function importSourceFile(file) {
    const formData = new FormData();
    formData.append('file', file);

    return fetch('/api/import', { method: 'POST', body: formData })
        .then(response => {
            if (response.ok) {
                return response.json();
            }

            if (response.status === 413) {
                throw new Error('The file is too large.');
            }

            return response.json()
                .catch(() => null)
                .then(body => {
                    throw new Error(body?.message ?? 'Failed to import file');
                });
        });
}
