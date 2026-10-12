/** HH:MM:SS for the clock of Quiz Handle and the duration of an attempt. */
export function formatDuration(ms: number) {
  const seconds = Math.floor(ms / 1000)
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${pad(Math.floor(seconds / 3600))}:${pad(Math.floor((seconds % 3600) / 60))}:${pad(seconds % 60)}`
}
