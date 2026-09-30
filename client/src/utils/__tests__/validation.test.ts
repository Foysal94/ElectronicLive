import { describe, expect, it } from 'vitest'
import { isValidEmail } from '../validation'

describe('Validation Utilities', () => {
  it('Should_validate_valid_email_addresses', () => {
    expect(isValidEmail('user@example.com')).toBe(true)
    expect(isValidEmail('john.doe+tag@sub.domain.co.uk')).toBe(true)
    expect(isValidEmail('user123@domain.org')).toBe(true)
  })

  it('Should_reject_invalid_email_addresses', () => {
    expect(isValidEmail('')).toBe(false)
    expect(isValidEmail('   ')).toBe(false)
    expect(isValidEmail('notanemail')).toBe(false)
    expect(isValidEmail('@nodomain.com')).toBe(false)
    expect(isValidEmail('user@')).toBe(false)
    expect(isValidEmail('user@domain')).toBe(false)
  })
})
