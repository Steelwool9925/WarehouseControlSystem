import { getBatteryColor, getRobotStatusColor } from '../lib/statusColor'
import './RobotFleet.css'

function RobotRow({ robot }) {
  return (
    <li className="robot-row">
      <span
        className="status-dot"
        style={{ backgroundColor: getRobotStatusColor(robot.status) }}
        aria-hidden="true"
      />
      <div className="robot-info">
        <div className="robot-name-line">
          <span className="robot-name">{robot.name}</span>
          <span className="robot-status">{robot.status}</span>
        </div>
        <div className="battery-track" aria-label={`Battery ${robot.batteryPercent}%`}>
          <div
            className="battery-fill"
            style={{
              width: `${robot.batteryPercent}%`,
              backgroundColor: getBatteryColor(robot.batteryPercent),
            }}
          />
        </div>
      </div>
      <span className="battery-label">{robot.batteryPercent}%</span>
    </li>
  )
}

/** List view: status dot + name/id + battery bar per robot. */
export default function RobotFleet({ robots }) {
  return (
    <ul className="robot-fleet">
      {robots.map((robot) => (
        <RobotRow key={robot.id} robot={robot} />
      ))}
    </ul>
  )
}
