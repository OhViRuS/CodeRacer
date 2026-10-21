import { useState } from 'react';

function useLanguage() {
    const [selectedLanguage, setSelectedLanguage] = useState(() => {
        try {
            return localStorage.getItem('language');
        } catch (e) {
            console.error('Failed to load language from localStorage', e);
            return null;
        }
    });

    function selectLanguage(language) {
        setSelectedLanguage(language);

        try {
            localStorage.setItem('language', language);
        } catch (e) {
            console.error('Failed to save language to localStorage', e);
        }
    }

    return {
        selectedLanguage,
        selectLanguage
    };
}

export default useLanguage;
