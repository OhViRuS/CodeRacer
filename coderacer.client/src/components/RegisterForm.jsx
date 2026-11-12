import { useState } from 'react';

function RegisterForm({ onRegister, onSwitchToLogin }) {
    const [userName, setUserName] = useState('');
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [confirmPassword, setConfirmPassword] = useState('');
    const [error, setError] = useState('');
    const [isSubmitting, setIsSubmitting] = useState(false);

    async function handleSubmit(event) {
        event.preventDefault();

        if (password !== confirmPassword) {
            setError('Passwords do not match');
            return;
        }

        setError('');
        setIsSubmitting(true);

        try {
            await onRegister(
                userName,
                email,
                password,
                confirmPassword
            );
        } catch (error) {
            setError(error.message);
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <form className="auth-form" onSubmit={handleSubmit}>
            <h2>Create account</h2>

            <label className="auth-field">
                <span>Username</span>
                <input
                    type="text"
                    value={userName}
                    onChange={event =>
                        setUserName(event.target.value)
                    }
                    maxLength={50}
                    pattern="[^@]+"
                    title="Username cannot contain @"
                    autoComplete="username"
                    required
                />
            </label>

            <label className="auth-field">
                <span>Email</span>
                <input
                    type="email"
                    value={email}
                    onChange={event =>
                        setEmail(event.target.value)
                    }
                    maxLength={320}
                    autoComplete="email"
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
                    minLength={8}
                    autoComplete="new-password"
                    required
                />
            </label>

            <label className="auth-field">
                <span>Confirm password</span>
                <input
                    type="password"
                    value={confirmPassword}
                    onChange={event =>
                        setConfirmPassword(event.target.value)
                    }
                    minLength={8}
                    autoComplete="new-password"
                    required
                />
            </label>

            {error && <p className="auth-error">{error}</p>}

            <button
                className="primary-btn auth-submit"
                type="submit"
                disabled={isSubmitting}
            >
                {isSubmitting ? 'Creating account...' : 'Register'}
            </button>

            <button
                className="auth-link-btn"
                type="button"
                onClick={onSwitchToLogin}
            >
                Back to login
            </button>
        </form>
    );
}

export default RegisterForm;
