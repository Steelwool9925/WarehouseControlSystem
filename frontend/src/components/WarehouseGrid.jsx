import { getRobotStatusColor } from '../lib/statusColor'
import './WarehouseGrid.css'

const GRID_SIZE = 10
const CELL = 40
const VIEWBOX_SIZE = GRID_SIZE * CELL

function cellCenter(position) {
  return { x: position.x * CELL + CELL / 2, y: position.y * CELL + CELL / 2 }
}

function GridLines() {
  const lines = []
  for (let i = 0; i <= GRID_SIZE; i++) {
    const pos = i * CELL
    lines.push(
      <line key={`v${i}`} className="grid-line" x1={pos} y1={0} x2={pos} y2={VIEWBOX_SIZE} />,
    )
    lines.push(
      <line key={`h${i}`} className="grid-line" x1={0} y1={pos} x2={VIEWBOX_SIZE} y2={pos} />,
    )
  }
  return <g>{lines}</g>
}

function InventoryRack({ location }) {
  const { x, y } = cellCenter(location.position)
  return (
    <g className="rack" transform={`translate(${x}, ${y})`}>
      <rect className="rack-body" x={-16} y={-16} width={32} height={32} rx={4} />
      <text className="rack-label" y={2}>
        {location.sku}
      </text>
    </g>
  )
}

function ChargingDock({ position }) {
  const { x, y } = cellCenter(position)
  return (
    <g className="dock" transform={`translate(${x}, ${y})`}>
      <rect className="dock-body" x={-18} y={-18} width={36} height={36} rx={6} />
    </g>
  )
}

function RobotMarker({ robot }) {
  const { x, y } = cellCenter(robot.position)
  return (
    <g className="robot" transform={`translate(${x}, ${y})`}>
      <circle r={12} fill={getRobotStatusColor(robot.status)} />
      <title>
        {robot.name} · {robot.status} · {robot.batteryPercent}%
      </title>
    </g>
  )
}

/**
 * SVG floor view: grid lines, inventory racks (labeled by SKU), charging docks (at each robot's
 * home station), and robots as color-coded circles that animate via a CSS transition on
 * `transform` when their position prop changes between polls.
 */
export default function WarehouseGrid({ robots, inventory }) {
  const uniqueHomeStations = new Map()
  for (const robot of robots) {
    const key = `${robot.homeStation.x},${robot.homeStation.y}`
    if (!uniqueHomeStations.has(key)) {
      uniqueHomeStations.set(key, robot.homeStation)
    }
  }

  return (
    <svg
      className="warehouse-grid"
      viewBox={`0 0 ${VIEWBOX_SIZE} ${VIEWBOX_SIZE}`}
      role="img"
      aria-label="Warehouse floor plan"
    >
      <rect className="floor" x={0} y={0} width={VIEWBOX_SIZE} height={VIEWBOX_SIZE} />
      <GridLines />
      {[...uniqueHomeStations.values()].map((position) => (
        <ChargingDock key={`${position.x},${position.y}`} position={position} />
      ))}
      {inventory.map((location) => (
        <InventoryRack key={location.sku} location={location} />
      ))}
      {robots.map((robot) => (
        <RobotMarker key={robot.id} robot={robot} />
      ))}
    </svg>
  )
}
