export function isStrongPassword(value: string): boolean {
  return value.length >= 8 && /[A-Za-z]/.test(value) && /\d/.test(value);
}
