import { useCallback, useEffect, useState } from 'react'
import * as api from './api'
import './App.css'
import KpiBar from './components/KpiBar'
import OrderQueue from './components/OrderQueue'
import RobotFleet from './components/RobotFleet'
import WarehouseGrid from './components/WarehouseGrid'

const POLL_INTERVAL_MS = 1500

const EMPTY_KPIS = {
  activeRobots: 0,
  idleRobots: 0,
  chargingRobots: 0,
  pendingOrders: 0,
  inProgressOrders: 0,
  fulfilledOrders: 0,
  averagePickTimeSeconds: null,
}

function App() {
  const [robots, setRobots] = useState([])
  const [orders, setOrders] = useState([])
  const [kpis, setKpis] = useState(EMPTY_KPIS)
  const [inventory, setInventory] = useState([])
  const [connected, setConnected] = useState(true)

  const refresh = useCallback(async () => {
    try {
      const [robotsData, ordersData, kpisData] = await Promise.all([
        api.getRobots(),
        api.getOrders(),
        api.getKpis(),
      ])
      setRobots(robotsData)
      setOrders(ordersData)
      setKpis(kpisData)
      setConnected(true)
    } catch {
      // Leave the last-known state in place — the grid freezes rather than blanking — and
      // just flag the connection as down.
      setConnected(false)
    }
  }, [])

  useEffect(() => {
    refresh()
    const intervalId = setInterval(refresh, POLL_INTERVAL_MS)
    return () => clearInterval(intervalId)
  }, [refresh])

  useEffect(() => {
    api.getInventory().then(setInventory).catch(() => setConnected(false))
  }, [])

  async function handleCreateOrder(customerReference, skus) {
    const order = await api.createOrder({ customerReference, skus })
    refresh() // pick up the new order immediately rather than waiting for the next poll
    return order
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <h1>Warehouse Control</h1>
        <span className={`connection-badge ${connected ? 'connected' : 'disconnected'}`}>
          {connected ? 'Connected' : 'Disconnected'}
        </span>
      </header>

      <section className="grid-area">
        <WarehouseGrid robots={robots} inventory={inventory} />
      </section>

      <aside className="sidebar-area">
        <KpiBar kpis={kpis} />
        <RobotFleet robots={robots} />
      </aside>

      <section className="queue-area">
        <OrderQueue orders={orders} availableSkus={inventory} onCreateOrder={handleCreateOrder} />
      </section>
    </div>
  )
}

export default App
