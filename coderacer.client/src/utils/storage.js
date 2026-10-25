export const STORAGE_KEYS = {
    LANGUAGE: 'language',
    LOBBY: 'lobby'
};

export function readStorage(key) {
    try {
        return localStorage.getItem(key);
    } catch (e) {
        console.error(`Failed to read ${key} from localStorage`, e);
        return null;
    }
}

export function writeStorage(key, value) {
    try {
        localStorage.setItem(key, value);
    } catch (e) {
        console.error(`Failed to save ${key} to localStorage`, e);
    }
}

export function removeStorage(key) {
    try {
        localStorage.removeItem(key);
    } catch (e) {
        console.error(`Failed to remove ${key} from localStorage`, e);
    }
}
