// TEMPORARY mock-data harness for the frontend-components plan — renders all four components
// against representative static props so each milestone can be visually verified without a
// live backend. Replaced by real polling/wiring in the app-shell-and-finish plan.
import KpiBar from './components/KpiBar'
import OrderQueue from './components/OrderQueue'
import RobotFleet from './components/RobotFleet'
import WarehouseGrid from './components/WarehouseGrid'

const mockRobots = [
  {
    id: 'r1',
    name: 'Atlas',
    homeStation: { x: 0, y: 0 },
    position: { x: 0, y: 0 },
    status: 'Idle',
    batteryPercent: 95,
    currentTaskId: null,
  },
  {
    id: 'r2',
    name: 'Bolt',
    homeStation: { x: 9, y: 0 },
    position: { x: 3, y: 2 },
    status: 'MovingToPick',
    batteryPercent: 60,
    currentTaskId: 't1',
  },
  {
    id: 'r3',
    name: 'Cog',
    homeStation: { x: 0, y: 9 },
    position: { x: 6, y: 7 },
    status: 'ReturningToStation',
    batteryPercent: 45,
    currentTaskId: null,
  },
  {
    id: 'r4',
    name: 'Dash',
    homeStation: { x: 9, y: 9 },
    position: { x: 9, y: 9 },
    status: 'Charging',
    batteryPercent: 15,
    currentTaskId: null,
  },
]

const mockInventory = [
  { sku: 'SKU-001', description: 'M8 Hex Bolts', position: { x: 2, y: 2 }, quantity: 40 },
  { sku: 'SKU-002', description: 'M8 Hex Nuts', position: { x: 4, y: 2 }, quantity: 40 },
  { sku: 'SKU-003', description: 'Ball Bearing 608ZZ', position: { x: 6, y: 2 }, quantity: 60 },
  { sku: 'SKU-004', description: 'Aluminum L-Bracket', position: { x: 2, y: 4 }, quantity: 25 },
]

const mockOrders = [
  {
    id: 'order-pending-000000',
    customerReference: 'walk-in',
    lineItemSkus: ['SKU-001'],
    status: 'Pending',
  },
  {
    id: 'order-inprogress-11111',
    customerReference: 'cust-42',
    lineItemSkus: ['SKU-002', 'SKU-003'],
    status: 'InProgress',
  },
  {
    id: 'order-fulfilled-222222',
    customerReference: null,
    lineItemSkus: ['SKU-004'],
    status: 'Fulfilled',
  },
]

const mockKpis = {
  activeRobots: 2,
  idleRobots: 1,
  chargingRobots: 1,
  pendingOrders: 1,
  inProgressOrders: 1,
  fulfilledOrders: 1,
  averagePickTimeSeconds: 95,
}

async function mockCreateOrder(customerReference, skus) {
  await new Promise((resolve) => setTimeout(resolve, 300))
  if (skus.includes('BAD-SKU')) {
    throw new Error("Unknown SKU: 'BAD-SKU'.")
  }
  console.log('mock order created', { customerReference, skus })
  return { id: 'mock-new-order', customerReference, lineItemSkus: skus, status: 'Pending' }
}

function App() {
  return (
    <main style={{ display: 'flex', flexDirection: 'column', gap: '1.5rem', padding: '1.5rem' }}>
      <h1 style={{ fontFamily: 'var(--font-ui)', margin: 0 }}>Warehouse Control — component check</h1>

      <div style={{ maxWidth: '28rem' }}>
        <WarehouseGrid robots={mockRobots} inventory={mockInventory} />
      </div>

      <RobotFleet robots={mockRobots} />

      <OrderQueue
        orders={mockOrders}
        availableSkus={mockInventory}
        onCreateOrder={mockCreateOrder}
      />

      <KpiBar kpis={mockKpis} />
    </main>
  )
}

export default App
