const vnd = new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 })

/** A whole VND amount, e.g. 250.000 ₫ (BR-17: prices are whole VND). */
export function formatVnd(amount: number) {
  return vnd.format(amount)
}
