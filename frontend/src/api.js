const BASE_URL = 'http://localhost:5299'

/**
 * Shared fetch wrapper: parses JSON and throws an Error carrying the backend's
 * `{ "error": "..." }` message on any non-2xx response. Every function below calls this
 * instead of duplicating fetch/error-handling logic.
 */
async function request(path, options) {
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: { 'Content-Type': 'application/json' },
    ...options,
  })

  const hasBody = response.status !== 204
  const body = hasBody ? await response.json().catch(() => null) : null

  if (!response.ok) {
    const message = body?.error ?? `Request failed with status ${response.status}`
    throw new Error(message)
  }

  return body
}

export function getRobots() {
  return request('/api/robots')
}

export function getInventory() {
  return request('/api/inventory')
}

export function getOrders() {
  return request('/api/orders')
}

export function getOrder(id) {
  return request(`/api/orders/${id}`)
}

export function createOrder(body) {
  return request('/api/orders', { method: 'POST', body: JSON.stringify(body) })
}

export function getTasks() {
  return request('/api/tasks')
}

export function runDispatch() {
  return request('/api/dispatch/run', { method: 'POST' })
}

export function getKpis() {
  return request('/api/kpis')
}
