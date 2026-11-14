import { useState } from 'react';

function LoginForm({ onLogin, onSwitchToRegister }) {
    const [userNameOrEmail, setUserNameOrEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);

    async function handleSubmit(event) {
        event.preventDefault();

        setError('');
        setIsSubmitting(true);

        try {
            await onLogin(userNameOrEmail, password);
        } catch (error) {
            setError(error.message);
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <form className="auth-form" onSubmit={handleSubmit}>
            <h2>Login</h2>

            <label className="auth-field">
                <span>Username or email</span>
                <input
                    type="text"
                    value={userNameOrEmail}
                    onChange={event =>
                        setUserNameOrEmail(event.target.value)
                    }
                    autoComplete="username"
                    required
                />
            </label>

            <label className="auth-field">
                <span>Password</span>
                <input
                    type="password"
                    value={password}
                    onChange={event =>
                        setPassword(event.target.value)
                    }
                    autoComplete="current-password"
                    required
                />
            </label>

            {error && <p className="auth-error">{error}</p>}

            <button
                className="primary-btn auth-submit"
                type="submit"
                disabled={isSubmitting}
            >
                {isSubmitting ? 'Logging in...' : 'Login'}
            </button>

            <button
                className="auth-link-btn"
                type="button"
                onClick={onSwitchToRegister}
            >
                Create account
            </button>
        </form>
    );
}

export default LoginForm;
