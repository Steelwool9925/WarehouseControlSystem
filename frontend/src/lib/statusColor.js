// Shared color semantics, reused by every component that shows robot/order status or battery.
// teal = healthy/idle/success, amber = active/in-progress, red = attention,
// neutral = waiting (not alert, success, or active).

const ROBOT_STATUS_COLOR = {
  Idle: 'var(--color-teal)',
  MovingToPick: 'var(--color-amber)',
  ReturningToStation: 'var(--color-amber)',
  Charging: 'var(--color-red)',
}

const ORDER_STATUS_COLOR = {
  Pending: 'var(--color-text-secondary)',
  InProgress: 'var(--color-amber)',
  Fulfilled: 'var(--color-teal)',
}

const LOW_BATTERY_THRESHOLD = 20
const MID_BATTERY_THRESHOLD = 50

export function getRobotStatusColor(status) {
  return ROBOT_STATUS_COLOR[status] ?? 'var(--color-text-secondary)'
}

export function getOrderStatusColor(status) {
  return ORDER_STATUS_COLOR[status] ?? 'var(--color-text-secondary)'
}

// The battery bar's own color follows the battery level, independent of the robot's status dot
// — a robot can be Idle at 15% (about to be sent home to charge) and the bar should already read
// as an attention color even though the status dot is still teal.
export function getBatteryColor(batteryPercent) {
  if (batteryPercent < LOW_BATTERY_THRESHOLD) return 'var(--color-red)'
  if (batteryPercent < MID_BATTERY_THRESHOLD) return 'var(--color-amber)'
  return 'var(--color-teal)'
}
