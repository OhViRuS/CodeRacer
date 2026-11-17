const MIN_SECONDS = 0.001;
const MAX_SECONDS = 86399;

function pad(value, length = 2) {
    return String(value).padStart(length, '0');
}

// Converts seconds to the "hh:mm:ss.fffffff" form that the server reads as a TimeSpan.
// The server accepts more than 1 millisecond and less than 1 day, so the value is clamped.
export function toTimeSpanString(seconds) {
    const clamped = Math.min(Math.max(seconds, MIN_SECONDS), MAX_SECONDS);
    const totalMilliseconds = Math.round(clamped * 1000);

    const hours = Math.floor(totalMilliseconds / 3_600_000);
    const minutes = Math.floor(totalMilliseconds / 60_000) % 60;
    const secs = Math.floor(totalMilliseconds / 1000) % 60;
    const milliseconds = totalMilliseconds % 1000;

    return `${pad(hours)}:${pad(minutes)}:${pad(secs)}.${pad(milliseconds, 3)}0000`;
}

// Reads a server TimeSpan ("[d.]hh:mm:ss[.fffffff]") back into seconds. Returns null if it is missing.
export function parseTimeSpan(value) {
    if (!value) {
        return null;
    }

    const match = /^(?:(\d+)\.)?(\d+):(\d{2}):(\d{2})(?:\.(\d+))?$/.exec(value);

    if (!match) {
        return null;
    }

    const [, days = '0', hours, minutes, secs, fraction = '0'] = match;

    return (
        Number(days) * 86400 +
        Number(hours) * 3600 +
        Number(minutes) * 60 +
        Number(secs) +
        Number(`0.${fraction}`)
    );
}

// 12.3 -> "12.3 s", 75.5 -> "1:15.5"
export function formatDuration(seconds) {
    if (seconds === null || seconds === undefined) {
        return '—';
    }

    const tenths = Math.round(seconds * 10);

    if (tenths < 600) {
        return `${(tenths / 10).toFixed(1)} s`;
    }

    const minutes = Math.floor(tenths / 600);
    const rest = (tenths % 600) / 10;

    return `${minutes}:${rest.toFixed(1).padStart(4, '0')}`;
}

export function formatDateTime(value) {
    if (!value) {
        return '—';
    }

    return new Date(value).toLocaleString();
}
