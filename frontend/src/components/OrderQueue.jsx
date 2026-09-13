import { useState } from 'react'
import { getOrderStatusColor } from '../lib/statusColor'
import './OrderQueue.css'

function OrderCard({ order }) {
  return (
    <li className="order-card" style={{ borderColor: getOrderStatusColor(order.status) }}>
      <div className="order-card-header">
        <span className="order-status" style={{ color: getOrderStatusColor(order.status) }}>
          {order.status}
        </span>
        <span className="order-id">{order.id.slice(0, 8)}</span>
      </div>
      {order.customerReference && (
        <div className="order-reference">{order.customerReference}</div>
      )}
      <ul className="order-skus">
        {order.lineItemSkus.map((sku, index) => (
          // SKUs aren't unique within an order (a duplicate line item is valid), so the
          // position is part of the identity here.
          <li key={`${sku}-${index}`}>{sku}</li>
        ))}
      </ul>
    </li>
  )
}

function PlaceOrderForm({ availableSkus, onCreateOrder }) {
  const [selectedSkus, setSelectedSkus] = useState([])
  const [customerReference, setCustomerReference] = useState('')
  const [error, setError] = useState(null)
  const [submitting, setSubmitting] = useState(false)

  function toggleSku(sku) {
    setSelectedSkus((current) =>
      current.includes(sku) ? current.filter((s) => s !== sku) : [...current, sku],
    )
  }

  async function handleSubmit(event) {
    event.preventDefault()
    if (selectedSkus.length === 0 || submitting) return

    setSubmitting(true)
    setError(null)
    try {
      await onCreateOrder(customerReference.trim() || undefined, selectedSkus)
      setSelectedSkus([])
      setCustomerReference('')
    } catch (err) {
      setError(err.message)
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <form className="place-order-form" onSubmit={handleSubmit}>
      <div className="sku-picker">
        {availableSkus.map(({ sku }) => (
          <button
            key={sku}
            type="button"
            className={`sku-chip${selectedSkus.includes(sku) ? ' selected' : ''}`}
            onClick={() => toggleSku(sku)}
            aria-pressed={selectedSkus.includes(sku)}
          >
            {sku}
          </button>
        ))}
      </div>
      <div className="form-row">
        <input
          type="text"
          className="reference-input"
          placeholder="Customer reference (optional)"
          value={customerReference}
          onChange={(event) => setCustomerReference(event.target.value)}
        />
        <button
          type="submit"
          className="submit-button"
          disabled={selectedSkus.length === 0 || submitting}
        >
          {submitting ? 'Placing…' : 'Place order'}
        </button>
      </div>
      {error && <div className="form-error">{error}</div>}
    </form>
  )
}

/**
 * Horizontally scrolling, status-colored order cards, plus the place-order form (SKU chip
 * picker + optional customer reference). `onCreateOrder` is expected to return the created
 * order or throw — the form surfaces a thrown error's message inline and keeps the selection
 * so the user can retry.
 */
export default function OrderQueue({ orders, availableSkus, onCreateOrder }) {
  return (
    <div className="order-queue">
      <PlaceOrderForm availableSkus={availableSkus} onCreateOrder={onCreateOrder} />
      <ul className="order-strip">
        {orders.map((order) => (
          <OrderCard key={order.id} order={order} />
        ))}
      </ul>
    </div>
  )
}
