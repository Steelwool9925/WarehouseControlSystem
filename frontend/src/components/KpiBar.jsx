import './KpiBar.css'

function formatPickTime(seconds) {
  if (seconds == null) return '—'
  const total = Math.round(seconds)
  const minutes = Math.floor(total / 60)
  const remainder = total % 60
  return `${minutes}:${String(remainder).padStart(2, '0')}`
}

function StatCell({ label, value }) {
  return (
    <div className="stat-cell">
      <div className="stat-value">{value}</div>
      <div className="stat-label">{label}</div>
    </div>
  )
}

/** Stat cells for robot/order counts and average pick time. */
export default function KpiBar({ kpis }) {
  return (
    <div className="kpi-bar">
      <StatCell label="Active" value={kpis.activeRobots} />
      <StatCell label="Idle" value={kpis.idleRobots} />
      <StatCell label="Charging" value={kpis.chargingRobots} />
      <StatCell label="Pending" value={kpis.pendingOrders} />
      <StatCell label="In Progress" value={kpis.inProgressOrders} />
      <StatCell label="Fulfilled" value={kpis.fulfilledOrders} />
      <StatCell label="Avg Pick Time" value={formatPickTime(kpis.averagePickTimeSeconds)} />
    </div>
  )
}
