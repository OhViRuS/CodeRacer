import { useState } from 'react';
import LoginForm from './LoginForm';
import RegisterForm from './RegisterForm';

function AuthPanel({
    user,
    isLoading,
    onLogin,
    onRegister,
    onLogout
}) {
    const [mode, setMode] = useState('login');

    if (isLoading) {
        return (
            <div className="auth-card">
                <p className="auth-status">Loading user...</p>
            </div>
        );
    }

    if (user) {
        return (
            <div className="auth-card auth-user">
                <div>
                    <span className="auth-label">Signed in as</span>
                    <strong>{user.userName}</strong>
                </div>

                <button
                    className="secondary-btn"
                    type="button"
                    onClick={onLogout}
                >
                    Logout
                </button>
            </div>
        );
    }

    return (
        <div className="auth-card">
            {mode === 'register' ? (
                <RegisterForm
                    onRegister={onRegister}
                    onSwitchToLogin={() => setMode('login')}
                />
            ) : (
                <LoginForm
                    onLogin={onLogin}
                    onSwitchToRegister={() => setMode('register')}
                />
            )}
        </div>
    );
}

export default AuthPanel;
