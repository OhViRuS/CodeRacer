import { useState } from 'react';
import { readStorage, STORAGE_KEYS, writeStorage } from '../utils/storage';

function useLanguage() {
    const [selectedLanguage, setSelectedLanguage] = useState(
        () => readStorage(STORAGE_KEYS.LANGUAGE)
    );

    function selectLanguage(language) {
        setSelectedLanguage(language);
        writeStorage(STORAGE_KEYS.LANGUAGE, language);
    }

    return {
        selectedLanguage,
        selectLanguage
    };
}

export default useLanguage;
