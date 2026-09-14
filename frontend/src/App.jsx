// Temporary placeholder proving the design tokens (dark palette + fonts) apply end-to-end.
// Replaced by the real control-room layout in the frontend-components and app-shell plans.
function App() {
  return (
    <main
      style={{
        minHeight: '100vh',
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'center',
        justifyContent: 'center',
        gap: '1rem',
        padding: '2rem',
        textAlign: 'center',
      }}
    >
      <h1 style={{ fontFamily: 'var(--font-ui)', margin: 0 }}>Warehouse Control</h1>
      <p style={{ color: 'var(--color-text-secondary)', maxWidth: '32rem' }}>
        Scaffold in place. The live fleet view lands in the next two plan-feature groups.
      </p>
      <code
        style={{
          fontFamily: 'var(--font-mono)',
          background: 'var(--color-surface)',
          border: '1px solid var(--color-border)',
          borderRadius: '0.375rem',
          padding: '0.5rem 0.75rem',
          color: 'var(--color-teal)',
        }}
      >
        ROBOT-001 · Idle · 100%
      </code>
    </main>
  )
}

export default App
