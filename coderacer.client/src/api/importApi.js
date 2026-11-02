// Keep in sync with SourceFileImporter on the server.
export const SUPPORTED_EXTENSIONS = ['.cs', '.cpp', '.py', '.js'];
export const MAX_FILE_SIZE_BYTES = 5 * 1024 * 1024;

// Returns an error message, or null when the file can be sent to the server.
export function validateSourceFile(file) {
    const name = file.name.toLowerCase();

    if (!SUPPORTED_EXTENSIONS.some(extension => name.endsWith(extension))) {
        return `Unsupported file type. Supported: ${SUPPORTED_EXTENSIONS.join(', ')}`;
    }

    if (file.size === 0) {
        return 'The file is empty.';
    }

    if (file.size > MAX_FILE_SIZE_BYTES) {
        return 'The file is larger than 5 MB.';
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
                throw new Error('The file is larger than 5 MB.');
            }

            return response.json()
                .catch(() => null)
                .then(body => {
                    throw new Error(body?.message ?? 'Failed to import file');
                });
        });
}
